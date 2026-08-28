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
        private static readonly Guid PropertiesGuid = new Guid("4E48A4D2-27D4-4C80-B8A1-E6E4DA9A2399");
        private static readonly Guid RightGuid = new Guid("AC615D29-590A-457C-8579-6BF4ACEC5C29");
        private static readonly Guid QuantityInsightGuid = new Guid("7EA0345F-1F62-4BD4-9ED0-3B25EB76A91B");
        private static PaletteSet? _workspace;
        private static PaletteSet? _properties;
        private static PaletteSet? _right;
        private static PaletteSet? _quantityInsight;
        private static WorkspacePanel? _workspacePanel;
        private static System.Windows.FrameworkElement? _propertiesVisual;
        private static RightPanel? _rightPanel;
        private static QuantityInsightPanel? _quantityInsightPanel;
        private static bool _preserveInspectionStatusOnNextShow;
        private static bool _propertiesVisibilityTransitionActive;

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
                _propertiesVisual = _workspacePanel.CreatePropertiesPaletteVisual();
                _rightPanel = new RightPanel();
                _quantityInsightPanel = new QuantityInsightPanel();

                _workspace = CreatePaletteSet(
                    "QS3D — Mô hình",
                    WorkspaceGuid,
                    DockSides.Left,
                    new DrawingSize(UserUiLayoutStore.WorkspacePaletteMinWidth, UserUiLayoutStore.WorkspacePaletteMinHeight),
                    new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight),
                    "Mô hình",
                    _workspacePanel);

                _properties = CreatePaletteSet(
                    "QS3D — Thuộc tính",
                    PropertiesGuid,
                    DockSides.Left,
                    new DrawingSize(UserUiLayoutStore.PropertiesPaletteMinWidth, UserUiLayoutStore.PropertiesPaletteMinHeight),
                    new WpfSize(layout.PropertiesPaletteWidth, layout.PropertiesPaletteHeight),
                    "Thuộc tính",
                    _propertiesVisual);
                _properties.StateChanged += OnPropertiesPaletteStateChanged;

                _right = CreatePaletteSet(
                    "QS3D — Bản vẽ & Lớp",
                    RightGuid,
                    DockSides.Right,
                    new DrawingSize(UserUiLayoutStore.RightPaletteMinWidth, UserUiLayoutStore.RightPaletteMinHeight),
                    new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight),
                    "Quản lý",
                    _rightPanel);

                _quantityInsight = CreatePaletteSet(
                    "QS3D — Diễn giải khối lượng",
                    QuantityInsightGuid,
                    DockSides.Right,
                    new DrawingSize(UserUiLayoutStore.QuantityPaletteMinWidth, UserUiLayoutStore.QuantityPaletteMinHeight),
                    new WpfSize(layout.QuantityPaletteWidth, layout.QuantityPaletteHeight),
                    "Khối lượng",
                    _quantityInsightPanel);
            }
            catch
            {
                DisposeCore(false);
                throw;
            }
        }

        private static PaletteSet CreatePaletteSet(
            string title,
            Guid guid,
            DockSides dock,
            DrawingSize minimumSize,
            WpfSize initialSize,
            string visualTitle,
            System.Windows.FrameworkElement visual)
        {
            PaletteSet? palette = null;
            try
            {
                palette = new PaletteSet(title, guid);
                palette.DockEnabled = DockSides.Left | DockSides.Right;
                palette.Dock = dock;
                palette.Visible = false;
                palette.KeepFocus = false;
                if (guid == WorkspaceGuid)
                    palette.MinimumSize = new DrawingSize(UserUiLayoutStore.WorkspacePaletteMinWidth, UserUiLayoutStore.WorkspacePaletteMinHeight);
                else
                    palette.MinimumSize = minimumSize;
                palette.DeviceIndependentSize = initialSize;
                palette.AddVisual(visualTitle, visual, true);
                return palette;
            }
            catch
            {
                if (palette != null)
                {
                    try { palette.Dispose(); }
                    catch
                    {
                        // Native construction rollback is best-effort. The important ownership
                        // boundary is that the exact pre-publication instance is attempted here.
                    }
                }
                throw;
            }
        }

        // The explicit QS3D command is the owner-facing workspace activation entry point. Keep the
        // BricsCAD host shell and native modelspace intact; ShowBimWorkspace only coordinates QS3D
        // palettes around that host-owned center surface.
        public static void Show() => ShowBimWorkspace();

        public static void ShowWorkspace()
        {
            try
            {
                EnsureCreated();
                _workspacePanel?.SetDedicatedPropertiesPaletteActive(false);
                SetVisibility(workspace: true, right: false, quantityInsight: false);
                SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Workspace");
            }
        }

        // Owner-reference BIM layout: one integrated two-column QS3D Workspace on the left,
        // native BricsCAD modelspace in the center, and Drawing/Layer Management on the right.
        // The dedicated Properties and Quantity palettes remain available on demand, but do not
        // auto-open in BIM because the reference keeps Properties embedded below Family.
        public static bool ShowBimWorkspace()
        {
            var preserveInspectionStatus = _preserveInspectionStatusOnNextShow;
            _preserveInspectionStatusOnNextShow = false;
            try
            {
                EnsureCreated();
                _workspacePanel?.SetDedicatedPropertiesPaletteActive(false);
                EnsureBimDockContract();
                SetVisibility(workspace: true, right: true, quantityInsight: false);
                SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);
                _rightPanel?.Refresh();
                if (!preserveInspectionStatus)
                    _workspacePanel?.SetStatus("MÔ HÌNH BIM • BLT3D workspace • Zone/Tầng/Mô hình + Family/Thuộc tính bên trái • viewport BricsCAD native ở giữa • Quản lý bản vẽ/lớp bên phải.");
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
                _workspacePanel?.SetDedicatedPropertiesPaletteActive(false);
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
                _workspacePanel?.SetDedicatedPropertiesPaletteActive(false);
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
            _workspacePanel?.SetDedicatedPropertiesPaletteActive(false);
            SetVisibility(workspace: false, right: false, quantityInsight: false);
        }

        public static void ShowSafeMode()
        {
            try
            {
                EnsureCreated();
                _workspacePanel?.SetDedicatedPropertiesPaletteActive(false);
                SetVisibility(workspace: true, right: false, quantityInsight: false);
                _workspacePanel?.SetStatus("Safe Mode: panel thuộc tính, bản vẽ/layer và diễn giải khối lượng đang tắt.");
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
            _preserveInspectionStatusOnNextShow = true;
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
            var ownerReferenceBimActive = workspaceVisible && rightVisible && !propertiesVisible && !quantityVisible;
            Dispose();
            EnsureCreated();
            _workspacePanel?.SetDedicatedPropertiesPaletteActive(propertiesVisible);
            if (ownerReferenceBimActive)
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
            UnsubscribeFromPropertiesPaletteStateChanges();
            DisposePalette(ref _properties);
            DisposePalette(ref _workspace);
            DisposePalette(ref _right);
            DisposePalette(ref _quantityInsight);
            _workspacePanel = null;
            _propertiesVisual = null;
            _rightPanel = null;
            _quantityInsightPanel = null;
            _preserveInspectionStatusOnNextShow = false;
            _propertiesVisibilityTransitionActive = false;
        }

        private static void UnsubscribeFromPropertiesPaletteStateChanges()
        {
            var properties = _properties;
            if (properties == null) return;

            try { properties.StateChanged -= OnPropertiesPaletteStateChanged; }
            catch
            {
                // The native host may already be tearing the PaletteSet down. DisposePalette still
                // owns the exact instance, and a replacement PaletteSet will install a fresh hook.
            }
        }

        private static void OnPropertiesPaletteStateChanged(object sender, PaletteSetStateEventArgs e)
        {
            if (!ReferenceEquals(sender, _properties) || _propertiesVisibilityTransitionActive) return;
            if (e.NewState != StateEventIndex.Show && e.NewState != StateEventIndex.Hide) return;

            try
            {
                _propertiesVisibilityTransitionActive = true;
                _workspacePanel?.SetDedicatedPropertiesPaletteActive(e.NewState == StateEventIndex.Show);
            }
            catch (Exception)
            {
                ReportPaletteFailure("Thuộc tính");
            }
            finally
            {
                _propertiesVisibilityTransitionActive = false;
            }
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

            // BricsCAD can restore stale host-owned palette dimensions after construction. Reapply
            // the normalized per-user fallback only when the host reports a non-finite or undersized
            // value; valid user-resized palettes remain untouched during explicit BIM activation.
            var layout = UserUiLayoutStore.Get();
            EnsurePaletteSize(
                _workspace,
                new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight),
                UserUiLayoutStore.WorkspacePaletteMinWidth,
                UserUiLayoutStore.WorkspacePaletteMinHeight);
            EnsurePaletteSize(
                _properties,
                new WpfSize(layout.PropertiesPaletteWidth, layout.PropertiesPaletteHeight),
                UserUiLayoutStore.PropertiesPaletteMinWidth,
                UserUiLayoutStore.PropertiesPaletteMinHeight);
            EnsurePaletteSize(
                _right,
                new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight),
                UserUiLayoutStore.RightPaletteMinWidth,
                UserUiLayoutStore.RightPaletteMinHeight);
            EnsurePaletteSize(
                _quantityInsight,
                new WpfSize(layout.QuantityPaletteWidth, layout.QuantityPaletteHeight),
                UserUiLayoutStore.QuantityPaletteMinWidth,
                UserUiLayoutStore.QuantityPaletteMinHeight);
        }

        private static void EnsurePaletteSize(PaletteSet? palette, WpfSize fallback, int minWidth, int minHeight)
        {
            if (palette == null) return;
            var useFallback = false;
            try
            {
                var size = palette.DeviceIndependentSize;
                useFallback =
                    double.IsNaN(size.Width) || double.IsInfinity(size.Width) ||
                    double.IsNaN(size.Height) || double.IsInfinity(size.Height) ||
                    size.Width < minWidth || size.Height < minHeight;
            }
            catch
            {
                useFallback = true;
            }

            if (!useFallback) return;
            try { palette.DeviceIndependentSize = fallback; }
            catch
            {
                // Native host state is best-effort; a failed size repair must not abort palette activation.
            }
        }

        // Legacy three-argument call sites represent integrated/isolated owner surfaces. Keep the
        // dedicated Properties PaletteSet opt-in only; the default BIM reference embeds Properties
        // in Workspace and therefore leaves the dedicated palette hidden.
        private static void SetVisibility(bool workspace, bool right, bool quantityInsight)
        {
            SetVisibility(workspace, properties: false, right, quantityInsight);
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
                var hasWorkspaceSize = TryGetPersistableSize(workspaceSize, out var workspaceWidth, out var workspaceHeight);
                var hasPropertiesSize = TryGetPersistableSize(propertiesSize, out var propertiesWidth, out var propertiesHeight);
                var hasRightSize = TryGetPersistableSize(rightSize, out var rightWidth, out var rightHeight);
                var hasQuantitySize = TryGetPersistableSize(quantitySize, out var quantityWidth, out var quantityHeight);

                UserUiLayoutStore.Update(layout =>
                {
                    if (hasWorkspaceSize)
                    {
                        layout.WorkspacePaletteWidth = workspaceWidth;
                        layout.WorkspacePaletteHeight = workspaceHeight;
                    }
                    if (hasPropertiesSize)
                    {
                        layout.PropertiesPaletteWidth = propertiesWidth;
                        layout.PropertiesPaletteHeight = propertiesHeight;
                    }
                    if (hasRightSize)
                    {
                        layout.RightPaletteWidth = rightWidth;
                        layout.RightPaletteHeight = rightHeight;
                    }
                    if (hasQuantitySize)
                    {
                        layout.QuantityPaletteWidth = quantityWidth;
                        layout.QuantityPaletteHeight = quantityHeight;
                    }
                });
            }
            catch
            {
                // UI preference persistence is best-effort and must never block palette teardown.
            }
        }

        private static bool TryGetPersistableSize(WpfSize? size, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (!size.HasValue) return false;

            var value = size.Value;
            if (double.IsNaN(value.Width) || double.IsInfinity(value.Width) ||
                double.IsNaN(value.Height) || double.IsInfinity(value.Height) ||
                value.Width <= 0d || value.Height <= 0d ||
                value.Width > int.MaxValue || value.Height > int.MaxValue)
                return false;

            width = checked((int)Math.Round(value.Width, MidpointRounding.AwayFromZero));
            height = checked((int)Math.Round(value.Height, MidpointRounding.AwayFromZero));
            return width > 0 && height > 0;
        }
    }
}
