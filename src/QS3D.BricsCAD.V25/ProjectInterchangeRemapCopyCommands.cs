using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeRemapCopyCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEREMAP", CommandFlags.Modal)]
        public void ImportRemappedSemanticCopy()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var fileDialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Semantic Snapshot thành bản sao đổi ID",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (fileDialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(fileDialog.FileName);
                var source = ProjectInterchangeValidatedSnapshotReader.Read(json);
                var suggested = ProjectInterchangeRemapCopyImporter.SuggestNamespace(source.Project.Id);
                var importNamespace = AskNamespace(suggested);
                if (importNamespace == null) return;

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var plan = ProjectInterchangeRemapCopyImporter.Plan(project, json, importNamespace);
                var examples = string.Join("\n", plan.Mappings
                    .Where(x => x.Kind == InterchangeRemapIdentityKind.Element)
                    .Take(5)
                    .Select(x => "• " + x.SourceId + " → " + x.TargetId));
                if (examples.Length == 0)
                    examples = string.Join("\n", plan.Mappings.Take(5).Select(x => "• " + x.SourceId + " → " + x.TargetId));

                var confirmation =
                    "IMPORT REMAPPED COPY / FEDERATED COPY\n\n" +
                    "Source project: " + plan.SourceProjectId + "\n" +
                    "Namespace: " + plan.ImportNamespace + "\n" +
                    "Zone +" + plan.ZonesToAdd.ToString(CultureInfo.InvariantCulture) +
                    " • Floor +" + plan.FloorsToAdd.ToString(CultureInfo.InvariantCulture) +
                    " • Family +" + plan.FamiliesToAdd.ToString(CultureInfo.InvariantCulture) +
                    " • Element +" + plan.ElementsToAdd.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Semantic property refs remap: " + plan.PropertyReferencesRemapped.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Source CAD handles discard: " + plan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "Generated ownership properties discard: " + plan.GeneratedOwnershipPropertiesDiscarded.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                    "Ví dụ mapping:\n" + examples + "\n\n" +
                    "Target hiện hữu KHÔNG bị replace/merge. Imported elements được đánh dirty để rebuild explicit. Không tự lưu .qsdb.\n\nTiếp tục?";
                if (MessageBox.Show(
                        confirmation,
                        "QS3D — Interchange Remapped Copy",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

                EnsureActive(document);
                var result = ProjectInterchangeRemapCopyImporter.Import(project, json, importNamespace);
                try { PaletteCoordinator.RefreshProject(); } catch { }
                var status =
                    "Interchange Remapped Copy: namespace " + result.ImportNamespace +
                    " • semantic +" + (result.ZonesAdded + result.FloorsAdded + result.FamiliesAdded + result.ElementsAdded).ToString(CultureInfo.InvariantCulture) +
                    " • refs remapped " + result.PropertyReferencesRemapped.ToString(CultureInfo.InvariantCulture) +
                    " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                    ". Rebuild explicit; chưa tự lưu .qsdb.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEREMAP lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEREMAP error: " + ex.Message + " Không có remapped-copy success claim nếu apply chưa hoàn tất.");
            }
        }

        private static string? AskNamespace(string suggested)
        {
            var window = new Window
            {
                Title = "QS3D — Namespace cho bản sao",
                Width = 480,
                Height = 190,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false
            };
            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock
            {
                Text = "Namespace xác định mapping ID ổn định. Dùng namespace khác nếu cần nhập cùng source thành một bản sao khác.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
            var textBox = new TextBox { Text = suggested ?? "source", MinHeight = 28 };
            panel.Children.Add(textBox);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var cancel = new Button { Content = "Hủy", MinWidth = 86, Margin = new Thickness(0, 0, 8, 0) };
            var ok = new Button { Content = "Tiếp tục", MinWidth = 96, IsDefault = true };
            cancel.Click += (_, __) => { window.DialogResult = false; };
            ok.Click += (_, __) => { window.DialogResult = true; };
            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            panel.Children.Add(buttons);
            window.Content = panel;
            textBox.SelectAll();
            textBox.Focus();
            return window.ShowDialog() == true ? textBox.Text : null;
        }

        private static void EnsureActive(Document document)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("Interchange remapped-copy import requires the DWG that started the operation to remain active.");
        }

        private static string ReadGuardedSnapshotText(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Interchange snapshot path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > ProjectInterchangeJsonValidator.MaxFileBytes)
                    throw new InvalidDataException("Semantic snapshot exceeds the guarded " + ProjectInterchangeJsonValidator.MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.");
                var length = checked((int)stream.Length);
                var bytes = new byte[length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) throw new EndOfStreamException("Semantic snapshot changed or ended while it was being read.");
                    offset += read;
                }
                if (stream.ReadByte() != -1)
                    throw new InvalidDataException("Semantic snapshot changed while it was being read.");
                try
                {
                    return StrictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException ex)
                {
                    throw new InvalidDataException("Semantic snapshot is not valid UTF-8.", ex);
                }
            }
        }
    }
}
