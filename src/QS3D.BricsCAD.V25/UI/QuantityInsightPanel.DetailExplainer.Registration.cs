using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private static readonly bool _quantityDetailExplainerRegistered = RegisterQuantityDetailExplainer();
        private bool _quantityDetailExplainerInstalled;
        private Border? _quantityDetailCard;
        private ComboBox? _quantityDetailSelector;
        private TextBlock? _quantityDetailEmptyHint;
        private TextBlock? _quantityDetailContext;
        private TextBlock? _quantityDetailCount;
        private TextBlock? _quantityDetailElementIds;
        private TextBlock? _quantityDetailSourceHandles;
        private TextBlock? _quantityDetailDrawingFingerprint;
        private Button? _quantityDetailLocateButton;
        private StackPanel? _quantityDetailBody;
        private readonly Dictionary<string, TextBlock> _quantityDetailValues = new Dictionary<string, TextBlock>(StringComparer.Ordinal);
        private IReadOnlyList<QuantityInsightDetailOption> _quantityDetailOptions = Array.Empty<QuantityInsightDetailOption>();

        private static bool RegisterQuantityDetailExplainer()
        {
            EventManager.RegisterClassHandler(typeof(QuantityInsightPanel), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantityDetailExplainerLoaded), true);
            return true;
        }

        private static void OnQuantityDetailExplainerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantityInsightPanel panel) panel.InstallQuantityDetailExplainer();
        }

        private void InstallQuantityDetailExplainer()
        {
            if (_quantityDetailExplainerInstalled || !(QuantityTree.Parent is Grid host)) return;
            _quantityDetailExplainerInstalled = true;
            host.RowDefinitions.Clear();
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star), MinHeight = 105d });
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(QuantityTree, 0);
            Grid.SetRow(EmptyHint, 0);
            _quantityDetailCard = BuildQuantityDetailCard();
            Grid.SetRow(_quantityDetailCard, 1);
            host.Children.Add(_quantityDetailCard);
            QuantityTree.SelectedItemChanged += OnQuantityDetailTreeSelectionChanged;
            QuantityTree.IsVisibleChanged += OnQuantityDetailTreeVisibilityChanged;
            ClearQuantityDetail("Chọn một dòng cấu kiện để xem diễn giải khối lượng chi tiết.");
        }

        private void OnQuantityDetailTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is QuantityInsightItemViewModel item) RefreshQuantityDetail(item);
            else ClearQuantityDetail("Chọn một dòng cấu kiện để xem diễn giải khối lượng chi tiết.");
        }

        private void OnQuantityDetailTreeVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (QuantityTree.Visibility != Visibility.Visible) ClearQuantityDetail("Chưa có dòng khối lượng hiện hành để xem chi tiết.");
        }

        private void OnQuantityDetailSelectionChanged(object sender, SelectionChangedEventArgs e) =>
            RenderQuantityDetail(_quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption);
    }
}
