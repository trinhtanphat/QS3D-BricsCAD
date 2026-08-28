using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionSummaryCountStabilitySmoke
    {
        internal static void Run()
        {
            EarlyKnownCountOverrunWinsBeforeUnexpectedRowValidation();
            PostTraversalCountDriftFailsClosed();
            PostTraversalNegativeCountFailsClosed();
            PostTraversalCountConflictFailsClosed();
            StableCountedAndStreamingSourcesRemainAccepted();
        }

        private static void EarlyKnownCountOverrunWinsBeforeUnexpectedRowValidation()
        {
            var source = new MutableCountRows(
                new QuantityRevisionRow?[] { Row("Volume", 1d, 2d), null },
                beforeCount: 1,
                afterGenericCount: 1);

            var error = Throws<InvalidOperationException>(() => new QuantityRevisionReport().Summarize(source));
            Contains(error.Message, "Count reported 1 rows but traversal produced 2");
            Equal(2, source.MoveNextCalls);
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var source = new MutableCountRows(
                new QuantityRevisionRow?[] { Row("Volume", 1d, 2d) },
                beforeCount: 1,
                afterGenericCount: 2);

            var error = Throws<InvalidOperationException>(() => new QuantityRevisionReport().Summarize(source));
            Contains(error.Message, "known Count changed during traversal from 1 to 2");
        }

        private static void PostTraversalNegativeCountFailsClosed()
        {
            var source = new MutableCountRows(
                new QuantityRevisionRow?[] { Row("Area", 2d, 3d) },
                beforeCount: 1,
                afterGenericCount: -1);

            var error = Throws<InvalidOperationException>(() => new QuantityRevisionReport().Summarize(source));
            Contains(error.Message, "negative known Count");
        }

        private static void PostTraversalCountConflictFailsClosed()
        {
            var source = new MutableCountRows(
                new QuantityRevisionRow?[] { Row("Length", 3d, 4d) },
                beforeCount: 1,
                afterGenericCount: 1,
                afterReadOnlyCount: 2,
                afterNonGenericCount: 1);

            var error = Throws<InvalidOperationException>(() => new QuantityRevisionReport().Summarize(source));
            Contains(error.Message, "conflicting known Counts");
        }

        private static void StableCountedAndStreamingSourcesRemainAccepted()
        {
            var counted = new MutableCountRows(
                new QuantityRevisionRow?[]
                {
                    Row("Volume", 1d, 2d),
                    Row("volume", 2d, 4d)
                },
                beforeCount: 2,
                afterGenericCount: 2);

            var countedResult = new QuantityRevisionReport().Summarize(counted);
            Equal(1, countedResult.Count);
            Equal("Volume", countedResult[0].QuantityName);
            Equal(3d, countedResult[0].Before);
            Equal(6d, countedResult[0].After);

            var streamed = new QuantityRevisionReport().Summarize(Stream(Row("Area", 5d, 8d)));
            Equal(1, streamed.Count);
            Equal(5d, streamed[0].Before);
            Equal(8d, streamed[0].After);
        }

        private static QuantityRevisionRow Row(string quantityName, double before, double after) =>
            new QuantityRevisionRow
            {
                ElementId = "E1",
                Category = "Beam",
                QuantityName = quantityName,
                Change = "Changed",
                Before = before,
                After = after
            };

        private static IEnumerable<QuantityRevisionRow> Stream(QuantityRevisionRow row)
        {
            yield return row;
        }

        private sealed class MutableCountRows : ICollection<QuantityRevisionRow>, IReadOnlyCollection<QuantityRevisionRow>, ICollection
        {
            private readonly IReadOnlyList<QuantityRevisionRow?> _rows;
            private readonly int _beforeCount;
            private readonly int _afterGenericCount;
            private readonly int? _afterReadOnlyCount;
            private readonly int? _afterNonGenericCount;
            private bool _completed;

            internal MutableCountRows(
                IReadOnlyList<QuantityRevisionRow?> rows,
                int beforeCount,
                int afterGenericCount,
                int? afterReadOnlyCount = null,
                int? afterNonGenericCount = null)
            {
                _rows = rows;
                _beforeCount = beforeCount;
                _afterGenericCount = afterGenericCount;
                _afterReadOnlyCount = afterReadOnlyCount;
                _afterNonGenericCount = afterNonGenericCount;
            }

            internal int MoveNextCalls { get; private set; }

            int ICollection<QuantityRevisionRow>.Count => _completed ? _afterGenericCount : _beforeCount;
            int IReadOnlyCollection<QuantityRevisionRow>.Count => _completed ? (_afterReadOnlyCount ?? _afterGenericCount) : _beforeCount;
            int ICollection.Count => _completed ? (_afterNonGenericCount ?? _afterGenericCount) : _beforeCount;
            bool ICollection<QuantityRevisionRow>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<QuantityRevisionRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<QuantityRevisionRow>.Add(QuantityRevisionRow item) => throw new NotSupportedException();
            void ICollection<QuantityRevisionRow>.Clear() => throw new NotSupportedException();
            bool ICollection<QuantityRevisionRow>.Contains(QuantityRevisionRow item) => throw new NotSupportedException();
            void ICollection<QuantityRevisionRow>.CopyTo(QuantityRevisionRow[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<QuantityRevisionRow>.Remove(QuantityRevisionRow item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<QuantityRevisionRow>
            {
                private readonly MutableCountRows _owner;
                private int _index = -1;

                internal Enumerator(MutableCountRows owner) => _owner = owner;

                public QuantityRevisionRow Current => _owner._rows[_index]!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (next >= _owner._rows.Count)
                    {
                        _owner._completed = true;
                        return false;
                    }
                    _index = next;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string actual, string expected)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (actual ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class QuantityRevisionSummaryCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRevisionSummaryCountStabilitySmoke.Run();
    }
}
