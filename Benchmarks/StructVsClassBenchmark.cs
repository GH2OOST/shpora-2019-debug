using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarks
{

    public class C
    {
        public int N;
        public string Str;

        #region Equality members

        protected bool Equals(C other)
        {
            return N == other.N && string.Equals(Str, other.Str);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((C)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (N * 397) ^ (Str?.GetHashCode() ?? 0);
            }
        }

        #endregion
    }
    public struct S : IEquatable<S>
    {
        public int N;
        public string Str;

        public int CompareTo(object obj)
        {
            var another = (S)obj;

            var nComp = N.CompareTo(another.N);
            return nComp != 0 ? 
                nComp : 
                string.Compare(Str, another.Str, StringComparison.Ordinal);
        }

        public bool Equals(S other)
        {
            return N == other.N && string.Equals(Str, other.Str);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is S other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (N * 397) ^ (Str != null ? Str.GetHashCode() : 0);
            }
        }
    }

    [MemoryDiagnoser]
    public class StructVsClassBenchmark
    {
        private C[] classArr;
        private S[] structArr;

        [GlobalSetup]
        public void Setup()
        {
            classArr = Enumerable.Range(0, 1000).Select(x => new C { N = x, Str = Guid.NewGuid().ToString() }).ToArray();
            structArr = classArr.Select(c => new S {N = c.N, Str = c.Str}).ToArray();
        }

        [Benchmark]
        public bool Class() => classArr.Contains(new C { N = 100, Str = "something" });

        [Benchmark]
        public bool Struct() => structArr.Contains(new S { N = 100, Str = "something" });
    }
}