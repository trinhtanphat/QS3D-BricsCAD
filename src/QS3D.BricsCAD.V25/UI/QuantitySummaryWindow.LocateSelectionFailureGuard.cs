using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using QS3D.Core.Reporting;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySummaryWindow
    {
        static QuantitySummaryWindow()
        {
            RegisterLocateSelectionFailureGuard();
        }

        private static void RegisterLocateSelectionFailureGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnSummaryLocateButtonClassClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(DataGrid),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnSummaryLocateSelectionChangedClass),
                true);
            EventManager.RegisterClassHandler(
                typeof(DataGrid),
                Control.MouseDoubleClickEvent,
                new MouseButtonEventHandler(OnSummaryLocateDoubleClickClass),
                true);
        }

        private static void OnSummaryLocateButtonClassClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) ||
                !string.Equals(button.Content as string, "Định vị", StringComparison.Ordinal))
                return;

            var owner = Window.GetWindow(button) as QuantitySummaryWindow;
            if (owner == null || !owner._initialized || !(owner.QuantityGrid.SelectedItem is QuantityReportRow)) return;
            owner.TryClearLocateSelectionForCurrentDocument();
        }

        private static void OnSummaryLocateSelectionChangedClass(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is DataGrid grid)) return;
            var owner = Window.GetWindow(grid) as QuantitySummaryWindow;
            if (owner == null || !owner._initialized || !ReferenceEquals(grid, owner.QuantityGrid)) return;
            if (!owner._detailMode || owner.AutoRevealCheck?.IsChecked != true ||
                !(grid.SelectedItem is QuantityReportRow) || e.AddedItems.Count == 0)
                return;

            owner.TryClearLocateSelectionForCurrentDocument();
        }

        private static void OnSummaryLocateDoubleClickClass(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is DataGrid grid)) return;
            var owner = Window.GetWindow(grid) as QuantitySummaryWindow;
            if (owner == null || !owner._initialized || !ReferenceEquals(grid, owner.QuantityGrid)) return;
            if (owner._detailMode && owner.AutoRevealCheck?.IsChecked == true) return;
            if (!(grid.SelectedItem is QuantityReportRow)) return;

            owner.TryClearLocateSelectionForCurrentDocument();
        }

        private void TryClearLocateSelectionForCurrentDocument()
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)) return;
            try
            {
                Cad.CadHandleService.Select(_document, Array.Empty<string>());
            }
            catch
            {
                // Best effort only: never mask the authoritative locate validation failure.
            }
        }
    }
}
