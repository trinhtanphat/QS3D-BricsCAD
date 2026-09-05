using System;
using System.Globalization;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private bool TryShowSingleFootingFamilyProperties(ProjectFamily family)
        {
            if (family == null || !SingleFootingContract.IsSingleFooting(family))
                return false;

            SingleFootingDimensions dimensions;
            try
            {
                dimensions = SingleFootingContract.Read(family);
            }
            catch (Exception ex)
            {
                // A malformed specialized family must not fall back to editable
                // generic keys that bypass its validation/native regeneration.
                _viewModel.Properties.Clear();
                AddSingleFootingReadOnlyRow("THÔNG TIN", "Tên Family", family.Name);
                AddSingleFootingReadOnlyRow("KIỂM TRA", "Thông số không hợp lệ", ex.Message);
                SetStatus("Móng đơn thiếu bộ thông số hợp lệ: " + ex.Message);
                return true;
            }

            _viewModel.Properties.Clear();
            AddSingleFootingReadOnlyRow("THÔNG TIN", "Tên Family", family.Name);
            AddSingleFootingReadOnlyRow("THÔNG TIN", "Loại cấu kiện", SingleFootingContract.SubtypeName);
            AddSingleFootingDimensionRow(family, "KÍCH THƯỚC ĐÁY", "L1", SingleFootingContract.L1Key, dimensions.L1M);
            AddSingleFootingDimensionRow(family, "KÍCH THƯỚC ĐÁY", "W1", SingleFootingContract.W1Key, dimensions.W1M);
            AddSingleFootingDimensionRow(family, "KÍCH THƯỚC ĐỈNH", "L2", SingleFootingContract.L2Key, dimensions.L2M);
            AddSingleFootingDimensionRow(family, "KÍCH THƯỚC ĐỈNH", "W2", SingleFootingContract.W2Key, dimensions.W2M);
            AddSingleFootingDimensionRow(family, "CHIỀU CAO", "H1", SingleFootingContract.H1Key, dimensions.H1M);
            AddSingleFootingDimensionRow(family, "CHIỀU CAO", "H2", SingleFootingContract.H2Key, dimensions.H2M);

            if (family.Properties.TryGetValue("Material", out var material) && !string.IsNullOrWhiteSpace(material))
                AddSingleFootingReadOnlyRow("VẬT LIỆU", "Vật liệu", material.Trim());

            AddSingleFootingReadOnlyRow(
                "KHỐI LƯỢNG",
                "Thể tích hình học",
                dimensions.VolumeM3.ToString("0.###", CultureInfo.InvariantCulture),
                "m³");
            return true;
        }

        private void AddSingleFootingReadOnlyRow(string group, string name, string value, string unit = "")
        {
            var row = new PropertyRowViewModel
            {
                Group = group,
                Name = name,
                Unit = unit,
                IsReadOnly = true
            };
            row.Value = value ?? string.Empty;
            _viewModel.Properties.Add(row);
        }

        private void AddSingleFootingDimensionRow(
            ProjectFamily family,
            string group,
            string name,
            string key,
            double valueM)
        {
            var row = new PropertyRowViewModel
            {
                Group = group,
                Name = name,
                Unit = "mm",
                EditorKind = PropertyRowViewModel.TextEditor
            };
            row.Value = FormatSingleFootingMm(valueM);
            row.Apply = value => ApplySingleFootingDimension(family, key, value);
            _viewModel.Properties.Add(row);
        }

        private string ApplySingleFootingDimension(ProjectFamily family, string key, string requested)
        {
            SingleFootingDimensions previous;
            try
            {
                previous = SingleFootingContract.Read(family);
            }
            catch (Exception ex)
            {
                SetStatus("Không thể đọc Móng đơn hiện hành: " + ex.Message);
                return string.Empty;
            }

            var previousM = ReadSingleFootingDimension(previous, key);
            var previousDisplay = FormatSingleFootingMm(previousM);
            if (!TryParseSingleFootingMillimeters(requested, out var millimeters))
            {
                SetStatus(DisplaySingleFootingDimension(key) + ": giá trị mm không hợp lệ; giữ giá trị cũ.");
                return previousDisplay;
            }

            SingleFootingDimensions next;
            try
            {
                next = SingleFootingContract.WithDimension(previous, key, millimeters / 1000d);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
            {
                SetStatus(DisplaySingleFootingDimension(key) + ": " + ex.Message);
                return previousDisplay;
            }

            var nextM = ReadSingleFootingDimension(next, key);
            if (nextM.Equals(previousM)) return previousDisplay;

            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                SetStatus("Không có bản vẽ BricsCAD active; không cập nhật Móng đơn.");
                return previousDisplay;
            }

            try
            {
                var project = ExistingProjectMutationContext.Require(document, "Cập nhật Móng đơn");
                var owned = project.FindFamily(family.Id);
                if (owned == null || !ReferenceEquals(owned, family) || !SingleFootingContract.IsSingleFooting(owned))
                    throw new InvalidOperationException("Family Móng đơn đã stale hoặc không còn thuộc project đang mở.");

                var regenerated = SingleFootingRegenerationService.ApplyFamilyDimensions(
                    document,
                    project,
                    owned,
                    next);
                SetStatus(
                    "Đã cập nhật " + DisplaySingleFootingDimension(key) + " = " + FormatSingleFootingMm(nextM) +
                    " mm • regenerate " + regenerated.ToString(CultureInfo.InvariantCulture) + " instance Móng đơn.");
                return FormatSingleFootingMm(nextM);
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is ArgumentException ||
                ex is OverflowException)
            {
                SetStatus("Không thể cập nhật " + DisplaySingleFootingDimension(key) + ": " + ex.Message);
                return previousDisplay;
            }
        }

        private static bool TryParseSingleFootingMillimeters(string value, out double millimeters)
        {
            var text = (value ?? string.Empty).Trim();
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out millimeters) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out millimeters)) return false;
            return !double.IsNaN(millimeters) && !double.IsInfinity(millimeters);
        }

        private static double ReadSingleFootingDimension(SingleFootingDimensions dimensions, string key)
        {
            if (string.Equals(key, SingleFootingContract.L1Key, StringComparison.OrdinalIgnoreCase)) return dimensions.L1M;
            if (string.Equals(key, SingleFootingContract.W1Key, StringComparison.OrdinalIgnoreCase)) return dimensions.W1M;
            if (string.Equals(key, SingleFootingContract.L2Key, StringComparison.OrdinalIgnoreCase)) return dimensions.L2M;
            if (string.Equals(key, SingleFootingContract.W2Key, StringComparison.OrdinalIgnoreCase)) return dimensions.W2M;
            if (string.Equals(key, SingleFootingContract.H1Key, StringComparison.OrdinalIgnoreCase)) return dimensions.H1M;
            if (string.Equals(key, SingleFootingContract.H2Key, StringComparison.OrdinalIgnoreCase)) return dimensions.H2M;
            throw new ArgumentException("Unknown Móng đơn dimension key: " + key, nameof(key));
        }

        private static string DisplaySingleFootingDimension(string key)
        {
            if (string.Equals(key, SingleFootingContract.L1Key, StringComparison.OrdinalIgnoreCase)) return "L1";
            if (string.Equals(key, SingleFootingContract.W1Key, StringComparison.OrdinalIgnoreCase)) return "W1";
            if (string.Equals(key, SingleFootingContract.L2Key, StringComparison.OrdinalIgnoreCase)) return "L2";
            if (string.Equals(key, SingleFootingContract.W2Key, StringComparison.OrdinalIgnoreCase)) return "W2";
            if (string.Equals(key, SingleFootingContract.H1Key, StringComparison.OrdinalIgnoreCase)) return "H1";
            if (string.Equals(key, SingleFootingContract.H2Key, StringComparison.OrdinalIgnoreCase)) return "H2";
            return key;
        }

        private static string FormatSingleFootingMm(double meters) =>
            (meters * 1000d).ToString("0.###", CultureInfo.InvariantCulture);
    }
}
