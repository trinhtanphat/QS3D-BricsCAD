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
    /// BricsCAD mutation paths intact. This layer restores the Family/Properties pane that the
    /// compact shell used to retire and exposes the existing parameter/Solid3D workflows in-place.
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
                new Action(panel.ApplyBlt3dFamilyWorkspace),
                DispatcherPriority.Loaded);
        }

        private void ApplyBlt3dFamilyWorkspace()
        {
            // Read the bootstrap flag deliberately: repositories building with warnings-as-errors
            // must not treat the registration field as an assigned-but-unused private field.
            if (!Blt3dFamilyWorkspaceBootstrapRegistered || _blt3dFamilyWorkspaceApplied) return;

            RestoreBlt3dWorkspaceColumns();
            EnsureBlt3dFoundationTree();

            // Attach the existing production family/subtype workflow first while the original
            // XAML labels are still present, then restyle/relabel and replace only the Add surface.
            AttachFamilySubtypeInteractions();
            ApplyBlt3dWorkspaceLabels();
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

            // BLT3D working layout: model tree | family/properties. The legacy room/inspection
            // dashboard remains retired so this does not reintroduce the old multi-panel bug.
            root.MinWidth = 520;
            workspace.MinWidth = 520;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            var modelColumn = workspace.ColumnDefinitions[0];
            modelColumn.MinWidth = 180;
            modelColumn.MaxWidth = 250;
            modelColumn.Width = new GridLength(220);

            var familySplitter = workspace.ColumnDefinitions[1];
            familySplitter.MinWidth = 0;
            familySplitter.MaxWidth = 4;
            familySplitter.Width = new GridLength(4);

            var familyColumn = workspace.ColumnDefinitions[2];
            familyColumn.MinWidth = 270;
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

        private void ApplyBlt3dWorkspaceLabels()
        {
            foreach (var text in FindVisualChildren<TextBlock>(this))
            {
                if (string.Equals(text.Text, "Zone", StringComparison.Ordinal))
                    text.Text = "Zone làm việc";
                else if (string.Equals(text.Text, "Tầng", StringComparison.Ordinal))
                    text.Text = "Tầng làm việc";
            }

            RenameBlt3dButton("+ Thêm", "+ Add");
            RenameBlt3dButton("＋  Add", "+ Add");
            RenameBlt3dButton("Xóa", "Delete");
            RenameBlt3dButton("Bóc chọn", "⚡ Nhập tự động");
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
