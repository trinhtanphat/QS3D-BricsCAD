using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Mouse-first project file workflow used by the QS3D Home ribbon and Start Center.
    /// No BricsCAD command strings are dispatched from this service.
    /// </summary>
    internal static class ProjectFileUiService
    {
        private const string ProjectFilter = "QS3D Project (*.blt3d;*.qsdb)|*.blt3d;*.qsdb|BLT3D Project (*.blt3d)|*.blt3d|QS3D Project (*.qsdb)|*.qsdb";
        private const string DrawingFilter = "BricsCAD Drawing (*.dwg)|*.dwg";

        public static void CreateNewDrawing()
        {
            try
            {
                var manager = Application.DocumentManager;
                var managerType = manager.GetType();
                object? created = null;

                var parameterless = managerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(method => string.Equals(method.Name, "Add", StringComparison.Ordinal)
                        && method.GetParameters().Length == 0);
                if (parameterless != null)
                {
                    created = parameterless.Invoke(manager, null);
                }
                else
                {
                    var withTemplate = managerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(method => string.Equals(method.Name, "Add", StringComparison.Ordinal)
                            && method.GetParameters().Length == 1
                            && method.GetParameters()[0].ParameterType == typeof(string));
                    if (withTemplate == null)
                        throw new InvalidOperationException("BricsCAD không cung cấp API tạo bản vẽ mới cho phiên bản này.");
                    created = withTemplate.Invoke(manager, new object[] { string.Empty });
                }

                var document = created as Document ?? manager.MdiActiveDocument;
                if (document == null)
                    throw new InvalidOperationException("BricsCAD không tạo được bản vẽ mới.");

                // This is the only mouse-first workflow that intentionally creates
                // a new canonical in-memory QS3D project. Save itself remains
                // existing-project-only and therefore cannot silently bootstrap.
                ProjectContextCoordinator.Forget(document);
                _ = ProjectContextCoordinator.GetOrCreate(document);
                System.Windows.MessageBox.Show(
                    "Đã tạo bản vẽ mới. Khi bắt đầu làm việc với QS3D, project sẽ được liên kết với bản vẽ này và có thể lưu bằng nút Lưu/Lưu thành.",
                    "QS3D — Tạo dự án mới",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ShowError("Tạo dự án mới", ex.InnerException);
            }
            catch (Exception ex)
            {
                ShowError("Tạo dự án mới", ex);
            }
        }

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
                ExistingProjectMutationContext.Require(document, "Lưu dự án");

                var stopwatch = Stopwatch.StartNew();
                InvokeAcadDocumentMethod(document, "Save");
                var path = ProjectContextCoordinator.Save(document);
                stopwatch.Stop();

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Dự án vừa lưu không thể được đọc lại để xác nhận.");

                ProjectOperationResultWindow.ShowSaveSuccess(path, project, stopwatch.ElapsedMilliseconds);
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
                ExistingProjectMutationContext.Require(document, "Lưu thành");
                var currentProjectPath = ProjectContextCoordinator.GetProjectPath(document);
                var defaultName = SafeStem(document.Name) + ".dwg";
                var dialog = new SaveFileDialog
                {
                    Title = "Lưu bản vẽ QS3D thành",
                    Filter = DrawingFilter,
                    DefaultExt = ".dwg",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = defaultName
                };
                if (dialog.ShowDialog() != true) return;

                var targetDrawingPath = Path.GetFullPath(dialog.FileName);
                var targetProjectPath = Path.ChangeExtension(targetDrawingPath, ".qsdb");
                if (!SamePath(currentProjectPath, targetProjectPath)
                    && (File.Exists(targetProjectPath) || File.Exists(targetProjectPath + ".bak")))
                {
                    throw new InvalidOperationException(
                        "Không thể Lưu thành vì project QS3D đích đã tồn tại (hoặc còn bản .bak): " + targetProjectPath +
                        ". Hãy chọn tên/vị trí khác để tránh ghi đè dữ liệu project.");
                }

                var stopwatch = Stopwatch.StartNew();
                InvokeAcadDocumentMethod(document, "SaveAs", targetDrawingPath, Type.Missing, Type.Missing);
                if (!SamePath(document.Name, targetDrawingPath))
                    throw new InvalidOperationException("BricsCAD đã thực hiện Save As nhưng bản vẽ hiện hành không chuyển sang đường dẫn đích.");

                var savedProjectPath = ProjectContextCoordinator.Save(document);
                if (!SamePath(savedProjectPath, targetProjectPath))
                    throw new InvalidOperationException("QS3D không thể chuyển liên kết project sang sidecar của DWG mới.");
                stopwatch.Stop();

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Dự án vừa lưu thành không thể được đọc lại để xác nhận.");

                ProjectOperationResultWindow.ShowSaveAsSuccess(
                    targetDrawingPath,
                    savedProjectPath,
                    project,
                    stopwatch.ElapsedMilliseconds);
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
                document = Application.DocumentManager.Open(drawingPath, false);
                Application.DocumentManager.MdiActiveDocument = document;
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

        private static void InvokeAcadDocumentMethod(Document document, string methodName, params object[] arguments)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Tên phương thức BricsCAD không hợp lệ.", nameof(methodName));

            var property = document.GetType().GetProperty("AcadDocument", BindingFlags.Instance | BindingFlags.Public);
            var acadDocument = property?.GetValue(document, null)
                ?? throw new InvalidOperationException("BricsCAD không cung cấp AcadDocument cho bản vẽ hiện hành.");

            try
            {
                acadDocument.GetType().InvokeMember(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding,
                    binder: null,
                    target: acadDocument,
                    args: arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException("BricsCAD không thực hiện được " + methodName + ".", ex.InnerException);
            }
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
