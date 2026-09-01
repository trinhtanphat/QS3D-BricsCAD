using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Measurement;

namespace QS3D.Core.Progress
{
    public sealed class ProjectDate : IComparable<ProjectDate>, IEquatable<ProjectDate>
    {
        public ProjectDate(int year, int month, int day)
        {
            Value = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        private ProjectDate(DateTime value)
        {
            Value = value;
        }

        public DateTime Value { get; }

        public static ProjectDate FromDateTime(DateTime value)
        {
            if (value.Kind != DateTimeKind.Unspecified)
                throw new ArgumentException("Project date must use DateTimeKind.Unspecified.", nameof(value));
            if (value.TimeOfDay != TimeSpan.Zero)
                throw new ArgumentException("Project date must not carry a time-of-day component.", nameof(value));
            return new ProjectDate(value);
        }

        public int CompareTo(ProjectDate? other) => other == null ? 1 : Value.CompareTo(other.Value);

        public bool Equals(ProjectDate? other) => other != null && Value.Equals(other.Value);

        public override bool Equals(object? obj) => Equals(obj as ProjectDate);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public sealed class ProgressMeasurement
    {
        public ProgressMeasurement(
            string measurementId,
            ProjectDate asOfDate,
            MeasurementTrace sourceMeasurement,
            decimal installedQuantity,
            decimal acceptedQuantity,
            string measurementBasis = "measured-quantity",
            string? evidenceReference = null)
        {
            MeasurementId = ProgressDomainContract.RequireToken(measurementId, nameof(measurementId));
            AsOfDate = asOfDate ?? throw new ArgumentNullException(nameof(asOfDate));
            if (sourceMeasurement == null) throw new ArgumentNullException(nameof(sourceMeasurement));

            SemanticIdentity = sourceMeasurement.SemanticIdentity;
            SourceIdentity = sourceMeasurement.SourceIdentity;
            QuantityKey = sourceMeasurement.QuantityKey;
            Unit = sourceMeasurement.Unit;
            MeasuredQuantity = ProgressDomainContract.ConvertQuantity(sourceMeasurement.NetValue, "measured quantity");
            MeasurementFingerprint = ProgressDomainContract.Sha256(sourceMeasurement.ToCanonicalString());
            MeasurementBasis = ProgressDomainContract.RequireToken(measurementBasis, nameof(measurementBasis));
            EvidenceReference = evidenceReference == null
                ? null
                : ProgressDomainContract.RequireText(evidenceReference, nameof(evidenceReference));

            if (installedQuantity < 0m)
                throw new ArgumentOutOfRangeException(nameof(installedQuantity));
            if (acceptedQuantity < 0m)
                throw new ArgumentOutOfRangeException(nameof(acceptedQuantity));
            if (installedQuantity > MeasuredQuantity)
                throw new ArgumentOutOfRangeException(nameof(installedQuantity), "Installed quantity cannot exceed the frozen measured quantity.");
            if (acceptedQuantity > installedQuantity)
                throw new ArgumentOutOfRangeException(nameof(acceptedQuantity), "Accepted quantity cannot exceed installed quantity.");

            InstalledQuantity = installedQuantity == 0m ? 0m : installedQuantity;
            AcceptedQuantity = acceptedQuantity == 0m ? 0m : acceptedQuantity;
        }

        public string MeasurementId { get; }
        public ProjectDate AsOfDate { get; }
        public string SemanticIdentity { get; }
        public string SourceIdentity { get; }
        public string QuantityKey { get; }
        public decimal MeasuredQuantity { get; }
        public decimal InstalledQuantity { get; }
        public decimal AcceptedQuantity { get; }
        public string Unit { get; }
        public string MeasurementFingerprint { get; }
        public string MeasurementBasis { get; }
        public string? EvidenceReference { get; }

        public string MeasurementIdentity =>
            ProgressDomainContract.MeasurementIdentity(SemanticIdentity, SourceIdentity, QuantityKey);

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            ProgressDomainContract.AppendToken(builder, "PM1");
            ProgressDomainContract.AppendToken(builder, MeasurementId);
            ProgressDomainContract.AppendToken(builder, AsOfDate.ToString());
            ProgressDomainContract.AppendToken(builder, SemanticIdentity);
            ProgressDomainContract.AppendToken(builder, SourceIdentity);
            ProgressDomainContract.AppendToken(builder, QuantityKey);
            ProgressDomainContract.AppendDecimal(builder, MeasuredQuantity);
            ProgressDomainContract.AppendDecimal(builder, InstalledQuantity);
            ProgressDomainContract.AppendDecimal(builder, AcceptedQuantity);
            ProgressDomainContract.AppendToken(builder, Unit);
            ProgressDomainContract.AppendToken(builder, MeasurementFingerprint);
            ProgressDomainContract.AppendToken(builder, MeasurementBasis);
            ProgressDomainContract.AppendNullableToken(builder, EvidenceReference);
            return builder.ToString();
        }
    }
    public sealed class ProgressSnapshot
    {
        public ProgressSnapshot(
            string snapshotId,
            int revision,
            ProjectDate dataDate,
            DateTime createdAtUtc,
            IEnumerable<ProgressMeasurement> measurements,
            string? supersedesSnapshotId = null)
        {
            SnapshotId = ProgressDomainContract.RequireToken(snapshotId, nameof(snapshotId));
            if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
            Revision = revision;
            DataDate = dataDate ?? throw new ArgumentNullException(nameof(dataDate));
            CreatedAtUtc = ProgressDomainContract.RequireUtc(createdAtUtc, nameof(createdAtUtc));
            SupersedesSnapshotId = supersedesSnapshotId == null
                ? null
                : ProgressDomainContract.RequireToken(supersedesSnapshotId, nameof(supersedesSnapshotId));
            if (string.Equals(SnapshotId, SupersedesSnapshotId, StringComparison.Ordinal))
                throw new ArgumentException("Progress snapshot cannot supersede itself.", nameof(supersedesSnapshotId));
            if (Revision == 1 && SupersedesSnapshotId != null)
                throw new ArgumentException("Progress snapshot revision 1 cannot supersede another snapshot.", nameof(supersedesSnapshotId));
            if (Revision > 1 && SupersedesSnapshotId == null)
                throw new ArgumentException("Progress snapshot revisions after 1 require a superseded snapshot id.", nameof(supersedesSnapshotId));

            var items = ProgressDomainContract.Snapshot(measurements, nameof(measurements), "progress measurements");
            items.Sort(CompareMeasurements);
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.AsOfDate.CompareTo(DataDate) > 0)
                    throw new ArgumentException("Progress measurement date cannot be after the snapshot data date.", nameof(measurements));
                if (!identities.Add(item.MeasurementIdentity))
                    throw new ArgumentException("Progress snapshot contains a duplicate measurement identity.", nameof(measurements));
                if (!ids.Add(item.MeasurementId))
                    throw new ArgumentException("Progress snapshot contains a duplicate measurement id.", nameof(measurements));
            }
            Measurements = new ReadOnlyCollection<ProgressMeasurement>(items.ToArray());
            CanonicalDigest = ProgressDomainContract.Sha256(ToCanonicalStringCore());
        }

