using System;
using QS3D.Core.Cost;

namespace QS3D.Core.Domain
{
    public sealed class ProjectTbqWorkspace
    {
        private readonly ProjectState _project;
        private readonly ProjectMetadataDictionary _metadata;

        private ProjectTbqWorkspace(ProjectState project, ProjectMetadataDictionary metadata)
        {
            _project = project;
            _metadata = metadata;
            _metadata.BindProject(project);
        }

        public static ProjectTbqWorkspace Open(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var metadata = project.Metadata as ProjectMetadataDictionary
                ?? throw new InvalidOperationException("TBQ project workspace requires the canonical project metadata store.");
            return new ProjectTbqWorkspace(project, metadata);
        }

        public bool HasValue => Current != null;
        public TbqProjectWorkspaceState? Current => ProjectTbqWorkspaceCodec.Read(_metadata);

        public void Replace(TbqProjectWorkspaceState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var value = ProjectTbqWorkspaceCodec.Value(state);
            if (_metadata.TryGetValue(ProjectTbqWorkspaceCodec.WorkspaceKey, out var existing) &&
                string.Equals(existing, value, StringComparison.Ordinal))
                return;

            _metadata.EnsureCanSetOwned(ProjectTbqWorkspaceCodec.WorkspaceKey);
            _project.Touch();
            _metadata.SetOwned(ProjectTbqWorkspaceCodec.WorkspaceKey, value);
        }

        public bool Clear()
        {
            if (!_metadata.ContainsKey(ProjectTbqWorkspaceCodec.WorkspaceKey))
            {
                ProjectTbqWorkspaceCodec.Read(_metadata);
                return false;
            }

            ProjectTbqWorkspaceCodec.Read(_metadata);
            _project.Touch();
            if (!_metadata.RemoveOwned(ProjectTbqWorkspaceCodec.WorkspaceKey))
                throw new InvalidOperationException("TBQ project workspace disappeared during removal.");
            return true;
        }

        public CostAdjustmentResult PreviewAdjustment(decimal adjustmentRatioPercent, decimal markupRatioPercent)
        {
            var current = RequireCurrent();
            return new CostAdjustmentService().AdjustByRatios(current.BaseTotal, adjustmentRatioPercent, markupRatioPercent);
        }

        public void ApplyAdjustment(decimal adjustmentRatioPercent, decimal markupRatioPercent)
        {
            var current = RequireCurrent();
            Replace(current.WithAdjustment(adjustmentRatioPercent, markupRatioPercent));
        }

        private TbqProjectWorkspaceState RequireCurrent()
        {
            return Current ?? throw new InvalidOperationException("The project has no TBQ workspace. Initialize it before running cost workflows.");
        }
    }
}
