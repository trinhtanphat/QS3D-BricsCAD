using System;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Features;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Stable FeatureId attachment and selected-feature context publication for Workspace navigation.
    /// Legacy TreeViewItem.Tag remains available to ElementCategory consumers during migration.
    /// </summary>
    internal static class WorkspaceFeatureSelectionPublisher
    {
        private static readonly DependencyProperty FeatureIdProperty = DependencyProperty.RegisterAttached(
            "FeatureId",
            typeof(string),
            typeof(WorkspaceFeatureSelectionPublisher),
            new FrameworkPropertyMetadata(null));

        private static readonly object RegistrationGate = new object();
        private static bool _registered;

        public static event EventHandler<SelectedFeatureContext>? SelectedFeatureChanged;

        public static SelectedFeatureContext? Current { get; private set; }

        public static void Attach(TreeViewItem item, FeatureId featureId)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.SetValue(FeatureIdProperty, featureId.Value);
        }

        public static bool EnsureRegistered()
        {
            lock (RegistrationGate)
            {
                if (_registered) return true;
                EventManager.RegisterClassHandler(
                    typeof(TreeViewItem),
                    TreeViewItem.SelectedEvent,
                    new RoutedEventHandler(OnSelected),
                    true);
                _registered = true;
                return true;
            }
        }

        private static void OnSelected(object sender, RoutedEventArgs e)
        {
            if (!(sender is TreeViewItem item)) return;
            var raw = item.GetValue(FeatureIdProperty) as string;
            if (string.IsNullOrWhiteSpace(raw)) return;

            if (!WorkspaceFeatureNavigationCatalog.Navigation.TrySelect(new FeatureId(raw), out var context) || context == null)
                return;

            Current = context;
            SelectedFeatureChanged?.Invoke(item, context);
        }
    }
}
