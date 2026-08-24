using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private static readonly bool _quantityMeasurementTraceHandlersRegistered = RegisterQuantityMeasurementTraceHandlers();

        private static bool RegisterQuantityMeasurementTraceHandlers()
        {
            // Geometry.cs remains the canonical producer of the face rows. Decorate the
            // already-existing S-gross action at load time so exact-face routing keeps the
            // same button/title seam and no second quantity UI is introduced.
            EventManager.RegisterClassHandler(
                typeof(Button),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantityMeasurementTraceButtonLoaded),
                true);
            return true;
        }

        private static void OnQuantityMeasurementTraceButtonLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            if (!(button.Content is string content) || !content.StartsWith("S gộp:", StringComparison.Ordinal)) return;

            var panel = FindQuantityInsightPanel(button);
            if (panel == null || !panel.TryResolveQuantityExactFaceButton(button, out var faceId)) return;
            var face = panel.FindQuantityMeasurementFace(faceId);
            if (face == null || !face.HasMeasurementTrace) return;

            button.Content = "S gộp: " + FormatQuantityMeasurement(face.MeasurementLength) +
                             " × " + FormatQuantityMeasurement(face.MeasurementHeight) +
                             " = " + FormatQuantityMeasurement(face.GrossArea) + " m²";
            button.ToolTip = "Exact BREP measurement trace: length × height reconciles to the authoritative face area. Click để chỉ highlight đúng native BREP face.";
        }

        private QuantityFormworkFaceExplanation? FindQuantityMeasurementFace(string faceId)
        {
            if (_quantityGeometryCurrent == null || string.IsNullOrWhiteSpace(faceId)) return null;
            var matches = _quantityGeometryCurrent.FormworkFaces
                .Where(x => string.Equals(x.FaceId, faceId, StringComparison.Ordinal))
                .ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static string FormatQuantityMeasurement(double value) =>
            value.ToString("0.###", CultureInfo.CurrentCulture);
    }
}
