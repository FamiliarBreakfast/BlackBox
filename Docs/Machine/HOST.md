# Host

**`Machine/Host.cs`** | Static class | 🚧 Placeholder

## Purpose

Main program loop managing the emulated system.

## Responsibilities

- Spawn and monitor Sandbox (PID 0)
- Update peripherals and filesystem
- Handle timed events
- Error/crash handling

## Current State

Contains basic test code only. Planned architecture:

```csharp
public static class Host
{
    static Host() { /* Initialize system */ }

    public static void Loop()
    {
        // Check sandbox status
        // Update timed events
        // Update peripherals/filesystem
        // Update shell
    }
}
```

## See Also

[Sandbox](SANDBOX.md) | [Shell](SHELL.md)
