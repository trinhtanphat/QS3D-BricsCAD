using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        static FloorLevelWindow()
        {
            // The BLT3D floor selector behaves like a radio choice even though the
            // reference column is rendered with CheckBox controls. The legacy click
            // handler already makes a newly checked row exclusive; this class handler
            // closes the inverse hole where clicking the active reference again could
            // leave the visible grid with no reference floor selected.
            EventManager.RegisterClassHandler(
                typeof(FloorLevelWindow),
                ButtonBase.ClickEvent,
                new RoutedEventHandler(OnBltReferenceRoutedClick),
                true);
        }

        private static void OnBltReferenceRoutedClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is FloorLevelWindow window) ||
                !(e.OriginalSource is CheckBox checkBox) ||
                !(checkBox.Tag is BltFloorRow row) ||
                !window.IsLoaded ||
                window.Dispatcher.HasShutdownStarted)
            {
                return;
            }

            // Run after the source CheckBox handler and TwoWay binding have settled so
            // the invariant observes the actual post-click state rather than the old one.
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (!window.IsLoaded || window.Dispatcher.HasShutdownStarted) return;
                    window.EnsureBltReferenceInvariant(row);
                }));
        }

        private void EnsureBltReferenceInvariant(BltFloorRow clickedRow)
        {
            if (_bltLoading || _bltFloors.Count == 0) return;

            var references = _bltFloors.Where(item => item.IsReference).ToList();
            if (references.Count == 1) return;

            var keeper = references.FirstOrDefault() ?? clickedRow ?? _bltFloors.First();
            _bltLoading = true;
            try
            {
                foreach (var item in _bltFloors)
                    item.IsReference = ReferenceEquals(item, keeper);
            }
            finally
            {
                _bltLoading = false;
            }

            if (references.Count == 0)
            {
                SetBltStatus("Tầng tham chiếu không thể bỏ chọn. Hãy chọn một tầng khác để đổi tầng tham chiếu.");
            }
            else
            {
                SetBltStatus("Đã đồng bộ lại lựa chọn tầng tham chiếu duy nhất.");
            }
        }
    }
}
