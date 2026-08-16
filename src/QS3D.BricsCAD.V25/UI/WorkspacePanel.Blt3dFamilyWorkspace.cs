using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// BLT3D-inspired workspace presentation that keeps the QS3D semantic model and native
    /// BricsCAD mutation paths intact. The visible contract is one integrated palette:
    /// model navigation on the left, Family list above Properties on the right.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly string[] Blt3dFoundationTreeOrder =
        {
            "Cọc", "Đài Cọc", "Dầm Móng", "Móng Băng", "Móng Bè", "Bê Tông Lót"
        };

        // This initializer runs before WorkspacePanel's existing static constructor. The handler
        // queues the BLT3D presentation until after the compact-shell Loaded handler has retired
        // the legacy columns, so the final visible layout is deterministic.
        private static readonly bool Blt3dFamilyWorkspaceBootstrapRegistered =
            RegisterBlt3dFamilyWorkspaceBootstrap();

        private bool _blt3dFamilyWorkspaceApplied;
        private Border? _blt3dFamilyModeChooser;
        private TextBlock? _blt3dFamilyModeTitle;

        private static bool RegisterBlt3dFamilyWorkspaceBootstrap()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dFamilyWorkspaceLoaded),
                true);
            return true;
        }

        private static void OnBlt3dFamilyWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(panel.ApplyBlt3dFamilyWorkspace));
        }

        private void ApplyBlt3dFamilyWorkspace()
        {
            // Read the bootstrap flag deliberately: repositories building with warnings-as-errors
            // must not treat the registration field as an assigned-but-unused private field.
            if (!Blt3dFamilyWorkspaceBootstrapRegistered || _blt3dFamilyWorkspaceApplied) return;

            RestoreBlt3dWorkspaceColumns();
            RestoreBlt3dFamilyRows();
            EnsureBlt3dFoundationTree();

            // Attach the existing production family/subtype workflow first while the original
            // XAML labels are still present, then restyle/relabel and replace only the Add surface.
            AttachFamilySubtypeInteractions();
            ApplyBlt3dWorkspaceLabels();
            ApplyBlt3dReferencePresentation();
            EnsureBlt3dFamilyModeChooser();
            RewireBlt3dFamilyAddActions();

            ModelTree.SelectedItemChanged -= OnBlt3dTreeSelectionChanged;
            ModelTree.SelectedItemChanged += OnBlt3dTreeSelectionChanged;
            AppendShortcutHint(FindButton("Delete"), "Delete khi Family list đang focus");
            _blt3dFamilyWorkspaceApplied = true;
        }

        private void RestoreBlt3dWorkspaceColumns()
        {
            var root = WorkspaceContentRoot;
            if (root == null) return;

            var workspace = root.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
            if (workspace == null) return;

            // Reference layout is ONE docked palette. Never allow a horizontal-scroll state to
            // push the model tree off-screen and leave Family/Properties looking like two windows.
            root.MinWidth = 0;
            workspace.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            var layout = Services.UserUiLayoutStore.Get();
            var modelColumn = workspace.ColumnDefinitions[0];
            modelColumn.MinWidth = 150;
            modelColumn.MaxWidth = 220;
            if (layout.ModelColumnWidth > 0)
                modelColumn.Width = new GridLength(Math.Max(modelColumn.MinWidth, Math.Min(modelColumn.MaxWidth, layout.ModelColumnWidth)));
            else
                modelColumn.Width = new GridLength(168);

            var familySplitter = workspace.ColumnDefinitions[1];
            familySplitter.MinWidth = 0;
            familySplitter.MaxWidth = 4;
            familySplitter.Width = new GridLength(4);

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

            HideRetiredDashboardBands(workspace);
        }

        private static void HideRetiredDashboardBands(Grid workspace)
        {
            // Defensive idempotence: the retired room/inspection bands must never surface when a
            // persisted layout or later visual-tree refresh re-applies child visibility.
            foreach (UIElement child in workspace.Children)
            {
                if (Grid.GetColumn(child) > 2)
                    child.Visibility = Visibility.Collapsed;
            }
        }

        private void RestoreBlt3dFamilyRows()
        {
            var familyGrid = FindVisualChildren<Grid>(this)
                .FirstOrDefault(grid =>
                    grid.RowDefinitions.Count == 3 &&
                    IsVisualDescendant(grid, FamilyList) &&
                    IsVisualDescendant(grid, PropertyList));
            if (familyGrid == null) return;

            // Match the reference palette: Family list on top, Properties below, both sharing the
            // same right-hand column instead of reading as independent stacked panes.
            familyGrid.RowDefinitions[0].MinHeight = 0;
            familyGrid.RowDefinitions[0].Height = new GridLength(55, GridUnitType.Star);
            familyGrid.RowDefinitions[1].MinHeight = 0;
            familyGrid.RowDefinitions[1].Height = new GridLength(4);
            familyGrid.RowDefinitions[2].MinHeight = 0;
            familyGrid.RowDefinitions[2].Height = new GridLength(45, GridUnitType.Star);
        }

        private void ApplyBlt3dWorkspaceLabels()
        {
            foreach (var text in FindVisualChildren<TextBlock>(this))
            {
                if (string.Equals(text.Text, "Zone", StringComparison.Ordinal))
                    text.Text = "Zone làm việc";
                else if (string.Equals(text.Text, "Tầng", StringComparison.Ordinal))
                    text.Text = "Tầng làm việc";
                else if (string.Equals(text.Text, "THUỘC TÍNH", StringComparison.Ordinal))
                    text.Text = "Thuộc tính";
                else if (string.Equals(text.Text, "Tìm Family / Type", StringComparison.Ordinal) ||
                         string.Equals(text.Text, "Tìm thuộc tính", StringComparison.Ordinal))
                    text.Visibility = Visibility.Collapsed;
            }

            RenameBlt3dButton("+ Thêm", "+ Add");
            RenameBlt3dButton("＋  Add", "+ Add");
            RenameBlt3dButton("Xóa", "Delete");
            // Keep the command truthful: it imports the currently selected CAD entities; there is
            // no background/autonomous importer behind this button yet.
            RenameBlt3dButton("Bóc chọn", "⚡ Nhập từ chọn");
        }

        private void ApplyBlt3dReferencePresentation()
        {
            // Left navigation: flatten the old card chrome and keep only the two selectors + tree,
            // matching the reference screenshot's narrow model-navigation rail.
            var scopeTitle = FindTextBlock("PHẠM VI LÀM VIỆC");
            CollapseNearestAncestor<DockPanel>(scopeTitle);

            var scopeCard = FindNearestAncestor<Border>(ZoneCombo);
            if (scopeCard != null)
            {
                scopeCard.Background = Brushes.Transparent;
                scopeCard.BorderThickness = new Thickness(0);
                scopeCard.Padding = new Thickness(0);
                scopeCard.Margin = new Thickness(0, 0, 0, 6);
            }

            CollapseNearestAncestor<DockPanel>(FindTextBlock("MÔ HÌNH"));

            // Right/top: this is a list area inside the same palette, not a second FAMILY / TYPE
            // window. Remove the redundant card heading and advanced wall toolbar from this view.
            CollapseNearestAncestor<DockPanel>(FindTextBlock("FAMILY / TYPE"));
            CollapseButton("Vẽ 3D");

            var advancedToolbarButton = FindButton("Giao tường");
            var advancedToolbarBand = FindNearestAncestor<Border>(advancedToolbarButton);
            if (advancedToolbarBand != null)
                advancedToolbarBand.Visibility = Visibility.Collapsed;
            else
            {
                CollapseButton("Giao tường");
                CollapseButton("Snap xem");
                CollapseButton("Snap áp");
                CollapseButton("Auto Host");
            }

            // Keep the three primary actions on the same compact toolbar as the reference.
            var addButton = FindButton("+ Add");
            var primaryToolbar = FindNearestAncestor<WrapPanel>(addButton);
            if (primaryToolbar != null)
            {
                primaryToolbar.HorizontalAlignment = HorizontalAlignment.Right;
                primaryToolbar.Margin = new Thickness(0, 0, 0, 5);
            }

            var familySearch = FamilySearch;
            familySearch.Margin = new Thickness(0, 0, 0, 5);

            // Property editing remains fully functional. Only collapse decorative count/legend
            // chrome; scope/search controls are deliberately retained because they change edit
            // semantics and must not be silently removed for visual mimicry.
            var propertyTitle = FindTextBlock("Thuộc tính");
            var propertyHeader = FindNearestAncestor<DockPanel>(propertyTitle);
            if (propertyHeader != null)
            {
                propertyHeader.Margin = new Thickness(0, 0, 0, 5);
                var countBadge = propertyHeader.Children.OfType<Border>().FirstOrDefault();
                if (countBadge != null)
                    countBadge.Visibility = Visibility.Collapsed;
            }

            var propertyLegend = FindTextBlock("Family • Kế thừa • Override • CAD/đo • Hệ thống • Selection");
            var propertyLegendBorder = FindNearestAncestor<Border>(propertyLegend);
            if (propertyLegendBorder != null)
                propertyLegendBorder.Visibility = Visibility.Collapsed;
        }

        private TextBlock? FindTextBlock(string text)
        {
            return FindVisualChildren<TextBlock>(this)
                .FirstOrDefault(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal));
        }

        private static T? FindNearestAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                if (current is T match) return match;
            }

            return null;
        }

        private static bool IsVisualDescendant(DependencyObject ancestor, DependencyObject descendant)
        {
            var current = descendant;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor)) return true;
                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static void CollapseNearestAncestor<T>(DependencyObject? child) where T : FrameworkElement
        {
            var ancestor = FindNearestAncestor<T>(child);
            if (ancestor != null)
                ancestor.Visibility = Visibility.Collapsed;
        }

        private void CollapseButton(string text)
        {
            var button = FindButton(text);
            if (button != null)
                button.Visibility = Visibility.Collapsed;
        }

        private void RenameBlt3dButton(string oldText, string newText)
        {
            foreach (var button in FindVisualChildren<Button>(this).Where(button =>
                         string.Equals(button.Content as string, oldText, StringComparison.Ordinal)))
                button.Content = newText;
        }

        private void EnsureBlt3dFoundationTree()
        {
            var foundation = ModelTree.Items
                .OfType<TreeViewItem>()
                .FirstOrDefault(item =>
                    string.Equals(item.Tag as string, ElementCategory.Foundation.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Header as string, "Móng", StringComparison.OrdinalIgnoreCase));
            if (foundation == null) return;

            foreach (var subtype in Blt3dFoundationTreeOrder)
            {
                var exists = foundation.Items
                    .OfType<TreeViewItem>()
                    .Any(item => string.Equals(item.Header as string, subtype, StringComparison.OrdinalIgnoreCase));
                if (exists) continue;

                foundation.Items.Add(new TreeViewItem
                {
                    Header = subtype,
                    Tag = ElementCategory.Foundation.ToString(),
                    MinHeight = 22,
                    Padding = new Thickness(2, 1, 2, 1),
                    Margin = new Thickness(0)
                });
            }

            foundation.IsExpanded = true;
            TuneTreeItem(foundation, 0);
        }

        private void EnsureBlt3dFamilyModeChooser()
        {
            if (_blt3dFamilyModeChooser != null) return;
            if (!(FamilyList.Parent is Panel parent)) return;

            var childIndex = parent.Children.IndexOf(FamilyList);
            if (childIndex < 0) return;

            parent.Children.RemoveAt(childIndex);

            var host = new Grid();
            parent.Children.Insert(childIndex, host);
            host.Children.Add(FamilyList);

            var chooser = new Border
            {
                Background = TryFindResource("Bg1Brush") as Brush ?? new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = TryFindResource("BorderStrongBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(chooser, 20);
            _blt3dFamilyModeChooser = chooser;
            host.Children.Add(chooser);

            var stack = new StackPanel();
            chooser.Child = stack;

            _blt3dFamilyModeTitle = new TextBlock
            {
                Text = "Chọn kiểu Family",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 9),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            stack.Children.Add(_blt3dFamilyModeTitle);

            var cards = new Grid();
            cards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stack.Children.Add(cards);

            var parameter = CreateBlt3dModeCard("◩", "Tham số", "Tạo Family bằng bộ tham số QS3D và Quick Schema hiện có.");
            parameter.Margin = new Thickness(0, 0, 5, 0);
            parameter.Click += OnBlt3dParameterModeClick;
            cards.Children.Add(parameter);

            var solid3d = CreateBlt3dModeCard("▣", "Solid3D", "Tạo Family rồi chuyển sang workflow dựng/capture Solid3D native BricsCAD.");
            solid3d.Margin = new Thickness(5, 0, 0, 0);
            solid3d.Click += OnBlt3dSolid3dModeClick;
            Grid.SetColumn(solid3d, 1);
            cards.Children.Add(solid3d);
        }

        private Button CreateBlt3dModeCard(string glyph, string label, string tooltip)
        {
            var content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(new TextBlock
            {
                Text = glyph,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 7)
            });
            content.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            return new Button
            {
                Content = content,
                MinHeight = 96,
                Padding = new Thickness(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = TryFindResource("Bg2Brush") as Brush,
                BorderBrush = TryFindResource("BorderStrongBrush") as Brush,
                BorderThickness = new Thickness(1),
                ToolTip = tooltip
            };
        }

        private void RewireBlt3dFamilyAddActions()
        {
            foreach (var button in FindVisualChildren<Button>(this).Where(IsBlt3dFamilyAddButton))
            {
                button.Click -= OnAddClick;
                button.Click -= OnFamilyAddModeClick;
                button.Click -= OnBlt3dFamilyAddClick;
                button.Click += OnBlt3dFamilyAddClick;
                button.ToolTip = "Add Family — chọn Tham số hoặc Solid3D";
            }

            var menu = FamilyList.ContextMenu;
            if (menu == null) return;
            foreach (var item in menu.Items.OfType<MenuItem>().Where(item =>
                         string.Equals(item.Header as string, "Nhân bản Family", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Header as string, "Thêm Family…", StringComparison.OrdinalIgnoreCase)))
            {
                item.Click -= OnAddClick;
                item.Click -= OnFamilyAddModeClick;
                item.Click -= OnBlt3dFamilyAddClick;
                item.Click += OnBlt3dFamilyAddClick;
                item.Header = "Thêm Family…";
            }
        }

        private static bool IsBlt3dFamilyAddButton(Button button)
        {
            var text = button.Content as string;
            return string.Equals(text, "+ Add", StringComparison.Ordinal) ||
                   string.Equals(text, "+ Thêm", StringComparison.Ordinal) ||
                   string.Equals(text, "＋  Add", StringComparison.Ordinal) ||
                   string.Equals(text, "Add", StringComparison.Ordinal);
        }

        private void OnBlt3dFamilyAddClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ShowBlt3dFamilyModeChooser();
        }

        private void ShowBlt3dFamilyModeChooser()
        {
            if (_blt3dFamilyModeChooser == null)
            {
                var fallback = CreateContextMenu();
                fallback.Items.Add(CreateMenuItem("Tham số", OnAddParameterFamilyClick));
                fallback.Items.Add(CreateMenuItem("Solid3D", OnAddSolid3dFamilyClick));
                fallback.PlacementTarget = FamilyList;
                fallback.Placement = PlacementMode.MousePoint;
                fallback.IsOpen = true;
                return;
            }

            var scope = !string.IsNullOrWhiteSpace(_familySubtypeFilter)
                ? _familySubtypeFilter
                : (_categoryFilter.HasValue ? _categoryFilter.Value.ToString() : "Family");
            if (_blt3dFamilyModeTitle != null)
                _blt3dFamilyModeTitle.Text = "Chọn kiểu Family — " + scope;

            FamilyList.Visibility = Visibility.Collapsed;
            _blt3dFamilyModeChooser.Visibility = Visibility.Visible;
            SetStatus("Chọn cách tạo " + scope + ": Tham số hoặc Solid3D.");
        }

        private void HideBlt3dFamilyModeChooser()
        {
            if (_blt3dFamilyModeChooser != null)
                _blt3dFamilyModeChooser.Visibility = Visibility.Collapsed;
            FamilyList.Visibility = Visibility.Visible;
        }

        private void OnBlt3dParameterModeClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            HideBlt3dFamilyModeChooser();
            CreateFamilyFromWorkspaceSubtype(false);
        }

        private void OnBlt3dSolid3dModeClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            HideBlt3dFamilyModeChooser();
            CreateFamilyFromWorkspaceSubtype(true);
        }

        private void OnBlt3dTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HideBlt3dFamilyModeChooser();
        }
    }
}
