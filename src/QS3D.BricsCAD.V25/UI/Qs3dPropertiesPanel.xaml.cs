using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Dedicated QS3D Properties surface. Its DataContext is bound by PaletteCoordinator to the
    /// WorkspacePanel DataContext, so Family/Instance edits and live CAD selection remain backed by
    /// the existing WorkspaceViewModel rather than a copied or mocked property model.
    /// </summary>
    public partial class Qs3dPropertiesPanel : UserControl
    {
        private const int MaxPropertySearchTokens = 12;

        public Qs3dPropertiesPanel()
        {
            InitializeComponent();
        }

        private void OnPropertiesDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is WorkspaceViewModel viewModel)
            {
                var view = CollectionViewSource.GetDefaultView(viewModel.Properties);
                if (view != null && view.CanGroup &&
                    !view.GroupDescriptions.OfType<PropertyGroupDescription>()
                        .Any(group => string.Equals(group.PropertyName, nameof(PropertyRowViewModel.Group), StringComparison.Ordinal)))
                {
                    view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PropertyRowViewModel.Group)));
                }
            }

            Dispatcher.BeginInvoke(new Action(ApplyPropertyFilter));
        }

        private void OnPropertiesPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F)
            {
                PropertySearch.Focus();
                PropertySearch.SelectAll();
                e.Handled = true;
                return;
            }

            if (modifiers == ModifierKeys.None && e.Key == Key.Enter && PropertyList.IsKeyboardFocusWithin)
            {
                var source = e.OriginalSource as DependencyObject;
                var combo = FindAncestor<ComboBox>(source);
                if (combo != null && combo.IsEditable)
                {
                    if (combo.IsDropDownOpen) return;
                    combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
                    e.Handled = true;
                    return;
                }

                var textBox = FindAncestor<TextBox>(source);
                if (textBox != null)
                {
                    textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                    e.Handled = true;
                    return;
                }
            }

            if (modifiers == ModifierKeys.None && e.Key == Key.Escape &&
                PropertySearch.IsKeyboardFocusWithin && !string.IsNullOrEmpty(PropertySearch.Text))
            {
                PropertySearch.Clear();
                e.Handled = true;
            }
        }

        private void OnPropertySearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPropertyFilter();
        }

        private void OnClearPropertySearchClick(object sender, RoutedEventArgs e)
        {
            PropertySearch.Clear();
            PropertySearch.Focus();
        }

        private void OnResetPropertyClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PropertyRowViewModel row)
                row.ResetValue();
        }

        private void ApplyPropertyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(PropertyList.ItemsSource);
            if (view == null) return;

            var text = PropertySearch.Text?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                return;
            }

            var tokens = text
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Where(token => token.Length > 0)
                .Take(MaxPropertySearchTokens)
                .ToArray();
            if (tokens.Length == 0)
            {
                view.Filter = null;
                view.Refresh();
                return;
            }

            view.Filter = item =>
            {
                if (!(item is PropertyRowViewModel row)) return false;
                return tokens.All(token => MatchesPropertyToken(row, token));
            };
            view.Refresh();
        }

        private static bool MatchesPropertyToken(PropertyRowViewModel row, string token)
        {
            return Contains(row.Group, token) ||
                   Contains(row.Name, token) ||
                   Contains(row.Unit, token) ||
                   Contains(row.Value, token) ||
                   Contains(row.EditorKind, token) ||
                   Contains(row.StateLabel, token) ||
                   Contains(row.StateSearchText, token) ||
                   row.Choices.Any(choice => Contains(choice, token));
        }

        private static bool Contains(string? value, string text)
        {
            if (value == null || value.Length == 0) return false;
            return value.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
        {
            var current = source;
            while (current != null)
            {
                if (current is T typed) return typed;
                if (current is ContentElement content)
                    current = ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent;
                else
                    current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
