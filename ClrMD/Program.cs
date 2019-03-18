using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ClrMD;
using Microsoft.Diagnostics.Runtime;

namespace ClrMD
{
    class Program
    {
        static void Main(string[] args)
        {
            var objects = Objects.CreateObjects();

            PrintObjectsWithClrMD();
            Console.ReadKey();
            
            GC.KeepAlive(objects);
        }

        private static void PrintObjectsWithClrMD()
        {
            var pid = Process.GetCurrentProcess().Id;
            
            using (var dt = DataTarget.AttachToProcess(pid, 3000, AttachFlag.Passive))
            {
                if (dt.PointerSize != IntPtr.Size)
                    throw new Exception();

                var runtime = dt.ClrVersions[0].CreateRuntime();

                var heap = runtime.Heap;

                Console.WriteLine(heap);

                var printers = new IClrObjectPrinter[]
                {
                    new DictionaryPrinter(),
                    new ConcurrentDictionaryPrinter(),
                    new ListPrinter(),
                    new ImmutableListPrinter()
                };

                ClrObjectPrinter.Print(heap, printers);
            }
        }
    }
}