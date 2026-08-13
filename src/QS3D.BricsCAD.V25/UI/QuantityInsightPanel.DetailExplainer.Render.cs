using System;
using System.Globalization;
using System.Windows;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void RenderQuantityDetail(QuantityInsightDetailOption? option)
        {
            if (option == null) { ClearQuantityDetail("Chọn một cấu kiện để xem số liệu chi tiết."); return; }
            var row = option.Row;
            if (_quantityDetailEmptyHint != null) _quantityDetailEmptyHint.Visibility = Visibility.Collapsed;
            if (_quantityDetailBody != null) _quantityDetailBody.Visibility = Visibility.Visible;
            if (_quantityDetailLocateButton != null) _quantityDetailLocateButton.IsEnabled = true;
            if (_quantityDetailContext != null) _quantityDetailContext.Text = QuantityDetailContext(row);
            if (_quantityDetailCount != null) _quantityDetailCount.Text = "Số lượng: " + row.Count.ToString("N0", CultureInfo.CurrentCulture);
            SetQuantityDetailValue("gross", row.GrossConcreteM3, "m³");
            SetQuantityDetailValue("deduction", row.DeductionM3, "m³");
            SetQuantityDetailValue("net", row.NetConcreteM3, "m³");
            SetQuantityDetailValue("formwork", row.FormworkM2, "m²");
            SetQuantityDetailValue("length", row.LengthM, "m");
            SetQuantityDetailValue("outer", row.OuterPerimeterM, "m");
            SetQuantityDetailValue("inner", row.InnerPerimeterM, "m");
            SetQuantityDetailValue("door", row.DoorAreaM2, "m²");
            SetQuantityDetailValue("side", row.SideAreaM2, "m²");
            SetQuantityDetailValue("bottom", row.BottomAreaM2, "m²");
            SetQuantityDetailValue("top", row.TopAreaM2, "m²");
            SetQuantityDetailValue("other", row.OtherAreaM2, "m²");
            SetQuantityDetailNullable("density", row.DensityKgM3, "kg/m³");
            SetQuantityDetailNullable("mass", row.MassKg, "kg");
            if (_quantityDetailElementIds != null) _quantityDetailElementIds.Text = "Element ID: " + QuantityDetailJoin(row.ElementIds);
            if (_quantityDetailSourceHandles != null) _quantityDetailSourceHandles.Text = "CAD handles: " + QuantityDetailJoin(row.SourceHandles);
            if (_quantityDetailDrawingFingerprint != null) _quantityDetailDrawingFingerprint.Text = "Drawing fingerprint: " + (string.IsNullOrWhiteSpace(row.DrawingFingerprint) ? "—" : row.DrawingFingerprint.Trim());
        }

        private void ClearQuantityDetail(string message)
        {
            _quantityDetailOptions = Array.Empty<QuantityInsightDetailOption>();
            if (_quantityDetailSelector != null) { _quantityDetailSelector.ItemsSource = null; _quantityDetailSelector.Visibility = Visibility.Collapsed; }
            if (_quantityDetailBody != null) _quantityDetailBody.Visibility = Visibility.Collapsed;
            if (_quantityDetailLocateButton != null) _quantityDetailLocateButton.IsEnabled = false;
            if (_quantityDetailEmptyHint != null) { _quantityDetailEmptyHint.Text = message ?? string.Empty; _quantityDetailEmptyHint.Visibility = Visibility.Visible; }
        }
    }
}
