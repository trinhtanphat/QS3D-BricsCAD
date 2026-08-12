using System;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Services
{
    public sealed class ProjectSession : IDisposable
    {
        private readonly QsdbProjectStore _store;
        private ProjectFileLock? _lock;
        private bool _recoveredFromBackup;

        public ProjectSession(ProjectState project, string path, QsdbProjectStore? store = null)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Project path is required.", nameof(path)) : System.IO.Path.GetFullPath(path);
            _store = store ?? new QsdbProjectStore();
            Audit = AuditTrail.ForProject(Project);
        }

        public ProjectState Project { get; private set; }
        public string Path { get; }
        public AuditTrail Audit { get; private set; }
        public bool HasWriteLock => _lock != null;

        public void AcquireWriteLock() => _lock = _lock ?? ProjectFileLock.Acquire(Path);

        public void Save()
        {
            if (_lock == null) throw new InvalidOperationException("Acquire the project write lock before saving.");
            var snapshot = ProjectStateSnapshot.Capture(Project);
            Audit.Record("PROJECT_SAVE", string.Empty, Path);
            try
            {
                if (_recoveredFromBackup)
                    _store.SavePreservingValidatedBackup(Project, Path);
                else
                    _store.Save(Project, Path);
                _recoveredFromBackup = false;
            }
            catch (Exception saveError)
            {
                try
                {
                    snapshot.Restore(Project);
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException("Project save failed and in-memory audit rollback also failed.", saveError, rollbackError);
                }
                throw;
            }
        }

        public void Reload()
        {
            if (_lock == null) throw new InvalidOperationException("Acquire the project write lock before reloading.");
            var result = _store.LoadWithBackupFallback(Path);
            var project = result.Project;
            var audit = AuditTrail.ForProject(project);
            audit.Record("PROJECT_RELOAD", string.Empty, Path);
            Project = project;
            Audit = audit;
            _recoveredFromBackup = result.RecoveredFromBackup;
        }

        public void Dispose()
        {
            _lock?.Dispose();
            _lock = null;
        }
    }
}
