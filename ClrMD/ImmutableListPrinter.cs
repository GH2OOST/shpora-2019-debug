using System;
using Microsoft.Diagnostics.Runtime;

namespace ClrMD
{
    public class ImmutableListPrinter : IClrObjectPrinter
    {
        public bool Supports(ClrType type)
        {
            return type.Name == "System.Collections.Immutable.ImmutableList<System.String>"; //TODO
        }

        public void Print(ClrObject clrObject)
        {
            if (clrObject.Type == null)
                return;

            var rootNode = clrObject.GetObjectField("_root");
            PrintImmutableListNode(rootNode);
        }

        private static void PrintImmutableListNode(ClrObject clrObject)
        {
            while (true)
            {
                if (clrObject.Type == null) return;
                var key = clrObject.GetObjectField("_key");
                if (key.Type == null) return;
                PrintImmutableListNode(clrObject.GetObjectField("_left"));
                var strKey = ClrMdHelper.ToString(key);
                Console.WriteLine(strKey);
                clrObject = clrObject.GetObjectField("_right");
            }
        }
    }
}