using System;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Namespace-local adapter for CAD builders that use the canonical Core project-state snapshot.
    /// It deliberately delegates all capture/restore semantics to QS3D.Core.Persistence so CAD
    /// replacement transactions do not grow a second snapshot implementation.
    /// </summary>
    internal sealed class ProjectStateSnapshot
    {
        private readonly QS3D.Core.Persistence.ProjectStateSnapshot _inner;

        private ProjectStateSnapshot(QS3D.Core.Persistence.ProjectStateSnapshot inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public static ProjectStateSnapshot Capture(ProjectState project)
        {
            return new ProjectStateSnapshot(QS3D.Core.Persistence.ProjectStateSnapshot.Capture(project));
        }

        public void Restore(ProjectState project)
        {
            _inner.Restore(project);
        }
    }
}
