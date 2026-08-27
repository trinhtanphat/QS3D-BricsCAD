using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private static readonly bool _foundationQuantityExactFacePresentationHandlersRegistered =
            RegisterFoundationQuantityExactFacePresentationHandlers();

        private sealed class QuantityExactFacePresentationTarget
        {
            public QuantityExactFacePresentationTarget(string faceId)
            {
                FaceId = faceId ?? string.Empty;
            }

            public string FaceId { get; }
        }

        private static bool RegisterFoundationQuantityExactFacePresentationHandlers()
        {
            // Keep the renderer and BREP authority unchanged. This presentation adapter runs only
            // after the exact-face title enters the loaded visual tree, moves FaceId into metadata,
            // and replaces the visible technical envelope with a human-friendly label.
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnFoundationQuantityExactFaceTitleLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                TextBlock.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnFoundationQuantityExactFaceTitleClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                TextBlock.MouseEnterEvent,
                new MouseEventHandler(OnFoundationQuantityExactFaceTitleMouseEnter),
                true);
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnFoundationQuantityExactFaceButtonClick),
                true);
            return true;
        }

        private static void OnFoundationQuantityExactFaceTitleLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            if (textBlock.Tag is QuantityExactFacePresentationTarget) return;

            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsDirectQuantityGeometryChild(textBlock)) return;
            if (!TryBuildFoundationQuantityExactFacePresentation(textBlock.Text, out var faceId, out var displayText)) return;

            textBlock.Tag = new QuantityExactFacePresentationTarget(faceId);
            textBlock.Text = displayText;
        }

        private static void OnFoundationQuantityExactFaceTitleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsDirectQuantityGeometryChild(textBlock)) return;
            if (!TryGetFoundationQuantityExactFaceIdentity(textBlock, out var faceId)) return;

            panel.ClearQuantityExactFaceHighlight();
            e.Handled = true;
            panel.LocateQuantityExactFace(faceId);
        }

        private static void OnFoundationQuantityExactFaceTitleMouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsDirectQuantityGeometryChild(textBlock)) return;
            if (!TryGetFoundationQuantityExactFaceIdentity(textBlock, out _)) return;

            textBlock.Cursor = Cursors.Hand;
            textBlock.ToolTip = "Click để highlight đúng native BREP face hiện hành; FaceId kỹ thuật được giữ riêng khỏi nhãn hiển thị.";
        }

        private static void OnFoundationQuantityExactFaceButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            var panel = FindQuantityInsightPanel(button);
            if (panel == null) return;
            if (!panel.TryResolveFoundationQuantityExactFaceButton(button, out var faceId)) return;

            panel.ClearQuantityExactFaceHighlight();
            e.Handled = true;
            panel.LocateQuantityExactFace(faceId);
        }

        private bool TryResolveFoundationQuantityExactFaceButton(Button button, out string faceId)
        {
            faceId = string.Empty;
            if (_quantityGeometryPanel == null || !_quantityGeometryPanel.Children.Contains(button)) return false;
            if (!(button.Content is string content)) return false;
            if (!content.StartsWith("S gộp:", StringComparison.Ordinal) &&
                !content.StartsWith("S còn:", StringComparison.Ordinal)) return false;

            var buttonIndex = _quantityGeometryPanel.Children.IndexOf(button);
            for (var index = buttonIndex - 1; index >= 0; index--)
            {
                if (!(_quantityGeometryPanel.Children[index] is TextBlock candidate)) continue;
                if (TryGetFoundationQuantityExactFaceIdentity(candidate, out faceId)) return true;
                if ((candidate.Text ?? string.Empty).StartsWith("VÁN KHUÔN THEO MẶT", StringComparison.Ordinal)) break;
            }

            faceId = string.Empty;
            return false;
        }

        private static bool TryGetFoundationQuantityExactFaceIdentity(FrameworkElement element, out string faceId)
        {
            faceId = string.Empty;
            if (!(element.Tag is QuantityExactFacePresentationTarget target)) return false;
            if (!TryParseQuantityExactFaceId(target.FaceId, out _, out _)) return false;
            faceId = target.FaceId;
            return true;
        }

        private static bool TryBuildFoundationQuantityExactFacePresentation(
            string? renderedText,
            out string faceId,
            out string displayText)
        {
            faceId = string.Empty;
            displayText = string.Empty;
            if (string.IsNullOrWhiteSpace(renderedText)) return false;

            var parts = renderedText!
                .Split(new[] { " • " }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .ToArray();
            if (parts.Length < 2 || !TryParseQuantityExactFaceId(parts[0], out _, out _)) return false;

            faceId = parts[0];
            var faceType = parts[parts.Length - 1];
            var semanticKey = parts.Length > 2
                ? string.Join(" • ", parts.Skip(1).Take(parts.Length - 2))
                : string.Empty;
            displayText = BuildFoundationQuantityExactFaceDisplayText(faceId, semanticKey, faceType);
            return true;
        }

        private static string BuildFoundationQuantityExactFaceDisplayText(
            string faceId,
            string semanticKey,
            string faceType)
        {
            if (TryFormatRaftFoundationSemanticFaceLabel(semanticKey, out var semanticLabel))
                return semanticLabel;

            var typeLabel = FriendlyQuantityExactFaceType(faceType);
            if (!string.IsNullOrWhiteSpace(semanticKey))
                return typeLabel + " • " + semanticKey + " • " + faceId;
            return typeLabel + " • " + faceId;
        }

        private static bool TryFormatRaftFoundationSemanticFaceLabel(string semanticKey, out string label)
        {
            label = string.Empty;
            const string prefix = "Side:OuterLoop:Edge";
            if (string.IsNullOrWhiteSpace(semanticKey) ||
                !semanticKey.StartsWith(prefix, StringComparison.Ordinal)) return false;

            var suffix = semanticKey.Substring(prefix.Length);
            if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var zeroBasedEdge) || zeroBasedEdge < 0)
                return false;

            label = "Mặt bên ngoài • Cạnh " + (zeroBasedEdge + 1).ToString(CultureInfo.CurrentCulture);
            return true;
        }

        private static string FriendlyQuantityExactFaceType(string faceType)
        {
            if (string.Equals(faceType, "Bottom", StringComparison.OrdinalIgnoreCase)) return "Mặt đáy";
            if (string.Equals(faceType, "Side", StringComparison.OrdinalIgnoreCase)) return "Mặt bên";
            if (string.Equals(faceType, "End", StringComparison.OrdinalIgnoreCase)) return "Mặt đầu";
            if (string.Equals(faceType, "Top", StringComparison.OrdinalIgnoreCase)) return "Mặt trên";
            return string.IsNullOrWhiteSpace(faceType) ? "BREP face" : "BREP face " + faceType.Trim();
        }
    }
}
