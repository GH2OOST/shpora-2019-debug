using System;
using Microsoft.Diagnostics.Runtime;

namespace ClrMD
{
    public class ListPrinter : IClrObjectPrinter
    {
        public bool Supports(ClrType type)
        {
            return type.Name == "System.Collections.Generic.List<System.String>";
        }

        public void Print(ClrObject clrObject)
        {
            if (clrObject.Type == null)
                return;
            var items = clrObject.GetObjectField("_items");
            if (items.Type == null)
                return;
            var len = items.Type.GetArrayLength(items);
            for (var i = 0; i < len; ++i)
            {
                var elementAddress = items.Type.GetArrayElementAddress(items, i);
                items.Type.Heap.ReadPointer(elementAddress, out var objectAddress);
                var obj = new ClrObject(
                    objectAddress,
                    items.Type.Heap.GetObjectType(objectAddress));

                if (obj.Type == null)
                    return;

                var strKey = ClrMdHelper.ToString(obj);

                Console.WriteLine(strKey);
            }

        }
    }
}