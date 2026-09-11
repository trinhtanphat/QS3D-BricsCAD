using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using QS3D.Core.Reporting;

namespace QS3D.Core.Review
{
    public enum BqReviewStatus
    {
        Pending = 0,
        Reviewed = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum BqReviewMetric
    {
        GrossConcreteM3 = 0,
        DeductionM3 = 1,
        NetConcreteM3 = 2,
        FormworkM2 = 3,
        LengthM = 4,
        OuterPerimeterM = 5,
        InnerPerimeterM = 6,
        DoorAreaM2 = 7,
        SideAreaM2 = 8,
        BottomAreaM2 = 9,
        TopAreaM2 = 10,
        OtherAreaM2 = 11,
        WidthM = 12,
        HeightM = 13,
        MassKg = 14
    }

    public sealed class BqManualAdjustment
    {
        public BqManualAdjustment(BqReviewMetric metric, double delta, string reason)
        {
            if (!Enum.IsDefined(typeof(BqReviewMetric), metric))
                throw new ArgumentOutOfRangeException(nameof(metric));
            BqReviewContract.RequireFinite(delta, nameof(delta));
            if (delta == 0d)
                throw new ArgumentException("BQ manual adjustment delta must be non-zero.", nameof(delta));

            Metric = metric;
            Delta = delta;
            Reason = BqReviewContract.RequireText(reason, nameof(reason), "BQ adjustment reason", 1024, required: true);
        }

        public BqReviewMetric Metric { get; }
        public double Delta { get; }
        public string Reason { get; }
    }

    public sealed class BqReviewEntry
    {
        public const int MaxAdjustments = 16;

