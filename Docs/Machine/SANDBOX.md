# Sandbox

**`Machine/Sandbox.cs`** | Static class | ✅ Complete

Roslyn-based C# execution engine running as PID 0.

## Main Execution Loop

Runs continuously as fast as possible:

```csharp
Sandbox.Run(() => { /* per-iteration code */ });
Sandbox.Stop();
await Sandbox.WaitForStop();
```

| Function | Description |
|----------|-------------|
| `Run(Action)` | Start execution loop |
| `Stop()` | Stop loop |
| `WaitForStop()` | Async wait for completion |
| `IsRunning` | Check if running |

## Direct Execution (Main Thread)

Persistent state for REPL:

```csharp
await Execute(string code, object? globals, CancellationToken)
await ExecuteFile(string path, ...)
T? Evaluate<T>(string expr, ...)
Reset()  // Clear all state
GetVariables()  // List defined variables
```

Variables persist between `Execute()` calls.

## Subprocess Management

Run code on separate threads (auto-terminate on completion):

```csharp
int Spawn(string code, object? globals)  // Returns PID (-1 on fail)
int SpawnFile(string path, ...)
bool Kill(int pid)
ProcessStatus? Status(int pid)
IEnumerable<int> ListPids()
await Wait(int pid)  // Wait for completion
```

**Process States:** Starting → Running → Exited

## Configuration

```csharp
AddReferences(params Assembly[])
AddReferences(params Type[])
AddImports(params string[])
CreateTimeoutToken(TimeSpan)
```

**Default imports:** `System`, `System.Collections.Generic`, `System.Linq`, `BlackBox.System`

## Security

- No unsafe code, overflow checking enabled
- Limited assembly access
- Sandboxed execution

## Types

```csharp
class ScriptExecutionResult {
    bool Success; object? ReturnValue;
    Exception? Exception; string? ErrorMessage;
}

class ProcessStatus {
    int Pid; ProcessState State;
    ScriptExecutionResult? Result;
    DateTime StartTime; DateTime? EndTime;
}

class ScriptVariable {
    string Name; Type Type; object? Value;
}
```
