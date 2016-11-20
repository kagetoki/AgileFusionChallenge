namespace ThreadUtils
{
    public class IterationCompletedEventArgs<T>
    {
        public T Result { get; private set; }
        public IterationCompletedEventArgs(T result)
        {
            Result = result;
        }
    }
}
