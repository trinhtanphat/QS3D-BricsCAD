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

        private void ConfigureFamilyTemplateUiAndCatalog()
        {
            if (_familyTemplateCatalogConfigured) return;
            _familyTemplateCatalogConfigured = true;

            FamilyTemplateCombo.ItemsSource = new[] { StandardFamilyTemplateCatalog.VietnamStandard01Id };
            FamilyTemplateCombo.SelectedIndex = 0;

            _familyListItemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListView));
            _familyListItemsSourceDescriptor?.AddValueChanged(FamilyList, OnFamilyCatalogItemsSourceChanged);
            ApplyFamilyCatalogGrouping();
        }

        private void OnFamilyCatalogItemsSourceChanged(object? sender, EventArgs e) => ApplyFamilyCatalogGrouping();

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
                if (!string.Equals(FamilyTemplateCombo.SelectedItem as string, StandardFamilyTemplateCatalog.VietnamStandard01Id, StringComparison.Ordinal))
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

                var store = new TemplateProfileStore();
                var id = "QS3D_USER_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var profile = store.ExportProject(project, id, Path.GetFileNameWithoutExtension(dialog.FileName));
                store.Save(profile, dialog.FileName);
                SetStatus("Đã lưu Family Template: " + dialog.FileName);
            }
            catch (Exception ex)
            {
                SetStatus("Lưu Family Template lỗi: " + ex.Message);
            }
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
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.LayeredWall: return 3;
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
