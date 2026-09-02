using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectPersistenceStamp
    {
        private const int MaximumSnapshotEntries = 10_000;
        private const string RecoveredFromBackupKey = "QS3D.RecoveredFromBackup";
        private const string ProjectBrowserWorkspaceStateKey = "QS3D.ProjectBrowser.WorkspaceState";
        private readonly ProjectState _project;
        private long _savedChangeVersion;
        private int _savedSchemaVersion;
        private string _savedDrawingPath;
        private string _savedDrawingFingerprint;
        private string _savedActiveZoneId;
        private string _savedActiveFloorId;
        private Dictionary<string, string> _savedMetadata;
        private string _savedNestedPersistedContent;

        public ProjectPersistenceStamp(ProjectState project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            var snapshot = CaptureStableSnapshot(project);
            _savedChangeVersion = snapshot.Boundary.ChangeVersion;
            _savedSchemaVersion = snapshot.Boundary.SchemaVersion;
            _savedDrawingPath = snapshot.Boundary.DrawingPath;
            _savedDrawingFingerprint = snapshot.Boundary.DrawingFingerprint;
            _savedActiveZoneId = snapshot.Boundary.ActiveZoneId;
            _savedActiveFloorId = snapshot.Boundary.ActiveFloorId;
            _savedMetadata = snapshot.Metadata;
            _savedNestedPersistedContent = snapshot.NestedPersistedContent;
        }

        public long SavedChangeVersion => _savedChangeVersion;

        public bool RequiresSave(ProjectState project)
        {
            EnsureSameProject(project);
            if (project.Metadata.TryGetValue(RecoveredFromBackupKey, out var recovered) &&
                string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (project.ChangeVersion != _savedChangeVersion)
                return true;

            var snapshot = CaptureStableSnapshot(project);
            return snapshot.Boundary.ChangeVersion != _savedChangeVersion ||
                   !PersistedScalarsMatch(snapshot.Boundary) ||
                   !MetadataMatches(snapshot.Metadata, _savedMetadata) ||
                   !string.Equals(snapshot.NestedPersistedContent, _savedNestedPersistedContent, StringComparison.Ordinal);
        }

        public void MarkSaved(ProjectState project)
        {
            EnsureSameProject(project);

            var snapshot = CaptureStableSnapshot(project);

            _savedChangeVersion = snapshot.Boundary.ChangeVersion;
            _savedSchemaVersion = snapshot.Boundary.SchemaVersion;
            _savedDrawingPath = snapshot.Boundary.DrawingPath;
            _savedDrawingFingerprint = snapshot.Boundary.DrawingFingerprint;
            _savedActiveZoneId = snapshot.Boundary.ActiveZoneId;
            _savedActiveFloorId = snapshot.Boundary.ActiveFloorId;
            _savedMetadata = snapshot.Metadata;
            _savedNestedPersistedContent = snapshot.NestedPersistedContent;
        }

        private void EnsureSameProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(project, _project))
                throw new InvalidOperationException("A persistence stamp cannot be reused for a different QS3D project.");
        }

        private bool PersistedScalarsMatch(PersistenceBoundary boundary)
        {
            return boundary.SchemaVersion == _savedSchemaVersion &&
                   string.Equals(boundary.DrawingPath, _savedDrawingPath, StringComparison.Ordinal) &&
                   string.Equals(boundary.DrawingFingerprint, _savedDrawingFingerprint, StringComparison.Ordinal) &&
                   string.Equals(boundary.ActiveZoneId, _savedActiveZoneId, StringComparison.Ordinal) &&
                   string.Equals(boundary.ActiveFloorId, _savedActiveFloorId, StringComparison.Ordinal);
        }

        private static StableSnapshot CaptureStableSnapshot(ProjectState project)
        {
            var boundary = new PersistenceBoundary(project);
            var metadata = SnapshotMetadata(project.Metadata);
            var nestedPersistedContent = SnapshotNestedPersistedContent(project, boundary);

            if (!boundary.Matches(project))
                throw new InvalidOperationException(
                    "Project state changed while the persistence stamp was materializing persisted content.");

            // Nested Family/Element state can change without incrementing the parent
            // ProjectState revision. Materialize a second complete pass and require the
            // same content so a mixed-time first pass cannot be accepted as a saved state.
            var secondMetadata = SnapshotMetadata(project.Metadata);
            var secondNestedPersistedContent = SnapshotNestedPersistedContent(project, boundary);

            if (!boundary.Matches(project) ||
                !MetadataMatches(secondMetadata, metadata) ||
                !string.Equals(secondNestedPersistedContent, nestedPersistedContent, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Nested persisted project state changed while the persistence stamp was materializing content.");

            return new StableSnapshot(boundary, secondMetadata, secondNestedPersistedContent);
        }

        private static Dictionary<string, string> SnapshotMetadata(IDictionary<string, string> metadata)
        {
            var items = SnapshotBounded(metadata, metadata.Count, "project metadata");
            var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!TracksSemanticDirtyState(item.Key)) continue;
                snapshot[item.Key] = item.Value;
            }
            return snapshot;
        }

        private static bool MetadataMatches(
            IReadOnlyDictionary<string, string> metadata,
            IReadOnlyDictionary<string, string> savedMetadata)
        {
            if (metadata.Count != savedMetadata.Count)
                return false;
            foreach (var item in metadata)
            {
                if (!savedMetadata.TryGetValue(item.Key, out var savedValue) ||
                    !string.Equals(item.Value, savedValue, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static string SnapshotNestedPersistedContent(ProjectState project, PersistenceBoundary boundary)
        {
            var snapshot = new StringBuilder();
            AppendString(snapshot, boundary.Name);
            AppendDateTime(snapshot, boundary.UpdatedUtc);

            var zones = SnapshotBounded(project.Zones, project.Zones.Count, "project zones");
            AppendSequenceCount(snapshot, zones.Count);
            foreach (var zone in zones)
            {
                AppendString(snapshot, zone?.Id);
                AppendString(snapshot, zone?.Name);
            }

            var floors = SnapshotBounded(project.Floors, project.Floors.Count, "project floors");
            AppendSequenceCount(snapshot, floors.Count);
            foreach (var floor in floors)
            {
                AppendString(snapshot, floor?.Id);
                AppendString(snapshot, floor?.Name);
                AppendDouble(snapshot, floor?.ElevationM ?? double.NaN);
            }

            var families = SnapshotBounded(project.Families, project.Families.Count, "project families");
            AppendSequenceCount(snapshot, families.Count);
            foreach (var family in families)
            {
                AppendString(snapshot, family?.Id);
                AppendString(snapshot, family?.Name);
                AppendInt32(snapshot, family == null ? int.MinValue : (int)family.Category);
                AppendStringMap(snapshot, family?.Properties, "family properties");
            }

            var ruleSnapshot = SnapshotBounded(project.QuantityRules, project.QuantityRules.Count, "project quantity rules");
            var rules = ruleSnapshot
                .OrderBy(x => x?.Id ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            AppendSequenceCount(snapshot, rules.Length);
            foreach (var rule in rules)
            {
                AppendString(snapshot, rule?.Id);
                AppendInt32(snapshot, rule == null ? int.MinValue : (int)rule.Category);
                AppendString(snapshot, rule?.OutputName);
                AppendString(snapshot, rule?.Expression);
                AppendString(snapshot, rule?.Version);
            }

            var elements = SnapshotBounded(project.Elements, project.Elements.Count, "project elements");
            AppendSequenceCount(snapshot, elements.Count);
            foreach (var element in elements)
            {
                AppendString(snapshot, element?.Id);
                AppendInt32(snapshot, element == null ? int.MinValue : (int)element.Category);
                AppendString(snapshot, element?.FamilyId);
                AppendString(snapshot, element?.FloorId);
                AppendString(snapshot, element?.ZoneId);
                AppendString(snapshot, element?.DrawingFingerprint);
                AppendInt32(snapshot, element == null ? int.MinValue : (int)element.Dirty);
                AppendDateTime(snapshot, element?.UpdatedUtc);
                AppendStringSequence(snapshot, element?.SourceHandles, "element source handles");
                AppendStringSequence(snapshot, element?.DependsOn, "element dependencies");
                AppendStringMap(snapshot, element?.Properties, "element properties");
                AppendDoubleMap(snapshot, element?.Quantities, "element quantities");
            }

            var auditSnapshot = SnapshotBounded(project.AuditEvents, project.AuditEvents.Count, "project audit events");
            var auditEvents = auditSnapshot
                .Select((item, index) => new { Item = item, Index = index })
                .OrderBy(x => x.Item?.Utc ?? DateTime.MinValue)
                .ThenBy(x => x.Index)
                .Select(x => x.Item)
                .ToArray();
            AppendSequenceCount(snapshot, auditEvents.Length);
            foreach (var audit in auditEvents)
            {
                AppendDateTime(snapshot, audit?.Utc);
                AppendString(snapshot, audit?.Action);
                AppendString(snapshot, audit?.ElementId);
                AppendString(snapshot, audit?.Detail);
                AppendString(snapshot, audit?.Actor);
                AppendString(snapshot, audit?.CorrelationId);
            }

            return snapshot.ToString();
        }

        private static void AppendStringMap(
            StringBuilder snapshot,
            IDictionary<string, string>? values,
            string collectionLabel)
        {
            if (values == null)
            {
                AppendSequenceCount(snapshot, -1);
                return;
            }

            var bounded = SnapshotBounded(values, values.Count, collectionLabel);
            var ordered = bounded.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            AppendSequenceCount(snapshot, ordered.Length);
            foreach (var item in ordered)
            {
                AppendString(snapshot, item.Key);
                AppendString(snapshot, item.Value ?? string.Empty);
            }
        }

        private static void AppendDoubleMap(
            StringBuilder snapshot,
            IDictionary<string, double>? values,
            string collectionLabel)
        {
            if (values == null)
            {
                AppendSequenceCount(snapshot, -1);
                return;
            }

            var bounded = SnapshotBounded(values, values.Count, collectionLabel);
            var ordered = bounded.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            AppendSequenceCount(snapshot, ordered.Length);
            foreach (var item in ordered)
            {
                AppendString(snapshot, item.Key);
                AppendDouble(snapshot, item.Value);
            }
        }

        private static void AppendStringSequence(
            StringBuilder snapshot,
            IList<string>? values,
            string collectionLabel)
        {
            if (values == null)
            {
                AppendSequenceCount(snapshot, -1);
                return;
            }

            var bounded = SnapshotBounded(values, values.Count, collectionLabel);
            AppendSequenceCount(snapshot, bounded.Count);
            foreach (var value in bounded) AppendString(snapshot, value);
        }

        private static List<T> SnapshotBounded<T>(IEnumerable<T> values, int knownCount, string collectionLabel)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            RequireSupportedCount(knownCount, collectionLabel);
            RequireStableCountEvidence(values, knownCount, collectionLabel, "before traversal");

            var bounded = new List<T>(knownCount);
            using (var enumerator = values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (bounded.Count == MaximumSnapshotEntries)
                        ThrowTooManyEntries(collectionLabel);
                    if (bounded.Count >= knownCount)
                        ThrowKnownCountMismatch(collectionLabel);
                    var value = enumerator.Current;
                    bounded.Add(value);
                }
            }

            if (bounded.Count != knownCount)
                ThrowKnownCountMismatch(collectionLabel);

            RequireStableCountEvidence(values, knownCount, collectionLabel, "after traversal");
            return bounded;
        }

        private static void RequireStableCountEvidence<T>(
            IEnumerable<T> values,
            int knownCount,
            string collectionLabel,
            string phase)
        {
            int? observed = null;
            if (values is ICollection<T> genericCollection)
                MergeCountEvidence(genericCollection.Count, knownCount, collectionLabel, phase, ref observed);
            if (values is IReadOnlyCollection<T> readOnlyCollection)
                MergeCountEvidence(readOnlyCollection.Count, knownCount, collectionLabel, phase, ref observed);
            if (values is System.Collections.ICollection nonGenericCollection)
                MergeCountEvidence(nonGenericCollection.Count, knownCount, collectionLabel, phase, ref observed);
        }

        private static void MergeCountEvidence(
            int candidate,
            int knownCount,
            string collectionLabel,
            string phase,
            ref int? observed)
        {
            RequireSupportedCount(candidate, collectionLabel);
            if (candidate != knownCount)
            {
                if (string.Equals(phase, "before traversal", StringComparison.Ordinal))
                    ThrowKnownCountMismatch(collectionLabel);
                throw new InvalidOperationException(
                    "Persistence stamp " + collectionLabel + " count changed or conflicted " + phase + ".");
            }
            if (observed.HasValue && observed.Value != candidate)
                throw new InvalidOperationException(
                    "Persistence stamp " + collectionLabel + " exposes conflicting count evidence " + phase + ".");
            observed = candidate;
        }

        private static void ThrowKnownCountMismatch(string collectionLabel)
        {
            throw new InvalidOperationException(
                "Persistence stamp " + collectionLabel + " known count does not match enumerated entry count.");
        }

        private static void RequireSupportedCount(int count, string collectionLabel)
        {
            if (count < 0)
                throw new InvalidOperationException(
                    "Persistence stamp " + collectionLabel + " reports an invalid negative count.");
            if (count > MaximumSnapshotEntries)
                ThrowTooManyEntries(collectionLabel);
        }

        private static void ThrowTooManyEntries(string collectionLabel)
        {
            throw new InvalidOperationException(
                "Persistence stamp " + collectionLabel + " supports at most " +
                MaximumSnapshotEntries.ToString(CultureInfo.InvariantCulture) + " entries.");
        }

        private static void AppendSequenceCount(StringBuilder snapshot, int value)
        {
            snapshot.Append('C').Append(value.ToString(CultureInfo.InvariantCulture)).Append(';');
        }

        private static void AppendInt32(StringBuilder snapshot, int value)
        {
            snapshot.Append('I').Append(value.ToString(CultureInfo.InvariantCulture)).Append(';');
        }

        private static void AppendDouble(StringBuilder snapshot, double value)
        {
            snapshot.Append('D').Append(value.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        }

        private static void AppendDateTime(StringBuilder snapshot, DateTime? value)
        {
            AppendString(snapshot, value.HasValue
                ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                : null);
        }

        private static void AppendString(StringBuilder snapshot, string? value)
        {
            if (value == null)
            {
                snapshot.Append("S-1:");
                return;
            }

            snapshot.Append('S')
                .Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(value);
        }

        private static bool TracksSemanticDirtyState(string key)
        {
            return !string.Equals(key, ProjectBrowserWorkspaceStateKey, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class StableSnapshot
        {
            public StableSnapshot(
                PersistenceBoundary boundary,
                Dictionary<string, string> metadata,
                string nestedPersistedContent)
            {
                Boundary = boundary;
                Metadata = metadata;
                NestedPersistedContent = nestedPersistedContent;
            }

            public PersistenceBoundary Boundary { get; }
            public Dictionary<string, string> Metadata { get; }
            public string NestedPersistedContent { get; }
        }

        private sealed class PersistenceBoundary
        {
            public PersistenceBoundary(ProjectState project)
            {
                ChangeVersion = project.ChangeVersion;
                SchemaVersion = project.SchemaVersion;
                Name = project.Name;
                DrawingPath = project.DrawingPath;
                DrawingFingerprint = project.DrawingFingerprint;
                ActiveZoneId = project.ActiveZoneId;
                ActiveFloorId = project.ActiveFloorId;
                UpdatedUtc = project.UpdatedUtc;
            }

            public long ChangeVersion { get; }
            public int SchemaVersion { get; }
            public string Name { get; }
            public string DrawingPath { get; }
            public string DrawingFingerprint { get; }
            public string ActiveZoneId { get; }
            public string ActiveFloorId { get; }
            public DateTime UpdatedUtc { get; }

            public bool Matches(ProjectState project)
            {
                return project.ChangeVersion == ChangeVersion &&
                       project.SchemaVersion == SchemaVersion &&
                       string.Equals(project.Name, Name, StringComparison.Ordinal) &&
                       string.Equals(project.DrawingPath, DrawingPath, StringComparison.Ordinal) &&
                       string.Equals(project.DrawingFingerprint, DrawingFingerprint, StringComparison.Ordinal) &&
                       string.Equals(project.ActiveZoneId, ActiveZoneId, StringComparison.Ordinal) &&
                       string.Equals(project.ActiveFloorId, ActiveFloorId, StringComparison.Ordinal) &&
                       project.UpdatedUtc == UpdatedUtc;
            }
        }
    }
}