        public string SnapshotId { get; }
        public int Revision { get; }
        public ProjectDate DataDate { get; }
        public DateTime CreatedAtUtc { get; }
        public string? SupersedesSnapshotId { get; }
        public IReadOnlyList<ProgressMeasurement> Measurements { get; }
        public string CanonicalDigest { get; }

        public string ToCanonicalString() => ToCanonicalStringCore();

        private string ToCanonicalStringCore()
        {
            var builder = new StringBuilder();
            ProgressDomainContract.AppendToken(builder, "PS1");
            ProgressDomainContract.AppendToken(builder, SnapshotId);
            ProgressDomainContract.AppendInt(builder, Revision);
            ProgressDomainContract.AppendToken(builder, DataDate.ToString());
            ProgressDomainContract.AppendToken(builder, CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            ProgressDomainContract.AppendNullableToken(builder, SupersedesSnapshotId);
            ProgressDomainContract.AppendInt(builder, Measurements.Count);
            for (var i = 0; i < Measurements.Count; i++)
                ProgressDomainContract.AppendToken(builder, Measurements[i].ToCanonicalString());
            return builder.ToString();
        }

        private static int CompareMeasurements(ProgressMeasurement left, ProgressMeasurement right)
        {
            var compare = StringComparer.Ordinal.Compare(left.SemanticIdentity, right.SemanticIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SourceIdentity, right.SourceIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.QuantityKey, right.QuantityKey);
            if (compare != 0) return compare;
            return StringComparer.Ordinal.Compare(left.MeasurementId, right.MeasurementId);
        }
    }

