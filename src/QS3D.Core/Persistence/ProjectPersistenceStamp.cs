using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectPersistenceStamp
    {
        private const string RecoveredFromBackupKey = "QS3D.RecoveredFromBackup";
        private readonly ProjectState _project;
        private long _savedChangeVersion;
        private string _savedDrawingPath;
        private string _savedDrawingFingerprint;
        private string _savedActiveZoneId;
        private string _savedActiveFloorId;
        private Dictionary<string, string> _savedMetadata;

        public ProjectPersistenceStamp(ProjectState project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _savedChangeVersion = project.ChangeVersion;
            _savedDrawingPath = project.DrawingPath;
            _savedDrawingFingerprint = project.DrawingFingerprint;
            _savedActiveZoneId = project.ActiveZoneId;
            _savedActiveFloorId = project.ActiveFloorId;
            _savedMetadata = SnapshotMetadata(project.Metadata);
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
                   !MetadataMatches(project.Metadata, _savedMetadata);
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
                snapshot[item.Key] = item.Value;
            return snapshot;
        }

        private static bool MetadataMatches(IDictionary<string, string> metadata, IReadOnlyDictionary<string, string> savedMetadata)
        {
            if (metadata.Count != savedMetadata.Count) return false;
            foreach (var item in metadata)
            {
                if (!savedMetadata.TryGetValue(item.Key, out var savedValue) ||
                    !string.Equals(item.Value, savedValue, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