        public BqReviewEntry(
            string elementId,
            string sourceSignature,
            BqReviewStatus status,
            string note,
            string reviewer,
            DateTime reviewedUtc,
            IEnumerable<BqManualAdjustment> adjustments)
        {
            ElementId = BqReviewContract.RequireIdentity(elementId, nameof(elementId), "BQ review element id");
            SourceSignature = BqReviewContract.RequireSignature(sourceSignature, nameof(sourceSignature));
            if (!Enum.IsDefined(typeof(BqReviewStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Note = BqReviewContract.RequireText(note, nameof(note), "BQ review note", 2048, required: false);
            Reviewer = BqReviewContract.RequireText(reviewer, nameof(reviewer), "BQ reviewer", 256, required: false);
            if (reviewedUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("BQ reviewed timestamp must be UTC.", nameof(reviewedUtc));
            ReviewedUtc = reviewedUtc;
            Adjustments = SnapshotAdjustments(adjustments);
            if (Status != BqReviewStatus.Pending && Reviewer.Length == 0)
                throw new ArgumentException("Non-pending BQ review requires a reviewer.", nameof(reviewer));
            if (Status == BqReviewStatus.Rejected && Note.Length == 0)
                throw new ArgumentException("Rejected BQ review requires a note.", nameof(note));
        }

        public string ElementId { get; }
        public string SourceSignature { get; }
        public BqReviewStatus Status { get; }
        public string Note { get; }
        public string Reviewer { get; }
        public DateTime ReviewedUtc { get; }
        public IReadOnlyList<BqManualAdjustment> Adjustments { get; }

        public BqManualAdjustment? FindAdjustment(BqReviewMetric metric)
        {
            for (var i = 0; i < Adjustments.Count; i++)
                if (Adjustments[i].Metric == metric) return Adjustments[i];
            return null;
        }

        private static IReadOnlyList<BqManualAdjustment> SnapshotAdjustments(IEnumerable<BqManualAdjustment> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new List<BqManualAdjustment>();
            var metrics = new HashSet<BqReviewMetric>();
            foreach (var adjustment in source)
            {
                if (result.Count >= MaxAdjustments)
                    throw new ArgumentException("BQ review supports at most 16 manual adjustments per element.", nameof(source));
                if (adjustment == null)
                    throw new ArgumentException("BQ review adjustment collection contains a null item.", nameof(source));
                if (!metrics.Add(adjustment.Metric))
                    throw new ArgumentException("BQ review contains duplicate adjustment metric: " + adjustment.Metric + ".", nameof(source));
                result.Add(adjustment);
            }
            return result.OrderBy(x => x.Metric).ToList().AsReadOnly();
        }
    }

    public sealed class BqReviewLedgerState
    {
        public const int MaxEntries = 10000;
        private readonly IReadOnlyList<BqReviewEntry> _entries;

        public BqReviewLedgerState(IEnumerable<BqReviewEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var snapshot = new List<BqReviewEntry>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (snapshot.Count >= MaxEntries)
                    throw new ArgumentException("BQ review ledger supports at most 10000 entries.", nameof(entries));
                if (entry == null)
                    throw new ArgumentException("BQ review ledger contains a null entry.", nameof(entries));
                if (!ids.Add(entry.ElementId))
                    throw new ArgumentException("BQ review ledger contains duplicate element id: " + entry.ElementId + ".", nameof(entries));
                snapshot.Add(entry);
            }
            _entries = snapshot
                .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ElementId, StringComparer.Ordinal)
                .ToList().AsReadOnly();
        }

        public static BqReviewLedgerState Empty { get; } = new BqReviewLedgerState(Array.Empty<BqReviewEntry>());
        public IReadOnlyList<BqReviewEntry> Entries => _entries;

        public BqReviewEntry? Find(string elementId)
        {
            var canonical = BqReviewContract.RequireIdentity(elementId, nameof(elementId), "BQ review element id");
            for (var i = 0; i < _entries.Count; i++)
                if (string.Equals(_entries[i].ElementId, canonical, StringComparison.OrdinalIgnoreCase)) return _entries[i];
            return null;
        }

        public BqReviewLedgerState Upsert(BqReviewEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            var next = new List<BqReviewEntry>(_entries.Count + 1);
            var replaced = false;
            for (var i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].ElementId, entry.ElementId, StringComparison.OrdinalIgnoreCase))
                {
                    next.Add(entry);
                    replaced = true;
                }
                else next.Add(_entries[i]);
            }
            if (!replaced)
            {
                if (_entries.Count >= MaxEntries)
                    throw new InvalidOperationException("BQ review ledger is at its 10000-entry capacity.");
                next.Add(entry);
            }
            return new BqReviewLedgerState(next);
        }

