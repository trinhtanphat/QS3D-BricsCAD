using System;
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Units;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class DirectDrawProjectPreviewContext
    {
        [ThreadStatic]
        private static DispatchPreviewScope? _dispatchScope;

        private DirectDrawProjectPreviewContext(
            ProjectState? defaultsProject,
            string expectedProjectId,
            long? expectedChangeVersion,
            LengthUnit expectedLengthUnit,
            Matrix3d expectedUcs)
        {
            DefaultsProject = defaultsProject;
            ExpectedProjectId = expectedProjectId ?? string.Empty;
            ExpectedChangeVersion = expectedChangeVersion;
            ExpectedLengthUnit = expectedLengthUnit;
            ExpectedUcs = expectedUcs;
        }

        public ProjectState? DefaultsProject { get; }
        public bool HasProject => DefaultsProject != null;
        public string ExpectedProjectId { get; }
        public long? ExpectedChangeVersion { get; }
        public LengthUnit ExpectedLengthUnit { get; }
        public Matrix3d ExpectedUcs { get; }

        public static IDisposable BeginDispatchScope(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var scope = new DispatchPreviewScope(document, CaptureCurrent(document), _dispatchScope);
            _dispatchScope = scope;
            return scope;
        }

        public static DirectDrawProjectPreviewContext Capture(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var scope = _dispatchScope;
            if (scope != null && ReferenceEquals(scope.Document, document))
                return scope.Preview;
            return CaptureCurrent(document);
        }

        private static DirectDrawProjectPreviewContext CaptureCurrent(Document document)
        {
            var expectedLengthUnit = CadUnitService.GetLengthUnit(document);
            var expectedUcs = document.Editor.CurrentUserCoordinateSystem;
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                return new DirectDrawProjectPreviewContext(null, string.Empty, null, expectedLengthUnit, expectedUcs);
            if (project == null || string.IsNullOrWhiteSpace(project.ProjectId))
                throw new InvalidOperationException("Direct Draw preview resolved an invalid QS3D project identity.");
            return new DirectDrawProjectPreviewContext(project, project.ProjectId.Trim(), project.ChangeVersion, expectedLengthUnit, expectedUcs);
        }

        public ProjectState ResolveForMutation(Document document, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation is required.", nameof(operation));

            EnsureCadContextFresh(document);

            if (HasProject)
            {
                var project = ExistingProjectMutationContext.Require(document, operation);
                if (!string.Equals(project.ProjectId, ExpectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "QS3D project đã thay đổi trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để dùng đúng project/Family defaults.");
                if (!ExpectedChangeVersion.HasValue || project.ChangeVersion != ExpectedChangeVersion.Value)
                    throw new InvalidOperationException(
                        "QS3D project đã được chỉnh sửa trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để dùng đúng project/Family defaults.");
                return project;
            }

            if (ProjectContextCoordinator.TryGetReadOnly(document, out _) || HasBackingStore(document))
                throw ProjectAppeared();

            var created = ProjectContextCoordinator.GetOrCreate(document);
            if (HasBackingStore(document))
            {
                // A sidecar can appear in the narrow gap between the read-only absence check
                // and GetOrCreate. Do not keep a speculative canonical bind in that case.
                ProjectContextCoordinator.Forget(document);
                throw ProjectAppeared();
            }
            return created;
        }

        private void EnsureCadContextFresh(Document document)
        {
            if (CadUnitService.GetLengthUnit(document) != ExpectedLengthUnit)
                throw new InvalidOperationException(
                    "Drawing unit policy đã thay đổi trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để tính lại kích thước.");
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(ExpectedUcs))
                throw new InvalidOperationException(
                    "Current UCS đã thay đổi trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để dùng đúng hệ tọa độ.");
        }

        private static bool HasBackingStore(Document document)
        {
            var path = ProjectContextCoordinator.GetProjectPath(document);
            return File.Exists(path) || File.Exists(path + ".bak");
        }

        private static InvalidOperationException ProjectAppeared() =>
            new InvalidOperationException(
                "QS3D project đã xuất hiện trong lúc xác nhận Direct Draw. Hãy chạy lại lệnh để dùng đúng project/Family defaults.");

        private sealed class DispatchPreviewScope : IDisposable
        {
            private readonly DispatchPreviewScope? _previous;
            private bool _disposed;

            public DispatchPreviewScope(
                Document document,
                DirectDrawProjectPreviewContext preview,
                DispatchPreviewScope? previous)
            {
                Document = document ?? throw new ArgumentNullException(nameof(document));
                Preview = preview ?? throw new ArgumentNullException(nameof(preview));
                _previous = previous;
            }

            public Document Document { get; }
            public DirectDrawProjectPreviewContext Preview { get; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (ReferenceEquals(_dispatchScope, this))
                    _dispatchScope = _previous;
            }
        }
    }
}
