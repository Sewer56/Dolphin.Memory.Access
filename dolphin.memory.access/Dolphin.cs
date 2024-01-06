using System;
using System.Diagnostics;
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;

namespace Dolphin.Memory.Access
{
    /// <summary>
    /// Helper class used to manage process memory in Dolphin Emulator.
    /// </summary>
    public class Dolphin
    {
        private const long EmulatedMemorySize = 0x2000000;
        private const long EmulatedMemoryBase = 0x80000000;

        private IntPtr  _pointerToEmulatedMemory = IntPtr.Zero;
        private IntPtr  _dolphinBaseAddress;
        private int     _dolphinModuleSize;

        private ICanReadWriteMemory _memory;
        private Process _process;

        /* Construction / Destruction */

        /// <summary>
        /// Creates a new instance of Dolphin emulator helper.
        /// </summary>
        public Dolphin(Process process)
        {
            _process = process;

            var mainModule      = _process.MainModule;
            _dolphinBaseAddress = mainModule.BaseAddress;
            _dolphinModuleSize  = mainModule.ModuleMemorySize;

            if (_process.Id == Process.GetCurrentProcess().Id)
                _memory = new Reloaded.Memory.Memory();
            else
                _memory = new ExternalMemory(_process);
        }

        /// <summary>
        /// Converts a Dolphin/GC Memory address in the form of 0x8XXXXXXX to a real memory address.
        /// </summary>
        /// <param name="address">Address in form of 0x8XXXXXXX to get real physical address of.</param>
        /// <returns><see cref="IntPtr.Zero"/> if Dolphin is not found or running, else the real memory address.</returns>
        public bool TryGetAddress(long address, out IntPtr realAddress)
        {
            if (TryGetBaseAddress(out var baseAddress))
            {
                realAddress = (IntPtr)((address - EmulatedMemoryBase) + (long) baseAddress);
                return true;
            }

            realAddress = IntPtr.Zero;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the base address of GameCube emulated memory.
        /// </summary>
        /// <returns></returns>
        public unsafe bool TryGetBaseAddress(out IntPtr emulatedBaseAddress)
        {
            // Check cached page address for potential valid page.
            if (_pointerToEmulatedMemory != IntPtr.Zero)
            {
                _memory.Read((nuint)_pointerToEmulatedMemory.ToInt64(), out emulatedBaseAddress);
                return emulatedBaseAddress != IntPtr.Zero; // If it's zero, a game is not running.
            }

            if (TryGetDolphinPage(out emulatedBaseAddress))
            {
                Span<byte> dolphinMainModule = new byte[_dolphinModuleSize];

                // Find pointer to emulated memory.
                _memory.ReadRaw((nuint)_dolphinBaseAddress.ToInt64(), dolphinMainModule);
                long readCount = _dolphinModuleSize - sizeof(IntPtr);

                fixed (byte* mainModulePtr = dolphinMainModule)
                {
                    var lastAddress    = (long) mainModulePtr + readCount;
                    var currentAddress = (long) mainModulePtr;

                    while (currentAddress < lastAddress)
                    {
                        var current = *(IntPtr*) currentAddress;
                        if (current == emulatedBaseAddress)
                        {
                            var offset = currentAddress - (long) mainModulePtr;
                            _pointerToEmulatedMemory = (IntPtr)((long)_dolphinBaseAddress + offset);
                            return true;
                        }

                        currentAddress += 1;
                    }
                }

                // Pointer not found but memory was found.
                // Suspicious but I'll allow it.
                return true;
            }

            // Not found.
            emulatedBaseAddress = IntPtr.Zero;
            return false;
        }

        /// <summary>
        /// Attempts to get the memory page belonging to Dolphin emulator, spitting out the address of emulated memory if it succeeds.
        /// </summary>
        private bool TryGetDolphinPage(out IntPtr baseAddress)
        {
            // Otherwise enumerate remaining pages.
            var enumerator = new MemoryPageEnumerator(_process);
            while (enumerator.MoveNext())
            {
                var page = enumerator.Current;
                if (IsDolphinPage(page))
                {
                    baseAddress = page.BaseAddress;
                    return true;
                }
            }

            baseAddress = IntPtr.Zero;
            return false;
        }

        /// <summary>
        /// Verifies whether a given memory page belongs to Dolphin.
        /// </summary>
        private unsafe bool IsDolphinPage(Native.Native.MEMORY_BASIC_INFORMATION memoryPage)
        {
            // Check if page mapped and right size.
            if (memoryPage.RegionSize == (IntPtr) EmulatedMemorySize && memoryPage.lType == Native.Native.PageType.Mapped)
            {
                // Note taken from from Dolphin Memory Engine:

                /*
                    Here, it's likely the right page, but it can happen that multiple pages with these criteria
                    exist and have nothing to do with the emulated memory. Only the right page has valid
                    working set information so an additional check is required that it is backed by physical
                    memory.
                */

                var setInformation = new Native.Native.PSAPI_WORKING_SET_EX_INFORMATION[1];
                setInformation[0].VirtualAddress = memoryPage.BaseAddress;

                if (!Native.Native.QueryWorkingSetEx(_process.Handle, setInformation, sizeof(Native.Native.PSAPI_WORKING_SET_EX_INFORMATION) * setInformation.Length)) 
                    return false;
                
                if (setInformation[0].VirtualAttributes.Valid)
                    return true;
            }

            return false;
        }
    }
}