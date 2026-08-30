using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportInputBoundSmoke
    {
        private const int MaximumInputElements = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownOversizeCountFailsBeforeEnumeration();
            StreamingOverrunFailsBeforeCurrent10001();
            ExactStreamingBoundaryRemainsAccepted();
            OrdinaryStreamingInputRemainsAccepted();
        }

        private static void KnownOversizeCountFailsBeforeEnumeration()
        {
            var source = new OversizeKnownSource();
            ExpectBoundFailure(() => QuantityReportBuilder.Group(source));
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("Quantity report known-oversize input was enumerated before admission rejection.");
        }

        private static void StreamingOverrunFailsBeforeCurrent10001()
        {
            var source = new StreamingSource(null);
            ExpectBoundFailure(() => QuantityReportBuilder.Group(source));
            Equal(MaximumInputElements + 1, source.MoveNextCalls, "streaming overrun MoveNext count");
            Equal(MaximumInputElements, source.CurrentReads, "streaming overrun Current read count");
        }

        private static void ExactStreamingBoundaryRemainsAccepted()
        {
            var source = new StreamingSource(MaximumInputElements);
            var rows = QuantityReportBuilder.Group(source);
            Equal(1, rows.Count, "exact-boundary group count");
            Equal(MaximumInputElements, rows[0].Count, "exact-boundary quantity row count");
            Equal(MaximumInputElements, rows[0].ElementIds.Count, "exact-boundary provenance count");
            Equal(MaximumInputElements + 1, source.MoveNextCalls, "exact-boundary MoveNext count");
            Equal(MaximumInputElements, source.CurrentReads, "exact-boundary Current read count");
        }

        private static void OrdinaryStreamingInputRemainsAccepted()
        {
            var source = new StreamingSource(3);
            var rows = QuantityReportBuilder.Group(source);
            Equal(1, rows.Count, "ordinary streaming group count");
            Equal(3, rows[0].Count, "ordinary streaming quantity row count");
            Equal(3, rows[0].ElementIds.Count, "ordinary streaming provenance count");
        }

        private static void ExpectBoundFailure(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("at most 10000 input elements", StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("Quantity report bound failure used an unexpected diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException("Quantity report accepted input beyond the supported 10000-element ceiling.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class OversizeKnownSource : IReadOnlyCollection<ElementInstance>
        {
            public bool EnumeratorRequested { get; private set; }
            public int Count => MaximumInputElements + 1;

            public IEnumerator<ElementInstance> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new InvalidOperationException("Known-oversize quantity source must fail before GetEnumerator.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingSource : IEnumerable<ElementInstance>
        {
            private readonly int? _length;
            private readonly FamilyDefinition _family = new FamilyDefinition("QB300", ElementCategory.Beam, "Concrete");

            internal StreamingSource(int? length)
            {
                _length = length;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<ElementInstance> GetEnumerator() => new StreamingEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class StreamingEnumerator : IEnumerator<ElementInstance>
            {
                private readonly StreamingSource _owner;
                private int _index = -1;

                internal StreamingEnumerator(StreamingSource owner)
                {
                    _owner = owner;
                }

                public ElementInstance Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return new ElementInstance(
                            "QB" + _index.ToString("D5", CultureInfo.InvariantCulture),
                            _owner._family,
                            "L1")
                        {
                            GrossConcreteM3 = 1d
                        };
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (_owner._length.HasValue && next >= _owner._length.Value)
                        return false;
                    _index = next;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
