# Shell

**`Machine/Shell.cs`** | Static class | ✅ Complete

Interactive C# REPL with Raylib keyboard integration.

## API

```csharp
Shell.Enable()
Shell.Disable()
bool IsEnabled
Shell.ProcessInput()  // Call from main loop
```

## Features

- Interactive C# REPL via Roslyn Sandbox
- Keyboard navigation (Left/Right arrows)
- Command history (Up/Down arrows)
- Persistent state across commands
- Slash commands for shell control

## Slash Commands

| Command | Description |
|---------|-------------|
| `/help` | Show commands |
| `/clear` | Clear screen |
| `/reset` | Reset sandbox state |
| `/vars` | Show variables |
| `/history` | Command history |
| `/exit` | Exit REPL |

## Usage

```csharp
// In Host.Loop()
Shell.Enable();
Shell.ProcessInput();  // Called each frame
```

## Examples

```
> var x = 10
> x * 2
=> 20

> Terminal.Write("Hello\n")
Hello

> /vars
Variables:
  x (Int32) = 10
```

## Keyboard Controls

- **Enter** - Execute
- **Backspace** - Delete char
- **Up/Down** - History navigation
- **Left/Right** - Cursor movement
- Printable chars - Insert at cursor

## See Also

[Sandbox](SANDBOX.md) | [Terminal](../System/TERMINAL.md)
