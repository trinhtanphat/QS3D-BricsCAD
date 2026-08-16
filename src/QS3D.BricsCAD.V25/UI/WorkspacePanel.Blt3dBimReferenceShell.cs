using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Final owner-reference polish for the MÔ HÌNH BIM workspace. The underlying controls and
    /// handlers stay production QS3D/BricsCAD controls; this layer only aligns the visible shell
    /// with the BLT3D reference after the compact and Family workspace passes have completed.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dBimReferenceShellRegistered = RegisterBlt3dBimReferenceShell();
        private bool _blt3dBimReferenceShellApplied;

        private static bool RegisterBlt3dBimReferenceShell()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dBimReferenceShellLoaded),
                true);
            return true;
        }

        private static void OnBlt3dBimReferenceShellLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.ApplyBlt3dBimReferenceShell));
        }

        private void ApplyBlt3dBimReferenceShell()
        {
            if (!Blt3dBimReferenceShellRegistered || _blt3dBimReferenceShellApplied) return;

            ApplyReferenceActionLabels();
            EnsureReferenceModelCategories();
            ApplyReferenceFooter();
            _blt3dBimReferenceShellApplied = true;
        }

        private void ApplyReferenceActionLabels()
        {
            // BLT3D calls this action "Nhập tự động". QS3D intentionally keeps the existing
            // guarded capture handler: it consumes the current CAD selection and never performs
            // an unbounded/background drawing scan.
            var import = FindButton("⚡ Nhập từ chọn") ?? FindButton("Bóc chọn");
            if (import != null)
            {
                import.Content = "⚡ Nhập tự động";
                import.ToolTip = "Nhập tự động từ selection CAD hiện tại vào đúng nhóm/Family đang làm việc; không quét nền toàn DWG.";
            }

            var add = FindButton("+ Add") ?? FindButton("+ Thêm");
            if (add != null)
            {
                add.Content = "+ Add";
                add.ToolTip = "Thêm hoặc nhân bản Family / Type theo nhóm mô hình đang chọn.";
            }

            var delete = FindButton("Delete") ?? FindButton("Xóa");
            if (delete != null)
            {
                delete.Content = "Delete";
                delete.ToolTip = "Xóa Family / Type đang chọn khi không còn cấu kiện sử dụng.";
            }
        }

        private void EnsureReferenceModelCategories()
        {
            // Reuse the generic category already supported by the core model instead of inventing
            // a second unsupported semantic type only for UI parity.
            var generic = ModelTree.Items.OfType<TreeViewItem>()
                .FirstOrDefault(item =>
                    string.Equals(item.Tag as string, ElementCategory.CustomQuantity.ToString(), StringComparison.OrdinalIgnoreCase));
            if (generic != null)
            {
                generic.Header = "Cấu kiện khác";
                generic.Tag = ElementCategory.CustomQuantity.ToString();
                generic.ToolTip = "Nhóm cấu kiện tổng quát dùng CustomQuantity cho các loại chưa có category chuyên biệt.";
            }
            else
            {
                generic = EnsureReferenceCategory(
                    "Cấu kiện khác",
                    ElementCategory.CustomQuantity,
                    "Nhóm cấu kiện tổng quát dùng CustomQuantity cho các loại chưa có category chuyên biệt.");
            }

            var steel = ModelTree.Items.OfType<TreeViewItem>()
                .FirstOrDefault(item => string.Equals(item.Header as string, "Kết cấu thép", StringComparison.OrdinalIgnoreCase));
            if (steel == null)
            {
                steel = new TreeViewItem
                {
                    Header = "Kết cấu thép",
                    Tag = ElementCategory.CustomQuantity.ToString(),
                    ToolTip = "Nhóm tương thích BLT3D; dùng CustomQuantity cho tới khi QS3D có steel builder chuyên biệt, tránh giả lập native BIM semantics.",
                    MinHeight = 22,
                    Padding = new Thickness(3, 1, 2, 1),
                    Margin = new Thickness(0)
                };
                var genericIndex = ModelTree.Items.IndexOf(generic);
                if (genericIndex >= 0) ModelTree.Items.Insert(genericIndex, steel);
                else ModelTree.Items.Add(steel);
            }
        }

        private TreeViewItem EnsureReferenceCategory(string header, ElementCategory category, string toolTip)
        {
            var existing = ModelTree.Items.OfType<TreeViewItem>()
                .FirstOrDefault(item => string.Equals(item.Header as string, header, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Tag = category.ToString();
                existing.ToolTip = toolTip;
                return existing;
            }

            var item = new TreeViewItem
            {
                Header = header,
                Tag = category.ToString(),
                ToolTip = toolTip,
                MinHeight = 22,
                Padding = new Thickness(3, 1, 2, 1),
                Margin = new Thickness(0)
            };
            ModelTree.Items.Add(item);
            return item;
        }

        private void ApplyReferenceFooter()
        {
            // The real BricsCAD viewport remains the centre preview. Make that contract explicit in
            // the workspace footer while retaining all existing native view-aid toggles and commands.
            foreach (var text in FindVisualChildren<TextBlock>(this))
            {
                if (string.Equals(text.Text, "VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK", StringComparison.Ordinal))
                {
                    text.Text = "BLT3D • VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK";
                    break;
                }
            }
        }
    }
}
