using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleCollectionCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GenericCountDriftFailsClosed();
            ReadOnlyCountDriftFailsClosed();
            NonGenericCountDriftFailsClosed();
            NegativePostTraversalCountFailsClosed();
            ConflictingPostTraversalCountsFailClosed();
            KnownCountUnderYieldFailsClosed();
            KnownCountOverrunFailsClosed();
            StableCountedCollectionSucceeds();
            StreamingCollectionSucceeds();
        }

        private static void GenericCountDriftFailsClosed()
        {
            var source = new GenericDriftCollection<CoordinationRule>(Rules(2), 2, 3);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-GENERIC", 1, source));
        }

        private static void ReadOnlyCountDriftFailsClosed()
        {
            var source = new ReadOnlyDriftCollection<CoordinationRule>(Rules(2), 2, 1);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-READONLY", 1, source));
        }

        private static void NonGenericCountDriftFailsClosed()
        {
            var source = new NonGenericDriftCollection<CoordinationRule>(Rules(2), 2, 4);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-NONGENERIC", 1, source));
        }

        private static void NegativePostTraversalCountFailsClosed()
        {
            var source = new GenericDriftCollection<CoordinationRule>(Rules(1), 1, -1);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-NEGATIVE", 1, source));
        }

        private static void ConflictingPostTraversalCountsFailClosed()
        {
            var source = new ConflictingAfterTraversalCollection<CoordinationRule>(Rules(2), 2, 2, 3);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-CONFLICT", 1, source));
        }

        private static void KnownCountUnderYieldFailsClosed()
        {
            var source = new GenericDriftCollection<CoordinationRule>(Rules(1), 2, 2);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-UNDER", 1, source));
        }

        private static void KnownCountOverrunFailsClosed()
        {
            var source = new GenericDriftCollection<CoordinationRule>(Rules(2), 1, 1);
            Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-OVER", 1, source));
        }

        private static void StableCountedCollectionSucceeds()
        {
            var source = new GenericDriftCollection<CoordinationRule>(Rules(2), 2, 2);
            var profile = new CoordinationRuleProfile("P-STABLE", 1, source);
            Equal(2, profile.Rules.Count, "stable counted source did not preserve both rules");
        }

        private static void StreamingCollectionSucceeds()
        {
            var profile = new CoordinationRuleProfile("P-STREAM", 1, StreamRules());
            Equal(2, profile.Rules.Count, "pure streaming source did not preserve both rules");
        }

        private static CoordinationRule[] Rules(int count)
        {
            var rules = new CoordinationRule[count];
            for (var i = 0; i < count; i++)
            {
                rules[i] = new CoordinationRule(
                    "R-" + i,
                    1,
                    "Pipe-" + i,
                    "Beam-" + i,
                    CoordinationRuleKind.HardClash,
                    "Error",
                    0d);
            }
            return rules;
        }

        private static IEnumerable<CoordinationRule> StreamRules()
        {
            yield return Rules(1)[0];
            yield return new CoordinationRule("R-STREAM-2", 1, "Duct", "Wall", CoordinationRuleKind.HardClash, "Error", 0d);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException(
                "CoordinationRuleCollectionCountStabilitySmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationRuleCollectionCountStabilitySmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private sealed class GenericDriftCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            public GenericDriftCollection(T[] items, int beforeCount, int afterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _afterCount = afterCount;
            }

            public int Count => _traversed ? _afterCount : _beforeCount;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }

            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            public ReadOnlyDriftCollection(T[] items, int beforeCount, int afterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _afterCount = afterCount;
            }

            public int Count => _traversed ? _afterCount : _beforeCount;
            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }
        }

        private sealed class NonGenericDriftCollection<T> : IEnumerable<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            public NonGenericDriftCollection(T[] items, int beforeCount, int afterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _afterCount = afterCount;
            }

            public int Count => _traversed ? _afterCount : _beforeCount;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }

            public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private sealed class ConflictingAfterTraversalCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _genericAfterCount;
            private readonly int _readOnlyAfterCount;
            private bool _traversed;

            public ConflictingAfterTraversalCollection(
                T[] items,
                int beforeCount,
                int genericAfterCount,
                int readOnlyAfterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _genericAfterCount = genericAfterCount;
                _readOnlyAfterCount = readOnlyAfterCount;
            }

            int ICollection<T>.Count => _traversed ? _genericAfterCount : _beforeCount;
            int IReadOnlyCollection<T>.Count => _traversed ? _readOnlyAfterCount : _beforeCount;
            bool ICollection<T>.IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }

            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }
    }
}
