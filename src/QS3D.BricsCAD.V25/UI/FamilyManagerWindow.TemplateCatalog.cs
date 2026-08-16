using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FamilyManagerWindow
    {
        private bool _familyTemplateCatalogConfigured;
        private DependencyPropertyDescriptor? _familyListItemsSourceDescriptor;
        private ComboBox? _familyTemplateCombo;

        private void ConfigureFamilyTemplateUiAndCatalog()
        {
            if (_familyTemplateCatalogConfigured) return;
            _familyTemplateCatalogConfigured = true;

            InstallFamilyTemplatePanel();
            InstallFamilyCatalogGroupStyle();
            _familyListItemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListView));
            _familyListItemsSourceDescriptor?.AddValueChanged(FamilyList, OnFamilyCatalogItemsSourceChanged);
            ApplyFamilyCatalogGrouping();
        }

        private void InstallFamilyTemplatePanel()
        {
            if (!(QsQuickWorkflowCard.Parent is StackPanel rightPanel)) return;

            var card = new Border { Margin = new Thickness(0, 0, 0, 10) };
            if (TryFindResource("ManagerCard") is Style cardStyle) card.Style = cardStyle;
            var stack = new StackPanel();
            card.Child = stack;

            var title = new TextBlock { Text = "FAMILY TEMPLATE" };
            if (TryFindResource("PanelTitle") is Style titleStyle) title.Style = titleStyle;
            stack.Children.Add(title);

            var row = new Grid { Margin = new Thickness(0, 2, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _familyTemplateCombo = new ComboBox
            {
                MinWidth = 220,
                Margin = new Thickness(0, 0, 8, 0),
                ItemsSource = new[] { StandardFamilyTemplateCatalog.VietnamStandard01Id },
                SelectedIndex = 0,
                ToolTip = "Thư viện Family chuẩn dùng chung giữa các project"
            };
            Grid.SetColumn(_familyTemplateCombo, 0);
            row.Children.Add(_familyTemplateCombo);

            var loadButton = new Button { Content = "Nạp Template", Margin = new Thickness(0, 0, 6, 0) };
            if (TryFindResource("AccentButton") is Style accentStyle) loadButton.Style = accentStyle;
            loadButton.Click += OnLoadFamilyTemplateClick;
            Grid.SetColumn(loadButton, 1);
            row.Children.Add(loadButton);

            var loadFileButton = new Button { Content = "Nạp file…", Margin = new Thickness(0, 0, 6, 0) };
            if (TryFindResource("DenseButton") is Style loadFileStyle) loadFileButton.Style = loadFileStyle;
            loadFileButton.Click += OnLoadFamilyTemplateFileClick;
            Grid.SetColumn(loadFileButton, 2);
            row.Children.Add(loadFileButton);

            var saveButton = new Button { Content = "Lưu Template" };
            if (TryFindResource("DenseButton") is Style denseStyle) saveButton.Style = denseStyle;
            saveButton.Click += OnSaveFamilyTemplateClick;
            Grid.SetColumn(saveButton, 3);
            row.Children.Add(saveButton);
            stack.Children.Add(row);

            var hint = new TextBlock
            {
                Text = "Nạp template chuẩn hoặc file .qs3dtpl Family-only. Import khớp Category + Name, giữ Family ID/custom property của project và không áp dụng rule/layer/BQ layout từ file.",
                TextWrapping = TextWrapping.Wrap
            };
            if (TryFindResource("Caption") is Style captionStyle) hint.Style = captionStyle;
            stack.Children.Add(hint);

            rightPanel.Children.Insert(0, card);
        }

        private void InstallFamilyCatalogGroupStyle()
        {
            FamilyList.GroupStyle.Clear();
            var groupStyle = new GroupStyle();
            var containerStyle = new Style(typeof(GroupItem));
            var template = new ControlTemplate(typeof(GroupItem));
            var expander = new FrameworkElementFactory(typeof(Expander));
            expander.SetValue(Expander.IsExpandedProperty, true);
            expander.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));
            expander.SetBinding(Expander.HeaderProperty, new Binding("Name"));
            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            expander.AppendChild(itemsPresenter);
            template.VisualTree = expander;
            containerStyle.Setters.Add(new Setter(Control.TemplateProperty, template));
            groupStyle.ContainerStyle = containerStyle;
            FamilyList.GroupStyle.Add(groupStyle);
        }

        private void OnFamilyCatalogItemsSourceChanged(object? sender, EventArgs e)
        {
            ApplyFamilyCatalogGrouping();

            // RefreshAll rebinds ItemsSource and selects the preferred Family while _loading is true,
            // intentionally suppressing SelectionChanged handlers. Defer one Quick Form refresh until
            // that synchronous rebind is complete so dimensions/material never remain from the old Family.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_loading) RefreshQuickWorkflow();
            }));
        }

        private void ApplyFamilyCatalogGrouping()
        {
            if (FamilyList.ItemsSource == null) return;
            var view = CollectionViewSource.GetDefaultView(FamilyList.ItemsSource);
            if (view == null || !view.CanGroup) return;

            using (view.DeferRefresh())
            {
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ProjectFamily.Category), FamilyCategoryGroupConverter.Instance));
                if (view is ListCollectionView listView)
                    listView.CustomSort = FamilyCatalogComparer.Instance;
            }
        }

        private void OnLoadFamilyTemplateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("nạp QS3D Family Template");
                if (!string.Equals(_familyTemplateCombo?.SelectedItem as string, StandardFamilyTemplateCatalog.VietnamStandard01Id, StringComparison.Ordinal))
                    throw new InvalidOperationException("Chọn QS3D Family Template hợp lệ trước khi nạp.");

                var project = ExistingProjectMutationContext.Require(_document, "Nạp QS3D Family Template");
                var previousId = (FamilyList.SelectedItem as ProjectFamily)?.Id ?? string.Empty;
                var result = ExecuteAtomic(
                    project,
                    () => StandardFamilyTemplateCatalog.ApplyVietnamStandard01(project),
                    "Nạp QS3D Family Template");

                var preferredId = previousId;
                if (string.IsNullOrWhiteSpace(preferredId) || project.FindFamily(preferredId) == null)
                {
                    preferredId = project.Families
                        .FirstOrDefault(x => x.Category == ElementCategory.ArchitecturalWall && string.Equals(x.Name, "Tường Gạch 200", StringComparison.OrdinalIgnoreCase))?.Id
                        ?? string.Empty;
                }

                RefreshAfterCommit(
                    () => RefreshAll(preferredId),
                    "Đã nạp " + StandardFamilyTemplateCatalog.VietnamStandard01Id +
                    " • thêm " + result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) +
                    " Family • cập nhật " + result.FamiliesUpdated.ToString(CultureInfo.InvariantCulture) +
                    " Family • áp dụng " + result.PropertiesApplied.ToString(CultureInfo.InvariantCulture) + " property.",
                    "Family template load");
            }
            catch (Exception ex)
            {
                SetStatus("Nạp Family Template lỗi: " + ex.Message);
            }
        }

        private void OnLoadFamilyTemplateFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("nạp file QS3D Family Template");
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Family Template",
                    Filter = "QS3D Template (*.qs3dtpl)|*.qs3dtpl|All files (*.*)|*.*",
                    DefaultExt = ".qs3dtpl",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog(this) != true) return;

                var store = new TemplateProfileStore();
                var profile = store.Load(dialog.FileName);
                var project = ExistingProjectMutationContext.Require(_document, "Nạp file QS3D Family Template");
                var previousId = (FamilyList.SelectedItem as ProjectFamily)?.Id ?? string.Empty;
                var result = ExecuteAtomic(
                    project,
                    () => FamilyTemplateImportService.Apply(project, profile),
                    "Nạp file QS3D Family Template");

                var preferredId = previousId;
                if (string.IsNullOrWhiteSpace(preferredId) || project.FindFamily(preferredId) == null)
                {
                    var firstSource = profile.Families.FirstOrDefault();
                    if (firstSource != null)
                    {
                        preferredId = project.Families
                            .FirstOrDefault(x => x.Category == firstSource.Category &&
                                                 string.Equals(x.Name, firstSource.Name, StringComparison.OrdinalIgnoreCase))?.Id
                            ?? string.Empty;
                    }
                }

                var ignoredSections = profile.QuantityRules.Count + profile.LayerMappings.Count + profile.VisibleBqColumns.Count;
                RefreshAfterCommit(
                    () => RefreshAll(preferredId),
                    "Đã nạp Family-only “" + profile.Name + "” • thêm " +
                    result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) + " Family • cập nhật " +
                    result.FamiliesUpdated.ToString(CultureInfo.InvariantCulture) + " Family • áp dụng " +
                    result.PropertiesApplied.ToString(CultureInfo.InvariantCulture) + " property • bỏ qua " +
                    ignoredSections.ToString(CultureInfo.InvariantCulture) + " mục rule/layer/BQ ngoài phạm vi Family.",
                    "Family template file import");
            }
            catch (Exception ex)
            {
                SetStatus("Nạp file Family Template lỗi: " + ex.Message);
            }
        }

        private void OnSaveFamilyTemplateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    throw new InvalidOperationException("Cần một QS3D project hiện hữu để lưu Family Template.");

                var dialog = new SaveFileDialog
                {
                    Title = "Lưu QS3D Family Template",
                    Filter = "QS3D Template (*.qs3dtpl)|*.qs3dtpl|All files (*.*)|*.*",
                    DefaultExt = ".qs3dtpl",
                    AddExtension = true,
                    FileName = "QS3D_FAMILY_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".qs3dtpl"
                };
                if (dialog.ShowDialog(this) != true) return;

                var id = "QS3D_USER_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var profile = CreateFamilyOnlyTemplateProfile(project, id, Path.GetFileNameWithoutExtension(dialog.FileName));
                new TemplateProfileStore().Save(profile, dialog.FileName);
                SetStatus(
                    "Đã lưu Family Template • " + profile.Families.Count.ToString(CultureInfo.InvariantCulture) +
                    " Family • không kèm rule/layer mapping/BQ layout của project: " + dialog.FileName);
            }
            catch (Exception ex)
            {
                SetStatus("Lưu Family Template lỗi: " + ex.Message);
            }
        }

        private static TemplateProfile CreateFamilyOnlyTemplateProfile(ProjectState project, string id, string name)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var profile = new TemplateProfile(id, name);
            foreach (var family in project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var copy = new ProjectFamily(family.Id, family.Name, family.Category);
                foreach (var property in family.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    copy.Properties[property.Key] = property.Value;
                profile.Families.Add(copy);
            }
            return profile;
        }

        private sealed class FamilyCategoryGroupConverter : IValueConverter
        {
            internal static readonly FamilyCategoryGroupConverter Instance = new FamilyCategoryGroupConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is ElementCategory category)) return "KHÁC";
                return GroupLabel(category);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
        }

        private sealed class FamilyCatalogComparer : IComparer
        {
            internal static readonly FamilyCatalogComparer Instance = new FamilyCatalogComparer();

            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (!(x is ProjectFamily left)) return -1;
                if (!(y is ProjectFamily right)) return 1;
                var group = GroupOrder(left.Category).CompareTo(GroupOrder(right.Category));
                if (group != 0) return group;
                var category = left.Category.CompareTo(right.Category);
                if (category != 0) return category;
                return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
            }
        }

        private static int GroupOrder(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam: return 0;
                case ElementCategory.Slab: return 1;
                case ElementCategory.Column: return 2;
                case ElementCategory.StructuralWall:
                case ElementCategory.ArchitecturalWall: return 3;
                case ElementCategory.Foundation: return 4;
                case ElementCategory.FloorFinish:
                case ElementCategory.WallFinish:
                case ElementCategory.Skirting:
                case ElementCategory.Waterproofing:
                case ElementCategory.CeilingFinish: return 5;
                default: return 6;
            }
        }

        private static string GroupLabel(ElementCategory category)
        {
            switch (GroupOrder(category))
            {
                case 0: return "DẦM";
                case 1: return "SÀN";
                case 2: return "CỘT";
                case 3: return "TƯỜNG";
                case 4: return "MÓNG";
                case 5: return "HOÀN THIỆN";
                default: return "KHÁC";
            }
        }
    }
}
