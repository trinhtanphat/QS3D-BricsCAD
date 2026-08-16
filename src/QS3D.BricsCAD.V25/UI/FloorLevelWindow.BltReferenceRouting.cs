using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private static readonly bool BltReferenceButtonRoutingRegistered = RegisterBltReferenceButtonRouting();

        private static bool RegisterBltReferenceButtonRouting()
        {
            // BLT3D renders the reference-floor selector as a CheckBox, but its behavior is
            // radio-like: while floors exist there must always be exactly one visible reference.
            // Own the routed Click before the legacy instance handler so clicking the currently
            // checked row cannot leave the grid in a transient zero-reference state.
            EventManager.RegisterClassHandler(
                typeof(CheckBox),
                ButtonBase.ClickEvent,
                new RoutedEventHandler(OnBltReferenceRoutedClick));
            return true;
        }

        private static void OnBltReferenceRoutedClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox) ||
                !(Window.GetWindow(checkBox) is FloorLevelWindow window) ||
                !(checkBox.Tag is BltFloorRow row))
                return;

            e.Handled = true;
            window.ApplyBltReferenceSelection(row, checkBox.IsChecked == true);
        }

        private void ApplyBltReferenceSelection(BltFloorRow row, bool requestedChecked)
        {
            if (_bltLoading) return;

            _bltLoading = true;
            try
            {
                // A click on the already-selected CheckBox reaches us after the TwoWay binding
                // has toggled it false. Re-applying exclusivity restores that row immediately;
                // a click on another row promotes it and clears every previous reference.
                foreach (var item in _bltFloors)
                    item.IsReference = ReferenceEquals(item, row);
            }
            finally { _bltLoading = false; }

            SetBltStatus(requestedChecked
                ? "Tầng tham chiếu: " + row.Name + "."
                : "Tầng tham chiếu phải luôn có một tầng được chọn; giữ nguyên “" + row.Name + "”.");
        }
    }
}
