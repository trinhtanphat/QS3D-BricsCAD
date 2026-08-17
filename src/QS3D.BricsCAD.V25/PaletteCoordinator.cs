using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using Bricscad.ApplicationServices;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using Application = Bricscad.ApplicationServices.Application;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    internal static class PaletteCoordinator
    {
        private static readonly Guid WorkspaceGuid = new Guid("B6D934DE-67ED-4F90-A7CF-A4DC0C4CDDF1");
        private static readonly Guid PropertiesGuid = new Guid("5D4A6E10-6B19-4FD4-B2A8-A8F090E87C01");
        private static readonly Guid RightGuid = new Guid("AC615D29-590A-457C-8579-6BF4ACEC5C29");
        private static readonly Guid QuantityInsightGuid = new Guid("7EA0345F-1F62-4BD4-9ED0-3B25EB76A91B");
        private static PaletteSet? _workspace;
        private static PaletteSet? _properties;
        private static PaletteSet? _right;
        private static PaletteSet? _quantityInsight;
        private static WorkspacePanel? _workspacePanel;
        private static Qs3dPropertiesPanel? _propertiesPanel;
        private static RightPanel? _rightPanel;
        private static QuantityInsightPanel? _quantityInsightPanel;

        public static bool IsWorkspaceVisible => _workspace != null && _workspace.Visible;
        public static bool IsPropertiesVisible => _properties != null && _properties.Visible;
        public static bool IsRightPanelVisible => _right != null && _right.Visible;
        public static bool IsQuantityInsightVisible => _quantityInsight != null && _quantityInsight.Visible;

        public static void EnsureCreated()
        {
            if (_workspace != null && _properties != null && _right != null && _quantityInsight != null) return;
            if (_workspace != null || _properties != null || _right != null || _quantityInsight != null) DisposeCore(false);

            var layout = UserUiLayoutStore.Get();
            try
            {
                _workspacePanel = new WorkspacePanel();
                _propertiesPanel = new Qs3dPropertiesPanel();
                _rightPanel = new RightPanel();
                _quantityInsightPanel = new QuantityInsightPanel();

                // Properties is a separate native PaletteSet, but it deliberately shares the exact
                // live WorkspaceViewModel instance. WPF keeps this binding current even when the
                // Workspace panel replaces its DataContext after document/project reset.
                BindingOperations.SetBinding(
                    _propertiesPanel,
                    FrameworkElement.DataContextProperty,
                    new Binding(nameof(FrameworkElement.DataContext))
                    {
                        Source = _workspacePanel,
                        Mode = BindingMode.OneWay
                    });

                _workspace = new PaletteSet("QS3D — Mô hình", WorkspaceGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Left,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(UserUiLayoutStore.WorkspacePaletteMinWidth, UserUiLayoutStore.WorkspacePaletteMinHeight)
                };
                _workspace.DeviceIndependentSize = new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight);
                _workspace.AddVisual("Mô hình", _workspacePanel, true);

                _properties = new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Left,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(UserUiLayoutStore.PropertiesPaletteMinWidth, UserUiLayoutStore.PropertiesPaletteMinHeight)
                };
                _properties.DeviceIndependentSize = new WpfSize(layout.PropertiesPaletteWidth, layout.PropertiesPaletteHeight);
                _properties.AddVisual("Thuộc tính", _propertiesPanel, true);

                _right = new PaletteSet("QS3D — Bản vẽ & Lớp", RightGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Right,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(UserUiLayoutStore.RightPaletteMinWidth, UserUiLayoutStore.RightPaletteMinHeight)
                };
                _right.DeviceIndependentSize = new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight);
                _right.AddVisual("Quản lý", _rightPanel, true);

                _quantityInsight = new PaletteSet("QS3D — Diễn giải khối lượng", QuantityInsightGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Right,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(UserUiLayoutStore.QuantityPaletteMinWidth, UserUiLayoutStore.QuantityPaletteMinHeight)
                };
                _quantityInsight.DeviceIndependentSize = new WpfSize(layout.QuantityPaletteWidth, layout.QuantityPaletteHeight);
                _quantityInsight.AddVisual("Khối lượng", _quantityInsightPanel, true);
            }
            catch
            {
                DisposeCore(false);
                throw;
            }
        }

        // The explicit QS3D command is the owner-facing workspace activation entry point. Fresh
        // runtime evidence showed that routing it through the isolated legacy Workspace path left
        // only one QS3D palette visible and made native BricsCAD Properties look like part of QS3D.
        // Always restore the coordinated BLT3D-style surface here; standalone palette commands
        // below remain available when an intentionally isolated tool window is needed.
        public static void Show() => ShowBimWorkspace();

        public static void ShowWorkspace()
        {
            try
            {
                EnsureCreated();
                SetVisibility(workspace: true, right: false, quantityInsight: false);
                SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Workspace");
            }
        }

        public static void ShowProperties()
        {
            try
            {
                EnsureCreated();
                SetVisibility(workspace: false, properties: true, right: false, quantityInsight: false);
                SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Thuộc tính");
            }
        }

        // The owner-reference BIM tab intentionally coordinates four independent QS3D palettes
        // around the real BricsCAD viewport. Model/Family and dedicated QS3D Properties are separate
        // left-side native palettes; Management and Quantity remain separate native palettes right.
        // A result is returned so the bounded Ribbon settle loop consumes a retry only after the
        // native palettes were successfully recreated/reasserted.
        public static bool ShowBimWorkspace()
        {
            try
            {
                EnsureCreated();
                EnsureBimDockContract();
                SetVisibility(workspace: true, right: true, quantityInsight: true);
                SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
                _rightPanel?.Refresh();
                _quantityInsightPanel?.RefreshQuantityInsights();
                _workspacePanel?.SetStatus("MÔ HÌNH BIM • BLT3D workspace • Mô hình + Family + Thuộc tính QS3D tách riêng bên trái • viewport BricsCAD native ở giữa • Quản lý + Diễn giải bên phải.");
                return true;
            }
            catch (Exception)
            {
                ReportPaletteFailure("MÔ HÌNH BIM");
                return false;
            }
        }

        public static void ShowDrawingManagement()
        {
            try
            {
                EnsureCreated();
                SetVisibility(workspace: false, right: true, quantityInsight: false);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Bản vẽ & Lớp");
            }
        }

        public static void ShowQuantityInsight()
        {
            try
            {
                EnsureCreated();
                SetVisibility(workspace: false, right: false, quantityInsight: true);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Diễn giải khối lượng");
            }
        }

        public static void Hide()
        {
            PersistPaletteLayout();
            SetVisibility(workspace: false, right: false, quantityInsight: false);
        }

        public static void ShowSafeMode()
        {
            try
            {
                EnsureCreated();
                SetVisibility(workspace: true, right: false, quantityInsight: false);
                _workspacePanel?.SetStatus("Safe Mode: Thuộc tính, bản vẽ/layer và diễn giải khối lượng đang tắt.");
                SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Safe Mode");
            }
        }

        public static void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)
        {
            EnsureCreated();
            ProjectState? project = null;
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document != null && ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                project = currentProject;
            _workspacePanel?.SetInspectionReadOnly(snapshots, project);
            _quantityInsightPanel?.SetInspectionReadOnly(snapshots, project);
        }

        public static void SetStatus(string status)
        {
            try
            {
                _workspacePanel?.SetStatus(status);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Status");
            }
        }

        public static void RefreshProject()
        {
            _workspacePanel?.RefreshProjectForActiveDocument();
            _quantityInsightPanel?.RefreshQuantityInsights();
        }

        public static void RefreshCad() { _rightPanel?.Refresh(); }
        public static void RefreshAll() { RefreshProject(); RefreshCad(); }

        public static void ResetForNoDocument()
        {
            ResetPreservingVisibility();
        }

        public static void ResetForUnavailableProject(string status)
        {
            _workspacePanel?.ClearProjectForUnavailableDocument(status);
            _quantityInsightPanel?.ClearQuantityInsights(status);
            try { _rightPanel?.Refresh(); }
            catch { }
        }

        private static void ResetPreservingVisibility()
        {
            if (_workspace == null && _properties == null && _right == null && _quantityInsight == null) return;
            var workspaceVisible = IsWorkspaceVisible;
            var propertiesVisible = IsPropertiesVisible;
            var rightVisible = IsRightPanelVisible;
            var quantityVisible = IsQuantityInsightVisible;
            Dispose();
            EnsureCreated();
            if (workspaceVisible && propertiesVisible && rightVisible)
                EnsureBimDockContract();
            SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);
        }

        public static void Dispose()
        {
            DisposeCore(true);
        }

        private static void DisposeCore(bool persistLayout)
        {
            if (persistLayout) PersistPaletteLayout();
            DisposePalette(ref _workspace);
            DisposePalette(ref _properties);
            DisposePalette(ref _right);
            DisposePalette(ref _quantityInsight);
            _workspacePanel = null;
            _propertiesPanel = null;
            _rightPanel = null;
            _quantityInsightPanel = null;
        }

        private static void DisposePalette(ref PaletteSet? palette)
        {
            var current = palette;
            palette = null;
            if (current == null) return;
            try { current.Dispose(); }
            catch
            {
                // Native palette teardown is best-effort; one failed palette must not block the others.
            }
        }

        private static void EnsureBimDockContract()
        {
            if (_workspace != null && _workspace.Dock != DockSides.Left)
                _workspace.Dock = DockSides.Left;
            if (_properties != null && _properties.Dock != DockSides.Left)
                _properties.Dock = DockSides.Left;
            if (_right != null && _right.Dock != DockSides.Right)
                _right.Dock = DockSides.Right;
            if (_quantityInsight != null && _quantityInsight.Dock != DockSides.Right)
                _quantityInsight.Dock = DockSides.Right;
        }

        // Preserve the existing three-argument visibility contract for isolated commands and source
        // guards. The full BIM combination is the one case where the dedicated Properties palette
        // participates automatically; callers that need Properties alone use the four-argument form.
        private static void SetVisibility(bool workspace, bool right, bool quantityInsight)
        {
            var properties = workspace && right && quantityInsight;
            SetVisibility(workspace, properties, right, quantityInsight);
        }

        private static void SetVisibility(bool workspace, bool properties, bool right, bool quantityInsight)
        {
            if (_workspace != null) _workspace.Visible = workspace;
            if (_properties != null) _properties.Visible = properties;
            if (_right != null) _right.Visible = right;
            if (_quantityInsight != null) _quantityInsight.Visible = quantityInsight;
        }

        private static void ReportPaletteFailure(string operation)
        {
            try
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D " + operation + " UI error: không thể hoàn tất thao tác giao diện.");
            }
            catch
            {
                // Error reporting must never recurse into palette creation or mask the original failure.
            }
        }

        private static void PersistPaletteLayout()
        {
            if (_workspace == null && _properties == null && _right == null && _quantityInsight == null) return;
            try
            {
                var workspaceSize = _workspace?.DeviceIndependentSize;
                var propertiesSize = _properties?.DeviceIndependentSize;
                var rightSize = _right?.DeviceIndependentSize;
                var quantitySize = _quantityInsight?.DeviceIndependentSize;
                UserUiLayoutStore.Update(layout =>
                {
                    if (workspaceSize.HasValue)
                    {
                        layout.WorkspacePaletteWidth = checked((int)Math.Round(workspaceSize.Value.Width, MidpointRounding.AwayFromZero));
                        layout.WorkspacePaletteHeight = checked((int)Math.Round(workspaceSize.Value.Height, MidpointRounding.AwayFromZero));
                    }
                    if (propertiesSize.HasValue)
                    {
                        layout.PropertiesPaletteWidth = checked((int)Math.Round(propertiesSize.Value.Width, MidpointRounding.AwayFromZero));
                        layout.PropertiesPaletteHeight = checked((int)Math.Round(propertiesSize.Value.Height, MidpointRounding.AwayFromZero));
                    }
                    if (rightSize.HasValue)
                    {
                        layout.RightPaletteWidth = checked((int)Math.Round(rightSize.Value.Width, MidpointRounding.AwayFromZero));
                        layout.RightPaletteHeight = checked((int)Math.Round(rightSize.Value.Height, MidpointRounding.AwayFromZero));
                    }
                    if (quantitySize.HasValue)
                    {
                        layout.QuantityPaletteWidth = checked((int)Math.Round(quantitySize.Value.Width, MidpointRounding.AwayFromZero));
                        layout.QuantityPaletteHeight = checked((int)Math.Round(quantitySize.Value.Height, MidpointRounding.AwayFromZero));
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
