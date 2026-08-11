using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Persistence
{
    public sealed class ProjectPersistenceStamp
    {
        private const string RecoveredFromBackupKey = "QS3D.RecoveredFromBackup";
        private readonly string _projectId;
        private long _savedChangeVersion;

        public ProjectPersistenceStamp(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            _projectId = project.ProjectId;
            _savedChangeVersion = project.ChangeVersion;
        }

        public long SavedChangeVersion => _savedChangeVersion;

        public bool RequiresSave(ProjectState project)
        {
            EnsureSameProject(project);
            if (project.Metadata.TryGetValue(RecoveredFromBackupKey, out var recovered) &&
                string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            return project.ChangeVersion != _savedChangeVersion;
        }

        public void MarkSaved(ProjectState project)
        {
            EnsureSameProject(project);
            _savedChangeVersion = project.ChangeVersion;
        }

        private void EnsureSameProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal))
                throw new InvalidOperationException("A persistence stamp cannot be reused for a different QS3D project.");
        }
    }
}
