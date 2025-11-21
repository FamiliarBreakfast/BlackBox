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
	private static ScriptOptions _scriptOptions;
	private static ScriptState? _currentState;
	private static readonly object _stateLock = new();

	// Main execution loop state
	private static bool _running;
	private static Task? _loopTask;
	private static readonly CancellationTokenSource _loopCts = new();
	private static Action? _loopAction;
	private static readonly object _loopLock = new();

	// Subprocess management
	private static readonly ConcurrentDictionary<int, SubProcess> _processes = new();
	private static int _nextPid = 1; // PID 0 is the sandbox itself

	static Sandbox()
	{
		// Initialize Roslyn scripting environment with security restrictions
		_scriptOptions = ScriptOptions.Default
			.WithReferences(
				typeof(object).Assembly,                    // mscorlib
				typeof(Console).Assembly,                   // System.Console
				typeof(IEnumerable<>).Assembly,            // System.Collections.Generic
				typeof(Enumerable).Assembly,                // System.Linq
				typeof(Terminal).Assembly     // BlackBox.System namespace
			)
			.WithImports(
				"System", //todo: reimplement important system types or selectively import
				"System.Collections.Generic",
				"System.Linq",
				"System.Text", //this should also possibly be excluded
				"BlackBox.System",
				"BlackBox.System.Peripherals"
			)
			.WithAllowUnsafe(false)                        // Disable unsafe code
			.WithCheckOverflow(true);                       // Enable overflow checking
	}

	/// <summary>
	/// Executes C# code in the sandboxed environment
	/// </summary>
	public static async Task<ScriptExecutionResult> Execute(string code, object? globals = null, CancellationToken cancellationToken = default)
	{
		try
		{
			lock (_stateLock)
			{
				// Create or continue script execution
				if (_currentState == null)
				{
					var script = CSharpScript.Create(code, _scriptOptions, globals?.GetType());
					_currentState = script.RunAsync(globals, cancellationToken).Result;
				}
				else
				{
					_currentState = _currentState.ContinueWithAsync(code, cancellationToken: cancellationToken).Result;
				}
			}

			return new ScriptExecutionResult
			{
				Success = true,
				ReturnValue = _currentState.ReturnValue,
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
	public static async Task<ScriptExecutionResult> ExecuteFile(string filePath, object? globals = null, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
		{
			return new ScriptExecutionResult
			{
				Success = false,
				ErrorMessage = $"File not found: {filePath}"
			};
		}

		string code = await File.ReadAllTextAsync(filePath, cancellationToken);
		return await Execute(code, globals, cancellationToken);
	}

	/// <summary>
	/// Resets the sandbox state, clearing all previous script context
	/// </summary>
	public static void Reset()
	{
		lock (_stateLock)
		{
			_currentState = null;
		}
	}

	/// <summary>
	/// Adds assembly references to the sandbox
	/// </summary>
	public static void AddReferences(params Assembly[] assemblies)
	{
		_scriptOptions = _scriptOptions.AddReferences(assemblies);
	}

	/// <summary>
	/// Adds assembly references by type
	/// </summary>
	public static void AddReferences(params Type[] types)
	{
		var assemblies = types.Select(t => t.Assembly).Distinct();
		_scriptOptions = _scriptOptions.AddReferences(assemblies);
	}

	/// <summary>
	/// Adds namespace imports to the sandbox
	/// </summary>
	public static void AddImports(params string[] namespaces)
	{
		_scriptOptions = _scriptOptions.AddImports(namespaces);
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
			var result = await CSharpScript.EvaluateAsync<T>(expression, _scriptOptions, globals, cancellationToken: cancellationToken);
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
		if (_currentState == null)
			return Enumerable.Empty<ScriptVariable>();

		return _currentState.Variables.Select(v => new ScriptVariable
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
		if (_running)
			throw new InvalidOperationException("Sandbox is already running");

		lock (_loopLock)
		{
			_loopAction = loopAction;
		}

		_running = true;

		_loopTask = Task.Run(() =>
		{
			while (_running && !_loopCts.Token.IsCancellationRequested)
			{
				try
				{
					lock (_loopLock)
					{
						_loopAction?.Invoke();
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
		}, _loopCts.Token);
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
		_running = false;
		_loopCts.Cancel();
	}

	/// <summary>
	/// Waits for the execution loop to finish
	/// </summary>
	public static async Task WaitForStop()
	{
		if (_loopTask != null)
		{
			await _loopTask;
		}
	}

	/// <summary>
	/// Checks if the sandbox is currently running
	/// </summary>
	public static bool IsRunning => _running;

	// ===== SUBPROCESS MANAGEMENT =====

	/// <summary>
	/// Spawns a subprocess to execute code on a separate thread
	/// Subprocess automatically terminates when execution completes
	/// </summary>
	public static int Spawn(string code, object? globals = null)
	{
		var pid = Interlocked.Increment(ref _nextPid);
		var process = new SubProcess(pid, code, _scriptOptions, globals);

		if (_processes.TryAdd(pid, process))
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
		if (_processes.TryRemove(pid, out var process))
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
		if (_processes.TryGetValue(pid, out var process))
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
		return _processes.Keys.OrderBy(k => k);
	}

	/// <summary>
	/// Waits for a subprocess to complete
	/// </summary>
	public static async Task<ScriptExecutionResult?> Wait(int pid)
	{
		if (_processes.TryGetValue(pid, out var process))
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
		var deadPids = _processes
			.Where(kv => kv.Value.GetStatus().State == ProcessState.Exited)
			.Select(kv => kv.Key)
			.ToList();

		foreach (var pid in deadPids)
		{
			_processes.TryRemove(pid, out _);
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
	private readonly int _pid;
	private readonly string _code;
	private readonly ScriptOptions _options;
	private readonly object? _globals;
	private readonly CancellationTokenSource _cts;
	private readonly TaskCompletionSource<ScriptExecutionResult> _completionSource;

	private ProcessState _state;
	private ScriptExecutionResult? _result;
	private DateTime _startTime;
	private DateTime? _endTime;
	private Task? _executionTask;

	public SubProcess(int pid, string code, ScriptOptions options, object? globals)
	{
		_pid = pid;
		_code = code;
		_options = options;
		_globals = globals;
		_cts = new CancellationTokenSource();
		_completionSource = new TaskCompletionSource<ScriptExecutionResult>();
		_state = ProcessState.Starting;
	}

	public void Start()
	{
		_startTime = DateTime.UtcNow;
		_state = ProcessState.Running;

		_executionTask = Task.Run(async () =>
		{
			try
			{
				var script = CSharpScript.Create(_code, _options, _globals?.GetType());
				var scriptState = await script.RunAsync(_globals, _cts.Token);

				_result = new ScriptExecutionResult
				{
					Success = true,
					ReturnValue = scriptState.ReturnValue,
					Exception = null
				};
			}
			catch (CompilationErrorException ex)
			{
				_result = new ScriptExecutionResult
				{
					Success = false,
					ReturnValue = null,
					Exception = ex,
					ErrorMessage = string.Join("\n", ex.Diagnostics)
				};
			}
			catch (Exception ex)
			{
				_result = new ScriptExecutionResult
				{
					Success = false,
					ReturnValue = null,
					Exception = ex,
					ErrorMessage = ex.Message
				};
			}
			finally
			{
				_state = ProcessState.Exited;
				_endTime = DateTime.UtcNow;
				_completionSource.TrySetResult(_result!);
			}
		}, _cts.Token);
	}

	public void Stop()
	{
		_cts.Cancel();
	}

	public ProcessStatus GetStatus()
	{
		return new ProcessStatus
		{
			Pid = _pid,
			State = _state,
			Result = _result,
			StartTime = _startTime,
			EndTime = _endTime
		};
	}

	public Task<ScriptExecutionResult> WaitForCompletion()
	{
		return _completionSource.Task;
	}
}