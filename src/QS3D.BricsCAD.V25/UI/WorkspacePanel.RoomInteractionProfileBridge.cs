using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Domain;
using QS3D.Core.Features;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private static readonly bool RoomInteractionProfileBridgeRegistered = RegisterRoomInteractionProfileBridge();
        private readonly InteractionSurfaceCoordinator _roomInteractionSurfaceCoordinator = new InteractionSurfaceCoordinator();
        private bool _roomInteractionProfileBridgeHooksApplied;

        internal InteractionSurfaceSnapshot RoomInteractionSurfaceSnapshot => _roomInteractionSurfaceCoordinator.Snapshot;

        private static bool RegisterRoomInteractionProfileBridge()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRoomInteractionProfileBridgeLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnRoomInteractionProfileBridgeUnloaded),
                true);
            return true;
        }

        private static void OnRoomInteractionProfileBridgeLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !RoomInteractionProfileBridgeRegistered) return;
            panel.EnsureRoomInteractionProfileBridgeHooks();
            panel.RefreshRoomInteractionProfileBridge();
        }

        private static void OnRoomInteractionProfileBridgeUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.UnwireRoomInteractionProfileBridgeHooks();
            panel._roomInteractionSurfaceCoordinator.ClearSelection();
        }

        private void EnsureRoomInteractionProfileBridgeHooks()
        {
            if (_roomInteractionProfileBridgeHooksApplied) return;
            ModelTree.SelectedItemChanged += OnRoomInteractionProfileBridgeTreeSelectionChanged;
            FamilyList.SelectionChanged += OnRoomInteractionProfileBridgeFamilySelectionChanged;
            _roomInteractionProfileBridgeHooksApplied = true;
        }

        private void UnwireRoomInteractionProfileBridgeHooks()
        {
            if (!_roomInteractionProfileBridgeHooksApplied) return;
            try { ModelTree.SelectedItemChanged -= OnRoomInteractionProfileBridgeTreeSelectionChanged; } catch { }
            try { FamilyList.SelectionChanged -= OnRoomInteractionProfileBridgeFamilySelectionChanged; } catch { }
            _roomInteractionProfileBridgeHooksApplied = false;
        }

        private void OnRoomInteractionProfileBridgeTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
            => RefreshRoomInteractionProfileBridge();

        private void OnRoomInteractionProfileBridgeFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
            => RefreshRoomInteractionProfileBridge();

        private void RefreshRoomInteractionProfileBridge()
        {
            if (!IsRoomInteractionContext())
            {
                if (_roomInteractionSurfaceCoordinator.SelectedFeature?.Id == RoomInteractionProfile.RoomId)
                    _roomInteractionSurfaceCoordinator.ClearSelection();
                return;
            }

            var selectedRoom = FamilyList.SelectedItem as ProjectFamily;
            if (selectedRoom != null && selectedRoom.Category != ElementCategory.Room)
                selectedRoom = null;

            RoomInteractionProfile.SelectAndBindInspectors(
                _roomInteractionSurfaceCoordinator,
                selectedRoom?.Id);
        }

        private bool IsRoomInteractionContext()
        {
            if (_categoryFilter == ElementCategory.Room) return true;
            if (!(ModelTree.SelectedItem is TreeViewItem item) || !(item.Tag is string tag)) return false;
            return Enum.TryParse(tag, true, out ElementCategory category) && category == ElementCategory.Room;
        }
    }
}