    [Flags]
    public enum ProgressSnapshotDeltaKind
    {
        None = 0,
        Added = 1,
        Removed = 2,
        SourceChanged = 4,
        MeasuredQuantityChanged = 8,
        InstalledQuantityChanged = 16,
        AcceptedQuantityChanged = 32
    }

    public sealed class ProgressSnapshotDeltaLine
    {
        internal ProgressSnapshotDeltaLine(
            string measurementIdentity,
            ProgressSnapshotDeltaKind kind,
            decimal? beforeMeasured,
            decimal? afterMeasured,
            decimal? beforeInstalled,
            decimal? afterInstalled,
            decimal? beforeAccepted,
            decimal? afterAccepted)
        {
            MeasurementIdentity = measurementIdentity;
            Kind = kind;
            BeforeMeasuredQuantity = beforeMeasured;
            AfterMeasuredQuantity = afterMeasured;
            BeforeInstalledQuantity = beforeInstalled;
            AfterInstalledQuantity = afterInstalled;
            BeforeAcceptedQuantity = beforeAccepted;
            AfterAcceptedQuantity = afterAccepted;
        }

        public string MeasurementIdentity { get; }
        public ProgressSnapshotDeltaKind Kind { get; }
        public decimal? BeforeMeasuredQuantity { get; }
        public decimal? AfterMeasuredQuantity { get; }
        public decimal? BeforeInstalledQuantity { get; }
        public decimal? AfterInstalledQuantity { get; }
        public decimal? BeforeAcceptedQuantity { get; }
        public decimal? AfterAcceptedQuantity { get; }
    }

    public sealed class ProgressSnapshotDelta
    {
        private ProgressSnapshotDelta(
            string beforeSnapshotId,
            string afterSnapshotId,
            IReadOnlyList<ProgressSnapshotDeltaLine> changes)
        {
            BeforeSnapshotId = beforeSnapshotId;
            AfterSnapshotId = afterSnapshotId;
            Changes = changes;
        }

        public string BeforeSnapshotId { get; }
        public string AfterSnapshotId { get; }
        public IReadOnlyList<ProgressSnapshotDeltaLine> Changes { get; }

        public static ProgressSnapshotDelta Compare(ProgressSnapshot before, ProgressSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));

