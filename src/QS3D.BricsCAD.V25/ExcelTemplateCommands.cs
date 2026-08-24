using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Microsoft.Win32;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ExcelTemplateCommands
    {
        private const long MaxMappingBytes = 64 * 1024;

        [CommandMethod("QS3DEXCELTEMPLATE", CommandFlags.UsePickSet)]
        public void ExportCompanyTemplate()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Xuất theo mẫu cần một QS3D project hiện hữu; lệnh không tạo project mới.");
                if (project.Elements.Count == 0)
                    throw new InvalidOperationException("Xuất theo mẫu chưa có semantic element để xuất.");

                var reviewedProjectId = project.Id;
                var reviewedVersion = project.ChangeVersion;
                var implied = Cad.EntitySnapshotReader.ReadImpliedSelection(document);
                var defaultScope = implied.Count > 0 ? "Selection" : "All";
                var scopePrompt = document.Editor.GetKeywords(
                    "\nPhạm vi mẫu Excel [Selection/Floor/Zone/All] <" + defaultScope + ">: ",
                    "Selection Floor Zone All");
                if (scopePrompt.Status != PromptStatus.OK && scopePrompt.Status != PromptStatus.None) return;
                var scope = scopePrompt.Status == PromptStatus.None ? defaultScope : scopePrompt.StringResult;

                var rowModePrompt = document.Editor.GetKeywords(
                    "\nDòng dữ liệu mẫu [Detail/Group] <Detail>: ",
                    "Detail Group");
                if (rowModePrompt.Status != PromptStatus.OK && rowModePrompt.Status != PromptStatus.None) return;
                var rowMode = rowModePrompt.Status == PromptStatus.None ? "Detail" : rowModePrompt.StringResult;

                var mappingModePrompt = document.Editor.GetKeywords(
                    "\nMapping mẫu [Default/Custom] <Default>: ",
                    "Default Custom");
                if (mappingModePrompt.Status != PromptStatus.OK && mappingModePrompt.Status != PromptStatus.None) return;
                var mappingMode = mappingModePrompt.Status == PromptStatus.None ? "Default" : mappingModePrompt.StringResult;

                var templateDialog = new OpenFileDialog
                {
                    Title = "QS3D • Chọn file mẫu Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (templateDialog.ShowDialog() != true) return;
                ValidateTemplatePath(templateDialog.FileName);

                QsWorkbookTemplateDefinition definition;
                string mappingLabel;
                if (string.Equals(mappingMode, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    var mappingDialog = new OpenFileDialog
                    {
                        Title = "QS3D • Chọn mapping JSON",
                        Filter = "QS3D Template Mapping (*.json)|*.json",
                        CheckFileExists = true,
                        Multiselect = false
                    };
                    if (mappingDialog.ShowDialog() != true) return;
                    definition = LoadCustomMapping(mappingDialog.FileName);
                    mappingLabel = Path.GetFileName(mappingDialog.FileName);
                }
                else
                {
                    definition = CreateDefaultMapping();
                    mappingLabel = "QS3D default";
                }

                var drawingName = string.IsNullOrWhiteSpace(document.Name)
                    ? "QS3D"
                    : Path.GetFileNameWithoutExtension(document.Name);
                var outputDialog = new SaveFileDialog
                {
                    Title = "QS3D • Xuất theo mẫu Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-QS3D-template.xlsx"
                };
                if (outputDialog.ShowDialog() != true) return;
                ValidateOutputPath(templateDialog.FileName, outputDialog.FileName);

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("Active DWG đã thay đổi trong lúc chọn template/mapping/output. Hãy chạy lại lệnh.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var promptProject)
                    || !string.Equals(promptProject.Id, reviewedProjectId, StringComparison.OrdinalIgnoreCase)
                    || promptProject.ChangeVersion != reviewedVersion)
                    throw new InvalidOperationException("Project đã thay đổi trong lúc chọn template/mapping/output. Hãy chạy lại lệnh.");

                // All user prompts, file choices and mapping validation complete before any
                // operation that may bind/update project unit state.
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DEXCELTEMPLATE")) return;

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("Active DWG đã thay đổi sau khi xác nhận unit policy. Hãy chạy lại lệnh.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)
                    || !string.Equals(currentProject.Id, reviewedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Project đã bị thay thế sau khi xác nhận unit policy. Hãy chạy lại lệnh.");
                var exportVersion = currentProject.ChangeVersion;

                var allScope = string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase);
                var elementIds = allScope ? Array.Empty<string>() : ResolveScope(document, currentProject, scope, implied);
                var preview = ProjectStateSnapshot.CreateDetachedCopy(currentProject);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                    .RegenerateDirty(preview);

                IReadOnlyList<QuantityReportRow> rows;
                if (string.Equals(rowMode, "Group", StringComparison.OrdinalIgnoreCase))
                {
                    rows = allScope
                        ? ProjectQuantityReportBuilder.Group(preview)
                        : ProjectQuantityReportBuilder.Group(preview, elementIds);
                }
                else if (string.Equals(rowMode, "Detail", StringComparison.OrdinalIgnoreCase))
                {
                    rows = allScope
                        ? ProjectQuantityReportBuilder.Detail(preview)
                        : ProjectQuantityReportBuilder.Detail(preview, elementIds);
                }
                else
                {
                    throw new InvalidOperationException("Template row mode không được hỗ trợ: " + rowMode + ".");
                }

                if (rows.Count == 0)
                    throw new InvalidOperationException("Phạm vi " + scope + " không có quantity row hợp lệ để xuất theo mẫu.");
                EnsureHandlesAreLive(document, rows);

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("Active DWG đã thay đổi trước khi ghi output. Hãy chạy lại lệnh.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var finalProject)
                    || !string.Equals(finalProject.Id, reviewedProjectId, StringComparison.OrdinalIgnoreCase)
                    || finalProject.ChangeVersion != exportVersion)
                    throw new InvalidOperationException("Project đã thay đổi trong lúc dựng quantity rows; output chưa được thay thế.");

                QsWorkbookTemplateExporter.Export(
                    templateDialog.FileName,
                    outputDialog.FileName,
                    rows,
                    definition);

                var status = "Xuất theo mẫu " + scope + "/" + rowMode + ": " + rows.Count +
                             " dòng • mapping " + mappingLabel + " • preview-regenerate " + regenerated +
                             " • " + outputDialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
            }
            catch (Exception error)
            {
                Report(document, "QS3DEXCELTEMPLATE", error);
            }
        }

        internal static QsWorkbookTemplateDefinition CreateDefaultMapping()
        {
            var fields = new[]
            {
                QsWorkbookTemplateField.Index,
                QsWorkbookTemplateField.Floor,
                QsWorkbookTemplateField.Zone,
                QsWorkbookTemplateField.FloorZone,
                QsWorkbookTemplateField.Category,
                QsWorkbookTemplateField.FamilyId,
                QsWorkbookTemplateField.FamilyName,
                QsWorkbookTemplateField.ElementName,
                QsWorkbookTemplateField.Material,
                QsWorkbookTemplateField.Note,
                QsWorkbookTemplateField.Count,
                QsWorkbookTemplateField.GrossConcreteM3,
                QsWorkbookTemplateField.DeductionM3,
                QsWorkbookTemplateField.NetConcreteM3,
                QsWorkbookTemplateField.FormworkM2,
                QsWorkbookTemplateField.LengthM,
                QsWorkbookTemplateField.OuterPerimeterM,
                QsWorkbookTemplateField.InnerPerimeterM,
                QsWorkbookTemplateField.DoorAreaM2,
                QsWorkbookTemplateField.SideAreaM2,
                QsWorkbookTemplateField.BottomAreaM2,
                QsWorkbookTemplateField.TopAreaM2,
                QsWorkbookTemplateField.OtherAreaM2,
                QsWorkbookTemplateField.DensityKgM3,
                QsWorkbookTemplateField.MassKg,
                QsWorkbookTemplateField.ElementIds,
                QsWorkbookTemplateField.SourceHandles,
                QsWorkbookTemplateField.DrawingFingerprint,
                QsWorkbookTemplateField.TraceKey
            };

            var mappings = fields.Select((field, index) =>
                new QsWorkbookTemplateMapping(field, ExcelColumn(index + 1))).ToArray();

            // Dedicated CHI_TIET templates commonly reserve a data block. The default
            // reserves rows 2..5001; templates with a footer/formulas inside that block
            // must use an explicit Custom mapping instead of being guessed by QS3D.
            return new QsWorkbookTemplateDefinition("CHI_TIET", 2, mappings, 5000);
        }

        internal static QsWorkbookTemplateDefinition LoadCustomMapping(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Mapping path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Template mapping JSON was not found.", fullPath);
            if (!string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Template mapping must use .json.");

            var info = new FileInfo(fullPath);
            if (info.Length <= 0 || info.Length > MaxMappingBytes)
                throw new InvalidDataException("Template mapping JSON must be between 1 byte and 64 KiB.");

            CustomMappingContract? contract;
            var serializer = new DataContractJsonSerializer(typeof(CustomMappingContract));
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                contract = serializer.ReadObject(stream) as CustomMappingContract;
            }
            if (contract == null) throw new InvalidDataException("Template mapping JSON is empty or invalid.");
            if (contract.Mappings == null || contract.Mappings.Count == 0)
                throw new InvalidDataException("Template mapping JSON requires at least one field mapping.");
            if (contract.Mappings.Count > Enum.GetValues(typeof(QsWorkbookTemplateField)).Length)
                throw new InvalidDataException("Template mapping JSON contains too many field mappings.");

            var mappings = new List<QsWorkbookTemplateMapping>(contract.Mappings.Count);
            foreach (var item in contract.Mappings)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Field) || string.IsNullOrWhiteSpace(item.Column))
                    throw new InvalidDataException("Each template mapping requires field and column.");
                QsWorkbookTemplateField field;
                if (!Enum.TryParse(item.Field.Trim(), true, out field) || !Enum.IsDefined(typeof(QsWorkbookTemplateField), field))
                    throw new InvalidDataException("Unknown template field: " + item.Field + ".");
                mappings.Add(new QsWorkbookTemplateMapping(field, item.Column));
            }

            return new QsWorkbookTemplateDefinition(
                contract.Worksheet ?? string.Empty,
                contract.FirstDataRow,
                mappings,
                contract.ReservedDataRows);
        }

        private static IReadOnlyList<string> ResolveScope(
            Document document,
            ProjectState project,
            string scope,
            IReadOnlyList<EntitySnapshot> implied)
        {
            if (string.Equals(scope, "Floor", StringComparison.OrdinalIgnoreCase))
            {
                var floor = project.FindFloor(project.ActiveFloorId)
                    ?? throw new InvalidOperationException("Template Floor cần một Floor/Level active hợp lệ.");
                return project.Elements
                    .Where(element => string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(element => element.Id).ToList().AsReadOnly();
            }

            if (string.Equals(scope, "Zone", StringComparison.OrdinalIgnoreCase))
            {
                var zone = project.FindZone(project.ActiveZoneId)
                    ?? throw new InvalidOperationException("Template Zone cần một Zone active hợp lệ.");
                return project.Elements
                    .Where(element => string.Equals(element.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(element => element.Id).ToList().AsReadOnly();
            }

            if (!string.Equals(scope, "Selection", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Template scope không được hỗ trợ: " + scope + ".");

            var snapshots = implied.Count > 0 ? implied : Cad.EntitySnapshotReader.ReadCurrentSelection(document);
            if (snapshots.Count == 0)
                throw new InvalidOperationException("Template Selection cần ít nhất một CAD object đã được QS3D theo dõi.");

            var selectedHandles = new HashSet<string>(
                snapshots.Select(snapshot => (snapshot.Handle ?? string.Empty).Trim()).Where(handle => handle.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            var elements = project.Elements
                .Where(element => SemanticReferenceHandles.MatchesSelection(element, selectedHandles)).ToList();
            if (elements.Count == 0)
                throw new InvalidOperationException("Template Selection không khớp semantic element nào.");

            var aliases = new HashSet<string>(
                elements.SelectMany(SemanticReferenceHandles.GetSelectionAliases),
                StringComparer.OrdinalIgnoreCase);
            var untracked = selectedHandles.Where(handle => !aliases.Contains(handle)).OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase).ToList();
            if (untracked.Count > 0)
                throw new InvalidOperationException("Template Selection trộn CAD object chưa thuộc semantic scope: " + string.Join(", ", untracked) + ".");

            return elements.Select(element => element.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList().AsReadOnly();
        }

        private static void EnsureHandlesAreLive(Document document, IReadOnlyList<QuantityReportRow> rows)
        {
            var expected = rows.SelectMany(row => row.SourceHandles)
                .Select(handle => Cad.CadHandleService.NormalizeHexHandle(handle)
                    ?? throw new InvalidOperationException("Template export contains an invalid CAD Handle: " + handle + "."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (expected.Count == 0)
                throw new InvalidOperationException("Template export scope has no CAD Handle provenance.");

            var live = Cad.CadHandleService.GetLiveHandles(document, expected);
            var missing = expected.Where(handle => !live.Contains(handle)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("Xuất theo mẫu bị chặn: " + missing.Count + " source Handle stale/missing. Chạy QS3DSYNCSOURCE hoặc recapture trước.");
        }

        private static void ValidateTemplatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("Template path is required.");
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("XLSX template was not found.", fullPath);
            if (!string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Company template must use .xlsx.");
        }

        private static void ValidateOutputPath(string templatePath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new InvalidDataException("Output path is required.");
            var template = Path.GetFullPath(templatePath);
            var output = Path.GetFullPath(outputPath);
            if (!string.Equals(Path.GetExtension(output), ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Template export output must use .xlsx.");
            if (string.Equals(template, output, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Output workbook must be different from the source template.");
        }

        private static string ExcelColumn(int column)
        {
            if (column < 1 || column > 16384) throw new ArgumentOutOfRangeException(nameof(column));
            var result = string.Empty;
            var current = column;
            while (current > 0)
            {
                current--;
                result = (char)('A' + current % 26) + result;
                current /= 26;
            }
            return result;
        }

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }

        [DataContract]
        internal sealed class CustomMappingContract
        {
            [DataMember(Name = "worksheet", IsRequired = true)]
            public string? Worksheet { get; set; }

            [DataMember(Name = "firstDataRow", IsRequired = true)]
            public int FirstDataRow { get; set; }

            [DataMember(Name = "reservedDataRows", IsRequired = true)]
            public int ReservedDataRows { get; set; }

            [DataMember(Name = "mappings", IsRequired = true)]
            public List<CustomMappingEntry>? Mappings { get; set; }
        }

        [DataContract]
        internal sealed class CustomMappingEntry
        {
            [DataMember(Name = "field", IsRequired = true)]
            public string? Field { get; set; }

            [DataMember(Name = "column", IsRequired = true)]
            public string? Column { get; set; }
        }
    }
}
