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
        private string _savedDrawingPath;
        private string _savedDrawingFingerprint;
        private string _savedActiveZoneId;
        private string _savedActiveFloorId;
        private Dictionary<string, string> _savedMetadata;
        private string _savedNestedPersistedContent;

        public ProjectPersistenceStamp(ProjectState project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _savedChangeVersion = project.ChangeVersion;
            _savedDrawingPath = project.DrawingPath;
            _savedDrawingFingerprint = project.DrawingFingerprint;
            _savedActiveZoneId = project.ActiveZoneId;
            _savedActiveFloorId = project.ActiveFloorId;
            _savedMetadata = SnapshotMetadata(project.Metadata);
            _savedNestedPersistedContent = SnapshotNestedPersistedContent(project);
        }

        public long SavedChangeVersion => _savedChangeVersion;

        public bool RequiresSave(ProjectState project)
        {
            EnsureSameProject(project);
            if (project.Metadata.TryGetValue(RecoveredFromBackupKey, out var recovered) &&
                string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            return project.ChangeVersion != _savedChangeVersion ||
                   !PersistedScalarsMatch(project) ||
                   !MetadataMatches(project.Metadata, _savedMetadata) ||
                   !string.Equals(SnapshotNestedPersistedContent(project), _savedNestedPersistedContent, StringComparison.Ordinal);
        }

        public void MarkSaved(ProjectState project)
        {
            EnsureSameProject(project);

            var savedChangeVersion = project.ChangeVersion;
            var savedDrawingPath = project.DrawingPath;
            var savedDrawingFingerprint = project.DrawingFingerprint;
            var savedActiveZoneId = project.ActiveZoneId;
            var savedActiveFloorId = project.ActiveFloorId;
            var savedMetadata = SnapshotMetadata(project.Metadata);
            var savedNestedPersistedContent = SnapshotNestedPersistedContent(project);

            _savedChangeVersion = savedChangeVersion;
            _savedDrawingPath = savedDrawingPath;
            _savedDrawingFingerprint = savedDrawingFingerprint;
            _savedActiveZoneId = savedActiveZoneId;
            _savedActiveFloorId = savedActiveFloorId;
            _savedMetadata = savedMetadata;
            _savedNestedPersistedContent = savedNestedPersistedContent;
        }

        private void EnsureSameProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(project, _project))
                throw new InvalidOperationException("A persistence stamp cannot be reused for a different QS3D project.");
        }

        private bool PersistedScalarsMatch(ProjectState project)
        {
            return string.Equals(project.DrawingPath, _savedDrawingPath, StringComparison.Ordinal) &&
                   string.Equals(project.DrawingFingerprint, _savedDrawingFingerprint, StringComparison.Ordinal) &&
                   string.Equals(project.ActiveZoneId, _savedActiveZoneId, StringComparison.Ordinal) &&
                   string.Equals(project.ActiveFloorId, _savedActiveFloorId, StringComparison.Ordinal);
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

        private static bool MetadataMatches(IDictionary<string, string> metadata, IReadOnlyDictionary<string, string> savedMetadata)
        {
            var items = SnapshotBounded(metadata, metadata.Count, "project metadata");
            var trackedCount = 0;
            foreach (var item in items)
            {
                if (!TracksSemanticDirtyState(item.Key)) continue;
                trackedCount++;
                if (!savedMetadata.TryGetValue(item.Key, out var savedValue) ||
                    !string.Equals(item.Value, savedValue, StringComparison.Ordinal))
                    return false;
            }
            return trackedCount == savedMetadata.Count;
        }

        private static string SnapshotNestedPersistedContent(ProjectState project)
        {
            var snapshot = new StringBuilder();
            AppendString(snapshot, project.Name);
            AppendDateTime(snapshot, project.UpdatedUtc);

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
            RequireSupportedCount(knownCount, collectionLabel);
            var bounded = new List<T>(knownCount);
            foreach (var value in values)
            {
                if (bounded.Count == MaximumSnapshotEntries)
                    ThrowTooManyEntries(collectionLabel);
                bounded.Add(value);
            }
            if (bounded.Count != knownCount)
                throw new InvalidOperationException(
                    "Persistence stamp " + collectionLabel + " known count does not match enumerated entry count.");
            return bounded;
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
    }
}
