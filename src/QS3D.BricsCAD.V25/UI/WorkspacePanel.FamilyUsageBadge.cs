using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private static readonly bool FamilyUsageClassHandlerRegistered = RegisterFamilyUsageClassHandler();
        private static readonly DependencyProperty FamilyUsageUpgradedProperty = DependencyProperty.RegisterAttached(
            "FamilyUsageUpgraded",
            typeof(bool),
            typeof(WorkspacePanel),
            new PropertyMetadata(false));

        private bool _familyUsageHooksApplied;
        private readonly FamilyUsageTextConverter _familyUsageConverter = new FamilyUsageTextConverter();

        private static bool RegisterFamilyUsageClassHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnFamilyUsageLoaded),
                true);
            return true;
        }

        private static void OnFamilyUsageLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.EnsureFamilyUsageHooks();
            panel.UpgradeFamilyUsageBadges();
        }

        private void EnsureFamilyUsageHooks()
        {
            _ = FamilyUsageClassHandlerRegistered;
            if (_familyUsageHooksApplied || FamilyList == null) return;
            FamilyList.ItemContainerGenerator.StatusChanged += OnFamilyUsageGeneratorStatusChanged;
            FamilyList.LayoutUpdated += OnFamilyUsageLayoutUpdated;
            _familyUsageHooksApplied = true;
        }

        private void OnFamilyUsageGeneratorStatusChanged(object? sender, EventArgs e)
        {
            if (FamilyList.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                UpgradeFamilyUsageBadges();
        }

        private void OnFamilyUsageLayoutUpdated(object? sender, EventArgs e) => UpgradeFamilyUsageBadges();

        private void UpgradeFamilyUsageBadges()
        {
            if (FamilyList == null) return;
            foreach (var item in FamilyList.Items)
            {
                var container = FamilyList.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container == null) continue;
                foreach (var textBlock in Descendants<TextBlock>(container))
                {
                    if ((bool)textBlock.GetValue(FamilyUsageUpgradedProperty)) continue;
                    var binding = BindingOperations.GetBinding(textBlock, TextBlock.TextProperty);
                    if (!string.Equals(binding?.Path?.Path, "Properties.Count", StringComparison.Ordinal)) continue;

                    var usageBinding = new MultiBinding
                    {
                        Converter = _familyUsageConverter,
                        Mode = BindingMode.OneWay
                    };
                    usageBinding.Bindings.Add(new Binding());
                    usageBinding.Bindings.Add(new Binding("DataContext.Status")
                    {
                        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBox), 1),
                        Mode = BindingMode.OneWay
                    });
                    BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, usageBinding);
                    textBlock.SetValue(FamilyUsageUpgradedProperty, true);
                    textBlock.ToolTip = "Số cấu kiện semantic hiện đang dùng Family / Type này";
                }
            }
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < count; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed) yield return typed;
                foreach (var nested in Descendants<T>(child)) yield return nested;
            }
        }
    }
}
