namespace GZipTest.ConsoleApp.Infrastructure
{
    internal interface IQueue<T>
    {
        void Enqueue(T value);

        bool TryDequeue(out T value);
    }
}