namespace LlamaLogic.LlamaPadPreview;

class PythonSynchronizationContext :
    SynchronizationContext,
    IDisposable
{
    class QueuedCallback
    {
        public SendOrPostCallback Callback { get; }
        public object? State { get; }
        public ManualResetEventSlim? Signal { get; }
        public Exception? Exception { get; set; }

        public QueuedCallback(SendOrPostCallback callback, object? state, ManualResetEventSlim? signal)
        {
            Callback = callback;
            State = state;
            Signal = signal;
        }
    }

    public PythonSynchronizationContext()
    {
        queuedCallbacks = new();
        queuedCallbacksCancellationTokenSource = new();
        processCallbacksThread = new Thread(ProcessCallbacks);
        processCallbacksThread.SetApartmentState(ApartmentState.STA);
        processCallbacksThread.IsBackground = true;
        processCallbacksThread.Start();
    }

    ~PythonSynchronizationContext() =>
        Dispose(false);

    readonly Thread processCallbacksThread;
    readonly AsyncProducerConsumerQueue<QueuedCallback> queuedCallbacks;
    readonly CancellationTokenSource queuedCallbacksCancellationTokenSource;

    public override SynchronizationContext CreateCopy() =>
        this;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            queuedCallbacksCancellationTokenSource.Cancel();
            queuedCallbacksCancellationTokenSource.Dispose();
        }
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);
        queuedCallbacks.Enqueue(new QueuedCallback(d, state, null));
    }

    void ProcessCallbacks()
    {
        SetSynchronizationContext(this);
        while (true)
        {
            QueuedCallback queuedCallback;
            try
            {
                queuedCallback = queuedCallbacks.Dequeue(queuedCallbacksCancellationTokenSource.Token);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            try
            {
                queuedCallback.Callback(queuedCallback.State);
            }
            catch (Exception ex)
            {
                queuedCallback.Exception = ex;
            }
            queuedCallback.Signal?.Set();
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);
        using var signal = new ManualResetEventSlim(false);
        var queuedCallback = new QueuedCallback(d, state, signal);
        queuedCallbacks.Enqueue(queuedCallback);
        signal.Wait();
        if (queuedCallback.Exception is { } exception)
            ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
