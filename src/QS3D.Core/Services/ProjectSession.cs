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

        public ProjectSession(ProjectState project, string path, QsdbProjectStore? store = null)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Project path is required.", nameof(path)) : System.IO.Path.GetFullPath(path);
            _store = store ?? new QsdbProjectStore();
            Audit = new AuditTrail();
        }

        public ProjectState Project { get; private set; }
        public string Path { get; }
        public AuditTrail Audit { get; }
        public bool HasWriteLock => _lock != null;

        public void AcquireWriteLock() => _lock = _lock ?? ProjectFileLock.Acquire(Path);

        public void Save()
        {
            if (_lock == null) throw new InvalidOperationException("Acquire the project write lock before saving.");
            _store.Save(Project, Path);
            Audit.Record("PROJECT_SAVE", string.Empty, Path);
        }

        public void Reload()
        {
            if (_lock == null) throw new InvalidOperationException("Acquire the project write lock before reloading.");
            Project = _store.Load(Path);
            Audit.Record("PROJECT_RELOAD", string.Empty, Path);
        }

        public void Dispose()
        {
            _lock?.Dispose();
            _lock = null;
        }
    }
}
