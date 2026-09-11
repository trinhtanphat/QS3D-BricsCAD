using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QS3D.Core.Review;

namespace QS3D.Core.Domain
{
    internal static class ProjectBqReviewLedgerCodec
    {
        internal const string ReservedRoot = "QS3D.BQReview.";
        internal const string Prefix = "QS3D.BQReview.v1.";
        internal const string LedgerKey = Prefix + "Ledger";
        private const string PayloadVersion = "1";
        private const int MaxPayloadChars = 1024 * 1024;

        internal static bool IsReservedKey(string key) =>
            key != null && key.StartsWith(ReservedRoot, StringComparison.OrdinalIgnoreCase);

        internal static BqReviewLedgerState? Read(IEnumerable<KeyValuePair<string, string>> metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            var found = false;
            string? payload = null;
            foreach (var pair in metadata)
            {
                if (!IsReservedKey(pair.Key)) continue;
                if (!string.Equals(pair.Key, LedgerKey, StringComparison.Ordinal))                    throw new FormatException("BQ review metadata contains an unsupported or non-canonical reserved key: " + pair.Key + ".");
                if (found) throw new FormatException("BQ review metadata contains duplicate ledger state.");
                found = true;
                payload = pair.Value ?? string.Empty;
            }
            return found ? Decode(payload ?? string.Empty) : null;
        }

        internal static string Value(BqReviewLedgerState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var fields = new List<string>
            {
                PayloadVersion,
                Int(state.Entries.Count)
            };

            for (var i = 0; i < state.Entries.Count; i++)
            {
                var entry = state.Entries[i];
                fields.Add(entry.ElementId);
                fields.Add(entry.SourceSignature);
                fields.Add(Int((int)entry.Status));
                fields.Add(entry.Note);
                fields.Add(entry.Reviewer);
                fields.Add(Utc(entry.ReviewedUtc));
                fields.Add(Int(entry.Adjustments.Count));
                for (var j = 0; j < entry.Adjustments.Count; j++)
                {
                    var adjustment = entry.Adjustments[j];
                    fields.Add(Int((int)adjustment.Metric));
                    fields.Add(Double(adjustment.Delta));
                    fields.Add(adjustment.Reason);
                }
            }

            var builder = new StringBuilder();
            for (var i = 0; i < fields.Count; i++) AppendField(builder, fields[i]);
            var payload = builder.ToString();
            if (payload.Length > MaxPayloadChars) throw PayloadTooLargeError();
            PersistedTextXml.Verify(payload, nameof(state), "BQ review ledger metadata");
            Decode(payload);
            return payload;
        }

        private static BqReviewLedgerState Decode(string payload)
        {
            try
            {
                if (payload.Length > MaxPayloadChars)
                    throw new FormatException("BQ review ledger exceeds the maximum supported metadata payload of 1 MiB characters.");
                PersistedTextXml.Verify(payload, nameof(payload), "BQ review ledger metadata");
                var offset = 0;
                var version = ReadField(payload, ref offset);
                if (!string.Equals(version, PayloadVersion, StringComparison.Ordinal))
                    throw new FormatException("BQ review ledger payload version is unsupported: " + version + ".");

                var count = ReadCount(payload, ref offset, BqReviewLedgerState.MaxEntries, "entry");
                var entries = new List<BqReviewEntry>(count);
                for (var i = 0; i < count; i++)
                {
                    var elementId = ReadField(payload, ref offset);
                    var sourceSignature = ReadField(payload, ref offset);
                    var statusValue = ReadInt(payload, ref offset, "status");
                    if (!Enum.IsDefined(typeof(BqReviewStatus), statusValue))
                        throw new FormatException("BQ review status is undefined: " + statusValue + ".");
                    var note = ReadField(payload, ref offset);
                    var reviewer = ReadField(payload, ref offset);
                    var reviewedUtc = ReadUtc(payload, ref offset);
                    var adjustmentCount = ReadCount(payload, ref offset, BqReviewEntry.MaxAdjustments, "adjustment");
                    var adjustments = new List<BqManualAdjustment>(adjustmentCount);
                    for (var j = 0; j < adjustmentCount; j++)
                    {
                        var metricValue = ReadInt(payload, ref offset, "adjustment metric");
                        if (!Enum.IsDefined(typeof(BqReviewMetric), metricValue))
                            throw new FormatException("BQ review adjustment metric is undefined: " + metricValue + ".");
                        adjustments.Add(new BqManualAdjustment(
                            (BqReviewMetric)metricValue,
                            ReadDouble(payload, ref offset, "adjustment delta"),
                            ReadField(payload, ref offset)));
                    }
                    entries.Add(new BqReviewEntry(elementId, sourceSignature, (BqReviewStatus)statusValue, note, reviewer, reviewedUtc, adjustments));
                }

                if (offset != payload.Length)
                    throw new FormatException("BQ review ledger contains trailing data.");
                return new BqReviewLedgerState(entries);
            }
            catch (FormatException) { throw; }
            catch (ArgumentException ex) { throw new FormatException("BQ review ledger metadata is invalid.", ex); }
            catch (OverflowException ex) { throw new FormatException("BQ review ledger metadata overflowed a supported numeric range.", ex); }
            catch (InvalidOperationException ex) { throw new FormatException("BQ review ledger metadata is inconsistent.", ex); }
        }

