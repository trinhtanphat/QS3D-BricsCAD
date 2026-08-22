using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class ExcelActiveSelectionSnapshot
    {
        public ExcelActiveSelectionSnapshot(string workbookPath, string worksheetName, int rowNumber)
        {
            WorkbookPath = workbookPath ?? throw new ArgumentNullException(nameof(workbookPath));
            WorksheetName = worksheetName ?? throw new ArgumentNullException(nameof(worksheetName));
            RowNumber = rowNumber;
        }

        public string WorkbookPath { get; }
        public string WorksheetName { get; }
        public int RowNumber { get; }
        public string IdentityKey => WorkbookPath + "\u001f" + WorksheetName + "\u001f" + RowNumber.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Bounded late-bound bridge to an already-running Excel instance. The service never starts
    /// Excel and never retains COM objects between calls; callers receive only plain value data.
    /// </summary>
    internal static class ExcelActiveSelectionService
    {
        private const string ExcelProgId = "Excel.Application";

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int CLSIDFromProgID(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID,
            out Guid lpclsid);

        [DllImport("oleaut32.dll", PreserveSig = true)]
        private static extern int GetActiveObject(
            ref Guid rclsid,
            IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        public static bool TryRead(out ExcelActiveSelectionSnapshot? snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            object? application = null;
            object? workbook = null;
            object? worksheet = null;
            object? activeCell = null;

            try
            {
                var clsidResult = CLSIDFromProgID(ExcelProgId, out var excelClassId);
                if (clsidResult < 0)
                {
                    error = "Microsoft Excel chưa được đăng ký trên máy này.";
                    return false;
                }

                var activeResult = GetActiveObject(ref excelClassId, IntPtr.Zero, out var activeApplication);
                if (activeResult < 0 || activeApplication == null)
                {
                    error = "Không tìm thấy Microsoft Excel đang mở.";
                    return false;
                }

                application = activeApplication;
                workbook = GetProperty(application, "ActiveWorkbook");
                worksheet = GetProperty(application, "ActiveSheet");
                activeCell = GetProperty(application, "ActiveCell");
                if (workbook == null || worksheet == null || activeCell == null)
                {
                    error = "Excel chưa có workbook, worksheet hoặc ô đang chọn hợp lệ.";
                    return false;
                }

                var workbookPath = (Convert.ToString(GetProperty(workbook, "FullName"), CultureInfo.InvariantCulture) ?? string.Empty).Trim();
                var worksheetName = (Convert.ToString(GetProperty(worksheet, "Name"), CultureInfo.InvariantCulture) ?? string.Empty).Trim();
                var rowNumber = Convert.ToInt32(GetProperty(activeCell, "Row"), CultureInfo.InvariantCulture);

                if (workbookPath.Length == 0)
                {
                    error = "Workbook Excel đang mở chưa được lưu thành file .xlsx.";
                    return false;
                }

                var fullPath = Path.GetFullPath(workbookPath);
                if (!string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Bám Excel chỉ nhận workbook QS3D .xlsx; dùng Truy ngược Excel cho workflow thủ công khác.";
                    return false;
                }
                if (!File.Exists(fullPath))
                {
                    error = "Workbook Excel đang chọn chưa tồn tại trên đĩa; hãy lưu file trước khi truy ngược.";
                    return false;
                }
                if (worksheetName.Length == 0)
                {
                    error = "Không đọc được tên worksheet Excel đang chọn.";
                    return false;
                }
                if (rowNumber < 1 || rowNumber > 1048576)
                {
                    error = "Dòng Excel đang chọn nằm ngoài giới hạn XLSX.";
                    return false;
                }

                snapshot = new ExcelActiveSelectionSnapshot(fullPath, worksheetName, rowNumber);
                return true;
            }
            catch (COMException)
            {
                error = "Không đọc được trạng thái Excel đang mở. Hãy bảo đảm workbook không ở trạng thái đóng/chuyển đổi.";
                return false;
            }
            catch (TargetInvocationException)
            {
                error = "Excel không trả về workbook/sheet/cell đang hoạt động qua COM.";
                return false;
            }
            catch (Exception ex)
            {
                error = "Không đọc được dòng Excel đang chọn: " + ex.Message;
                return false;
            }
            finally
            {
                ReleaseCom(activeCell);
                ReleaseCom(worksheet);
                ReleaseCom(workbook);
                ReleaseCom(application);
            }
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

        private static void ReleaseCom(object? value)
        {
            if (value == null) return;
            try
            {
                if (Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
            }
            catch
            {
                // COM cleanup is best-effort. Never turn cleanup into a host failure.
            }
        }
    }
}
