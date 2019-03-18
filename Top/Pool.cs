using System.Collections.Generic;

namespace Top
{
    public class Pool<T>
    {
        private readonly PoolConfig<T> poolConfig;
        private readonly object sync = new object();
        private readonly Stack<T> items = new Stack<T>();

        public Pool(PoolConfig<T> poolConfig)
        {
            this.poolConfig = poolConfig;
        }

        private void Release(T item)
        {
            poolConfig.Cleanup(item);
            lock (sync)
                items.Push(item);
        }

        public Disposable<T> Acquire()
        {
            lock (sync)
                return new Disposable<T>(items.Count > 0 ? items.Pop() : poolConfig.CreateInstance(), Release);
        }    
    }
}