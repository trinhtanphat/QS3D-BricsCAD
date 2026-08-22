using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QS3D.BricsCAD.V25.UI.ViewModels;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        static QuantityInsightPanel()
        {
            RegisterLocateSelectionFailureGuard();
        }

        private static void RegisterLocateSelectionFailureGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnInsightLocateButtonClassClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(TreeView),
                TreeView.SelectedItemChangedEvent,
                new RoutedPropertyChangedEventHandler<object>(OnInsightLocateSelectedItemChangedClass),
                true);
            EventManager.RegisterClassHandler(
                typeof(TreeView),
                Control.MouseDoubleClickEvent,
                new MouseButtonEventHandler(OnInsightLocateDoubleClickClass),
                true);
        }

        private static void OnInsightLocateButtonClassClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) ||
                !string.Equals(button.Content as string, "Định vị", StringComparison.Ordinal))
                return;

            var owner = FindInsightOwner(button);
            if (owner == null || !(owner.QuantityTree.SelectedItem is QuantityInsightItemViewModel)) return;
            owner.TryClearLocateSelectionForCurrentDocument();
        }

        private static void OnInsightLocateSelectedItemChangedClass(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(sender is TreeView tree)) return;
            var owner = FindInsightOwner(tree);
            if (owner == null || !ReferenceEquals(tree, owner.QuantityTree)) return;
            if (owner.AutoRevealCheck?.IsChecked != true || !(e.NewValue is QuantityInsightItemViewModel)) return;

            owner.TryClearLocateSelectionForCurrentDocument();
        }

        private static void OnInsightLocateDoubleClickClass(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is TreeView tree)) return;
            var owner = FindInsightOwner(tree);
            if (owner == null || !ReferenceEquals(tree, owner.QuantityTree)) return;
            if (owner.AutoRevealCheck?.IsChecked == true || !(tree.SelectedItem is QuantityInsightItemViewModel)) return;

            owner.TryClearLocateSelectionForCurrentDocument();
        }

        private static QuantityInsightPanel? FindInsightOwner(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is QuantityInsightPanel owner) return owner;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void TryClearLocateSelectionForCurrentDocument()
        {
            var document = _boundDocument;
            if (document == null || !ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, document)) return;
            try
            {
                Cad.CadHandleService.Select(document, Array.Empty<string>());
            }
            catch
            {
                // Best effort only: never mask the authoritative locate validation failure.
            }
        }
    }
}
