using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfTransientKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TransientGrowthFailsBeforeCurrent();
            TransientShrinkFailsBeforeCurrent();
            TransientNegativeFailsBeforeCurrent();
            TransientConflictFailsBeforeCurrent();
            StableCountedAndStreamingRemainAccepted();
        }

        private static void TransientGrowthFailsBeforeCurrent() => AssertTransientRejected(TransientMode.Growth);
        private static void TransientShrinkFailsBeforeCurrent() => AssertTransientRejected(TransientMode.Shrink);
        private static void TransientNegativeFailsBeforeCurrent() => AssertTransientRejected(TransientMode.Negative);
        private static void TransientConflictFailsBeforeCurrent() => AssertTransientRejected(TransientMode.Conflict);

        private static void AssertTransientRejected(TransientMode mode)
        {
            var source = new TransientCountCollection<BcfComponentReference>(mode, Component());
            Throws<ArgumentException>(() => new BcfViewpoint(GuidFor(1), Camera(), source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void StableCountedAndStreamingRemainAccepted()
        {
            var component = Component();
            var counted = new TransientCountCollection<BcfComponentReference>(TransientMode.Stable, component);
            var countedViewpoint = new BcfViewpoint(GuidFor(2), Camera(), counted);
            Equal(1, countedViewpoint.Components.Count);
            Equal(1, counted.CurrentReads);

            var streamedViewpoint = new BcfViewpoint(GuidFor(3), Camera(), Stream(component));
            Equal(1, streamedViewpoint.Components.Count);
        }

        private static BcfComponentReference Component() =>
            new BcfComponentReference("QS3D-TRANSIENT", "0000000000000000000001");

        private static BcfOrthogonalCamera Camera() =>
            new BcfOrthogonalCamera(
                new BcfPoint3(0d, 0d, 0d),
                new BcfPoint3(0d, 0d, -1d),
                new BcfPoint3(0d, 1d, 0d),
                1d,
                1d);

        private static string GuidFor(int index) =>
            index.ToString("x8") + "-0000-0000-0000-000000000000";

        private static IEnumerable<T> Stream<T>(T item)
        {
            yield return item;
        }

        private enum TransientMode
        {
            Stable,
            Growth,
            Shrink,
            Negative,
            Conflict
        }

        private sealed class TransientCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T _item;
            private readonly TransientMode _mode;
            private bool _transient;

            internal TransientCountCollection(TransientMode mode, T item)
            {
                _mode = mode;
                _item = item;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            int ICollection<T>.Count => CountForSurface(0);
            int IReadOnlyCollection<T>.Count => CountForSurface(1);
            int ICollection.Count => CountForSurface(2);
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            private int CountForSurface(int surface)
            {
                if (!_transient || _mode == TransientMode.Stable) return 1;
                switch (_mode)
                {
                    case TransientMode.Growth: return 2;
                    case TransientMode.Shrink: return 0;
                    case TransientMode.Negative: return -1;
                    case TransientMode.Conflict: return surface == 1 ? 2 : 1;
                    default: return 1;
                }
            }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private bool _yielded;

                internal Enumerator(TransientCountCollection<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._transient = false;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_yielded) return false;
                    _yielded = true;
                    _owner._transient = _owner._mode != TransientMode.Stable;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