        private static void AppendField(StringBuilder builder, string value)
        {
            value = value ?? string.Empty;
            var lengthToken = value.Length.ToString(CultureInfo.InvariantCulture);
            var prospectiveLength = (long)builder.Length + lengthToken.Length + 1L + value.Length;
            if (prospectiveLength > MaxPayloadChars) throw PayloadTooLargeError();
            builder.Append(lengthToken);
            builder.Append(':');
            builder.Append(value);
        }

        private static string ReadField(string payload, ref int offset)
        {
            var colon = payload.IndexOf(':', offset);
            if (colon <= offset) throw new FormatException("BQ review ledger field length is missing.");
            var token = payload.Substring(offset, colon - offset);
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 0 ||
                !string.Equals(token, length.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("BQ review ledger field length is invalid or non-canonical.");
            offset = colon + 1;
            if (length > payload.Length - offset)
                throw new FormatException("BQ review ledger field exceeds available data.");
            var value = payload.Substring(offset, length);
            offset += length;
            return value;
        }

        private static int ReadCount(string payload, ref int offset, int maximum, string label)
        {
            var value = ReadInt(payload, ref offset, label + " count");
            if (value < 0 || value > maximum)
                throw new FormatException("BQ review " + label + " count is outside the supported range 0.." + maximum + ".");
            return value;
        }

        private static int ReadInt(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            if (!int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ||
                !string.Equals(token, value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new FormatException("BQ review " + label + " is invalid or non-canonical.");
            return value;
        }

        private static double ReadDouble(string payload, ref int offset, string label)
        {
            var token = ReadField(payload, ref offset);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) ||
                !string.Equals(token, Double(value), StringComparison.Ordinal))                throw new FormatException("BQ review " + label + " is invalid or non-canonical.");
            return value == 0d ? 0d : value;
        }

        private static DateTime ReadUtc(string payload, ref int offset)
        {
            var token = ReadField(payload, ref offset);
            if (!DateTime.TryParseExact(token, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ||
                value.Kind != DateTimeKind.Utc || !string.Equals(token, Utc(value), StringComparison.Ordinal))
                throw new FormatException("BQ review timestamp is invalid, non-UTC or non-canonical.");
            return value;
        }

        private static string Double(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "BQ review numeric value must be finite.");
            return value == 0d ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Utc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("BQ review timestamp must be UTC.", nameof(value));
            return value.ToString("O", CultureInfo.InvariantCulture);
        }

        private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static InvalidOperationException PayloadTooLargeError() =>
            new InvalidOperationException("BQ review ledger exceeds the maximum supported metadata payload of 1 MiB characters.");
    }
}
