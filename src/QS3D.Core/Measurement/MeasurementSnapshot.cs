using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace QS3D.Core.Measurement
{
    /// <summary>
    /// Immutable, deterministic snapshot of already-computed canonical measurement traces.
    /// This type does not calculate or reconcile quantities; MeasurementTrace remains the
    /// authoritative explanation of each measured value.
    /// </summary>
    public sealed class MeasurementSnapshot
    {
        public MeasurementSnapshot(IEnumerable<MeasurementTrace> traces)
        {
            if (traces == null) throw new ArgumentNullException(nameof(traces));

            var items = new List<MeasurementTrace>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trace in traces)
            {
                if (trace == null)
                    throw new ArgumentException("Measurement snapshot traces cannot contain null entries.", nameof(traces));

                var identity = IdentityKey(trace);
                if (!identities.Add(identity))
                    throw new ArgumentException(
                        "Measurement snapshot contains duplicate measurement identity: " +
                        trace.SemanticIdentity + "/" + trace.SourceIdentity + "/" + trace.QuantityKey + ".",
                        nameof(traces));

                items.Add(trace);
            }

            items.Sort(CompareTraces);
            Traces = new ReadOnlyCollection<MeasurementTrace>(items.ToArray());
        }

        public IReadOnlyList<MeasurementTrace> Traces { get; }

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            AppendToken(builder, "MS1");
            AppendCount(builder, Traces.Count);
            for (var i = 0; i < Traces.Count; i++)
                AppendToken(builder, Traces[i].ToCanonicalString());
            return builder.ToString();
        }

        private static string IdentityKey(MeasurementTrace trace)
        {
            // MeasurementTrace rejects control characters, so U+001F cannot occur in any
            // of these canonical identity tokens and is safe as an internal separator.
            return trace.SemanticIdentity + "\u001f" + trace.SourceIdentity + "\u001f" + trace.QuantityKey;
        }

        private static int CompareTraces(MeasurementTrace left, MeasurementTrace right)
        {
            var compare = StringComparer.Ordinal.Compare(left.SemanticIdentity, right.SemanticIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SourceIdentity, right.SourceIdentity);
            if (compare != 0) return compare;
            return StringComparer.Ordinal.Compare(left.QuantityKey, right.QuantityKey);
        }

        private static void AppendToken(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        private static void AppendCount(StringBuilder builder, int value)
        {
            AppendToken(builder, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
