namespace DiscordBot;

public class PartiallyFinishedValue<TUpdate, TResult>
{
    private readonly List<Func<TResult?, Task>> _finishedEvents = new();
    private readonly List<Func<TUpdate, Task>> _updateEvents = new();

    public delegate Task<TResult?> WorkerType(Func<TUpdate, Task> update);

    private bool _finished;
    private TResult? _result;

    public PartiallyFinishedValue()
    {
        _finished = false;
    }

    public PartiallyFinishedValue(TResult? result) : this()
    {
        Resolve(result);
    }

    public WorkerType Worker
    {
        set => _ = Invoke(value);
    }

    public event Func<TUpdate, Task> OnUpdate
    {
        add => _updateEvents.Add(value);
        remove => _updateEvents.Remove(value);
    }

    public event Func<TResult?, Task> Finished
    {
        add
        {
            _finishedEvents.Add(value);
            if (_finished)
                value.Invoke(_result);
        }
        remove => _finishedEvents.Remove(value);
    }


    private void Resolve(TResult? res)
    {
        _result = res;
        _finished = true;
        _finishedEvents.ForEach(e => e.Invoke(res));
    }

    public async Task<TResult?> Result()
    {
        while (!_finished) await Task.Delay(300);

        return _result;
    }

    public Task Update(TUpdate value)
    {
        _updateEvents.ForEach(e => e.Invoke(value));
        return Task.CompletedTask;
    }

    private async Task Invoke(WorkerType worker)
    {
        var result = await worker.Invoke(Update);

        Resolve(result);
    }
}