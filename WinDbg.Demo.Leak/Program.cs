using System;
using System.Threading;

namespace WinDbg.Demo.Leak
{
    internal class EventProducer
    {
        public EventProducer()
        {
            new Timer(_ => Event(this, c++), null, 1000, 1000);
        }

        public event EventHandler<int> Event = delegate {  };

        private int c;
    }
    
    internal class EntryPoint
    {
        public static void Main(string[] args)
        {
            var eventProducer = new EventProducer();
            
            while (true)
            {
                var buffer = new byte[100 * 1024];
                
                var random = new Random();

                void Action() => random.NextBytes(buffer);

                Action();
                
                eventProducer.Event += (sender, eventArgs) => Console.WriteLine($"{eventArgs} {buffer.Length}");
                
               // GC.Collect(2);
                Thread.Sleep(1000);
            }
        }
    }
}