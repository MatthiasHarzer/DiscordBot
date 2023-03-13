namespace DiscordBot;

/// <summary>
/// A return value that can be updated and finished.
/// </summary>
/// <typeparam name="TUpdate">The type of updates to expect</typeparam>
/// <typeparam name="TResult">The type of the result to expect</typeparam>
public class PartiallyFinishedValue<TUpdate, TResult>
{
    private readonly List<Func<TResult?, Task>> _finishedEvents = new();
    private readonly List<Func<TUpdate, Task>> _updateEvents = new();
    private readonly TaskCompletionSource<TResult?> _resultCompleter = new();


    /// <summary>
    /// The worker function that will be invoked.
    /// Can call the update function to update the value.
    /// The return value will be the final result.
    /// </summary>
    public delegate Task<TResult?> WorkerType(Func<TUpdate, Task> update);

    /// <summary>
    /// Whether the value is finished or not.
    /// </summary>
    public bool IsFinished => _resultCompleter.Task.IsCompleted;

    /// <summary>
    /// The worker function that will be invoked.
    /// Takes exactly one argument, the update function.
    /// </summary>
    public WorkerType Worker
    {
        set => _ = InvokeWorker(value);
    }


    public Task<TResult?> Result => _resultCompleter.Task;

    /// <summary>
    /// Creates a new PartiallyFinishedValue.
    /// </summary>
    public PartiallyFinishedValue()
    {
    }

    /// <summary>
    /// Creates a new PartiallyFinishedValue with a worker.
    /// </summary>
    /// <param name="worker"></param>
    public PartiallyFinishedValue(WorkerType worker) : this()
    {
        Worker = worker;
    }

    /// <summary>
    /// Creates a new PartiallyFinishedValue that resolves immediately.
    /// </summary>
    /// <param name="result">The result of the return value</param>
    public PartiallyFinishedValue(TResult? result) : this()
    {
        Resolve(result);
    }

    /// <summary>
    /// The callback that will be invoked when the value is updated.
    /// </summary>
    public event Func<TUpdate, Task> OnUpdate
    {
        add => _updateEvents.Add(value);
        remove => _updateEvents.Remove(value);
    }

    /// <summary>
    /// The callback that will be invoked when the value is finished.
    /// </summary>
    public event Func<TResult?, Task> OnFinished
    {
        add
        {
            _finishedEvents.Add(value);
            if (IsFinished)
                value.Invoke(_resultCompleter.Task.Result);
        }
        remove => _finishedEvents.Remove(value);
    }

    /// <summary>
    /// Update the return value with the given value.
    /// </summary>
    /// <param name="value">The new value</param>
    /// <returns></returns>
    public Task Update(TUpdate value)
    {
        _updateEvents.ForEach(e => e.Invoke(value));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Finishes the return value with the given result.
    /// </summary>
    /// <param name="res">The return value</param>
    private void Resolve(TResult? res)
    {
        if (_resultCompleter.Task.IsCompleted) return;

        _resultCompleter.SetResult(res);
        _finishedEvents.ForEach(e => e.Invoke(res));
    }

    /// <summary>
    /// Starts the worker, awaits the result and resolves the return value.
    /// </summary>
    /// <param name="worker">The worker.</param>
    private async Task InvokeWorker(WorkerType worker)
    {
        var result = await worker.Invoke(Update);

        Resolve(result);
    }
    
    public void ForceFinish(TResult? result)
    {
        Resolve(result);
    }
}