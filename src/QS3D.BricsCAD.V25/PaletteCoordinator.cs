using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    internal static class PaletteCoordinator
    {
        private static readonly Guid WorkspaceGuid = new Guid("B6D934DE-67ED-4F90-A7CF-A4DC0C4CDDF1");
        private static readonly Guid RightGuid = new Guid("AC615D29-590A-457C-8579-6BF4ACEC5C29");
        private static PaletteSet? _workspace;
        private static PaletteSet? _right;
        private static WorkspacePanel? _workspacePanel;
        private static RightPanel? _rightPanel;

        public static bool IsWorkspaceVisible => _workspace != null && _workspace.Visible;
        public static bool IsRightPanelVisible => _right != null && _right.Visible;

        public static void EnsureCreated()
        {
            if (_workspace != null && _right != null) return;
            var layout = UserUiLayoutStore.Get();
            _workspacePanel = new WorkspacePanel();
            _rightPanel = new RightPanel();
            _workspace = new PaletteSet("QS3D — Mô hình", WorkspaceGuid)
            {
                DockEnabled = DockSides.Left | DockSides.Right,
                Dock = DockSides.Left,
                Visible = false,
                KeepFocus = false,
                MinimumSize = new DrawingSize(460, 420)
            };
            _workspace.DeviceIndependentSize = new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight);
            _workspace.AddVisual("Mô hình", _workspacePanel, true);

            _right = new PaletteSet("QS3D — Bản vẽ & Lớp", RightGuid)
            {
                DockEnabled = DockSides.Left | DockSides.Right,
                Dock = DockSides.Right,
                Visible = false,
                KeepFocus = false,
                MinimumSize = new DrawingSize(255, 420)
            };
            _right.DeviceIndependentSize = new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight);
            _right.AddVisual("Quản lý", _rightPanel, true);
        }

        public static void Show()
        {
            EnsureCreated();
            if (_workspace != null) _workspace.Visible = true;
            if (_right != null) _right.Visible = true;
            RefreshAll();
            SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
        }

        public static void Hide()
        {
            PersistPaletteLayout();
            if (_workspace != null) _workspace.Visible = false;
            if (_right != null) _right.Visible = false;
        }

        public static void ShowSafeMode()
        {
            EnsureCreated();
            if (_workspace != null) _workspace.Visible = true;
            if (_right != null) _right.Visible = false;
            _workspacePanel?.SetStatus("Safe Mode: panel bản vẽ/layer đang tắt.");
            SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
        }

        public static void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)
        {
            EnsureCreated();
            ProjectState? project = null;
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document != null && ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                project = currentProject;
            _workspacePanel?.SetInspectionReadOnly(snapshots, project);
        }

        public static void SetStatus(string status) { EnsureCreated(); _workspacePanel?.SetStatus(status); }
        public static void RefreshProject() { EnsureCreated(); _workspacePanel?.RefreshProject(); }
        public static void RefreshCad() { EnsureCreated(); _rightPanel?.Refresh(); }
        public static void RefreshAll() { RefreshProject(); RefreshCad(); }

        public static void ResetForNoDocument()
        {
            ResetPreservingVisibility();
        }

        public static void ResetForUnavailableProject(string status)
        {
            EnsureCreated();
            _workspacePanel?.ClearProject(status);
            try { _rightPanel?.Refresh(); }
            catch { }
        }

        private static void ResetPreservingVisibility()
        {
            if (_workspace == null && _right == null) return;
            var workspaceVisible = IsWorkspaceVisible;
            var rightVisible = IsRightPanelVisible;
            Dispose();
            EnsureCreated();
            if (_workspace != null) _workspace.Visible = workspaceVisible;
            if (_right != null) _right.Visible = rightVisible;
        }

        public static void Dispose()
        {
            PersistPaletteLayout();
            if (_workspace != null) { _workspace.Dispose(); _workspace = null; }
            if (_right != null) { _right.Dispose(); _right = null; }
            _workspacePanel = null;
            _rightPanel = null;
        }

        private static void PersistPaletteLayout()
        {
            if (_workspace == null && _right == null) return;
            try
            {
                var workspaceSize = _workspace?.DeviceIndependentSize;
                var rightSize = _right?.DeviceIndependentSize;
                UserUiLayoutStore.Update(layout =>
                {
                    if (workspaceSize.HasValue)
                    {
                        layout.WorkspacePaletteWidth = checked((int)Math.Round(workspaceSize.Value.Width, MidpointRounding.AwayFromZero));
                        layout.WorkspacePaletteHeight = checked((int)Math.Round(workspaceSize.Value.Height, MidpointRounding.AwayFromZero));
                    }
                    if (rightSize.HasValue)
                    {
                        layout.RightPaletteWidth = checked((int)Math.Round(rightSize.Value.Width, MidpointRounding.AwayFromZero));
                        layout.RightPaletteHeight = checked((int)Math.Round(rightSize.Value.Height, MidpointRounding.AwayFromZero));
                    }
                });
            }
            catch
            {
                // UI preference persistence is best-effort and must never block palette teardown.
            }
        }
    }
}
