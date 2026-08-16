using System;
using System.Diagnostics;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Mouse-first project file workflow used by the QS3D Home ribbon.
    /// No BricsCAD command strings are dispatched from this service.
    /// </summary>
    internal static class ProjectFileUiService
    {
        private const string ProjectFilter = "QS3D Project (*.blt3d;*.qsdb)|*.blt3d;*.qsdb|BLT3D Project (*.blt3d)|*.blt3d|QS3D Project (*.qsdb)|*.qsdb";

        public static void OpenProjectFromPicker()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Mở dự án QS3D",
                    Filter = ProjectFilter,
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;
                OpenProject(dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError("Mở dự án", ex);
            }
        }

        public static void SaveCurrentProject()
        {
            try
            {
                var document = RequireActiveDocument();
                var stopwatch = Stopwatch.StartNew();
                var path = ProjectContextCoordinator.Save(document);
                stopwatch.Stop();

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Dự án vừa lưu không thể được đọc lại để xác nhận.");

                ProjectOperationResultWindow.ShowSaveSuccess(path, project, stopwatch.ElapsedMilliseconds, false);
            }
            catch (Exception ex)
            {
                ShowError("Lưu dự án", ex);
            }
        }

        public static void SaveCurrentProjectAs()
        {
            try
            {
                var document = RequireActiveDocument();
                var project = ExistingProjectMutationContext.Require(document, "Lưu thành");
                var defaultName = SafeStem(document.Name) + ".blt3d";
                var dialog = new SaveFileDialog
                {
                    Title = "Lưu dự án QS3D thành",
                    Filter = ProjectFilter,
                    DefaultExt = ".blt3d",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = defaultName
                };
                if (dialog.ShowDialog() != true) return;

                var stopwatch = Stopwatch.StartNew();
                var canonicalPath = ProjectContextCoordinator.Save(document);
                var targetPath = Path.GetFullPath(dialog.FileName);
                if (!SamePath(canonicalPath, targetPath))
                {
                    var targetDirectory = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrWhiteSpace(targetDirectory)) Directory.CreateDirectory(targetDirectory);
                    if (File.Exists(targetPath)) File.Copy(targetPath, targetPath + ".bak", true);
                    File.Copy(canonicalPath, targetPath, true);
                }
                stopwatch.Stop();

                ProjectOperationResultWindow.ShowSaveSuccess(targetPath, project, stopwatch.ElapsedMilliseconds, true);
            }
            catch (Exception ex)
            {
                ShowError("Lưu thành", ex);
            }
        }

        internal static void OpenProject(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath)) throw new ArgumentException("Đường dẫn project không hợp lệ.", nameof(projectPath));
            var fullProjectPath = Path.GetFullPath(projectPath);
            if (!File.Exists(fullProjectPath)) throw new FileNotFoundException("Không tìm thấy tệp dự án QS3D.", fullProjectPath);

            var total = Stopwatch.StartNew();
            var read = Stopwatch.StartNew();
            var store = new QsdbProjectStore();
            var importedProject = store.Load(fullProjectPath);
            read.Stop();

            var drawingPath = ResolveDrawingPath(fullProjectPath, importedProject);
            if (!File.Exists(drawingPath))
                throw new FileNotFoundException("Project tham chiếu tới bản vẽ không còn tồn tại: " + drawingPath, drawingPath);

            var bind = Stopwatch.StartNew();
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document != null && SamePath(document.Name, drawingPath))
            {
                if (ProjectContextCoordinator.TryGetCached(document, out _) && ProjectContextCoordinator.HasPendingChanges(document))
                {
                    var discard = System.Windows.MessageBox.Show(
                        "Dự án đang mở có thay đổi chưa lưu. Mở tệp project đã chọn sẽ nạp lại dữ liệu và bỏ các thay đổi đó.\n\nTiếp tục?",
                        "QS3D — Mở dự án",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (discard != MessageBoxResult.Yes) return;
                }
            }
            else
            {
                Application.DocumentManager.Open(drawingPath, false);
                document = Application.DocumentManager.MdiActiveDocument;
            }

            if (document == null || !SamePath(document.Name, drawingPath))
                throw new InvalidOperationException("BricsCAD không kích hoạt được bản vẽ được liên kết với project.");

            var canonicalPath = ProjectContextCoordinator.GetProjectPath(document);
            if (!SamePath(canonicalPath, fullProjectPath))
                PublishSelectedProject(store, importedProject, fullProjectPath, canonicalPath);

            ProjectContextCoordinator.Forget(document);
            var project = ProjectContextCoordinator.Reload(document);
            bind.Stop();
            total.Stop();

            try { PaletteCoordinator.RefreshProject(); }
            catch { }
            ProjectOperationResultWindow.ShowOpenSuccess(fullProjectPath, project, read.ElapsedMilliseconds, bind.ElapsedMilliseconds, total.ElapsedMilliseconds);
        }

        private static void PublishSelectedProject(QsdbProjectStore store, ProjectState project, string selectedPath, string canonicalPath)
        {
            if (File.Exists(canonicalPath))
            {
                var replace = System.Windows.MessageBox.Show(
                    "Bản vẽ này đã có project QS3D:\n" + canonicalPath +
                    "\n\nBạn đang mở:\n" + selectedPath +
                    "\n\nThay project hiện tại bằng tệp đã chọn? QS3D sẽ giữ bản sao .bak của project cũ.",
                    "QS3D — Xác nhận project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (replace != MessageBoxResult.Yes)
                    throw new OperationCanceledException("Đã hủy thay thế project hiện tại.");

                store.Save(project, canonicalPath);
                return;
            }

            if (File.Exists(canonicalPath + ".bak"))
                throw new InvalidOperationException("Project chính đang thiếu nhưng còn file .bak tại " + canonicalPath + ".bak. Hãy khôi phục project này trước khi nhập project khác để tránh mất dữ liệu.");

            store.SaveNew(project, canonicalPath);
        }

        private static string ResolveDrawingPath(string projectPath, ProjectState project)
        {
            var stored = project.DrawingPath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(stored))
            {
                if (Path.IsPathRooted(stored)) return Path.GetFullPath(stored);
                var directory = Path.GetDirectoryName(projectPath) ?? string.Empty;
                var relativeCandidate = Path.GetFullPath(Path.Combine(directory, stored));
                if (File.Exists(relativeCandidate)) return relativeCandidate;
            }

            var sameStem = Path.ChangeExtension(projectPath, ".dwg");
            if (File.Exists(sameStem)) return Path.GetFullPath(sameStem);
            throw new InvalidDataException("Tệp project không chứa đường dẫn DWG hợp lệ và không tìm thấy DWG cùng tên cạnh project.");
        }

        private static Document RequireActiveDocument()
        {
            return Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("BricsCAD chưa có bản vẽ đang hoạt động.");
        }

        private static string SafeStem(string path)
        {
            try
            {
                var stem = Path.GetFileNameWithoutExtension(path);
                return string.IsNullOrWhiteSpace(stem) ? "QS3D-Project" : stem;
            }
            catch
            {
                return "QS3D-Project";
            }
        }

        private static bool SamePath(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            try
            {
                return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void ShowError(string operation, Exception exception)
        {
            if (exception is OperationCanceledException) return;
            System.Windows.MessageBox.Show(
                exception.Message,
                "QS3D — " + operation,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