        public BqReviewLedgerState Remove(string elementId, out bool removed)
        {
            var canonical = BqReviewContract.RequireIdentity(elementId, nameof(elementId), "BQ review element id");
            var next = new List<BqReviewEntry>(_entries.Count);
            removed = false;
            for (var i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].ElementId, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    removed = true;
                    continue;
                }
                next.Add(_entries[i]);
            }
            return removed ? new BqReviewLedgerState(next) : this;
        }
    }

    public static class BqReviewService
    {
        private static readonly BqReviewMetric[] Metrics =
        {
            BqReviewMetric.GrossConcreteM3,
            BqReviewMetric.DeductionM3,
            BqReviewMetric.NetConcreteM3,
            BqReviewMetric.FormworkM2,
            BqReviewMetric.LengthM,
            BqReviewMetric.OuterPerimeterM,
            BqReviewMetric.InnerPerimeterM,
            BqReviewMetric.DoorAreaM2,
            BqReviewMetric.SideAreaM2,
            BqReviewMetric.BottomAreaM2,
            BqReviewMetric.TopAreaM2,
            BqReviewMetric.OtherAreaM2,
            BqReviewMetric.WidthM,
            BqReviewMetric.HeightM,
            BqReviewMetric.MassKg
        };

        public static string ElementId(QuantityReportRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.Count != 1 || row.ElementIds.Count != 1)
                throw new InvalidOperationException("BQ review requires a detail row with exactly one semantic element.");
            return BqReviewContract.RequireIdentity(row.ElementIds[0], nameof(row), "BQ detail element id");
        }

        public static string SourceSignature(QuantityReportRow row)
        {
            var first = CanonicalSource(row);
            var second = CanonicalSource(row);
            if (!string.Equals(first, second, StringComparison.Ordinal))
                throw new InvalidOperationException("BQ detail row changed while its review source signature was being captured.");
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(second));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        public static BqReviewEntry CreateEntry(
            QuantityReportRow row,
            BqReviewStatus status,
            string note,
            string reviewer,
            DateTime reviewedUtc,
            IEnumerable<BqManualAdjustment> adjustments)
        {
            var entry = new BqReviewEntry(
                ElementId(row),
                SourceSignature(row),
                status,
                note,
                reviewer,
                reviewedUtc,
                adjustments);
            for (var i = 0; i < entry.Adjustments.Count; i++)
            {
                var adjustment = entry.Adjustments[i];
                var measured = MeasuredValue(row, adjustment.Metric);
                var adjusted = measured + adjustment.Delta;
                BqReviewContract.RequireFinite(adjusted, nameof(adjustments));
                if (adjusted < 0d)
                    throw new ArgumentException("BQ manual adjustment cannot make the proposed quantity negative.", nameof(adjustments));
            }
            return entry;
        }

        public static bool IsCurrent(BqReviewEntry? entry, QuantityReportRow row)
        {
            if (entry == null) return false;
            return string.Equals(entry.ElementId, ElementId(row), StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(entry.SourceSignature, SourceSignature(row), StringComparison.Ordinal);
        }

        public static IReadOnlyList<BqReviewMetric> AvailableMetrics(QuantityReportRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            ElementId(row);
            var result = new List<BqReviewMetric>();
            for (var i = 0; i < Metrics.Length; i++)
                if (HasEvidence(row, Metrics[i])) result.Add(Metrics[i]);
            return result.AsReadOnly();
        }

        public static double MeasuredValue(QuantityReportRow row, BqReviewMetric metric)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            ElementId(row);
            if (!HasEvidence(row, metric))
                throw new InvalidOperationException("BQ detail row has no authoritative evidence for metric " + metric + ".");
            var value = ReadMetric(row, metric);
            BqReviewContract.RequireFinite(value, nameof(row));
            return value;
        }

        public static double ProposedAdjustedValue(QuantityReportRow row, BqReviewEntry entry, BqReviewMetric metric)
        {
            RequireCurrent(entry, row);
            var value = MeasuredValue(row, metric);
            var adjustment = entry.FindAdjustment(metric);
            if (adjustment == null) return value;
            var adjusted = value + adjustment.Delta;
            BqReviewContract.RequireFinite(adjusted, nameof(entry));
            if (adjusted < 0d)
                throw new InvalidOperationException("BQ approved adjustment cannot make the effective quantity negative.");
            return adjusted;
        }

        public static double EffectiveApprovedValue(QuantityReportRow row, BqReviewEntry? entry, BqReviewMetric metric)
        {
            var measured = MeasuredValue(row, metric);
            if (entry == null || entry.Status != BqReviewStatus.Approved || !IsCurrent(entry, row))
                return measured;
            return ProposedAdjustedValue(row, entry, metric);
        }

        private static void RequireCurrent(BqReviewEntry entry, QuantityReportRow row)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (!IsCurrent(entry, row))
                throw new InvalidOperationException("BQ review entry is stale for the current authoritative detail row and must be reviewed again.");
        }

        private static bool HasEvidence(QuantityReportRow row, BqReviewMetric metric)
        {
            switch (metric)
            {
                case BqReviewMetric.GrossConcreteM3: return row.HasGrossConcreteM3Evidence;
                case BqReviewMetric.DeductionM3: return row.HasDeductionM3Evidence;
                case BqReviewMetric.NetConcreteM3: return row.HasNetConcreteM3Evidence;
                case BqReviewMetric.FormworkM2: return row.HasFormworkM2Evidence;
                case BqReviewMetric.LengthM: return row.HasLengthMEvidence;
                case BqReviewMetric.OuterPerimeterM: return row.HasOuterPerimeterMEvidence;
                case BqReviewMetric.InnerPerimeterM: return row.HasInnerPerimeterMEvidence;
                case BqReviewMetric.DoorAreaM2: return row.HasDoorAreaM2Evidence;
                case BqReviewMetric.SideAreaM2: return row.HasSideAreaM2Evidence;
                case BqReviewMetric.BottomAreaM2: return row.HasBottomAreaM2Evidence;
                case BqReviewMetric.TopAreaM2: return row.HasTopAreaM2Evidence;
                case BqReviewMetric.OtherAreaM2: return row.HasOtherAreaM2Evidence;
                case BqReviewMetric.WidthM: return row.HasWidthMEvidence;
                case BqReviewMetric.HeightM: return row.HasHeightMEvidence;
                case BqReviewMetric.MassKg: return row.MassKg.HasValue;
                default: throw new ArgumentOutOfRangeException(nameof(metric));
            }
        }

        private static double ReadMetric(QuantityReportRow row, BqReviewMetric metric)
        {
            switch (metric)
            {
                case BqReviewMetric.GrossConcreteM3: return row.GrossConcreteM3;
                case BqReviewMetric.DeductionM3: return row.DeductionM3;
                case BqReviewMetric.NetConcreteM3: return row.NetConcreteM3;
                case BqReviewMetric.FormworkM2: return row.FormworkM2;
                case BqReviewMetric.LengthM: return row.LengthM;
                case BqReviewMetric.OuterPerimeterM: return row.OuterPerimeterM;
                case BqReviewMetric.InnerPerimeterM: return row.InnerPerimeterM;
                case BqReviewMetric.DoorAreaM2: return row.DoorAreaM2;
                case BqReviewMetric.SideAreaM2: return row.SideAreaM2;
                case BqReviewMetric.BottomAreaM2: return row.BottomAreaM2;
                case BqReviewMetric.TopAreaM2: return row.TopAreaM2;
                case BqReviewMetric.OtherAreaM2: return row.OtherAreaM2;
                case BqReviewMetric.WidthM: return row.WidthM;
                case BqReviewMetric.HeightM: return row.HeightM;
                case BqReviewMetric.MassKg: return row.MassKg!.Value;
                default: throw new ArgumentOutOfRangeException(nameof(metric));
            }
        }

        private static string CanonicalSource(QuantityReportRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            var builder = new StringBuilder();
            Add(builder, ElementId(row));
            Add(builder, row.Floor ?? string.Empty);
            Add(builder, row.Zone ?? string.Empty);
            Add(builder, row.Category ?? string.Empty);
            Add(builder, row.FamilyId ?? string.Empty);
            Add(builder, row.FamilyName ?? string.Empty);
            Add(builder, row.ElementName ?? string.Empty);
            Add(builder, row.Material ?? string.Empty);
            Add(builder, row.Note ?? string.Empty);
            Add(builder, row.DrawingFingerprint ?? string.Empty);
            var handles = row.SourceHandles
                .Select(x => BqReviewContract.RequireIdentity(x, nameof(row), "BQ source handle"))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Add(builder, handles.Length.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < handles.Length; i++) Add(builder, handles[i]);
            Add(builder, row.Count.ToString(CultureInfo.InvariantCulture));
            AddDouble(builder, row.GrossConcreteM3);
            AddDouble(builder, row.DeductionM3);
            AddDouble(builder, row.NetConcreteM3);
            AddDouble(builder, row.FormworkM2);
            AddDouble(builder, row.GrossFormworkM2);
            AddDouble(builder, row.ConcreteContactDeductionM2);
            AddDouble(builder, row.NetFormworkM2);
            AddDouble(builder, row.LengthM);
            AddDouble(builder, row.WidthM);
            AddDouble(builder, row.HeightM);
            AddDouble(builder, row.OuterPerimeterM);
            AddDouble(builder, row.InnerPerimeterM);
            AddDouble(builder, row.DoorAreaM2);
            AddDouble(builder, row.SideAreaM2);
            AddDouble(builder, row.BottomAreaM2);
            AddDouble(builder, row.TopAreaM2);
            AddDouble(builder, row.OtherAreaM2);
            AddBool(builder, row.HasGrossConcreteM3Evidence);
            AddBool(builder, row.HasDeductionM3Evidence);
            AddBool(builder, row.HasNetConcreteM3Evidence);
            AddBool(builder, row.HasFormworkM2Evidence);
            AddBool(builder, row.HasGrossFormworkM2Evidence);
            AddBool(builder, row.HasConcreteContactDeductionM2Evidence);
            AddBool(builder, row.HasNetFormworkM2Evidence);
            AddBool(builder, row.HasLengthMEvidence);
            AddBool(builder, row.HasWidthMEvidence);
            AddBool(builder, row.HasHeightMEvidence);
            AddBool(builder, row.HasOuterPerimeterMEvidence);
            AddBool(builder, row.HasInnerPerimeterMEvidence);
            AddBool(builder, row.HasDoorAreaM2Evidence);
            AddBool(builder, row.HasSideAreaM2Evidence);
            AddBool(builder, row.HasBottomAreaM2Evidence);
            AddBool(builder, row.HasTopAreaM2Evidence);
            AddBool(builder, row.HasOtherAreaM2Evidence);
            AddNullableDouble(builder, row.DensityKgM3);
            AddNullableDouble(builder, row.MassKg);
            return builder.ToString();
        }

        private static void Add(StringBuilder builder, string value)
        {
            value = value ?? string.Empty;
            BqReviewContract.RequireXml(value, nameof(value), "BQ review source text");
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        private static void AddDouble(StringBuilder builder, double value)
        {
            BqReviewContract.RequireFinite(value, nameof(value));
            Add(builder, value == 0d ? "0" : value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AddNullableDouble(StringBuilder builder, double? value)
        {
            if (!value.HasValue) { Add(builder, "N"); return; }
            BqReviewContract.RequireFinite(value.Value, nameof(value));
            Add(builder, "V" + (value.Value == 0d ? "0" : value.Value.ToString("R", CultureInfo.InvariantCulture)));
        }

        private static void AddBool(StringBuilder builder, bool value) => Add(builder, value ? "1" : "0");
    }

    internal static class BqReviewContract
    {
        internal static string RequireIdentity(string value, string parameterName, string label)
        {
            if (value == null) throw new ArgumentNullException(parameterName);
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException(label + " must be canonical and non-blank.", parameterName);            if (value.Length > 512)
                throw new ArgumentException(label + " exceeds 512 characters.", parameterName);
            RequireXml(value, parameterName, label);
            return value;
        }

        internal static string RequireSignature(string value, string parameterName)
        {
            if (value == null) throw new ArgumentNullException(parameterName);
            if (value.Length != 64 || value.Any(c => !(c >= '0' && c <= '9') && !(c >= 'A' && c <= 'F')))
                throw new ArgumentException("BQ review source signature must be 64 uppercase hexadecimal characters.", parameterName);
            return value;
        }

        internal static string RequireText(string value, string parameterName, string label, int maxLength, bool required)
        {
            value = value ?? string.Empty;
            if (required && string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(label + " is required.", parameterName);
            if (value.Length > maxLength)
                throw new ArgumentException(label + " exceeds " + maxLength.ToString(CultureInfo.InvariantCulture) + " characters.", parameterName);
            if (value.Length != 0 && !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException(label + " must not contain leading or trailing whitespace.", parameterName);
            RequireXml(value, parameterName, label);
            return value;
        }

        internal static void RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "BQ review numeric value must be finite.");
        }
        internal static void RequireXml(string value, string parameterName, string label)
        {
            try { XmlConvert.VerifyXmlChars(value ?? string.Empty); }
            catch (XmlException ex)
            {
                throw new ArgumentException(label + " contains characters that cannot be persisted to QSDB XML.", parameterName, ex);
            }
        }
    }
}
