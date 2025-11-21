# Process

**`System/Process.cs`** | `BlackBox.System` | 🚧 Placeholder

Userspace process management (wraps Sandbox subprocess API).

## Planned API

```csharp
// Creation
int Spawn(string code)
int SpawnFile(string path)

// Control
bool Kill(int pid)
ProcessResult Wait(int pid)
bool IsAlive(int pid)
ProcessState GetState(int pid)

// Information
IEnumerable<int> List()
int CurrentPid { get; }
ProcessInfo GetInfo(int pid)

// IPC
Send(int pid, object message)
object? Receive()
bool HasMessage()
```

## Planned Types

```csharp
class ProcessResult {
    int Pid; bool Success;
    object? Value; string? Error;
}

class ProcessInfo {
    int Pid; ProcessState State;
    DateTime StartTime; DateTime? EndTime;
    string? Name;
}

enum ProcessState {
    Starting, Running, Sleeping, Waiting, Exited
}
```

## See Also

[Sandbox](../Machine/SANDBOX.md) | [IO](IO.md)
