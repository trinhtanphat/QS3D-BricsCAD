using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const int MaxPropertySearchTokens = 12;

        private void OnWorkspaceDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PreviewKeyDown -= OnPropertyFilterShortcut;
            PreviewKeyDown += OnPropertyFilterShortcut;
            Dispatcher.BeginInvoke(new Action(ApplyPropertyFilter));
        }

        private void OnPropertyFilterShortcut(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F)
            {
                PropertySearch?.Focus();
                PropertySearch?.SelectAll();
                e.Handled = true;
                return;
            }

            if (modifiers == ModifierKeys.None && e.Key == Key.Enter &&
                PropertyList != null && PropertyList.IsKeyboardFocusWithin)
            {
                var source = e.OriginalSource as DependencyObject;
                var combo = FindPropertyEditorAncestor<ComboBox>(source);
                if (combo != null && combo.IsEditable)
                {
                    if (combo.IsDropDownOpen) return;
                    combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
                    e.Handled = true;
                    return;
                }

                var textBox = FindPropertyEditorAncestor<TextBox>(source);
                if (textBox != null)
                {
                    textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                    e.Handled = true;
                    return;
                }
            }

            if (modifiers == ModifierKeys.None && e.Key == Key.Escape &&
                PropertySearch != null && PropertySearch.IsKeyboardFocusWithin &&
                !string.IsNullOrEmpty(PropertySearch.Text))
            {
                PropertySearch.Clear();
                e.Handled = true;
            }
        }

        private static T? FindPropertyEditorAncestor<T>(DependencyObject? source) where T : DependencyObject
        {
            var current = source;
            while (current != null)
            {
                if (current is T typed) return typed;
                current = ParentOf(current);
            }
            return null;
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

            var tokens = text
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
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
    }
}
