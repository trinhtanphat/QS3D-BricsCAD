using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void RenderQuantityDetail(QuantityInsightDetailOption? option)
        {
            if (option == null) { ClearQuantityDetail("Chọn một cấu kiện để xem số liệu chi tiết."); return; }
            var row = option.Row;
            var geometry = RefreshQuantityGeometry(option);
            if (_quantityDetailEmptyHint != null) _quantityDetailEmptyHint.Visibility = Visibility.Collapsed;
            if (_quantityDetailBody != null) _quantityDetailBody.Visibility = Visibility.Visible;
            if (_quantityDetailLocateButton != null) _quantityDetailLocateButton.IsEnabled = true;
            if (_quantityDetailContext != null) _quantityDetailContext.Text = QuantityDetailContext(row);
            if (_quantityDetailCount != null) _quantityDetailCount.Text = "Số lượng: " + row.Count.ToString("N0", CultureInfo.CurrentCulture);
            SetQuantityDetailValue("gross", geometry?.GrossVolume ?? row.GrossConcreteM3, "m³");
            SetQuantityDetailValue("deduction", geometry?.DeductionVolume ?? row.DeductionM3, "m³");
            SetQuantityDetailValue("net", geometry?.NetVolume ?? row.NetConcreteM3, "m³");
            SetQuantityDetailValue("formwork", geometry?.NetFormworkArea ?? row.FormworkM2, "m²");
            SetQuantityDetailValue("length", row.LengthM, "m");
            SetQuantityDetailValue("outer", row.OuterPerimeterM, "m");
            SetQuantityDetailValue("inner", row.InnerPerimeterM, "m");
            SetQuantityDetailValue("door", row.DoorAreaM2, "m²");
            SetQuantityDetailValue("side", GeometryFaceArea(geometry, "Side", row.SideAreaM2), "m²");
            SetQuantityDetailValue("bottom", GeometryFaceArea(geometry, "Bottom", row.BottomAreaM2), "m²");
            SetQuantityDetailValue("top", GeometryFaceArea(geometry, "Top", row.TopAreaM2), "m²");
            SetQuantityDetailValue("other", GeometryOtherArea(geometry, row.OtherAreaM2), "m²");
            SetQuantityDetailNullable("density", row.DensityKgM3, "kg/m³");
            SetQuantityDetailNullable("mass", row.MassKg, "kg");
            if (_quantityDetailElementIds != null) _quantityDetailElementIds.Text = "Element ID: " + QuantityDetailJoin(row.ElementIds);
            if (_quantityDetailSourceHandles != null) _quantityDetailSourceHandles.Text = "CAD handles: " + QuantityDetailJoin(row.SourceHandles);
            if (_quantityDetailDrawingFingerprint != null) _quantityDetailDrawingFingerprint.Text = "Drawing fingerprint: " + (string.IsNullOrWhiteSpace(row.DrawingFingerprint) ? "—" : row.DrawingFingerprint.Trim());
            RenderQuantityGeometry(geometry);
        }

        private static double GeometryFaceArea(QS3D.Core.Reporting.QuantityGeometryExplanation? geometry, string faceType, double fallback)
        {
            if (geometry == null) return fallback;
            return geometry.FormworkFaces
                .Where(x => string.Equals(x.FaceType, faceType, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.NetArea);
        }

        private static double GeometryOtherArea(QS3D.Core.Reporting.QuantityGeometryExplanation? geometry, double fallback)
        {
            if (geometry == null) return fallback;
            return geometry.FormworkFaces
                .Where(x => string.Equals(x.FaceType, "Other", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x.FaceType, "End", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.NetArea);
        }

        private void ClearQuantityDetail(string message)
        {
            _quantityDetailOptions = Array.Empty<QuantityInsightDetailOption>();
            _quantityGeometryCurrent = null;
            _quantityGeometryError = string.Empty;
            if (_quantityGeometryPanel != null) _quantityGeometryPanel.Children.Clear();
            if (_quantityGeometryScroll != null) _quantityGeometryScroll.Visibility = Visibility.Collapsed;
            if (_quantityDetailSelector != null) { _quantityDetailSelector.ItemsSource = null; _quantityDetailSelector.Visibility = Visibility.Collapsed; }
            if (_quantityDetailBody != null) _quantityDetailBody.Visibility = Visibility.Collapsed;
            if (_quantityDetailLocateButton != null) _quantityDetailLocateButton.IsEnabled = false;
            if (_quantityDetailEmptyHint != null) { _quantityDetailEmptyHint.Text = message ?? string.Empty; _quantityDetailEmptyHint.Visibility = Visibility.Visible; }
        }
    }
}
