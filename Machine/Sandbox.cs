using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Collections.Concurrent;

namespace BlackBox.Machine;

//this is init/PID 0
//programs launched by the user will run in dedicated threads managed here

//manages the Roslyn sandbox
//the border between userspace and hostspace

public static class Sandbox
{
	private static ScriptOptions scriptOptions;
	private static ScriptState? currentState;
	private static readonly ReaderWriterLockSlim StateLock = new(LockRecursionPolicy.SupportsRecursion);

	// Main execution loop state
	private static bool running;
	private static Task? loopTask;
	private static readonly CancellationTokenSource LoopCts = new();
	private static Action? loopAction;
	private static readonly object LoopLock = new();

	// Subprocess management
	private static readonly ConcurrentDictionary<int, SubProcess> Processes = new();
	private static int nextPid = 1; // PID 0 is the sandbox itself

	static Sandbox()
	{
		var assemblyBuilder = new SandboxAssemblyBuilder();
		assemblyBuilder.BuildSandboxAssembly();

		// Initialize Roslyn scripting environment with security restrictions
		scriptOptions = ScriptOptions.Default
			.AddReferences(assemblyBuilder.GetReferences())  // Modified base assemblies
			.WithImports(
				"System",
				"System.Collections.Generic",
				"System.Linq",
				"System.Text"
			)
			.WithAllowUnsafe(false)                         // Disable unsafe code
			.WithCheckOverflow(true);                       // Enable overflow checking
	}

	/// <summary>
	/// Executes C# code in the sandboxed environment
	/// </summary>
	public static ScriptExecutionResult Execute(string code, object? globals = null, CancellationToken cancellationToken = default)
	{
		try
		{
			ScriptState? stateBeforeExecution;
			ScriptState resultState;

			StateLock.EnterWriteLock();
			try
			{
				// Remember the state before execution
				stateBeforeExecution = currentState;

				// Create or continue script execution
				if (currentState == null)
				{
					var script = CSharpScript.Create(code, scriptOptions, globals?.GetType());
					resultState = script.RunAsync(globals, cancellationToken).Result;
				}
				else
				{
					resultState = currentState.ContinueWithAsync(code, cancellationToken: cancellationToken).Result;
				}

				// Only update currentState if it wasn't modified by a nested Execute() call
				// If a nested call updated it, that state takes precedence (it has the nested execution's variables)
				if (currentState == stateBeforeExecution)
				{
					currentState = resultState;
				}
				// else: nested execution updated currentState, keep that updated state
			}
			finally
			{
				StateLock.ExitWriteLock();
			}

			return new ScriptExecutionResult
			{
				Success = true,
				ReturnValue = currentState!.ReturnValue,
				Exception = null
			};
		}
		catch (CompilationErrorException ex)
		{
			return new ScriptExecutionResult
			{
				Success = false,
				ReturnValue = null,
				Exception = ex,
				ErrorMessage = string.Join("\n", ex.Diagnostics)
			};
		}
		catch (Exception ex)
		{
			return new ScriptExecutionResult
			{
				Success = false,
				ReturnValue = null,
				Exception = ex,
				ErrorMessage = ex.Message
			};
		}
	}

	/// <summary>
	/// Executes code from a file in the sandboxed environment
	/// </summary>
	public static ScriptExecutionResult ExecuteFile(string filePath, object? globals = null, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
		{
			return new ScriptExecutionResult
			{
				Success = false,
				ErrorMessage = $"File not found: {filePath}"
			};
		}

		string code = File.ReadAllText(filePath);
		return Execute(code, globals, cancellationToken);
	}