            var beforeByIdentity = Index(before.Measurements);
            var afterByIdentity = Index(after.Measurements);
            var identities = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var identity in beforeByIdentity.Keys) identities.Add(identity);
            foreach (var identity in afterByIdentity.Keys) identities.Add(identity);

            var changes = new List<ProgressSnapshotDeltaLine>();
            foreach (var identity in identities)
            {
                var hasBefore = beforeByIdentity.TryGetValue(identity, out var beforeItem);
                var hasAfter = afterByIdentity.TryGetValue(identity, out var afterItem);
                if (!hasBefore)
                {
                    changes.Add(new ProgressSnapshotDeltaLine(
                        identity,
                        ProgressSnapshotDeltaKind.Added,
                        null,
                        afterItem!.MeasuredQuantity,
                        null,
                        afterItem.InstalledQuantity,
                        null,
                        afterItem.AcceptedQuantity));
                    continue;
                }
                if (!hasAfter)
                {
                    changes.Add(new ProgressSnapshotDeltaLine(
                        identity,
                        ProgressSnapshotDeltaKind.Removed,
                        beforeItem!.MeasuredQuantity,
                        null,
                        beforeItem.InstalledQuantity,
                        null,
                        beforeItem.AcceptedQuantity,
                        null));
                    continue;
                }

                var kind = ProgressSnapshotDeltaKind.None;
                if (!string.Equals(beforeItem!.MeasurementFingerprint, afterItem!.MeasurementFingerprint, StringComparison.Ordinal) ||
                    !string.Equals(beforeItem.Unit, afterItem.Unit, StringComparison.Ordinal))
                    kind |= ProgressSnapshotDeltaKind.SourceChanged;
                if (beforeItem.MeasuredQuantity != afterItem.MeasuredQuantity)
                    kind |= ProgressSnapshotDeltaKind.MeasuredQuantityChanged;
                if (beforeItem.InstalledQuantity != afterItem.InstalledQuantity)
                    kind |= ProgressSnapshotDeltaKind.InstalledQuantityChanged;
                if (beforeItem.AcceptedQuantity != afterItem.AcceptedQuantity)
                    kind |= ProgressSnapshotDeltaKind.AcceptedQuantityChanged;

                if (kind != ProgressSnapshotDeltaKind.None)
                {
                    changes.Add(new ProgressSnapshotDeltaLine(
                        identity,
                        kind,
                        beforeItem.MeasuredQuantity,
                        afterItem.MeasuredQuantity,
                        beforeItem.InstalledQuantity,
                        afterItem.InstalledQuantity,
                        beforeItem.AcceptedQuantity,
                        afterItem.AcceptedQuantity));
                }
            }

            return new ProgressSnapshotDelta(
                before.SnapshotId,
                after.SnapshotId,
                new ReadOnlyCollection<ProgressSnapshotDeltaLine>(changes.ToArray()));
        }

        private static Dictionary<string, ProgressMeasurement> Index(IReadOnlyList<ProgressMeasurement> measurements)
        {
            var result = new Dictionary<string, ProgressMeasurement>(StringComparer.Ordinal);
            for (var i = 0; i < measurements.Count; i++)
                result.Add(measurements[i].MeasurementIdentity, measurements[i]);
            return result;
        }
    }

    internal static class ProgressDomainContract
    {
        internal const int MaximumEntries = 10000;

        internal static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Progress identity token is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Progress identity token must not contain surrounding whitespace.", parameterName);
            RequireValidUtf16(value, parameterName, "Progress identity token");
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]) || char.IsWhiteSpace(value[i]))
                    throw new ArgumentException("Progress identity token must not contain whitespace or control characters.", parameterName);
            }
            return value;
        }

        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Progress text is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Progress text must not contain surrounding whitespace.", parameterName);
            RequireValidUtf16(value, parameterName, "Progress text");
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    throw new ArgumentException("Progress text must not contain control characters.", parameterName);
            }
            return value;
        }

        private static void RequireValidUtf16(string value, string parameterName, string label)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw new ArgumentException(label + " must not contain malformed UTF-16.", parameterName);
                    i++;
                    continue;
                }

                if (char.IsLowSurrogate(value[i]))
                    throw new ArgumentException(label + " must not contain malformed UTF-16.", parameterName);
            }
        }

        internal static string RequireCurrency(string value, string parameterName)
        {
            value = RequireToken(value, parameterName);
            if (value.Length != 3)
                throw new ArgumentException("Currency must contain exactly three upper-case ASCII letters.", parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] < 'A' || value[i] > 'Z')
                    throw new ArgumentException("Currency must contain exactly three upper-case ASCII letters.", parameterName);
            }
            return value;
        }

        internal static DateTime RequireUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Progress technical timestamps must use UTC.", parameterName);
            return value;
        }

        internal static decimal ConvertQuantity(double value, string label)
        {
            decimal converted;
            try
            {
                converted = checked((decimal)value);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Progress " + label + " cannot be represented as decimal.", ex);
            }
            if (value != 0d && converted == 0m)
                throw new OverflowException("Progress " + label + " underflowed to decimal zero.");
            return converted == 0m ? 0m : converted;
        }

        internal static List<T> Snapshot<T>(IEnumerable<T> source, string parameterName, string label) where T : class
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var knownCount = SnapshotKnownCount(source, parameterName, label);

            var result = new List<T>();
            using (var enumerator = source.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStable(source, knownCount, parameterName, label);
                    if (!enumerator.MoveNext())
                        break;
                    RequireKnownCountStable(source, knownCount, parameterName, label);
                    if (knownCount.HasValue && result.Count >= knownCount.Value)
                        throw CountMismatch(knownCount.Value, result.Count + 1, parameterName, label);
                    if (result.Count == MaximumEntries)
                        throw TooMany(parameterName, label);

                    var item = enumerator.Current;
                    RequireKnownCountStable(source, knownCount, parameterName, label);
                    if (item == null)
                        throw new ArgumentException(label + " cannot contain null entries.", parameterName);
                    result.Add(item);
                }
            }
            if (knownCount.HasValue && knownCount.Value != result.Count)
                throw CountMismatch(knownCount.Value, result.Count, parameterName, label);

            RequireKnownCountStable(source, knownCount, parameterName, label);
            return result;
        }

        private static void RequireKnownCountStable<T>(IEnumerable<T> source, int? expectedKnownCount, string parameterName, string label) where T : class
        {
            var observedKnownCount = SnapshotKnownCount(source, parameterName, label);
            if (expectedKnownCount != observedKnownCount)
                throw new ArgumentException(label + " known count changed during traversal.", parameterName);
        }

        private static int? SnapshotKnownCount<T>(IEnumerable<T> source, string parameterName, string label) where T : class
        {
            int? knownCount = null;
            if (source is ICollection<T> collection)
                ObserveCount(collection.Count, ref knownCount, parameterName, label);
            if (source is IReadOnlyCollection<T> readOnlyCollection)
                ObserveCount(readOnlyCollection.Count, ref knownCount, parameterName, label);
            if (source is ICollection nonGenericCollection)
                ObserveCount(nonGenericCollection.Count, ref knownCount, parameterName, label);
            return knownCount;
        }

        private static void ObserveCount(int count, ref int? knownCount, string parameterName, string label)
        {
            if (count < 0)
                throw new ArgumentException(label + " reports a negative known count.", parameterName);
            if (count > MaximumEntries)
                throw TooMany(parameterName, label);
            if (knownCount.HasValue && knownCount.Value != count)
                throw new ArgumentException(label + " reports conflicting known counts.", parameterName);
            knownCount = count;
        }

        private static ArgumentException CountMismatch(int knownCount, int observedCount, string parameterName, string label) =>
            new ArgumentException(
                label + " traversal produced " + observedCount.ToString(CultureInfo.InvariantCulture) +
                " entries but its reported known count was " + knownCount.ToString(CultureInfo.InvariantCulture) + ".",
                parameterName);

        private static ArgumentException TooMany(string parameterName, string label) =>
            new ArgumentException(label + " supports at most " + MaximumEntries + " entries.", parameterName);

        internal static string MeasurementIdentity(string semanticIdentity, string sourceIdentity, string quantityKey) =>
            RequireToken(semanticIdentity, nameof(semanticIdentity)) + "\u001f" +
            RequireToken(sourceIdentity, nameof(sourceIdentity)) + "\u001f" +
            RequireToken(quantityKey, nameof(quantityKey));

        internal static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        internal static void AppendToken(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        internal static void AppendNullableToken(StringBuilder builder, string? value)
        {
            AppendToken(builder, value == null ? "N" : "V" + value);
        }

        internal static void AppendInt(StringBuilder builder, int value) =>
            AppendToken(builder, value.ToString(CultureInfo.InvariantCulture));

        internal static void AppendDecimal(StringBuilder builder, decimal value) =>
            AppendToken(builder, value.ToString("G29", CultureInfo.InvariantCulture));
    }
}
