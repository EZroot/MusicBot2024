public class TaskQueue
{
    private readonly Queue<Func<Task>> _taskQueue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Enqueue(Func<Task> task)
    {
        await _semaphore.WaitAsync();
        try
        {
            _taskQueue.Enqueue(task);
            if (_taskQueue.Count == 1) // If this is the only task in the queue, start processing
            {
                await ProcessQueue();

            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessQueue()
    {
        while (_taskQueue.Count > 0)
        {
            Func<Task> task = _taskQueue.Dequeue();
            await task();
        }
    }
}
