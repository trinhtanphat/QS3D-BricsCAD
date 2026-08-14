using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only completion of the reference category tree. Existing semantic tags are
    /// reused so the extra labels route through the same canonical Family/Type selection logic.
    /// No project state is mutated by this augmenter.
    /// </summary>
    internal static class ReferenceWorkspaceTreeAugmenter
    {
        private static readonly object RegistrationGate = new object();
        private static bool _registered;

        public static bool EnsureRegistered()
        {
            lock (RegistrationGate)
            {
                if (_registered) return true;
                try
                {
                    EventManager.RegisterClassHandler(
                        typeof(WorkspacePanel),
                        FrameworkElement.LoadedEvent,
                        new RoutedEventHandler(OnWorkspaceLoaded),
                        true);
                    _registered = true;
                    return true;
                }
                catch
                {
                    // This augmenter is presentation-only. A transient WPF registration failure must
                    // not poison WorkspacePanel type initialization; leave the latch clear so a later
                    // caller can retry the exact same class-handler registration.
                    return false;
                }
            }
        }

        private static void OnWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || panel.ModelTree == null) return;
            EnsureReferenceTree(panel.ModelTree);
        }

        private static void EnsureReferenceTree(TreeView tree)
        {
            var grid = EnsureTop(tree, "Lưới Trục", "Grid");
            EnsureChild(grid, "Lưới Thẳng", "Grid");
            EnsureChild(grid, "Lưới Cong", "Grid");

            var finish = EnsureTop(tree, "HT_Phòng", null);
            EnsureChild(finish, "Phòng", "Room");
            EnsureChild(finish, "Sàn Hoàn Thiện", "FloorFinish");
            EnsureChild(finish, "Chống Thấm", "Waterproofing");
            EnsureChild(finish, "Chân Tường", "Skirting");
            EnsureChild(finish, "Hoàn Thiện Tường", "WallFinish");
            EnsureChild(finish, "Trần Hoàn Thiện", "CeilingFinish");
            EnsureChild(finish, "Trát Trần", "CeilingFinish");
            EnsureChild(finish, "Lan Can", "Railing");

            var beam = EnsureTop(tree, "Dầm", "Beam");
            EnsureChild(beam, "Dầm HCN", "Beam");
            EnsureChild(beam, "Giằng Tường", "Beam");
            EnsureChild(beam, "Lanh Tô", "Beam");

            var slab = EnsureTop(tree, "Sàn", "Slab");
            EnsureChild(slab, "Sàn Đặc", "Slab");
            EnsureChild(slab, "Đường Dốc", "Slab");
            EnsureChild(slab, "Lỗ Mở Sàn", "Slab");

            var canopy = EnsureTop(tree, "Mái Hắt", null);
            EnsureChild(canopy, "Mái Hắt Diện Tích", "Slab");
            EnsureChild(canopy, "Mái Hắt Biên Dạng", "Slab");

            var column = EnsureTop(tree, "Cột", "Column");
            EnsureChild(column, "Cột", "Column");

            var structuralWall = EnsureTop(tree, "Vách", "StructuralWall");
            EnsureChild(structuralWall, "Vách BTCT", "StructuralWall");

            var architecture = EnsureTop(tree, "Tường KT", null);
            EnsureChild(architecture, "Tường Gạch", "ArchitecturalWall");
            EnsureChild(architecture, "Vách Kính", "GlassWall");
            EnsureChild(architecture, "Trụ Tường", "WallPier");

            var opening = EnsureTop(tree, "Cửa", null);
            EnsureChild(opening, "Lỗ Mở Vách", "WallOpening");
            EnsureChild(opening, "Cửa Đi", "Door");

            var stair = EnsureTop(tree, "Cầu Thang", "Stair");
            EnsureChild(stair, "Cầu Thang", "Stair");

            var foundation = EnsureTop(tree, "Móng", "Foundation");
            EnsureChild(foundation, "Cọc", "Foundation");
            EnsureChild(foundation, "Đài Cọc", "Foundation");
            EnsureChild(foundation, "Dầm Móng", "Foundation");
            EnsureChild(foundation, "Móng Băng", "Foundation");
            EnsureChild(foundation, "Móng Bè", "Foundation");
            EnsureChild(foundation, "Bê Tông Lót", "Foundation");

            var earthwork = EnsureTop(tree, "Đào đắp", "Earthwork");
            EnsureChild(earthwork, "Đào đắp hố móng", "Earthwork");
            EnsureChild(earthwork, "Khối Đất", "Earthwork");
            EnsureChild(earthwork, "Khối giao đào", "Earthwork");
            EnsureChild(earthwork, "Khối đất sau trừ", "Earthwork");

            var custom = EnsureTop(tree, "KL Tùy chỉnh", "CustomQuantity");
            EnsureChild(custom, "KL Chiều dài", "CustomQuantity");
            EnsureChild(custom, "KL Diện tích", "CustomQuantity");
            EnsureChild(custom, "KL Thể tích", "CustomQuantity");
            EnsureChild(custom, "KL Biên dạng", "CustomQuantity");
            EnsureChild(custom, "KL Mặt phẳng", "CustomQuantity");

            EnsureTop(tree, "Modeling", null);
        }

        private static TreeViewItem EnsureTop(TreeView tree, string header, string? tag)
        {
            var item = tree.Items.OfType<TreeViewItem>()
                .FirstOrDefault(candidate => HeaderEquals(candidate, header));
            if (item == null)
            {
                item = NewItem(header, tag);
                tree.Items.Add(item);
            }
            else if (item.Tag == null && !string.IsNullOrWhiteSpace(tag))
            {
                item.Tag = tag;
            }
            return item;
        }

        private static void EnsureChild(TreeViewItem parent, string header, string tag)
        {
            var child = parent.Items.OfType<TreeViewItem>()
                .FirstOrDefault(candidate => HeaderEquals(candidate, header));
            if (child == null)
            {
                parent.Items.Add(NewItem(header, tag));
            }
            else if (child.Tag == null)
            {
                child.Tag = tag;
            }
        }

        private static TreeViewItem NewItem(string header, string? tag) =>
            new TreeViewItem { Header = header, Tag = tag };

        private static bool HeaderEquals(TreeViewItem item, string expected) =>
            string.Equals(item.Header as string, expected, StringComparison.OrdinalIgnoreCase);
    }
}
