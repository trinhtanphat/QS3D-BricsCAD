using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private DispatcherTimer? _excelFollowTimer;
        private bool _excelFollowBusy;
        private bool _excelFollowChangingToggle;
        private bool _excelFollowUnloadedHooked;
        private string _lastExcelFollowObservedIdentity = string.Empty;
        private string _lastExcelFollowError = string.Empty;

        private void OnExcelActiveRowClick(object sender, RoutedEventArgs e)
        {
            TryLocateActiveExcelRow(followMode: false);
        }

        private void OnExcelFollowChecked(object sender, RoutedEventArgs e)
        {
            if (_excelFollowChangingToggle) return;
            StartExcelFollow();
        }

        private void OnExcelFollowUnchecked(object sender, RoutedEventArgs e)
        {
            if (_excelFollowChangingToggle) return;
            StopExcelFollow(updateStatus: true);
        }

        private void StartExcelFollow()
        {
            if (_selectionGeometryFallback)
            {
                _viewModel.Status = "Bám Excel cần một QS3D project semantic; selection CAD read-only không hỗ trợ live follow.";
                StopExcelFollow(updateStatus: false);
                return;
            }

            EnsureExcelFollowTimer();
            _lastExcelFollowObservedIdentity = string.Empty;
            _lastExcelFollowError = string.Empty;
            _excelFollowTimer!.Start();
            _viewModel.Status = "Bám Excel: đang theo dõi workbook/sheet/dòng CHI_TIET đang chọn; mọi locate vẫn kiểm tra provenance trước khi đổi PICKFIRST.";
            TryLocateActiveExcelRow(followMode: true);
        }

        private void EnsureExcelFollowTimer()
        {
            if (_excelFollowTimer == null)
            {
                _excelFollowTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(750)
                };
                _excelFollowTimer.Tick += OnExcelFollowTick;
            }

            if (_excelFollowUnloadedHooked) return;
            Unloaded += OnExcelFollowPanelUnloaded;
            _excelFollowUnloadedHooked = true;
        }

        private void OnExcelFollowTick(object? sender, EventArgs e)
        {
            if (ExcelFollowCheck?.IsChecked != true)
            {
                StopExcelFollow(updateStatus: false);
                return;
            }

            if (!global::QS3D.BricsCAD.V25.PaletteCoordinator.IsQuantityInsightVisible)
            {
                StopExcelFollow(updateStatus: false);
                return;
            }

            TryLocateActiveExcelRow(followMode: true);
        }

        private void OnExcelFollowPanelUnloaded(object sender, RoutedEventArgs e)
        {
            StopExcelFollow(updateStatus: false);
        }

        private void StopExcelFollow(bool updateStatus)
        {
            _excelFollowTimer?.Stop();
            _excelFollowBusy = false;
            _lastExcelFollowObservedIdentity = string.Empty;
            _lastExcelFollowError = string.Empty;

            if (ExcelFollowCheck?.IsChecked == true)
            {
                _excelFollowChangingToggle = true;
                try { ExcelFollowCheck.IsChecked = false; }
                finally { _excelFollowChangingToggle = false; }
            }

            if (updateStatus)
                _viewModel.Status = "Bám Excel đã tắt. Selection CAD hiện tại được giữ nguyên.";
        }

        private bool TryLocateActiveExcelRow(bool followMode)
        {
            if (_excelFollowBusy) return false;
            _excelFollowBusy = true;
            try
            {
                var document = BcadApplication.DocumentManager.MdiActiveDocument;
                if (!TryRequireExcelBoundProject(document, followMode, out var project)) return false;

                if (!ExcelActiveSelectionService.TryRead(out var snapshot, out var readError) || snapshot == null)
                {
                    ReportExcelFollowError(readError, followMode);
                    return false;
                }

                if (followMode)
                {
                    if (string.Equals(_lastExcelFollowObservedIdentity, snapshot.IdentityKey, StringComparison.OrdinalIgnoreCase))
                        return true;
                    _lastExcelFollowObservedIdentity = snapshot.IdentityKey;
                }

                if (!string.Equals(snapshot.WorksheetName, "CHI_TIET", StringComparison.OrdinalIgnoreCase))
                {
                    ReportExcelFollowError(
                        "Dòng Excel đang chọn phải nằm trong sheet CHI_TIET của workbook ED2 QS3D.",
                        followMode);
                    return false;
                }
                if (snapshot.RowNumber < 2)
                {
                    ReportExcelFollowError("Hãy chọn một dòng dữ liệu CHI_TIET, không chọn dòng tiêu đề.", followMode);
                    return false;
                }

                var lookup = XlsxHandleReader.ReadHandleLookup(snapshot.WorkbookPath, snapshot.RowNumber);
                if (!lookup.IsModernSchema || !lookup.IsEd2Detail ||
                    !string.Equals(lookup.WorksheetName, "CHI_TIET", StringComparison.OrdinalIgnoreCase))
                {
                    ReportExcelFollowError(
                        "Bám Excel chỉ tự động định vị dòng QS3D ED2 CHI_TIET có Element ID + CAD Handle + Drawing Fingerprint hiện đại.",
                        followMode);
                    return false;
                }

                var projectVersion = project.ChangeVersion;
                var resolution = ExcelLocateResolutionService.ResolveModern(document!, project, lookup);

                var activeAgain = BcadApplication.DocumentManager.MdiActiveDocument;
                if (!ReferenceEquals(activeAgain, document) ||
                    !ProjectContextCoordinator.TryGetReadOnly(document!, out var currentProject) ||
                    !ReferenceEquals(currentProject, project) ||
                    currentProject.ChangeVersion != projectVersion ||
                    !SameProjectIdentity(currentProject))
                {
                    ReportExcelFollowError(
                        "DWG/project thay đổi trong lúc đọc Excel; selection được giữ nguyên. Hãy chọn lại dòng sau khi bảng QS3D được làm mới.",
                        followMode);
                    if (followMode) StopExcelFollow(updateStatus: false);
                    return false;
                }

                // Every workbook / semantic / fingerprint / Handle / live-ObjectId check above must
                // succeed before PICKFIRST is replaced. Failure paths intentionally leave selection intact.
                document!.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray());

                var zoomed = global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document);
                var snapshots = Cad.EntitySnapshotReader.ReadImpliedSelection(document);
                SetInspectionReadOnly(snapshots, currentProject);

                _lastExcelFollowError = string.Empty;
                _viewModel.Status = (followMode ? "Bám Excel" : "Dòng Excel đang chọn") +
                                    ": CHI_TIET dòng " + snapshot.RowNumber.ToString("N0") +
                                    " • " + resolution.ObjectIds.Count.ToString("N0") + " đối tượng CAD" +
                                    (zoomed ? " • đã zoom/highlight." : " • đã chọn đúng đối tượng nhưng chưa thể zoom vùng chọn.");
                return true;
            }
            catch (Exception ex)
            {
                ReportExcelFollowError("Không thể truy ngược dòng Excel đang chọn: " + ex.Message, followMode);
                return false;
            }
            finally
            {
                _excelFollowBusy = false;
            }
        }

        private bool TryRequireExcelBoundProject(
            Document? document,
            bool followMode,
            out QS3D.Core.Domain.ProjectState project)
        {
            project = null!;
            if (document == null)
            {
                ReportExcelFollowError("Không có bản vẽ BricsCAD đang hoạt động.", followMode);
                if (followMode) StopExcelFollow(updateStatus: false);
                return false;
            }
            if (_boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                ReportExcelFollowError("Bảng khối lượng đang bound với DWG khác hoặc đã cũ; bấm Làm mới trước khi dùng Excel.", followMode);
                if (followMode) StopExcelFollow(updateStatus: false);
                return false;
            }
            if (_selectionGeometryFallback)
            {
                ReportExcelFollowError("Excel traceback cần QS3D project semantic, không dùng được với geometry fallback.", followMode);
                if (followMode) StopExcelFollow(updateStatus: false);
                return false;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out project) || !SameProjectIdentity(project))
            {
                ReportExcelFollowError("QS3D project đã thay đổi hoặc không còn khả dụng; bấm Làm mới trước khi dùng Excel.", followMode);
                if (followMode) StopExcelFollow(updateStatus: false);
                return false;
            }
            return true;
        }

        private void ReportExcelFollowError(string message, bool followMode)
        {
            var safeMessage = string.IsNullOrWhiteSpace(message) ? "Không đọc được trạng thái Excel hiện hành." : message.Trim();
            if (followMode && string.Equals(_lastExcelFollowError, safeMessage, StringComparison.Ordinal)) return;
            _lastExcelFollowError = safeMessage;
            _viewModel.Status = (followMode ? "Bám Excel: " : "Excel: ") + safeMessage + " Selection CAD không thay đổi.";
        }
    }
}
