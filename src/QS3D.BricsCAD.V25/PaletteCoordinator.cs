using System;
using System.Collections.Generic;
using System.Drawing;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Model;

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

        public static void EnsureCreated()
        {
            if (_workspace != null && _right != null) return;
            _workspacePanel = new WorkspacePanel();
            _rightPanel = new RightPanel();
            _workspace = new PaletteSet("QS3D — Mô hình", WorkspaceGuid)
            {
                DockEnabled = DockSides.Left | DockSides.Right,
                Dock = DockSides.Left,
                Visible = false,
                KeepFocus = false,
                MinimumSize = new Size(460, 420)
            };
            _workspace.Size = new Size(540, 720);
            _workspace.AddVisual("Mô hình", _workspacePanel, true);

            _right = new PaletteSet("QS3D — Bản vẽ & Lớp", RightGuid)
            {
                DockEnabled = DockSides.Left | DockSides.Right,
                Dock = DockSides.Right,
                Visible = false,
                KeepFocus = false,
                MinimumSize = new Size(255, 420)
            };
            _right.Size = new Size(300, 720);
            _right.AddVisual("Quản lý", _rightPanel, true);
        }

        public static void Show()
        {
            EnsureCreated();
            if (_workspace != null) _workspace.Visible = true;
            if (_right != null) _right.Visible = true;
            RefreshAll();
        }

        public static void Hide()
        {
            if (_workspace != null) _workspace.Visible = false;
            if (_right != null) _right.Visible = false;
        }

        public static void ShowSafeMode()
        {
            EnsureCreated();
            if (_workspace != null) _workspace.Visible = true;
            if (_right != null) _right.Visible = false;
            _workspacePanel?.SetStatus("Safe Mode: panel bản vẽ/layer đang tắt.");
        }

        public static void SetInspection(IReadOnlyList<EntitySnapshot> snapshots) { EnsureCreated(); _workspacePanel?.SetInspection(snapshots); }
        public static void SetStatus(string status) { EnsureCreated(); _workspacePanel?.SetStatus(status); }
        public static void RefreshProject() { EnsureCreated(); _workspacePanel?.RefreshProject(); }
        public static void RefreshCad() { EnsureCreated(); _rightPanel?.Refresh(); }
        public static void RefreshAll() { RefreshProject(); RefreshCad(); }

        public static void Dispose()
        {
            if (_workspace != null) { _workspace.Dispose(); _workspace = null; }
            if (_right != null) { _right.Dispose(); _right = null; }
            _workspacePanel = null;
            _rightPanel = null;
        }
    }
}
