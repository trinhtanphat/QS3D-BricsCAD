using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI.ViewModels;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel
    {
        private static readonly bool XrefScaleClassHandlerRegistered = RegisterXrefScaleClassHandler();
        private bool _xrefScaleHooked;
        private bool _xrefScaleRefreshQueued;

        private static bool RegisterXrefScaleClassHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnXrefScaleLoaded),
                true);
            return true;
        }

        private static void OnXrefScaleLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is RightPanel panel)) return;
            panel.EnsureXrefScaleHook();
            panel.QueueXrefScaleRefresh();
        }

        private void EnsureXrefScaleHook()
        {
            _ = XrefScaleClassHandlerRegistered;
            if (_xrefScaleHooked) return;
            _viewModel.Drawings.CollectionChanged += OnDrawingScaleCollectionChanged;
            _xrefScaleHooked = true;
        }

        private void OnDrawingScaleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => QueueXrefScaleRefresh();

        private void QueueXrefScaleRefresh()
        {
            if (_xrefScaleRefreshQueued) return;
            _xrefScaleRefreshQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    _xrefScaleRefreshQueued = false;
                    ApplyXrefScaleState();
                }));
        }

        private void ApplyXrefScaleState()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            IReadOnlyDictionary<string, DrawingReferenceSnapshot> byName;
            try
            {
                byName = DrawingCatalogReader.ReadReferences(document)
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                foreach (var row in _viewModel.Drawings)
                    row.ScaleText = row.IsXref ? "—" : "1:1";
                return;
            }

            foreach (var row in _viewModel.Drawings)
            {
                if (!row.IsXref)
                {
                    row.ScaleText = "1:1";
                    continue;
                }

                row.ScaleText = byName.TryGetValue(row.Name, out var snapshot)
                    ? snapshot.ScaleText
                    : "—";
            }
        }
    }
}
