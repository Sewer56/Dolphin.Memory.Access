<div align="center">
	<h1>Dolphin Memory Access</h1>
	<img src="https://i.imgur.com/Y0ohMsN.png" width="250" align="center" />
	<br/> <br/>
	<strong>A handy tool for accessing emulator memory from C#<br/></strong>
</div>

# About This Project

This project is a simple, easy to use .NET Standard library that allows you to access the memory of Dolphin emulator on Windows. I wrote this mainly for personal use.

# Usage

Simply instantiate the class `Dolphin`, passing an instance of `System.Diagnostics.Process` belonging to Dolphin emulator.

```csharp
_process = Process.GetProcessesByName("dolphin")[0];
_dolphin = new Dolphin(_process);
```

Then use the API of the `Dolphin`, you can either get an address to the start of emulated memory.

```csharp
// Operation fails if no game is currently running.
if (_dolphin.TryGetBaseAddress(out IntPtr initialBaseAddress)) 
{
	// Do things with memory.
}
```

Or more conveniently, get the real address of a variable in emulated memory:
```csharp
// Operation fails if no game is currently running.
// 0x805EF958: Current stage ID in NTSC US Shadow The Hedgehog (GUPE8P)
if (_dolphin.TryGetAddress(0x805EF958, out IntPtr initialBaseAddress)) 
{
	// Do things with memory.
}
```

PS. Just remember that emulated memory is `Big Endian` as opposed to our `Little Endian` x86/x64 chips. Consider using another library such as [Reloaded.Memory](https://github.com/Reloaded-Project/Reloaded.Memory)'s Endian class to flip the endian of variables.

# Other Notes

This tiny 9KB library is barebones and does does not handle events such as process exit for you.

It's the user's responsibility to listen to these events, otherwise you'll run into exceptions if e.g. you try to read Dolphin Memory after Dolphin closes.