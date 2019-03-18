using System;
using System.Linq;

namespace JPEG.Utilities
{
    public static class MathEx
    {
        public static double Sum(int from, int to, Func<int, double> function)
            => Enumerable.Range(from, to - from).Sum(function);

        public static void Loop(int from, int to, Action<int> function)
        {
            for (var i = from; i < to; i++)
                function(i);
        }

        public static double SumByTwoVariables(int from1, int to1, int from2, int to2, Func<int, int, double> function)
            => Sum(from1, to1, x => Sum(from2, to2, y => function(x, y)));

        public static void LoopByTwoVariables(int from1, int to1, int from2, int to2, Action<int, int> function)
            => Loop(from1, to1, x => Loop(from2, to2, y => function(x, y)));
    }
}