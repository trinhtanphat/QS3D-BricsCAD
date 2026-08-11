using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private void OnWorkspaceDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ApplyPropertyFilter));
        }

        private void OnPropertySearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPropertyFilter();
        }

        private void OnClearPropertySearchClick(object sender, RoutedEventArgs e)
        {
            if (PropertySearch == null) return;
            PropertySearch.Clear();
            PropertySearch.Focus();
        }

        private void ApplyPropertyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(PropertyList?.ItemsSource);
            if (view == null) return;

            var text = PropertySearch?.Text?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                return;
            }

            view.Filter = item =>
            {
                if (!(item is PropertyRowViewModel row)) return false;
                return Contains(row.Group, text) ||
                       Contains(row.Name, text) ||
                       Contains(row.Unit, text) ||
                       Contains(row.Value, text) ||
                       (row.IsReadOnly && Contains("CAD đọc khóa", text)) ||
                       (row.CanReset && Contains("Instance override", text));
            };
            view.Refresh();
        }

        private static bool Contains(string? value, string text)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}