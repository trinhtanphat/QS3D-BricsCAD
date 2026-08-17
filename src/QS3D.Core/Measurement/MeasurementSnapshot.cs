using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace QS3D.Core.Measurement
{
    public sealed class MeasurementSnapshot
    {
        private const int MaximumTraceCount = 10000;

        public MeasurementSnapshot(IEnumerable<MeasurementTrace> traces)
        {
            if (traces == null) throw new ArgumentNullException(nameof(traces));
            RequireSupportedCount(traces, nameof(traces));

            var items = new List<MeasurementTrace>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var trace in traces)
            {
                if (items.Count >= MaximumTraceCount)
                    throw TraceCountError(nameof(traces));
                if (trace == null)
                    throw new ArgumentException("Measurement snapshot traces cannot contain null entries.", nameof(traces));

                var identity = IdentityKey(trace);
                if (!identities.Add(identity))
                    throw new ArgumentException("Measurement snapshot contains duplicate measurement identity: " + trace.SemanticIdentity + "/" + trace.SourceIdentity + "/" + trace.QuantityKey + ".", nameof(traces));

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

        private static void RequireSupportedCount(IEnumerable<MeasurementTrace> traces, string paramName)
        {
            int? knownCount = null;
            if (traces is ICollection<MeasurementTrace> collection)
                ValidateKnownCount(collection.Count, ref knownCount, paramName);
            if (traces is IReadOnlyCollection<MeasurementTrace> readOnlyCollection)
                ValidateKnownCount(readOnlyCollection.Count, ref knownCount, paramName);
            if (traces is System.Collections.ICollection nonGenericCollection)
                ValidateKnownCount(nonGenericCollection.Count, ref knownCount, paramName);
        }

        private static void ValidateKnownCount(int count, ref int? knownCount, string paramName)
        {
            if (count < 0)
                throw new InvalidOperationException("Measurement snapshot collection reports a negative known count.");
            if (count > MaximumTraceCount)
                throw TraceCountError(paramName);
            if (knownCount.HasValue && knownCount.Value != count)
                throw new ArgumentException("Measurement snapshot count contracts disagree.", paramName);
            knownCount = count;
        }

        private static ArgumentException TraceCountError(string paramName)
        {
            return new ArgumentException("Measurement snapshot accepts at most " + MaximumTraceCount + " traces.", paramName);
        }

        private static string IdentityKey(MeasurementTrace trace) => trace.SemanticIdentity + "\u001f" + trace.SourceIdentity + "\u001f" + trace.QuantityKey;

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