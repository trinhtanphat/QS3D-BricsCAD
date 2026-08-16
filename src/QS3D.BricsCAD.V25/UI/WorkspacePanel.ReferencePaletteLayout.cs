using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Final presentation pass for the owner-approved BLT3D reference palette.
    ///
    /// WorkspacePanel has several legacy presentation partials that all listen to Loaded. The
    /// compact-shell pass can therefore win the race and retire the Family/Properties column,
    /// leaving only the oversized header/footer chrome visible. This pass deliberately runs at
    /// ApplicationIdle, after the Loaded/ContextIdle compatibility passes, and establishes one
    /// authoritative visual contract without changing any production click handlers or model data.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool ReferencePaletteLayoutRegistered = RegisterReferencePaletteLayout();
        private bool _referenceFooterRefreshAttached;

        private static bool RegisterReferencePaletteLayout()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnReferencePaletteLoaded),
                true);
            return true;
        }

        private static void OnReferencePaletteLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !ReferencePaletteLayoutRegistered)
                return;

            // Run after CompactShell (Loaded), BLT3D restore (DispatcherPriority.Loaded) and the
            // functional-action compatibility pass (ContextIdle). This prevents a later pass from
            // reopening the old header/footer or hiding the Family/Properties column again.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(panel.ApplyReferencePaletteLayout));
        }

        private void ApplyReferencePaletteLayout()
        {
            var root = WorkspaceContentRoot;
            if (root == null || root.RowDefinitions.Count < 3)
                return;

            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            root.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            // Reference screenshot: no branded dashboard header. The palette starts immediately
            // with Zone/Floor + model tree at left and Family/Properties at right.
            var header = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 0);
            if (header != null)
                header.Visibility = Visibility.Collapsed;

            root.RowDefinitions[0].MinHeight = 0;
            root.RowDefinitions[0].MaxHeight = 0;
            root.RowDefinitions[0].Height = new GridLength(0);
            root.RowDefinitions[1].MinHeight = 0;
            root.RowDefinitions[1].MaxHeight = double.PositiveInfinity;
            root.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            root.RowDefinitions[2].MinHeight = 0;
            root.RowDefinitions[2].MaxHeight = 28;
            root.RowDefinitions[2].Height = new GridLength(28);

            RestoreReferenceWorkspaceColumns(root);
            RestoreReferenceFamilyRows();

            // Reapply the BLT3D labels/presentation after all competing Loaded handlers have run.
            // These helpers preserve the existing commands; only visibility, wording and density
            // are changed here.
            ApplyBlt3dWorkspaceLabels();
            ApplyBlt3dReferencePresentation();
            ApplyReferencePropertyDensity();
            ApplyReferenceToolbarLabels();
            ApplyReferenceFooter(root);
        }

        private void RestoreReferenceWorkspaceColumns(Grid root)
        {
            var workspace = root.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
            if (workspace == null)
                return;

            workspace.MinWidth = 0;

            // Match the reference palette rather than a persisted legacy dashboard width.
            var modelColumn = workspace.ColumnDefinitions[0];
            modelColumn.MinWidth = 0;
            modelColumn.MaxWidth = 168;
            modelColumn.Width = new GridLength(168);

            var splitter = workspace.ColumnDefinitions[1];
            splitter.MinWidth = 0;
            splitter.MaxWidth = 4;
            splitter.Width = new GridLength(4);

            var familyColumn = workspace.ColumnDefinitions[2];
            familyColumn.MinWidth = 0;
            familyColumn.MaxWidth = double.PositiveInfinity;
            familyColumn.Width = new GridLength(1, GridUnitType.Star);

            for (var index = 3; index < workspace.ColumnDefinitions.Count; index++)
            {
                var retired = workspace.ColumnDefinitions[index];
                retired.MinWidth = 0;
                retired.MaxWidth = 0;
                retired.Width = new GridLength(0);
            }

            foreach (UIElement child in workspace.Children)
                child.Visibility = Grid.GetColumn(child) <= 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RestoreReferenceFamilyRows()
        {
            var familyGrid = FindVisualChildren<Grid>(this)
                .FirstOrDefault(grid =>
                    grid.RowDefinitions.Count == 3 &&
                    IsVisualDescendant(grid, FamilyList) &&
                    IsVisualDescendant(grid, PropertyList));
            if (familyGrid == null)
                return;

            familyGrid.RowDefinitions[0].MinHeight = 0;
            familyGrid.RowDefinitions[0].Height = new GridLength(55, GridUnitType.Star);
            familyGrid.RowDefinitions[1].MinHeight = 0;
            familyGrid.RowDefinitions[1].Height = new GridLength(4);
            familyGrid.RowDefinitions[2].MinHeight = 0;
            familyGrid.RowDefinitions[2].Height = new GridLength(45, GridUnitType.Star);
        }

        private void ApplyReferencePropertyDensity()
        {
            // The reference keeps the Properties body clean: title + grouped values. The legacy
            // scope/filter chrome remains in the visual tree for compatibility but is not shown in
            // this compact palette.
            var scopeLabel = FindTextBlock("Phạm vi sửa");
            var scopeGrid = FindNearestAncestor<Grid>(scopeLabel);
            if (scopeGrid != null)
                scopeGrid.Visibility = Visibility.Collapsed;

            var propertySearchBorder = FindNearestAncestor<Border>(PropertySearch);
            if (propertySearchBorder != null)
                propertySearchBorder.Visibility = Visibility.Collapsed;

            CollapseButton("Làm mới");
            CollapseButton("Vẽ 3D");
        }

        private void ApplyReferenceToolbarLabels()
        {
            RenameBlt3dButton("⚡ Nhập từ chọn", "⚡ Nhập tự động");
            RenameBlt3dButton("Bóc chọn", "⚡ Nhập tự động");

            var add = FindButton("+ Add");
            var delete = FindButton("Delete");
            var import = FindButton("⚡ Nhập tự động");
            foreach (var button in new[] { add, delete, import })
            {
                if (button == null) continue;
                button.MinHeight = 24;
                button.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        private void ApplyReferenceFooter(Grid root)
        {
            var footerBorder = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 2);
            if (footerBorder == null)
                return;

            footerBorder.Visibility = Visibility.Visible;
            footerBorder.Padding = new Thickness(6, 2, 6, 2);

            var footer = footerBorder.Child as DockPanel;
            if (footer != null)
            {
                var liveSemantic = FindVisualChildren<TextBlock>(footer)
                    .FirstOrDefault(text => string.Equals(text.Text, "LIVE SEMANTIC", StringComparison.Ordinal));
                var legacyStatus = liveSemantic == null
                    ? null
                    : FindNearestAncestor<StackPanel>(liveSemantic);
                if (legacyStatus != null)
                    legacyStatus.Visibility = Visibility.Collapsed;
            }

            var health = FindButton("Kiểm tra");
            if (health != null)
                health.Visibility = Visibility.Collapsed;

            if (!_referenceFooterRefreshAttached)
            {
                ZoneCombo.SelectionChanged += OnReferenceFooterContextChanged;
                FloorCombo.SelectionChanged += OnReferenceFooterContextChanged;
                _referenceFooterRefreshAttached = true;
            }

            RenderReferenceFooterContext();
        }

        private void OnReferenceFooterContextChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderReferenceFooterContext();
        }

        private void RenderReferenceFooterContext()
        {
            if (_footerContextText == null)
                return;

            // Let the existing footer adapter resolve the active BricsCAD project/floor, then trim
            // its verbose dashboard sentence down to the reference caption.
            RefreshFooterContext();
            var text = _footerContextText.Text ?? string.Empty;
            var floor = ExtractReferenceFooterPart(text, "FLOOR  ", "   •   CAO ĐỘ  ");
            var elevation = ExtractReferenceFooterPart(text, "CAO ĐỘ  ", null);
            _footerContextText.Text = "Tầng " + floor + "   ·   Cao độ " + elevation;
            _footerContextText.TextAlignment = TextAlignment.Left;
            _footerContextText.Margin = new Thickness(10, 0, 0, 0);
        }

        private static string ExtractReferenceFooterPart(string text, string prefix, string? suffix)
        {
            var start = text.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
                return "—";

            start += prefix.Length;
            var end = suffix == null ? text.Length : text.IndexOf(suffix, start, StringComparison.Ordinal);
            if (end < start)
                end = text.Length;

            var value = text.Substring(start, end - start).Trim();
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }
    }
}
