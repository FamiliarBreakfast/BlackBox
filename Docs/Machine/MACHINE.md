# Machine Layer

Hostspace components - inaccessible from userspace.

## Components

### [Host](HOST.md) - `Machine/Host.cs`
Main program loop managing the system.
**Status:** 🚧 Placeholder

### [Sandbox](SANDBOX.md) - `Machine/Sandbox.cs`
Roslyn C# execution engine (PID 0).
**Status:** ✅ Complete

### [Shell](SHELL.md) - `Machine/Shell.cs`
Interactive REPL interface.
**Status:** ✅ Complete

## Architecture

```
Host (main loop)
  └─→ Sandbox (PID 0, execution loop)
       ├─→ Shell (REPL on main thread)
       └─→ Subprocesses (PID 1+, separate threads)
```

## Static Design

All Machine classes are static (singleton pattern):
- **Host** - Single main loop
- **Sandbox** - Single execution environment
- **Shell** - Single REPL instance
