# BlackBox Documentation

C# scripting sandbox built on Roslyn with strict userspace/hostspace separation.

## Quick Links

- [Machine Layer](Machine/MACHINE.md) - Host, Sandbox, Shell
- [System Layer](System/SYSTEM.md) - Userspace APIs
- [Files](Files/FILES.md) - Default filesystem

## Architecture

**Hostspace (Machine):** Host loop → Sandbox (PID 0) → Shell
**Userspace (System):** APIs accessible from sandboxed code

## Core Features

**Sandbox (✅ Complete)**
- Main execution loop (runs continuously)
- Direct code execution (persistent state for REPL)
- Subprocess management (separate threads, auto-terminate)

**Terminal (✅ Complete)**
- Raylib window with shader support
- VT100-like terminal emulator
- Character buffer with color support

**Shell (✅ Complete)**
- Interactive C# REPL
- Command history, cursor navigation
- Slash commands (`/help`, `/vars`, `/reset`, etc.)

## Quick Start

```csharp
// Execute code
await Sandbox.Execute("var x = 10; return x * 2;");

// Spawn subprocess
int pid = Sandbox.Spawn("/* background task */");
await Sandbox.Wait(pid);

// Run main loop
Sandbox.Run(() => { /* per-iteration logic */ });
```

## Security

- No unsafe code, overflow checking enabled
- Limited assembly access, sandboxed execution
- Virtual filesystem (no direct host access)

## Status

**Complete:** Sandbox, Terminal, Window, Serial, Shell
**Placeholder:** Host loop, Filesystem, Process, IO, Peripherals

---

**.NET 9.0** | **Microsoft.CodeAnalysis.CSharp.Scripting 4.12.0**
