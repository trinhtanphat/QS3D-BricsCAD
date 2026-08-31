using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqTransientKnownCountSmoke
    {
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
            var source = new TransientCountGroups(mode, Group());
            Throws<InvalidOperationException>(() => new MepTbqProjectionService().BuildReport(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void StableCountedAndStreamingRemainAccepted()
        {
            var group = Group();
            var counted = new TransientCountGroups(TransientMode.Stable, group);
            var report = new MepTbqProjectionService().BuildReport(counted);
            Equal(1, report.Count);
            Equal(1, counted.CurrentReads);

            var streamed = new MepTbqProjectionService().BuildReport(Stream(group));
            Equal(1, streamed.Count);
        }

        private static MepQuantityGroup Group() =>
            new MepQuantityService().Aggregate(new[]
            {
                new MepElement("E-TRANSIENT", MepElementKind.Pipe, "CHW", "DN50", "L1", lengthM: 1d)
            })[0];

        private static IEnumerable<MepQuantityGroup> Stream(MepQuantityGroup group)
        {
            yield return group;
        }

        private enum TransientMode
        {
            Stable,
            Growth,
            Shrink,
            Negative,
            Conflict
        }

        private sealed class TransientCountGroups : ICollection<MepQuantityGroup>, IReadOnlyCollection<MepQuantityGroup>, ICollection
        {
            private readonly MepQuantityGroup _group;
            private readonly TransientMode _mode;
            private bool _transient;

            internal TransientCountGroups(TransientMode mode, MepQuantityGroup group)
            {
                _mode = mode;
                _group = group;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            int ICollection<MepQuantityGroup>.Count => CountForSurface(0);
            int IReadOnlyCollection<MepQuantityGroup>.Count => CountForSurface(1);
            int ICollection.Count => CountForSurface(2);
            bool ICollection<MepQuantityGroup>.IsReadOnly => true;
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

            public IEnumerator<MepQuantityGroup> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<MepQuantityGroup>.Add(MepQuantityGroup item) => throw new NotSupportedException();
            void ICollection<MepQuantityGroup>.Clear() => throw new NotSupportedException();
            bool ICollection<MepQuantityGroup>.Contains(MepQuantityGroup item) => throw new NotSupportedException();
            void ICollection<MepQuantityGroup>.CopyTo(MepQuantityGroup[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<MepQuantityGroup>.Remove(MepQuantityGroup item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<MepQuantityGroup>
            {
                private readonly TransientCountGroups _owner;
                private bool _yielded;

                internal Enumerator(TransientCountGroups owner) => _owner = owner;

                public MepQuantityGroup Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._transient = false;
                        return _owner._group;
                    }
                }

                object IEnumerator.Current => Current;

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
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class MepTbqTransientKnownCountSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MepTbqTransientKnownCountSmoke.Run();
    }
}
