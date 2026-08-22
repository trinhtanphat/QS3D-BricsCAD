using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SupportBundleCommands
    {
        [CommandMethod("QS3DSUPPORTBUNDLE", CommandFlags.Modal)]
        public void ExportSupportBundle()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất QS3D Support Bundle (ẩn dữ liệu dự án)",
                    Filter = "Text report (*.txt)|*.txt",
                    DefaultExt = ".txt",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = "QS3D-Support-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt"
                };
                if (dialog.ShowDialog() != true) return;

                var pluginAssembly = typeof(SupportBundleCommands).Assembly;
                var coreAssembly = typeof(ProjectState).Assembly;
                var brxAssembly = typeof(Application).Assembly;
                var tdAssembly = typeof(Database).Assembly;
                var lines = new List<string>
                {
                    "QS3D_SUPPORT_BUNDLE_V1",
                    "privacy=No drawing path, source/generated handles, semantic IDs, Family names, project metadata, user name or machine name are included.",
                    "generated_utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    "plugin_product_version=" + InformationalVersion(pluginAssembly),
                    "plugin_assembly_version=" + AssemblyVersion(pluginAssembly),
                    "core_product_version=" + InformationalVersion(coreAssembly),
                    "core_assembly_version=" + AssemblyVersion(coreAssembly),
                    "brx_assembly_version=" + AssemblyVersion(brxAssembly),
                    "td_assembly_version=" + AssemblyVersion(tdAssembly),
                    "process_64bit=" + Bool(Environment.Is64BitProcess),
                    "os_64bit=" + Bool(Environment.Is64BitOperatingSystem),
                    "interactive=" + Bool(Environment.UserInteractive)
                };

                if (ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    lines.Add("project_available=true");
                    lines.Add("project_schema=" + project.SchemaVersion.ToString(CultureInfo.InvariantCulture));
                    lines.Add("zone_count=" + project.Zones.Count.ToString(CultureInfo.InvariantCulture));
                    lines.Add("floor_count=" + project.Floors.Count.ToString(CultureInfo.InvariantCulture));
                    lines.Add("family_count=" + project.Families.Count.ToString(CultureInfo.InvariantCulture));
                    lines.Add("element_count=" + project.Elements.Count.ToString(CultureInfo.InvariantCulture));
                    lines.Add("dirty_element_count=" + project.Elements.Count(x => x.Dirty != ElementDirtyFlags.None).ToString(CultureInfo.InvariantCulture));
                    lines.Add("has_drawing_fingerprint=" + Bool(!string.IsNullOrWhiteSpace(project.DrawingFingerprint)));

                    foreach (var group in project.Elements
                        .GroupBy(x => x.Category)
                        .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
                    {
                        lines.Add("category." + SafeToken(group.Key.ToString()) + "=" + group.Count().ToString(CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    lines.Add("project_available=false");
                }

                PublishSupportBundle(dialog.FileName, lines);
                FinalizeSupportBundleUi(document, dialog.FileName);
            }
            catch (System.Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3DSUPPORTBUNDLE error: " + ex.Message); }
                catch { }
            }
        }

        private static void PublishSupportBundle(string path, IReadOnlyList<string> lines)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Support Bundle path is required.", nameof(path));
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Support Bundle path must have a parent directory.");
            Directory.CreateDirectory(directory);

            var temp = Path.Combine(
                directory,
                "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                {
                    foreach (var line in lines)
                        writer.WriteLine(line ?? string.Empty);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                    File.Replace(temp, fullPath, null, true);
                else
                    File.Move(temp, fullPath);
            }
            finally
            {
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch
                {
                    // Temp cleanup is best-effort and must not mask the original publish failure.
                }
            }
        }

        private static void FinalizeSupportBundleUi(Document document, string path)
        {
            try
            {
                PaletteCoordinator.SetStatus("Đã xuất Support Bundle: " + path);
                document.Editor.WriteMessage("\nQS3D Support Bundle đã xuất: " + path);
                document.Editor.WriteMessage("\nBundle không chứa DWG path, handles, semantic IDs, Family names, project metadata, user/machine name.");
            }
            catch (System.Exception ex)
            {
                try { document.Editor.WriteMessage("\n[QS3D] Cảnh báo UI sau export Support Bundle: " + ex.Message); }
                catch { }
            }
        }

        private static string AssemblyVersion(Assembly assembly) =>
            assembly.GetName().Version?.ToString() ?? "unknown";

        private static string InformationalVersion(Assembly assembly)
        {
            var attribute = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            return string.IsNullOrWhiteSpace(attribute?.InformationalVersion)
                ? AssemblyVersion(assembly)
                : attribute!.InformationalVersion;
        }

        private static string Bool(bool value) => value ? "true" : "false";

        private static string SafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars);
        }
    }
}
