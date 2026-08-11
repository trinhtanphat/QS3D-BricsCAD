using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class DirectDrawProjectPreviewContext
    {
        private DirectDrawProjectPreviewContext(ProjectState? defaultsProject, string expectedProjectId)
        {
            DefaultsProject = defaultsProject;
            ExpectedProjectId = expectedProjectId ?? string.Empty;
        }

        public ProjectState? DefaultsProject { get; }
        public bool HasProject => DefaultsProject != null;
        public string ExpectedProjectId { get; }

        public static DirectDrawProjectPreviewContext Capture(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                return new DirectDrawProjectPreviewContext(null, string.Empty);
            if (project == null || string.IsNullOrWhiteSpace(project.ProjectId))
                throw new InvalidOperationException("Direct Draw preview resolved an invalid QS3D project identity.");
            return new DirectDrawProjectPreviewContext(project, project.ProjectId.Trim());
        }

        public ProjectState ResolveForMutation(Document document, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation is required.", nameof(operation));

            if (HasProject)
            {
                var project = ExistingProjectMutationContext.Require(document, operation);
                if (!string.Equals(project.ProjectId, ExpectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "QS3D project đã thay đổi trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để dùng đúng project/Family defaults.");
                return project;
            }

            if (ProjectContextCoordinator.TryGetReadOnly(document, out _))
                throw new InvalidOperationException(
                    "QS3D project đã xuất hiện trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để dùng đúng project/Family defaults.");

            return ProjectContextCoordinator.GetOrCreate(document);
        }
    }
}
