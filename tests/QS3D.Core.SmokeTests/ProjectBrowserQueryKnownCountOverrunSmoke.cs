using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryKnownCountOverrunSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var source = new ThrowingTailCountedSource<string>(1, "F1", "F2");
            try
            {
                _ = new ProjectBrowserQueryOptions(floorIds: source);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Count 1", StringComparison.Ordinal) < 0 ||
                    ex.Message.IndexOf("exceeded during traversal", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(
                        "ProjectBrowserQueryKnownCountOverrunSmoke expected the known-Count overrun failure, actual: " + ex.Message);
                if (source.TailRequested)
                    throw new InvalidOperationException(
                        "ProjectBrowserQueryKnownCountOverrunSmoke advanced the counted enumerable after observing the first item beyond Count.");
                if (source.Yielded != 2)
                    throw new InvalidOperationException(
                        "ProjectBrowserQueryKnownCountOverrunSmoke expected exactly two yielded values at rejection, actual: " + source.Yielded + ".");
                return;
            }

            throw new InvalidOperationException(
                "ProjectBrowserQueryKnownCountOverrunSmoke expected the first item beyond known Count to fail closed.");
        }

        private sealed class ThrowingTailCountedSource<T> : ICollection<T>
        {
            private readonly int _count;
            private readonly T _first;
            private readonly T _overrun;

            internal ThrowingTailCountedSource(int count, T first, T overrun)
            {
                _count = count;
                _first = first;
                _overrun = overrun;
            }

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int Yielded { get; private set; }
            internal bool TailRequested { get; private set; }

            public IEnumerator<T> GetEnumerator() => Values().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Values()
            {
                Yielded++;
                yield return _first;
                Yielded++;
                yield return _overrun;
                TailRequested = true;
                throw new InvalidOperationException("Counted enumerable tail must not be requested after overrun.");
            }

            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
