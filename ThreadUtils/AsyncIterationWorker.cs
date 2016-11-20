using System;
using System.Threading;

namespace ThreadUtils
{
    public class AsyncIterationWorker<T>
    {
        private readonly Func<T> _job;
        private Thread _executionThread;
        private ManualResetEvent _resetEvent;
        public event EventHandler<IterationCompletedEventArgs<T>> IterationCompleted;
        public bool IsStarted { get; private set; }
        public bool IsRunning { get; private set; }
        public int Delay { get; set; }
        public AsyncIterationWorker(Func<T> jobToRepeat, int delay = 0)
        {
            if(jobToRepeat == null) { throw new ArgumentNullException("job"); }
            if(delay < 0) { throw new ArgumentException("delay should be grater or equal than 0"); }
            _job = jobToRepeat;
            _executionThread = new Thread(new ThreadStart(DoJob)) { IsBackground = true };
            _resetEvent = new ManualResetEvent(false);
            Delay = delay;
        }

        private void DoJob()
        {
            while (true)
            {
                _resetEvent.WaitOne();
                T result = _job();
                IterationCompleted?.Invoke(this, new IterationCompletedEventArgs<T>(result));
                if(Delay > 0)
                {
                    Thread.Sleep(Delay);
                }
            }
        }

        public void Start()
        {
            if (IsRunning) { return; }
            if (!IsStarted)
            {
                _executionThread.Start();
                IsStarted = true;
            }
            _resetEvent.Set();
            IsRunning = true;
        }
        public void Stop()
        {
            _resetEvent.Reset();
            IsRunning = false;
        }
    }
}
