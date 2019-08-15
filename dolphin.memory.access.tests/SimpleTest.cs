using System;
using System.Diagnostics;
using Xunit;

namespace Dolphin.Memory.Access.Tests
{
    public class SimpleTest
    {
        /* Repeat: Dolphin Should be Running and Playing a Game for These Tests */
        private Process _process;
        private Dolphin _dolphin;

        public SimpleTest()
        {
            try
            {
                _process = Process.GetProcessesByName("dolphin")[0];
                _dolphin = new Dolphin(_process);
            }
            catch (Exception e)
            {
                throw new Exception("Dolphin is not running.");
            }
        }


        /* Repeat: Dolphin Should be Running and Playing a Game for These Tests */

        [Fact]
        public void TryGetDolphinBaseAddress()
        {
            bool gotBaseAddress = _dolphin.TryGetBaseAddress(out var initialBaseAddress);
            bool gotCachedBaseAddress = _dolphin.TryGetBaseAddress(out var cachedBaseAddress);

            Assert.NotEqual(IntPtr.Zero, initialBaseAddress);
            Assert.True(gotBaseAddress);
            Assert.True(gotCachedBaseAddress);
            Assert.Equal(initialBaseAddress, cachedBaseAddress);
        }
    }
}