	/// <summary>
	/// Resets the sandbox state, clearing all previous script context
	/// </summary>
	public static void Reset()
	{
		StateLock.EnterWriteLock();
		try
		{
			currentState = null;
		}
		finally
		{
			StateLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Adds assembly references to the sandbox
	/// </summary>
	public static void AddReferences(params Assembly[] assemblies)
	{
		scriptOptions = scriptOptions.AddReferences(assemblies);
	}

	/// <summary>
	/// Adds assembly references by type
	/// </summary>
	public static void AddReferences(params Type[] types)
	{
		var assemblies = types.Select(t => t.Assembly).Distinct();
		scriptOptions = scriptOptions.AddReferences(assemblies);
	}

	/// <summary>
	/// Adds namespace imports to the sandbox
	/// </summary>
	public static void AddImports(params string[] namespaces)
	{
		scriptOptions = scriptOptions.AddImports(namespaces);
	}

	/// <summary>
	/// Sets timeout for script execution
	/// </summary>
	public static CancellationTokenSource CreateTimeoutToken(TimeSpan timeout)
	{
		var cts = new CancellationTokenSource();
		cts.CancelAfter(timeout);
		return cts;
	}

	/// <summary>
	/// Evaluates an expression and returns the result
	/// </summary>
	public static async Task<T?> Evaluate<T>(string expression, object? globals = null, CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await CSharpScript.EvaluateAsync<T>(expression, scriptOptions, globals, cancellationToken: cancellationToken);
			return result;
		}
		catch
		{
			return default;
		}
	}

	/// <summary>
	/// Gets all variables currently defined in the script state
	/// </summary>
	public static IEnumerable<ScriptVariable> GetVariables()
	{
		if (currentState == null)
			return Enumerable.Empty<ScriptVariable>();

		return currentState.Variables.Select(v => new ScriptVariable
		{
			Name = v.Name,
			Type = v.Type,
			Value = v.Value
		});
	}

	// ===== MAIN EXECUTION LOOP =====

	/// <summary>
	/// Starts the main sandbox execution loop with a custom action
	/// </summary>
	public static void Run(Action loopAction)
	{
		if (running)
			throw new InvalidOperationException("Sandbox is already running");

		lock (LoopLock)
		{
			Sandbox.loopAction = loopAction;
		}

		running = true;

		loopTask = Task.Run(() =>
		{
			while (running && !LoopCts.Token.IsCancellationRequested)
			{
				try
				{
					lock (LoopLock)
					{
						Sandbox.loopAction?.Invoke();
					}

					// Automatically cleanup finished subprocesses
					CleanupDeadProcesses();
				}
				catch (Exception ex)
				{
					// Log or handle loop errors without crashing
					Console.Error.WriteLine($"Sandbox loop error: {ex.Message}");
				}
			}
		}, LoopCts.Token);
	}

	/// <summary>
	/// Starts the main sandbox execution loop (empty loop)
	/// </summary>
	public static void Run()
	{
		Run(() => { });
	}

	/// <summary>
	/// Stops the main sandbox execution loop
	/// </summary>
	public static void Stop()
	{
		running = false;
		LoopCts.Cancel();
	}

	/// <summary>
	/// Waits for the execution loop to finish
	/// </summary>
	public static async Task WaitForStop()
	{
		if (loopTask != null)
		{
			await loopTask;
		}
	}

	/// <summary>
	/// Checks if the sandbox is currently running
	/// </summary>
	public static bool IsRunning => running;

	// ===== SUBPROCESS MANAGEMENT =====

	/// <summary>
	/// Spawns a subprocess to execute code on a separate thread
	/// Subprocess automatically terminates when execution completes
	/// </summary>
	public static int Spawn(string code, object? globals = null)
	{
		var pid = Interlocked.Increment(ref nextPid);
		var process = new SubProcess(pid, code, scriptOptions, globals);

		if (Processes.TryAdd(pid, process))
		{
			process.Start();
			return pid;
		}

		return -1; // Failed to spawn
	}

	/// <summary>
	/// Spawns a subprocess from a file on a separate thread
	/// </summary>
	public static async Task<int> SpawnFile(string filePath, object? globals = null)
	{
		if (!File.Exists(filePath))
			return -1;

		var code = await File.ReadAllTextAsync(filePath);
		return Spawn(code, globals);
	}

	/// <summary>
	/// Kills a subprocess by PID
	/// </summary>
	public static bool Kill(int pid)
	{
		if (Processes.TryRemove(pid, out var process))
		{
			process.Stop();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Gets the status of a subprocess
	/// </summary>
	public static ProcessStatus? Status(int pid)
	{
		if (Processes.TryGetValue(pid, out var process))
		{
			return process.GetStatus();
		}
		return null;
	}

	/// <summary>
	/// Lists all active subprocess PIDs
	/// </summary>
	public static IEnumerable<int> ListPids()
	{
		return Processes.Keys.OrderBy(k => k);
	}

	/// <summary>
	/// Waits for a subprocess to complete
	/// </summary>
	public static async Task<ScriptExecutionResult?> Wait(int pid)
	{
		if (Processes.TryGetValue(pid, out var process))
		{
			return await process.WaitForCompletion();
		}
		return null;
	}

	/// <summary>
	/// Removes dead processes from the process table
	/// </summary>
	private static void CleanupDeadProcesses()
	{
		var deadPids = Processes
			.Where(kv => kv.Value.GetStatus().State == ProcessState.Exited)
			.Select(kv => kv.Key)
			.ToList();

		foreach (var pid in deadPids)
		{
			Processes.TryRemove(pid, out _);
		}
	}
}

public class ScriptExecutionResult
{
	public bool Success { get; set; }
	public object? ReturnValue { get; set; }
	public Exception? Exception { get; set; }
	public string? ErrorMessage { get; set; }
}

public class ScriptVariable
{
	public string Name { get; set; } = "";
	public Type Type { get; set; } = typeof(object);
	public object? Value { get; set; }
}

// ===== SUBPROCESS TYPES =====

public enum ProcessState
{
	Starting,
	Running,
	Exited
}

public class ProcessStatus
{
	public int Pid { get; set; }
	public ProcessState State { get; set; }
	public ScriptExecutionResult? Result { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime? EndTime { get; set; }
}

internal class SubProcess
{
	private readonly int pid;
	private readonly string code;
	private readonly ScriptOptions options;
	private readonly object? globals;
	private readonly CancellationTokenSource cts;
	private readonly TaskCompletionSource<ScriptExecutionResult> completionSource;

	private ProcessState state;
	private ScriptExecutionResult? result;
	private DateTime startTime;
	private DateTime? endTime;
	private Task? executionTask;

	public SubProcess(int pid, string code, ScriptOptions options, object? globals)
	{
		this.pid = pid;
		this.code = code;
		this.options = options;
		this.globals = globals;
		cts = new CancellationTokenSource();
		completionSource = new TaskCompletionSource<ScriptExecutionResult>();
		state = ProcessState.Starting;
	}

	public void Start()
	{
		startTime = DateTime.UtcNow;
		state = ProcessState.Running;

		executionTask = Task.Run(async () =>
		{
			try
			{
				var script = CSharpScript.Create(code, options, globals?.GetType());
				var scriptState = await script.RunAsync(globals, cts.Token);

				result = new ScriptExecutionResult
				{
					Success = true,
					ReturnValue = scriptState.ReturnValue,
					Exception = null
				};
			}
			catch (CompilationErrorException ex)
			{
				result = new ScriptExecutionResult
				{
					Success = false,
					ReturnValue = null,
					Exception = ex,
					ErrorMessage = string.Join("\n", ex.Diagnostics)
				};
			}
			catch (Exception ex)
			{
				result = new ScriptExecutionResult
				{
					Success = false,
					ReturnValue = null,
					Exception = ex,
					ErrorMessage = ex.Message
				};
			}
			finally
			{
				state = ProcessState.Exited;
				endTime = DateTime.UtcNow;
				completionSource.TrySetResult(result!);
			}
		}, cts.Token);
	}

	public void Stop()
	{
		cts.Cancel();
	}

	public ProcessStatus GetStatus()
	{
		return new ProcessStatus
		{
			Pid = pid,
			State = state,
			Result = result,
			StartTime = startTime,
			EndTime = endTime
		};
	}

	public Task<ScriptExecutionResult> WaitForCompletion()
	{
		return completionSource.Task;
	}
}