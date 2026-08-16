using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private static readonly bool BltProjectButtonRoutingRegistered = RegisterBltProjectButtonRouting();

        private static bool RegisterBltProjectButtonRouting()
        {
            // Button class handlers run before each Button's instance Click handler. This lets
            // the Project Setup shell override the legacy Project Properties route without
            // changing the XAML event contract used by older builds.
            EventManager.RegisterClassHandler(
                typeof(Button),
                ButtonBase.ClickEvent,
                new RoutedEventHandler(OnBltProjectButtonRoutedClick));
            return true;
        }

        private static void OnBltProjectButtonRoutedClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(Window.GetWindow(button) is FloorLevelWindow window))
                return;

            var label = NormalizeButtonLabel(button.Content);
            if (!string.Equals(label, "Thuộc tính dự án", StringComparison.CurrentCultureIgnoreCase))
                return;

            // The BLT3D Project Properties entry has its own bounded command/surface. The
            // legacy handler on FloorLevelWindow used Project Tools for both top-nav buttons,
            // which made two visibly different BLT3D entries perform the same action.
            e.Handled = true;
            window.OpenDedicatedBltProjectProperties();
        }

        private void OpenDedicatedBltProjectProperties()
        {
            try
            {
                EnsureBoundDrawingIsActive("mở Thuộc tính dự án");
                _document.SendStringToExecute("QS3DPROJECTPROPERTIES ", true, false, false);
                SetBltStatus("Đã gửi lệnh Thuộc tính dự án tới surface QS3DPROJECTPROPERTIES riêng.");
            }
            catch (Exception ex)
            {
                SetBltStatus("Thuộc tính dự án lỗi: " + ex.Message);
            }
        }
    }
}
