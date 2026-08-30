using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportBuilderKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunRejectsBeforeUnexpectedCurrentRead();
            TransientGrowthRejectsBeforeCurrentRead();
            TransientShrinkRejectsBeforeCurrentRead();
            TransientNegativeCountRejectsBeforeCurrentRead();
            TransientCrossInterfaceConflictRejectsBeforeCurrentRead();
            StableCountedSourcePreservesGrouping();
            PureStreamingSourceRemainsSupported();
        }

        private static void KnownCountOverrunRejectsBeforeUnexpectedCurrentRead()
        {
            var source = new HostileElementCollection(
                new[] { Element("QR-A", 2d), Element("QR-B", 3d) },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1);

            ThrowsCountIntegrity(() => QuantityReportBuilder.Group(source));
            Equal(1, source.CurrentReads);
        }

        private static void TransientGrowthRejectsBeforeCurrentRead()
        {
            var source = new HostileElementCollection(
                new[] { Element("QR-GROW", 1d) },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1,
                mutateOnMoveNextCall: 1,
                mutatedGenericCount: 2,
                mutatedReadOnlyCount: 2,
                mutatedNonGenericCount: 2);

            ThrowsCountIntegrity(() => QuantityReportBuilder.Group(source));
            Equal(0, source.CurrentReads);
        }

        private static void TransientShrinkRejectsBeforeCurrentRead()
        {
            var source = new HostileElementCollection(
                new[] { Element("QR-SHRINK", 1d), Element("QR-SHRINK-2", 1d) },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2,
                mutateOnMoveNextCall: 1,
                mutatedGenericCount: 1,
                mutatedReadOnlyCount: 1,
                mutatedNonGenericCount: 1);

            ThrowsCountIntegrity(() => QuantityReportBuilder.Group(source));
            Equal(0, source.CurrentReads);
        }

        private static void TransientNegativeCountRejectsBeforeCurrentRead()
        {
            var source = new HostileElementCollection(
                new[] { Element("QR-NEG", 1d) },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1,
                mutateOnMoveNextCall: 1,
                mutatedGenericCount: -1,
                mutatedReadOnlyCount: -1,
                mutatedNonGenericCount: -1);

            ThrowsCountIntegrity(() => QuantityReportBuilder.Group(source));
            Equal(0, source.CurrentReads);
        }

        private static void TransientCrossInterfaceConflictRejectsBeforeCurrentRead()
        {
            var source = new HostileElementCollection(
                new[] { Element("QR-CONFLICT", 1d) },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1,
                mutateOnMoveNextCall: 1,
                mutatedGenericCount: 1,
                mutatedReadOnlyCount: 2,
                mutatedNonGenericCount: 1);

            ThrowsCountIntegrity(() => QuantityReportBuilder.Group(source));
            Equal(0, source.CurrentReads);
        }

        private static void StableCountedSourcePreservesGrouping()
        {
            var source = new HostileElementCollection(
                new[] { Element("QR-STABLE-A", 2d), Element("QR-STABLE-B", 3d) },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2);

            var rows = QuantityReportBuilder.Group(source);
            Equal(1, rows.Count);
            Equal(2, rows[0].Count);
            Near(5d, rows[0].LengthM);
            Equal(2, rows[0].ElementIds.Count);
            Equal(2, source.CurrentReads);
        }

        private static void PureStreamingSourceRemainsSupported()
        {
            var rows = QuantityReportBuilder.Group(Stream(Element("QR-STREAM-A", 2d), Element("QR-STREAM-B", 3d)));
            Equal(1, rows.Count);
            Equal(2, rows[0].Count);
            Near(5d, rows[0].LengthM);
        }

        private static IEnumerable<ElementInstance> Stream(params ElementInstance[] elements)
        {
            foreach (var element in elements)
                yield return element;
        }

        private static ElementInstance Element(string id, double lengthM)
        {
            var family = new FamilyDefinition("QR Count Wall", ElementCategory.ArchitecturalWall, "Concrete");
            return new ElementInstance(id, family, "Floor-1")
            {
                LengthM = lengthM,
                GrossConcreteM3 = lengthM / 10d,
                FormworkM2 = lengthM * 2d
            };
        }

        private static void ThrowsCountIntegrity(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Expected Count-integrity rejection, got: " + ex.Message, ex);
            }

            throw new InvalidOperationException("Expected QuantityReportBuilder Count-integrity rejection.");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private sealed class HostileElementCollection : ICollection<ElementInstance>, IReadOnlyCollection<ElementInstance>, ICollection
        {
            private readonly ElementInstance[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int _mutateOnMoveNextCall;
            private readonly int _mutatedGenericCount;
            private readonly int _mutatedReadOnlyCount;
            private readonly int _mutatedNonGenericCount;
            private bool _mutated;

            internal HostileElementCollection(
                ElementInstance[] items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                int mutateOnMoveNextCall = int.MaxValue,
                int mutatedGenericCount = 0,
                int mutatedReadOnlyCount = 0,
                int mutatedNonGenericCount = 0)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _mutateOnMoveNextCall = mutateOnMoveNextCall;
                _mutatedGenericCount = mutatedGenericCount;
                _mutatedReadOnlyCount = mutatedReadOnlyCount;
                _mutatedNonGenericCount = mutatedNonGenericCount;
            }

            int ICollection<ElementInstance>.Count => _mutated ? _mutatedGenericCount : _genericCount;
            int IReadOnlyCollection<ElementInstance>.Count => _mutated ? _mutatedReadOnlyCount : _readOnlyCount;
            int ICollection.Count => _mutated ? _mutatedNonGenericCount : _nonGenericCount;
            bool ICollection<ElementInstance>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<ElementInstance> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ElementInstance>.Add(ElementInstance item) => throw new NotSupportedException();
            void ICollection<ElementInstance>.Clear() => throw new NotSupportedException();
            bool ICollection<ElementInstance>.Contains(ElementInstance item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<ElementInstance>.CopyTo(ElementInstance[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<ElementInstance>.Remove(ElementInstance item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private sealed class Enumerator : IEnumerator<ElementInstance>
            {
                private readonly HostileElementCollection _owner;
                private int _index = -1;

                internal Enumerator(HostileElementCollection owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_owner.MoveNextCalls >= _owner._mutateOnMoveNextCall)
                        _owner._mutated = true;
                    return _index < _owner._items.Length;
                }

                public ElementInstance Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
