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
            _savedChangeVersion = project.ChangeVersion;
            _savedDrawingPath = project.DrawingPath;
            _savedDrawingFingerprint = project.DrawingFingerprint;
            _savedActiveZoneId = project.ActiveZoneId;
            _savedActiveFloorId = project.ActiveFloorId;
            _savedMetadata = SnapshotMetadata(project.Metadata);
            _savedNestedPersistedContent = SnapshotNestedPersistedContent(project);
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
            var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in metadata)
            {
                if (!TracksSemanticDirtyState(item.Key)) continue;
                snapshot[item.Key] = item.Value;
            }
            return snapshot;
        }

        private static bool MetadataMatches(IDictionary<string, string> metadata, IReadOnlyDictionary<string, string> savedMetadata)
        {
            var trackedCount = 0;
            foreach (var item in metadata)
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

            AppendSequenceCount(snapshot, project.Zones.Count);
            foreach (var zone in project.Zones)
            {
                AppendString(snapshot, zone?.Id);
                AppendString(snapshot, zone?.Name);
            }

            AppendSequenceCount(snapshot, project.Floors.Count);
            foreach (var floor in project.Floors)
            {
                AppendString(snapshot, floor?.Id);
                AppendString(snapshot, floor?.Name);
                AppendDouble(snapshot, floor?.ElevationM ?? double.NaN);
            }

            AppendSequenceCount(snapshot, project.Families.Count);
            foreach (var family in project.Families)
            {
                AppendString(snapshot, family?.Id);
                AppendString(snapshot, family?.Name);
                AppendInt32(snapshot, family == null ? int.MinValue : (int)family.Category);
                AppendStringMap(snapshot, family?.Properties);
            }

            var rules = project.QuantityRules
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

            AppendSequenceCount(snapshot, project.Elements.Count);
            foreach (var element in project.Elements)
            {
                AppendString(snapshot, element?.Id);
                AppendInt32(snapshot, element == null ? int.MinValue : (int)element.Category);
                AppendString(snapshot, element?.FamilyId);
                AppendString(snapshot, element?.FloorId);
                AppendString(snapshot, element?.ZoneId);
                AppendString(snapshot, element?.DrawingFingerprint);
                AppendInt32(snapshot, element == null ? int.MinValue : (int)element.Dirty);
                AppendDateTime(snapshot, element?.UpdatedUtc);
                AppendStringSequence(snapshot, element?.SourceHandles);
                AppendStringSequence(snapshot, element?.DependsOn);
                AppendStringMap(snapshot, element?.Properties);
                AppendDoubleMap(snapshot, element?.Quantities);
            }

            var auditEvents = project.AuditEvents
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

        private static void AppendStringMap(StringBuilder snapshot, IDictionary<string, string>? values)
        {
            if (values == null)
            {
                AppendSequenceCount(snapshot, -1);
                return;
            }

            var ordered = values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            AppendSequenceCount(snapshot, ordered.Length);
            foreach (var item in ordered)
            {
                AppendString(snapshot, item.Key);
                AppendString(snapshot, item.Value ?? string.Empty);
            }
        }

        private static void AppendDoubleMap(StringBuilder snapshot, IDictionary<string, double>? values)
        {
            if (values == null)
            {
                AppendSequenceCount(snapshot, -1);
                return;
            }

            var ordered = values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            AppendSequenceCount(snapshot, ordered.Length);
            foreach (var item in ordered)
            {
                AppendString(snapshot, item.Key);
                AppendDouble(snapshot, item.Value);
            }
        }

        private static void AppendStringSequence(StringBuilder snapshot, IList<string>? values)
        {
            if (values == null)
            {
                AppendSequenceCount(snapshot, -1);
                return;
            }

            AppendSequenceCount(snapshot, values.Count);
            foreach (var value in values) AppendString(snapshot, value);
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
