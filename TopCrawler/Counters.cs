using System.Threading;

namespace TopCrawler
{
    class Counter
    {
        private long _counter;

        public Counter()
        {
            _counter = 0;
        }

        public void Increment()
        {
            Interlocked.Increment(ref _counter);
        }

        public long Read()
        {
            return _counter;
        }

        public long Refresh()
        {
            return Interlocked.Exchange(ref _counter, 0);
        }

        public void Add(long value)
        {
            Interlocked.Add(ref _counter, value);
        }

        public void Set(long value)
        {
            Interlocked.Exchange(ref _counter, value);
        }
    }
}
