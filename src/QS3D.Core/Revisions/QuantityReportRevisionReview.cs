using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.Revisions
{
    public enum QuantityReportRevisionChangeKind
    {
        Added = 0,
        Removed = 1,
        Changed = 2
    }

    /// <summary>
    /// Immutable, CAD-independent copy of one authoritative BQ detail row.
    /// Native handles and drawing fingerprints are deliberately not revision keys.
    /// </summary>
    public sealed class QuantityReportRevisionRowSnapshot
    {
        internal QuantityReportRevisionRowSnapshot(string stableKey, QuantityReportRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            StableKey = stableKey ?? string.Empty;
            Floor = row.Floor ?? string.Empty;
            Zone = row.Zone ?? string.Empty;
            Category = row.Category ?? string.Empty;
            FamilyId = row.FamilyId ?? string.Empty;
            FamilyName = row.FamilyName ?? string.Empty;
            ElementName = row.ElementName ?? string.Empty;
            Material = row.Material ?? string.Empty;
            Note = row.Note ?? string.Empty;
            Count = row.Count;
            GrossConcreteM3 = row.GrossConcreteM3;
            DeductionM3 = row.DeductionM3;
            NetConcreteM3 = row.NetConcreteM3;
            FormworkM2 = row.FormworkM2;
            LengthM = row.LengthM;
            OuterPerimeterM = row.OuterPerimeterM;
            InnerPerimeterM = row.InnerPerimeterM;
            DoorAreaM2 = row.DoorAreaM2;
            SideAreaM2 = row.SideAreaM2;
            BottomAreaM2 = row.BottomAreaM2;
            TopAreaM2 = row.TopAreaM2;
            OtherAreaM2 = row.OtherAreaM2;
            DensityKgM3 = row.DensityKgM3;
            MassKg = row.MassKg;
        }

        public string StableKey { get; }
        public string Floor { get; }
        public string Zone { get; }
        public string Category { get; }
        public string FamilyId { get; }
        public string FamilyName { get; }
        public string ElementName { get; }
        public string Material { get; }
        public string Note { get; }
        public int Count { get; }
        public double GrossConcreteM3 { get; }
        public double DeductionM3 { get; }
        public double NetConcreteM3 { get; }
        public double FormworkM2 { get; }
        public double LengthM { get; }
        public double OuterPerimeterM { get; }
        public double InnerPerimeterM { get; }
        public double DoorAreaM2 { get; }
        public double SideAreaM2 { get; }
        public double BottomAreaM2 { get; }
        public double TopAreaM2 { get; }
        public double OtherAreaM2 { get; }
        public double? DensityKgM3 { get; }
        public double? MassKg { get; }
    }

    public sealed class QuantityReportRevisionSnapshot
    {
        internal QuantityReportRevisionSnapshot(
            string projectId,
            string snapshotId,
            long sourceChangeVersion,
            RevisionSnapshot semanticRevision,
            IEnumerable<QuantityReportRevisionRowSnapshot> rows)
        {
            ProjectId = projectId ?? string.Empty;
            SnapshotId = snapshotId ?? string.Empty;
            SourceChangeVersion = sourceChangeVersion;
            SemanticRevision = semanticRevision ?? throw new ArgumentNullException(nameof(semanticRevision));
            Rows = (rows ?? throw new ArgumentNullException(nameof(rows))).ToList().AsReadOnly();
        }

        public string ProjectId { get; }
        public string SnapshotId { get; }
        public long SourceChangeVersion { get; }
        public IReadOnlyList<QuantityReportRevisionRowSnapshot> Rows { get; }
        internal RevisionSnapshot SemanticRevision { get; }
    }

    public sealed class QuantityReportRevisionChange
    {
        internal QuantityReportRevisionChange(
            QuantityReportRevisionChangeKind kind,
            string stableKey,
            QuantityReportRevisionRowSnapshot? before,
            QuantityReportRevisionRowSnapshot? after,
            IEnumerable<string> changedFields)
        {
            Kind = kind;
            StableKey = stableKey ?? string.Empty;
            Before = before;
            After = after;
            ChangedFields = (changedFields ?? throw new ArgumentNullException(nameof(changedFields))).ToList().AsReadOnly();
        }

        public QuantityReportRevisionChangeKind Kind { get; }
        public string StableKey { get; }
        public QuantityReportRevisionRowSnapshot? Before { get; }
        public QuantityReportRevisionRowSnapshot? After { get; }
        public IReadOnlyList<string> ChangedFields { get; }
    }

    public sealed class QuantityReportRevisionDiff
    {
        internal QuantityReportRevisionDiff(
            string projectId,
            string beforeSnapshotId,
            string afterSnapshotId,
            int semanticDeltaCount,
            IEnumerable<QuantityReportRevisionChange> changes)
        {
            ProjectId = projectId ?? string.Empty;
            BeforeSnapshotId = beforeSnapshotId ?? string.Empty;
            AfterSnapshotId = afterSnapshotId ?? string.Empty;
            SemanticDeltaCount = semanticDeltaCount;
            Changes = (changes ?? throw new ArgumentNullException(nameof(changes))).ToList().AsReadOnly();
        }

        public string ProjectId { get; }
        public string BeforeSnapshotId { get; }
        public string AfterSnapshotId { get; }
        public int SemanticDeltaCount { get; }
        public IReadOnlyList<QuantityReportRevisionChange> Changes { get; }
        public int AddedCount => Changes.Count(x => x.Kind == QuantityReportRevisionChangeKind.Added);
        public int RemovedCount => Changes.Count(x => x.Kind == QuantityReportRevisionChangeKind.Removed);
        public int ChangedCount => Changes.Count(x => x.Kind == QuantityReportRevisionChangeKind.Changed);
    }

    /// <summary>
    /// Builds revision review data from the existing authoritative quantity report
    /// and semantic revision engines. It never regenerates or mutates live state.
    /// </summary>
    public sealed class QuantityReportRevisionService
    {
        private const double QuantityTolerance = 1e-9;

        public QuantityReportRevisionSnapshot Capture(ProjectState project, string snapshotId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var projectId = CanonicalIdentity(project.ProjectId, "project id");
            var identity = CanonicalIdentity(snapshotId, "quantity report snapshot id");
            var sourceChangeVersion = project.ChangeVersion;
            if (sourceChangeVersion < 0) throw new InvalidOperationException("Project change version cannot be negative.");

            var revision = new RevisionService().Capture(project, identity);
            var rows = ProjectQuantityReportBuilder.Detail(project)
                .Select(ToSnapshotRow)
                .OrderBy(x => x.StableKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.StableKey, StringComparer.Ordinal)
                .ToList();

            if (project.ChangeVersion != sourceChangeVersion)
                throw new InvalidOperationException("Project changed while the quantity report revision snapshot was being captured.");

            var revisionIds = new HashSet<string>(
                revision.Elements.Select(x => CanonicalIdentity(x.ElementId, "revision element id")),
                StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
                if (!revisionIds.Contains(row.StableKey))
                    throw new InvalidOperationException("Quantity report row has no matching semantic revision element: " + row.StableKey + ".");

            var result = new QuantityReportRevisionSnapshot(projectId, identity, sourceChangeVersion, revision, rows);
            ValidateSnapshot(result, "captured");
            return result;
        }

        public QuantityReportRevisionDiff Compare(QuantityReportRevisionSnapshot before, QuantityReportRevisionSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            ValidateSnapshot(before, "before");
            ValidateSnapshot(after, "after");
            if (!string.Equals(before.ProjectId, after.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity report revision snapshots belong to different projects.");
            if (string.Equals(before.SnapshotId, after.SnapshotId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Quantity report revision snapshots must have distinct snapshot ids.");

            // RevisionService remains the semantic diff/validation authority. The
            // report comparison below only compares its authoritative BQ row view.
            var semanticDeltas = new RevisionService().Compare(before.SemanticRevision, after.SemanticRevision);
            var left = Index(before.Rows, "before");
            var right = Index(after.Rows, "after");
            var changes = new List<QuantityReportRevisionChange>();

            foreach (var key in right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase))
                changes.Add(new QuantityReportRevisionChange(QuantityReportRevisionChangeKind.Added, key, null, right[key], Array.Empty<string>()));
            foreach (var key in left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase))
                changes.Add(new QuantityReportRevisionChange(QuantityReportRevisionChangeKind.Removed, key, left[key], null, Array.Empty<string>()));
            foreach (var key in left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var fields = ChangedFields(left[key], right[key]);
                if (fields.Count != 0)
                    changes.Add(new QuantityReportRevisionChange(QuantityReportRevisionChangeKind.Changed, key, left[key], right[key], fields));
            }

            var ordered = changes
                .OrderBy(x => x.Kind)
                .ThenBy(x => x.StableKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.StableKey, StringComparer.Ordinal)
                .ToList();
            return new QuantityReportRevisionDiff(before.ProjectId, before.SnapshotId, after.SnapshotId, semanticDeltas.Count, ordered);
        }

        private static QuantityReportRevisionRowSnapshot ToSnapshotRow(QuantityReportRow row)
        {
            if (row == null) throw new InvalidOperationException("Quantity report contains a null detail row.");
            if (row.Count != 1 || row.ElementIds.Count != 1)
                throw new InvalidOperationException("Quantity report revision capture requires one semantic element per detail row.");
            var stableKey = CanonicalIdentity(row.ElementIds[0], "quantity report stable element key");
            var result = new QuantityReportRevisionRowSnapshot(stableKey, row);
            ValidateRow(result, "captured");
            return result;
        }

        private static Dictionary<string, QuantityReportRevisionRowSnapshot> Index(
            IEnumerable<QuantityReportRevisionRowSnapshot> rows,
            string label)
        {
            var result = new Dictionary<string, QuantityReportRevisionRowSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (row == null) throw new InvalidOperationException("Quantity report " + label + " snapshot contains a null row.");
                ValidateRow(row, label);
                if (result.ContainsKey(row.StableKey))
                    throw new InvalidOperationException("Quantity report " + label + " snapshot contains duplicate stable key: " + row.StableKey + ".");
                result.Add(row.StableKey, row);
            }
            return result;
        }

        private static void ValidateSnapshot(QuantityReportRevisionSnapshot snapshot, string label)
        {
            CanonicalIdentity(snapshot.ProjectId, label + " project id");
            var snapshotId = CanonicalIdentity(snapshot.SnapshotId, label + " snapshot id");
            if (snapshot.SourceChangeVersion < 0)
                throw new InvalidOperationException("Quantity report " + label + " change version cannot be negative.");
            if (!string.Equals(snapshot.SemanticRevision.Id, snapshotId, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity report " + label + " snapshot identity does not match its semantic revision identity.");
            Index(snapshot.Rows, label);
        }

        private static void ValidateRow(QuantityReportRevisionRowSnapshot row, string label)
        {
            var key = CanonicalIdentity(row.StableKey, label + " stable key");
            if (row.Count != 1) throw new InvalidOperationException("Quantity report " + label + " detail row count must be one: " + key + ".");
            Finite(row.GrossConcreteM3, key, nameof(row.GrossConcreteM3));
            Finite(row.DeductionM3, key, nameof(row.DeductionM3));
            Finite(row.NetConcreteM3, key, nameof(row.NetConcreteM3));
            Finite(row.FormworkM2, key, nameof(row.FormworkM2));
            Finite(row.LengthM, key, nameof(row.LengthM));
            Finite(row.OuterPerimeterM, key, nameof(row.OuterPerimeterM));
            Finite(row.InnerPerimeterM, key, nameof(row.InnerPerimeterM));
            Finite(row.DoorAreaM2, key, nameof(row.DoorAreaM2));
            Finite(row.SideAreaM2, key, nameof(row.SideAreaM2));
            Finite(row.BottomAreaM2, key, nameof(row.BottomAreaM2));
            Finite(row.TopAreaM2, key, nameof(row.TopAreaM2));
            Finite(row.OtherAreaM2, key, nameof(row.OtherAreaM2));
            if (row.DensityKgM3.HasValue) Finite(row.DensityKgM3.Value, key, nameof(row.DensityKgM3));
            if (row.MassKg.HasValue) Finite(row.MassKg.Value, key, nameof(row.MassKg));
        }

        private static IReadOnlyList<string> ChangedFields(
            QuantityReportRevisionRowSnapshot before,
            QuantityReportRevisionRowSnapshot after)
        {
            var fields = new List<string>();
            Add(fields, nameof(before.Floor), before.Floor, after.Floor);
            Add(fields, nameof(before.Zone), before.Zone, after.Zone);
            Add(fields, nameof(before.Category), before.Category, after.Category);
            AddIdentity(fields, nameof(before.FamilyId), before.FamilyId, after.FamilyId);
            Add(fields, nameof(before.FamilyName), before.FamilyName, after.FamilyName);
            Add(fields, nameof(before.ElementName), before.ElementName, after.ElementName);
            Add(fields, nameof(before.Material), before.Material, after.Material);
            Add(fields, nameof(before.Note), before.Note, after.Note);
            if (before.Count != after.Count) fields.Add(nameof(before.Count));
            Add(fields, nameof(before.GrossConcreteM3), before.GrossConcreteM3, after.GrossConcreteM3, before.StableKey);
            Add(fields, nameof(before.DeductionM3), before.DeductionM3, after.DeductionM3, before.StableKey);
            Add(fields, nameof(before.NetConcreteM3), before.NetConcreteM3, after.NetConcreteM3, before.StableKey);
            Add(fields, nameof(before.FormworkM2), before.FormworkM2, after.FormworkM2, before.StableKey);
            Add(fields, nameof(before.LengthM), before.LengthM, after.LengthM, before.StableKey);
            Add(fields, nameof(before.OuterPerimeterM), before.OuterPerimeterM, after.OuterPerimeterM, before.StableKey);
            Add(fields, nameof(before.InnerPerimeterM), before.InnerPerimeterM, after.InnerPerimeterM, before.StableKey);
            Add(fields, nameof(before.DoorAreaM2), before.DoorAreaM2, after.DoorAreaM2, before.StableKey);
            Add(fields, nameof(before.SideAreaM2), before.SideAreaM2, after.SideAreaM2, before.StableKey);
            Add(fields, nameof(before.BottomAreaM2), before.BottomAreaM2, after.BottomAreaM2, before.StableKey);
            Add(fields, nameof(before.TopAreaM2), before.TopAreaM2, after.TopAreaM2, before.StableKey);
            Add(fields, nameof(before.OtherAreaM2), before.OtherAreaM2, after.OtherAreaM2, before.StableKey);
            Add(fields, nameof(before.DensityKgM3), before.DensityKgM3, after.DensityKgM3, before.StableKey);
            Add(fields, nameof(before.MassKg), before.MassKg, after.MassKg, before.StableKey);
            return fields.AsReadOnly();
        }

        private static void Add(ICollection<string> fields, string name, string before, string after)
        {
            if (!string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.Ordinal)) fields.Add(name);
        }

        private static void AddIdentity(ICollection<string> fields, string name, string before, string after)
        {
            if (!string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.OrdinalIgnoreCase)) fields.Add(name);
        }

        private static void Add(ICollection<string> fields, string name, double before, double after, string key)
        {
            if (Math.Abs(RevisionMath.Subtract(after, before, key + "/" + name)) > QuantityTolerance) fields.Add(name);
        }

        private static void Add(ICollection<string> fields, string name, double? before, double? after, string key)
        {
            if (!before.HasValue || !after.HasValue)
            {
                if (before.HasValue != after.HasValue) fields.Add(name);
                return;
            }
            Add(fields, name, before.Value, after.Value, key);
        }

        private static void Finite(double value, string key, string field)
        {
            RevisionMath.Finite(value, key + "/" + field);
        }

        private static string CanonicalIdentity(string value, string label)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException(label + " is required.");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException(label + " must not contain surrounding whitespace: " + raw + ".");
            if (raw.Any(char.IsControl)) throw new InvalidOperationException(label + " contains control characters.");
            try
            {
                XmlConvert.VerifyXmlChars(raw);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException(label + " contains characters that are invalid in XML.", ex);
            }
            return raw;
        }
    }
}
