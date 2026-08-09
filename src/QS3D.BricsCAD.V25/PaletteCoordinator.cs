using System;
using System.Collections.Generic;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Model;

namespace QS3D.BricsCAD.V25
{
    internal static class PaletteCoordinator
    {
        private static readonly Guid WorkspaceGuid = new Guid("B6D934DE-67ED-4F90-A7CF-A4DC0C4CDDF1");
        private static readonly Guid RightGuid = new Guid("AC615D29-590A-457C-8579-6BF4ACEC5C29");
        private static PaletteSet? _workspace; private static PaletteSet? _right; private static WorkspacePanel? _workspacePanel;
        public static void EnsureCreated()
        {
            if (_workspace != null && _right != null) return;
            _workspacePanel = new WorkspacePanel(); var rightPanel = new RightPanel();
            _workspace = new PaletteSet("QS3D — Mô hình", WorkspaceGuid) { DockEnabled = DockSides.Left | DockSides.Right, Dock = DockSides.Left, Visible = false };
            _workspace.AddVisual("Mô hình", _workspacePanel, true);
            _right = new PaletteSet("QS3D — Bản vẽ & Lớp", RightGuid) { DockEnabled = DockSides.Left | DockSides.Right, Dock = DockSides.Right, Visible = false };
            _right.AddVisual("Quản lý", rightPanel, true);
        }
        public static void Show() { EnsureCreated(); if (_workspace != null) _workspace.Visible = true; if (_right != null) _right.Visible = true; }
        public static void Hide() { if (_workspace != null) _workspace.Visible = false; if (_right != null) _right.Visible = false; }
        public static void SetInspection(IReadOnlyList<EntitySnapshot> snapshots) { EnsureCreated(); _workspacePanel?.SetInspection(snapshots); }
        public static void Dispose() { if (_workspace != null) { _workspace.Dispose(); _workspace = null; } if (_right != null) { _right.Dispose(); _right = null; } _workspacePanel = null; }
    }
}
