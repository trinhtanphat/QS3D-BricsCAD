using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace QS3D.BricsCAD.V25.Services
{
    internal enum ExcelModelRowWorkbookKind
    {
        Ed2,
        Customer
    }

    internal sealed class ExcelModelRowCandidate
    {
        public ExcelModelRowCandidate(string workbookPath, int rowNumber, ExcelModelRowWorkbookKind workbookKind)
        {
            WorkbookPath = workbookPath ?? throw new ArgumentNullException(nameof(workbookPath));
            RowNumber = rowNumber;
            WorkbookKind = workbookKind;
        }

        public string WorkbookPath { get; }
        public int RowNumber { get; }
        public string WorksheetName => "CHI_TIET";
        public ExcelModelRowWorkbookKind WorkbookKind { get; }
    }

    /// <summary>
    /// Discovers one model-backed detail row in an already-running, saved Excel workbook and,
    /// only after the caller has revalidated the row through the canonical XLSX provenance
    /// readers, activates that row. COM values are discovery/navigation hints only; they never
    /// become quantity or provenance authority.
    /// </summary>
    internal static class ExcelModelRowActivationService
    {
        private const string ExcelProgId = "Excel.Application";
        private const int MaxRows = 1048576;
        private const int MaxColumns = 16384;
        private const long MaxDiscoveryCells = 4000000L;
        private const int MaxHeaderRows = 10;

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int CLSIDFromProgID(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID,
            out Guid lpclsid);

        [DllImport("oleaut32.dll", PreserveSig = true)]
        private static extern int GetActiveObject(
            ref Guid rclsid,
            IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        public static bool TryFindActiveWorkbookRow(
            string elementId,
            string drawingFingerprint,
            out ExcelModelRowCandidate? candidate,
            out string error)
        {
            candidate = null;
            error = string.Empty;
            var expectedElementId = CanonicalIdentity(elementId, "QS3D Element ID");
            var expectedFingerprint = CanonicalIdentity(drawingFingerprint, "QS3D Drawing Fingerprint");

            object? application = null;
            object? workbook = null;
            object? worksheets = null;
            object? detailSheet = null;
            object? traceSheet = null;
            try
            {
                if (!TryGetRunningExcel(out application, out error)) return false;
                workbook = GetProperty(application!, "ActiveWorkbook");
                if (workbook == null)
                {
                    error = "Excel chưa có workbook đang hoạt động.";
                    return false;
                }

                if (!TryReadSavedWorkbookPath(workbook, out var workbookPath, out error)) return false;
                worksheets = GetProperty(workbook, "Worksheets");
                if (worksheets == null)
                {
                    error = "Workbook Excel không cung cấp collection Worksheets.";
                    return false;
                }

                detailSheet = TryGetWorksheet(worksheets, "CHI_TIET");
                if (detailSheet == null)
                {
                    error = "Workbook đang mở không có worksheet CHI_TIET được QS3D hỗ trợ.";
                    return false;
                }

                traceSheet = TryGetWorksheet(worksheets, "TRACE_MODEL");
                if (traceSheet != null)
                {
                    var traceValues = ReadBoundedUsedRange(traceSheet, out var traceFirstRow, out var traceFirstColumn);
                    var sourceRow = FindCustomerDetailRow(traceValues, traceFirstRow, traceFirstColumn, expectedElementId, expectedFingerprint);
                    candidate = new ExcelModelRowCandidate(workbookPath, sourceRow, ExcelModelRowWorkbookKind.Customer);
                    return true;
                }

                var detailValues = ReadBoundedUsedRange(detailSheet, out var detailFirstRow, out var detailFirstColumn);
                var detailRow = FindEd2DetailRow(detailValues, detailFirstRow, detailFirstColumn, expectedElementId, expectedFingerprint);
                candidate = new ExcelModelRowCandidate(workbookPath, detailRow, ExcelModelRowWorkbookKind.Ed2);
                return true;
            }
            catch (InvalidDataException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (COMException)
            {
                error = "Không đọc được workbook Excel đang mở. Hãy lưu workbook và thử lại.";
                return false;
            }
            catch (TargetInvocationException ex)
            {
                error = "Excel không trả về trạng thái workbook qua COM: " + (ex.InnerException?.Message ?? ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                error = "Không tìm được dòng Excel từ cấu kiện: " + ex.Message;
                return false;
            }
            finally
            {
                ReleaseCom(traceSheet);
                ReleaseCom(detailSheet);
                ReleaseCom(worksheets);
                ReleaseCom(workbook);
                ReleaseCom(application);
            }
        }

        /// <summary>
        /// Navigation-only mutation. Callers must complete disk-backed provenance validation
        /// before invoking this method. The active workbook is rebound and required to remain
        /// saved and identical to the reviewed path before any Excel selection changes.
        /// </summary>
        public static bool TryActivateValidatedRow(ExcelModelRowCandidate candidate, out string error)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            error = string.Empty;
            object? application = null;
            object? workbook = null;
            object? worksheets = null;
            object? detailSheet = null;
            object? cells = null;
            object? targetCell = null;
            try
            {
                if (!TryGetRunningExcel(out application, out error)) return false;
                workbook = GetProperty(application!, "ActiveWorkbook");
                if (workbook == null)
                {
                    error = "Excel không còn workbook đang hoạt động; không thay đổi selection.";
                    return false;
                }

                if (!TryReadSavedWorkbookPath(workbook, out var activePath, out error)) return false;
                if (!string.Equals(activePath, Path.GetFullPath(candidate.WorkbookPath), StringComparison.OrdinalIgnoreCase))
                {
                    error = "Workbook Excel đang hoạt động đã thay đổi sau bước kiểm tra; không thay đổi selection.";
                    return false;
                }
                if (candidate.RowNumber < 2 || candidate.RowNumber > MaxRows)
                {
                    error = "Dòng Excel đã kiểm tra nằm ngoài giới hạn CHI_TIET.";
                    return false;
                }

                worksheets = GetProperty(workbook, "Worksheets");
                detailSheet = worksheets == null ? null : TryGetWorksheet(worksheets, candidate.WorksheetName);
                if (detailSheet == null)
                {
                    error = "Worksheet CHI_TIET đã biến mất sau bước kiểm tra; không thay đổi selection.";
                    return false;
                }

                cells = GetProperty(detailSheet, "Cells");
                if (cells == null)
                {
                    error = "Excel không cung cấp Cells cho CHI_TIET; không thay đổi selection.";
                    return false;
                }
                targetCell = GetIndexedProperty(cells, "Item", candidate.RowNumber, 1);
                if (targetCell == null)
                {
                    error = "Không resolve được ô Excel tại dòng đã kiểm tra.";
                    return false;
                }

                InvokeMethod(detailSheet, "Activate");
                InvokeMethod(targetCell, "Select");
                return true;
            }
            catch (COMException)
            {
                error = "Excel từ chối kích hoạt dòng đã kiểm tra; không có model/provenance nào bị thay đổi.";
                return false;
            }
            catch (TargetInvocationException ex)
            {
                error = "Excel không kích hoạt được dòng đã kiểm tra: " + (ex.InnerException?.Message ?? ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                error = "Không kích hoạt được dòng Excel đã kiểm tra: " + ex.Message;
                return false;
            }
            finally
            {
                ReleaseCom(targetCell);
                ReleaseCom(cells);
                ReleaseCom(detailSheet);
                ReleaseCom(worksheets);
                ReleaseCom(workbook);
                ReleaseCom(application);
            }
        }

        private static int FindEd2DetailRow(
            DetachedRange values,
            int firstRow,
            int firstColumn,
            string elementId,
            string drawingFingerprint)
        {
            var header = FindUniqueHeader(
                values,
                firstRow,
                firstColumn,
                new[] { "QS3D Element ID", "QS3D Drawing Fingerprint" },
                "ED2 CHI_TIET");
            var matches = new List<int>();
            for (var localRow = header.LocalRow + 1; localRow <= values.RowCount; localRow++)
            {
                if (!string.Equals(values.Text(localRow, header.Columns["QS3D Element ID"]), elementId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(values.Text(localRow, header.Columns["QS3D Drawing Fingerprint"]), drawingFingerprint, StringComparison.OrdinalIgnoreCase)) continue;
                matches.Add(firstRow + localRow - 1);
                if (matches.Count > 1) break;
            }
            if (matches.Count == 0)
                throw new InvalidDataException("ED2 CHI_TIET không có dòng nào khớp Element ID + Drawing Fingerprint của cấu kiện đang chọn.");
            if (matches.Count > 1)
                throw new InvalidDataException("ED2 CHI_TIET có nhiều dòng cùng Element ID + Drawing Fingerprint; từ chối nhảy dòng mơ hồ.");
            return matches[0];
        }

        private static int FindCustomerDetailRow(
            DetachedRange values,
            int firstRow,
            int firstColumn,
            string elementId,
            string drawingFingerprint)
        {
            var header = FindUniqueHeader(
                values,
                firstRow,
                firstColumn,
                new[] { "SHEET", "ROW", "QS3D Element ID", "QS3D Drawing Fingerprint" },
                "TRACE_MODEL");
            var matches = new List<int>();
            for (var localRow = header.LocalRow + 1; localRow <= values.RowCount; localRow++)
            {
                if (!string.Equals(values.Text(localRow, header.Columns["SHEET"]), "CHI_TIET", StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(values.Text(localRow, header.Columns["QS3D Element ID"]), elementId, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(values.Text(localRow, header.Columns["QS3D Drawing Fingerprint"]), drawingFingerprint, StringComparison.OrdinalIgnoreCase)) continue;

                var rowText = values.Text(localRow, header.Columns["ROW"]);
                if (!int.TryParse(rowText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sourceRow) || sourceRow < 2 || sourceRow > MaxRows)
                    throw new InvalidDataException("TRACE_MODEL ROW của cấu kiện đang chọn không hợp lệ.");
                matches.Add(sourceRow);
                if (matches.Count > 1) break;
            }
            if (matches.Count == 0)
                throw new InvalidDataException("TRACE_MODEL không có CHI_TIET nào khớp Element ID + Drawing Fingerprint của cấu kiện đang chọn.");
            if (matches.Count > 1)
                throw new InvalidDataException("TRACE_MODEL có nhiều CHI_TIET cùng Element ID + Drawing Fingerprint; từ chối nhảy dòng mơ hồ.");
            return matches[0];
        }

        private static HeaderProjection FindUniqueHeader(
            DetachedRange values,
            int firstRow,
            int firstColumn,
            IReadOnlyList<string> requiredHeaders,
            string label)
        {
            var candidates = new List<HeaderProjection>();
            var lastHeaderRow = Math.Min(values.RowCount, Math.Max(0, MaxHeaderRows - firstRow + 1));
            for (var localRow = 1; localRow <= lastHeaderRow; localRow++)
            {
                var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var duplicateRequired = false;
                for (var localColumn = 1; localColumn <= values.ColumnCount; localColumn++)
                {
                    var text = values.Text(localRow, localColumn);
                    foreach (var required in requiredHeaders)
                    {
                        if (!string.Equals(text, required, StringComparison.OrdinalIgnoreCase)) continue;
                        if (columns.ContainsKey(required)) duplicateRequired = true;
                        else columns.Add(required, localColumn);
                    }
                }
                if (duplicateRequired)
                    throw new InvalidDataException(label + " chứa header định danh bị trùng.");
                if (columns.Count == requiredHeaders.Count)
                    candidates.Add(new HeaderProjection(localRow, columns));
            }
            if (candidates.Count != 1)
                throw new InvalidDataException(label + " phải có đúng một hàng header định danh QS3D trong 10 dòng đầu.");
            return candidates[0];
        }

        private static DetachedRange ReadBoundedUsedRange(object worksheet, out int firstRow, out int firstColumn)
        {
            object? usedRange = null;
            object? rows = null;
            object? columns = null;
            try
            {
                usedRange = GetProperty(worksheet, "UsedRange") ?? throw new InvalidDataException("Excel worksheet không có UsedRange.");
                rows = GetProperty(usedRange, "Rows") ?? throw new InvalidDataException("Excel UsedRange không có Rows.");
                columns = GetProperty(usedRange, "Columns") ?? throw new InvalidDataException("Excel UsedRange không có Columns.");
                var rowCount = Convert.ToInt32(GetProperty(rows, "Count"), CultureInfo.InvariantCulture);
                var columnCount = Convert.ToInt32(GetProperty(columns, "Count"), CultureInfo.InvariantCulture);
                firstRow = Convert.ToInt32(GetProperty(usedRange, "Row"), CultureInfo.InvariantCulture);
                firstColumn = Convert.ToInt32(GetProperty(usedRange, "Column"), CultureInfo.InvariantCulture);
                if (rowCount <= 0 || columnCount <= 0 || firstRow <= 0 || firstColumn <= 0 ||
                    firstRow + rowCount - 1 > MaxRows || firstColumn + columnCount - 1 > MaxColumns)
                    throw new InvalidDataException("Excel UsedRange nằm ngoài giới hạn XLSX.");
                if ((long)rowCount * columnCount > MaxDiscoveryCells)
                    throw new InvalidDataException("Excel UsedRange quá lớn cho CAD → Excel tự động; thu gọn UsedRange hoặc dùng workflow thủ công.");

                var raw = GetProperty(usedRange, "Value2");
                return DetachedRange.From(raw, rowCount, columnCount);
            }
            finally
            {
                ReleaseCom(columns);
                ReleaseCom(rows);
                ReleaseCom(usedRange);
            }
        }

        private static bool TryReadSavedWorkbookPath(object workbook, out string fullPath, out string error)
        {
            fullPath = string.Empty;
            error = string.Empty;
            var saved = Convert.ToBoolean(GetProperty(workbook, "Saved"), CultureInfo.InvariantCulture);
            if (!saved)
            {
                error = "Workbook Excel đang có thay đổi chưa lưu; hãy Save trước khi CAD → Excel để provenance trên đĩa khớp workbook đang xem.";
                return false;
            }
            var path = (Convert.ToString(GetProperty(workbook, "FullName"), CultureInfo.InvariantCulture) ?? string.Empty).Trim();
            if (path.Length == 0)
            {
                error = "Workbook Excel chưa được lưu thành file .xlsx.";
                return false;
            }
            fullPath = Path.GetFullPath(path);
            if (!string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                error = "CAD → Excel chỉ nhận workbook .xlsx có provenance QS3D.";
                return false;
            }
            if (!File.Exists(fullPath))
            {
                error = "Workbook Excel đang mở không tồn tại trên đĩa; hãy Save trước khi truy vết.";
                return false;
            }
            return true;
        }

        private static bool TryGetRunningExcel(out object? application, out string error)
        {
            application = null;
            error = string.Empty;
            var clsidResult = CLSIDFromProgID(ExcelProgId, out var excelClassId);
            if (clsidResult < 0)
            {
                error = "Microsoft Excel chưa được đăng ký trên máy này.";
                return false;
            }
            var activeResult = GetActiveObject(ref excelClassId, IntPtr.Zero, out var activeApplication);
            if (activeResult < 0 || activeApplication == null)
            {
                error = "CAD → Excel cần một Microsoft Excel đang mở; QS3D không tự khởi động Excel.";
                return false;
            }
            application = activeApplication;
            return true;
        }

        private static object? TryGetWorksheet(object worksheets, string name)
        {
            try
            {
                return GetIndexedProperty(worksheets, "Item", name);
            }
            catch (COMException)
            {
                return null;
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        private static string CanonicalIdentity(string value, string label)
        {
            var canonical = (value ?? string.Empty).Trim();
            if (canonical.Length == 0 || canonical.IndexOfAny(new[] { '\r', '\n', '\t', '\0' }) >= 0)
                throw new ArgumentException(label + " is required and must be canonical.");
            return canonical;
        }

        private static object? GetProperty(object target, string propertyName)
        {
            return target.GetType().InvokeMember(
                propertyName,
                BindingFlags.GetProperty,
                null,
                target,
                Array.Empty<object>());
        }

        private static object? GetIndexedProperty(object target, string propertyName, params object[] arguments)
        {
            return target.GetType().InvokeMember(
                propertyName,
                BindingFlags.GetProperty,
                null,
                target,
                arguments);
        }

        private static void InvokeMethod(object target, string methodName)
        {
            target.GetType().InvokeMember(
                methodName,
                BindingFlags.InvokeMethod,
                null,
                target,
                Array.Empty<object>());
        }

        private static void ReleaseCom(object? value)
        {
            if (value == null) return;
            try
            {
                if (Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
            }
            catch
            {
                // Best-effort cleanup only; never convert cleanup into a host failure.
            }
        }

        private sealed class HeaderProjection
        {
            public HeaderProjection(int localRow, Dictionary<string, int> columns)
            {
                LocalRow = localRow;
                Columns = columns;
            }

            public int LocalRow { get; }
            public Dictionary<string, int> Columns { get; }
        }

        private sealed class DetachedRange
        {
            private readonly object? _scalar;
            private readonly Array? _array;
            private readonly int _rowLowerBound;
            private readonly int _columnLowerBound;

            private DetachedRange(object? scalar, Array? array, int rowCount, int columnCount)
            {
                _scalar = scalar;
                _array = array;
                RowCount = rowCount;
                ColumnCount = columnCount;
                _rowLowerBound = array == null ? 0 : array.GetLowerBound(0);
                _columnLowerBound = array == null ? 0 : array.GetLowerBound(1);
            }

            public int RowCount { get; }
            public int ColumnCount { get; }

            public static DetachedRange From(object? raw, int rowCount, int columnCount)
            {
                if (raw is Array array)
                {
                    if (array.Rank != 2 || array.GetLength(0) != rowCount || array.GetLength(1) != columnCount)
                        throw new InvalidDataException("Excel UsedRange.Value2 có kích thước không nhất quán.");
                    return new DetachedRange(null, array, rowCount, columnCount);
                }
                if (rowCount != 1 || columnCount != 1)
                    throw new InvalidDataException("Excel UsedRange.Value2 không trả về ma trận cho vùng nhiều ô.");
                return new DetachedRange(raw, null, 1, 1);
            }

            public string Text(int localRow, int localColumn)
            {
                if (localRow < 1 || localRow > RowCount || localColumn < 1 || localColumn > ColumnCount)
                    return string.Empty;
                object? value;
                if (_array == null) value = _scalar;
                else value = _array.GetValue(_rowLowerBound + localRow - 1, _columnLowerBound + localColumn - 1);
                return (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Trim();
            }
        }
    }
}
