using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Runtime values shared by the BLT3D-familiar TOOL Ribbon controls and their commands.
    /// The values are deliberately process-local: changing the Ribbon field never mutates a
    /// drawing/project until the corresponding command is explicitly executed.
    /// </summary>
    internal static class BltToolRuntimeState
    {
        private const double MaxPileEmbedMillimeters = 100000d;
        private static double _pileEmbedMillimeters = 1000d;

        public static double PileEmbedMillimeters => _pileEmbedMillimeters;

        public static bool TrySetPileEmbedMillimeters(string? value, out string message)
        {
            var raw = (value ?? string.Empty).Trim();
            double parsed;
            if ((!double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed)
                 && !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                || !IsFinite(parsed)
                || parsed < 0d
                || parsed > MaxPileEmbedMillimeters)
            {
                message = "Ngàm cọc phải là số hữu hạn từ 0 đến "
                          + MaxPileEmbedMillimeters.ToString("0", CultureInfo.InvariantCulture)
                          + " mm.";
                return false;
            }

            _pileEmbedMillimeters = parsed;
            message = "Ngàm vào đài = " + parsed.ToString("0.###", CultureInfo.CurrentCulture) + " mm.";
            return true;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Functional backing commands for the owner-reference TOOL surface.
    /// Geometry actions operate only on explicitly selected native entities and are transactional.
    /// MCP actions expose truthful local configuration/transport diagnostics; they never report a
    /// protocol connection merely because a TCP socket can be opened.
    /// </summary>
    public sealed class BltToolCommands
    {
        private const double LeanConcreteThicknessMm = 100d;
        private const double LeanConcreteOverhangMm = 100d;
        private const double ExcavationClearanceMm = 500d;
        private const double ExcavationExtraBottomMm = 300d;
        private const int McpConnectTimeoutMilliseconds = 2000;

        [CommandMethod("QS3DBLTPILELOWER", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void LowerPilesToPileCap()
        {
            Run("QS3DBLTPILELOWER", document =>
            {
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DBLTPILELOWER")) return;

                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    Report(document, "Hạ cọc: chưa chọn đối tượng cọc native.");
                    return;
                }

                var pileIds = CadHandleService.Resolve(document, snapshots.Select(snapshot => snapshot.Handle));
                if (pileIds.Count == 0)
                {
                    Report(document, "Hạ cọc: selection không còn resolve được trong bản vẽ hiện tại.");
                    return;
                }

                var capPrompt = new PromptEntityOptions("\nChọn đài/móng làm chuẩn đáy để hạ cọc: ");
                var capResult = document.Editor.GetEntity(capPrompt);
                if (capResult.Status != PromptStatus.OK) return;

                var embedMillimeters = PromptMillimeters(
                    document.Editor,
                    "Ngàm vào đài a (mm)",
                    BltToolRuntimeState.PileEmbedMillimeters,
                    allowZero: true);
                if (!embedMillimeters.HasValue) return;
                if (!BltToolRuntimeState.TrySetPileEmbedMillimeters(
                        embedMillimeters.Value.ToString("R", CultureInfo.InvariantCulture),
                        out _))
                    throw new InvalidOperationException("Giá trị ngàm cọc không hợp lệ.");

                var embedDrawing = CadUnitService.MetersToDrawingUnits(document, embedMillimeters.Value / 1000d);
                var embedRequestedPositive = embedMillimeters.Value > 0d;
                var moved = 0;
                var alreadyAligned = 0;

                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var cap = transaction.GetObject(capResult.ObjectId, OpenMode.ForRead, false) as Entity;
                    if (cap == null || cap.IsErased)
                        throw new InvalidOperationException("Đài/móng chuẩn không còn tồn tại.");
                    var capExtents = RequireExtents(cap, "đài/móng chuẩn");
                    if (!GeometryOffsetPrecision.TryAddNonNegative(
                            capExtents.MinPoint.Z,
                            embedDrawing,
                            embedRequestedPositive,
                            out var targetPileTopZ))
                        throw new InvalidOperationException("Ngàm cọc dương không thể biểu diễn ổn định tại cao độ đài hiện tại.");

                    foreach (var id in pileIds)
                    {
                        if (id.IsNull || id.IsErased || id == capResult.ObjectId) continue;
                        var pile = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                        if (pile == null || pile.IsErased) continue;
                        var pileExtents = RequireExtents(pile, "cọc " + pile.Handle);
                        var deltaZ = targetPileTopZ - pileExtents.MaxPoint.Z;
                        RequireFinite(deltaZ, "độ dịch Z cọc");
                        if (Math.Abs(deltaZ) <= 1e-9d)
                        {
                            alreadyAligned++;
                            continue;
                        }

                        pile.TransformBy(Matrix3d.Displacement(new Vector3d(0d, 0d, deltaZ)));
                        moved++;
                    }

                    transaction.Commit();
                }

                document.Editor.Regen();
                Report(
                    document,
                    "Hạ cọc: đã căn " + moved + " đối tượng; " + alreadyAligned
                    + " đối tượng đã đúng cao độ; đỉnh cọc = đáy đài + "
                    + embedMillimeters.Value.ToString("0.###", CultureInfo.CurrentCulture) + " mm.");
            });
        }

        [CommandMethod("QS3DBLTLEANCONCRETE", CommandFlags.Modal)]
        public void CreateLeanConcrete()
        {
            Run("QS3DBLTLEANCONCRETE", document =>
            {
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DBLTLEANCONCRETE")) return;
                var referenceId = PromptReferenceEntity(document, "Chọn móng/đài để tạo bê tông lót: ");
                if (!referenceId.HasValue) return;

                var thicknessMm = PromptMillimeters(document.Editor, "Chiều dày bê tông lót (mm)", LeanConcreteThicknessMm, false);
                if (!thicknessMm.HasValue) return;
                var overhangMm = PromptMillimeters(document.Editor, "Phần vươn mỗi phía (mm)", LeanConcreteOverhangMm, true);
                if (!overhangMm.HasValue) return;

                var thickness = CadUnitService.MetersToDrawingUnits(document, thicknessMm.Value / 1000d);
                var overhang = CadUnitService.MetersToDrawingUnits(document, overhangMm.Value / 1000d);
                var overhangRequestedPositive = overhangMm.Value > 0d;
                string handle;

                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var reference = transaction.GetObject(referenceId.Value, OpenMode.ForRead, false) as Entity;
                    if (reference == null || reference.IsErased)
                        throw new InvalidOperationException("Móng/đài tham chiếu không còn tồn tại.");
                    var extents = RequireExtents(reference, "móng/đài tham chiếu");
                    if (!GeometryOffsetPrecision.TryExpandBoth(
                            extents.MinPoint.X,
                            extents.MaxPoint.X,
                            overhang,
                            overhangRequestedPositive,
                            out var minX,
                            out _,
                            out var width))
                        throw new InvalidOperationException("Phần vươn bê tông lót theo X không thể biểu diễn ổn định.");
                    if (!GeometryOffsetPrecision.TryExpandBoth(
                            extents.MinPoint.Y,
                            extents.MaxPoint.Y,
                            overhang,
                            overhangRequestedPositive,
                            out var minY,
                            out _,
                            out var depth))
                        throw new InvalidOperationException("Phần vươn bê tông lót theo Y không thể biểu diễn ổn định.");
                    if (!GeometryOffsetPrecision.TrySubtractNonNegative(
                            extents.MinPoint.Z,
                            thickness,
                            true,
                            out var minZ))
                        throw new InvalidOperationException("Chiều dày bê tông lót không thể biểu diễn ổn định tại cao độ hiện tại.");

                    width = Positive(width, "bề rộng bê tông lót");
                    depth = Positive(depth, "chiều sâu bê tông lót");
                    thickness = Positive(thickness, "chiều dày bê tông lót");
                    var desiredMin = new Point3d(minX, minY, minZ);
                    handle = AppendBox(document, transaction, width, depth, thickness, desiredMin, reference.Layer);
                    transaction.Commit();
                }

                document.Editor.Regen();
                Report(
                    document,
                    "Bê tông lót: đã tạo native Solid3d " + handle
                    + " theo bounding box móng, dày " + thicknessMm.Value.ToString("0.###", CultureInfo.CurrentCulture)
                    + " mm, vươn " + overhangMm.Value.ToString("0.###", CultureInfo.CurrentCulture)
                    + " mm mỗi phía. Đây là hình học native, chưa tự gán semantic Family.");
            });
        }

        [CommandMethod("QS3DBLTFOUNDATIONEXCAVATE", CommandFlags.Modal)]
        public void CreateFoundationExcavationVolume()
        {
            Run("QS3DBLTFOUNDATIONEXCAVATE", document =>
            {
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DBLTFOUNDATIONEXCAVATE")) return;
                var referenceId = PromptReferenceEntity(document, "Chọn móng/đài để tạo thể tích hố đào: ");
                if (!referenceId.HasValue) return;

                var clearanceMm = PromptMillimeters(document.Editor, "Khoảng thao tác mỗi phía (mm)", ExcavationClearanceMm, true);
                if (!clearanceMm.HasValue) return;
                var extraBottomMm = PromptMillimeters(document.Editor, "Đào sâu thêm dưới đáy móng (mm)", ExcavationExtraBottomMm, true);
                if (!extraBottomMm.HasValue) return;

                var clearance = CadUnitService.MetersToDrawingUnits(document, clearanceMm.Value / 1000d);
                var extraBottom = CadUnitService.MetersToDrawingUnits(document, extraBottomMm.Value / 1000d);
                var clearanceRequestedPositive = clearanceMm.Value > 0d;
                var extraBottomRequestedPositive = extraBottomMm.Value > 0d;
                string handle;

                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var reference = transaction.GetObject(referenceId.Value, OpenMode.ForRead, false) as Entity;
                    if (reference == null || reference.IsErased)
                        throw new InvalidOperationException("Móng/đài tham chiếu không còn tồn tại.");
                    var extents = RequireExtents(reference, "móng/đài tham chiếu");
                    if (!GeometryOffsetPrecision.TryExpandBoth(
                            extents.MinPoint.X,
                            extents.MaxPoint.X,
                            clearance,
                            clearanceRequestedPositive,
                            out var minX,
                            out _,
                            out var width))
                        throw new InvalidOperationException("Khoảng thao tác hố đào theo X không thể biểu diễn ổn định.");
                    if (!GeometryOffsetPrecision.TryExpandBoth(
                            extents.MinPoint.Y,
                            extents.MaxPoint.Y,
                            clearance,
                            clearanceRequestedPositive,
                            out var minY,
                            out _,
                            out var depth))
                        throw new InvalidOperationException("Khoảng thao tác hố đào theo Y không thể biểu diễn ổn định.");
                    if (!GeometryOffsetPrecision.TryExpandLower(
                            extents.MinPoint.Z,
                            extents.MaxPoint.Z,
                            extraBottom,
                            extraBottomRequestedPositive,
                            out var bottomZ,
                            out var height))
                        throw new InvalidOperationException("Độ đào sâu thêm không thể biểu diễn ổn định tại cao độ hiện tại.");

                    width = Positive(width, "bề rộng hố đào");
                    depth = Positive(depth, "chiều sâu hố đào");
                    height = Positive(height, "chiều cao thể tích hố đào");
                    var desiredMin = new Point3d(minX, minY, bottomZ);
                    handle = AppendBox(document, transaction, width, depth, height, desiredMin, reference.Layer);
                    transaction.Commit();
                }

                document.Editor.Regen();
                Report(
                    document,
                    "Đào hố móng: đã tạo native Solid3d thể tích đào " + handle
                    + " theo bounding box móng, khoảng thao tác " + clearanceMm.Value.ToString("0.###", CultureInfo.CurrentCulture)
                    + " mm, sâu thêm " + extraBottomMm.Value.ToString("0.###", CultureInfo.CurrentCulture)
                    + " mm. Lệnh không tự Boolean trừ địa hình và không giả lập semantic Earthwork.");
            });
        }

        [CommandMethod("QS3DMCPSETTINGS", CommandFlags.Modal)]
        public void ConfigureMcpEndpoint()
        {
            Run("QS3DMCPSETTINGS", document =>
            {
                var current = McpEndpointConfiguration.DescribeEffective();
                Report(document, "MCP hiện tại: " + current);

                var options = new PromptStringOptions(
                    "\nNhập MCP endpoint (http://host:port, https://host:port, tcp://host:port) hoặc CLEAR để xóa cấu hình lưu: ")
                {
                    AllowSpaces = true
                };
                var result = document.Editor.GetString(options);
                if (result.Status != PromptStatus.OK) return;
                var raw = (result.StringResult ?? string.Empty).Trim();
                if (string.Equals(raw, "CLEAR", StringComparison.OrdinalIgnoreCase))
                {
                    McpEndpointConfiguration.ClearSaved();
                    Report(document, "Đã xóa endpoint MCP lưu cục bộ. Biến môi trường QS3D_MCP_ENDPOINT, nếu có, vẫn được ưu tiên.");
                    return;
                }

                if (!McpEndpointConfiguration.TryParse(raw, out var endpoint, out var error))
                    throw new InvalidOperationException(error);
                McpEndpointConfiguration.Save(endpoint!);
                Report(document, "Đã lưu MCP endpoint: " + endpoint + ". " + McpEndpointConfiguration.DescribeEffective());
            });
        }

        [CommandMethod("QS3DMCPDOCS", CommandFlags.Modal)]
        public void OpenMcpDocs()
        {
            Run("QS3DMCPDOCS", document =>
            {
                var path = McpEndpointConfiguration.WriteLocalGuide();
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    Report(document, "Đã mở tài liệu MCP cục bộ: " + path);
                }
                catch (Exception ex)
                {
                    Report(document, "Đã tạo tài liệu MCP tại " + path + " nhưng không mở được bằng shell: " + ex.Message);
                }
            });
        }

        [CommandMethod("QS3DMCPCHECK", CommandFlags.Modal)]
        public void CheckMcpTransport()
        {
            Run("QS3DMCPCHECK", document =>
            {
                if (!McpEndpointConfiguration.TryGetEffective(out var endpoint, out var source, out var error))
                {
                    Report(document, "MCP chưa sẵn sàng: " + error);
                    return;
                }

                var probe = ProbeTcp(endpoint!, McpConnectTimeoutMilliseconds);
                Report(
                    document,
                    "MCP endpoint " + endpoint + " (" + source + "): " + probe.Message
                    + " Kiểm tra này chỉ xác nhận đường TCP, không xác nhận MCP protocol/health.");
            });
        }

        [CommandMethod("QS3DAIDASHBOARD", CommandFlags.Modal)]
        public void ShowAiDashboard()
        {
            Run("QS3DAIDASHBOARD", document =>
            {
                string endpointText;
                string sourceText;
                string transportText;
                if (McpEndpointConfiguration.TryGetEffective(out var endpoint, out var source, out var error))
                {
                    var probe = ProbeTcp(endpoint!, McpConnectTimeoutMilliseconds);
                    endpointText = endpoint!.ToString();
                    sourceText = source;
                    transportText = probe.Message + " (transport only; không phải MCP protocol health).";
                }
                else
                {
                    endpointText = "Chưa cấu hình";
                    sourceText = "-";
                    transportText = error;
                }

                var text = "QS3D AI / MCP\n\n"
                           + "Endpoint: " + endpointText + "\n"
                           + "Nguồn: " + sourceText + "\n"
                           + "Trạng thái: " + transportText + "\n\n"
                           + "Cài đặt: QS3DMCPSETTINGS\n"
                           + "Kiểm tra: QS3DMCPCHECK\n"
                           + "Tài liệu: QS3DMCPDOCS";
                MessageBox.Show(text, "QS3D AI Dashboard", MessageBoxButton.OK, MessageBoxImage.Information);
                Report(document, "Đã mở bảng điều khiển AI/MCP.");
            });
        }

        private static void Run(string operation, Action<Document> action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                action(document);
            }
            catch (Exception ex)
            {
                Report(document, operation + " lỗi: " + ex.Message);
            }
        }

        private static ObjectId? PromptReferenceEntity(Document document, string prompt)
        {
            var result = document.Editor.GetEntity(new PromptEntityOptions("\n" + prompt));
            return result.Status == PromptStatus.OK ? result.ObjectId : (ObjectId?)null;
        }

        private static double? PromptMillimeters(Editor editor, string caption, double defaultValue, bool allowZero)
        {
            var options = new PromptDoubleOptions(
                "\n" + caption + " <" + defaultValue.ToString("0.###", CultureInfo.CurrentCulture) + ">: ")
            {
                AllowNegative = false,
                AllowZero = allowZero,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.None) return defaultValue;
            if (result.Status != PromptStatus.OK) return null;
            if (!IsFinite(result.Value) || result.Value < 0d || (!allowZero && result.Value <= 0d))
                throw new InvalidOperationException(caption + " phải là số hữu hạn " + (allowZero ? ">= 0" : "> 0") + ".");
            return result.Value;
        }

        private static Extents3d RequireExtents(Entity entity, string label)
        {
            Extents3d extents;
            try
            {
                extents = entity.GeometricExtents;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(label + " không cung cấp geometric extents hợp lệ.", ex);
            }

            RequireFinite(extents.MinPoint.X, label + " min X");
            RequireFinite(extents.MinPoint.Y, label + " min Y");
            RequireFinite(extents.MinPoint.Z, label + " min Z");
            RequireFinite(extents.MaxPoint.X, label + " max X");
            RequireFinite(extents.MaxPoint.Y, label + " max Y");
            RequireFinite(extents.MaxPoint.Z, label + " max Z");
            return extents;
        }

        private static string AppendBox(
            Document document,
            Transaction transaction,
            double width,
            double depth,
            double height,
            Point3d desiredMin,
            string layer)
        {
            var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
            var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
            if (document.Database.CurrentSpaceId != modelSpaceId)
                throw new InvalidOperationException("TOOL 3D geometry chỉ tạo trong Model Space.");
            var modelSpace = (BlockTableRecord)transaction.GetObject(modelSpaceId, OpenMode.ForWrite);

            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateBox(width, depth, height);
                var initial = RequireExtents(solid, "khối TOOL mới");
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(
                    desiredMin.X - initial.MinPoint.X,
                    desiredMin.Y - initial.MinPoint.Y,
                    desiredMin.Z - initial.MinPoint.Z)));
                if (!string.IsNullOrWhiteSpace(layer)) solid.Layer = layer;
                modelSpace.AppendEntity(solid);
                transaction.AddNewlyCreatedDBObject(solid, true);
                return solid.Handle.ToString();
            }
            catch
            {
                solid.Dispose();
                throw;
            }
        }

        private static double Positive(double value, string label)
        {
            RequireFinite(value, label);
            if (!(value > 0d)) throw new InvalidOperationException(label + " phải > 0.");
            return value;
        }

        private static void RequireFinite(double value, string label)
        {
            if (!IsFinite(value)) throw new InvalidOperationException(label + " phải là số hữu hạn.");
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void Report(Document document, string message) =>
            document.Editor.WriteMessage("\nQS3D " + message);

        private static McpProbeResult ProbeTcp(Uri endpoint, int timeoutMilliseconds)
        {
            var port = endpoint.Port;
            if (port <= 0 || port > 65535)
                return new McpProbeResult(false, "port không hợp lệ.");

            try
            {
                using (var client = new TcpClient())
                {
                    var asyncResult = client.BeginConnect(endpoint.Host, port, null, null);
                    using (var wait = asyncResult.AsyncWaitHandle)
                    {
                        if (!wait.WaitOne(timeoutMilliseconds))
                            return new McpProbeResult(false, "TCP timeout sau " + timeoutMilliseconds + " ms.");
                        client.EndConnect(asyncResult);
                    }
                    return new McpProbeResult(true, "TCP reachable.");
                }
            }
            catch (Exception ex)
            {
                return new McpProbeResult(false, "TCP unavailable: " + ex.Message);
            }
        }

        private sealed class McpProbeResult
        {
            public McpProbeResult(bool reachable, string message)
            {
                Reachable = reachable;
                Message = message;
            }

            public bool Reachable { get; }
            public string Message { get; }
        }
    }

    internal static class McpEndpointConfiguration
    {
        private const string EnvironmentVariableName = "QS3D_MCP_ENDPOINT";
        private const string ConfigFileName = "mcp-endpoint.txt";

        public static bool TryGetEffective(out Uri? endpoint, out string source, out string error)
        {
            var environment = (Environment.GetEnvironmentVariable(EnvironmentVariableName) ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(environment))
            {
                if (TryParse(environment, out endpoint, out error))
                {
                    source = "environment " + EnvironmentVariableName;
                    return true;
                }
                source = "environment " + EnvironmentVariableName;
                error = source + " không hợp lệ: " + error;
                return false;
            }

            var path = ConfigPath();
            if (File.Exists(path))
            {
                var saved = File.ReadAllText(path, Encoding.UTF8).Trim();
                if (TryParse(saved, out endpoint, out error))
                {
                    source = "saved config";
                    return true;
                }
                source = "saved config";
                error = "Cấu hình lưu không hợp lệ: " + error;
                return false;
            }

            endpoint = null;
            source = "none";
            error = "chưa cấu hình endpoint. Dùng QS3DMCPSETTINGS hoặc biến môi trường " + EnvironmentVariableName + ".";
            return false;
        }

        public static bool TryParse(string raw, out Uri? endpoint, out string error)
        {
            endpoint = null;
            var value = (raw ?? string.Empty).Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || string.IsNullOrWhiteSpace(parsed.Host))
            {
                error = "endpoint phải là absolute URI có host.";
                return false;
            }

            if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(parsed.Scheme, "tcp", StringComparison.OrdinalIgnoreCase))
            {
                error = "chỉ hỗ trợ scheme http, https hoặc tcp.";
                return false;
            }

            if (string.Equals(parsed.Scheme, "tcp", StringComparison.OrdinalIgnoreCase) && parsed.Port <= 0)
            {
                error = "tcp endpoint phải khai báo port.";
                return false;
            }

            endpoint = parsed;
            error = string.Empty;
            return true;
        }

        public static void Save(Uri endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            var directory = Path.GetDirectoryName(ConfigPath());
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Không resolve được thư mục cấu hình MCP.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(ConfigPath(), endpoint.AbsoluteUri, new UTF8Encoding(false));
        }

        public static void ClearSaved()
        {
            var path = ConfigPath();
            if (File.Exists(path)) File.Delete(path);
        }

        public static string DescribeEffective()
        {
            return TryGetEffective(out var endpoint, out var source, out var error)
                ? endpoint + " (" + source + ")"
                : error;
        }

        public static string WriteLocalGuide()
        {
            var path = Path.Combine(Path.GetTempPath(), "QS3D-MCP-README.txt");
            var text =
                "QS3D MCP / AI local integration\r\n"
                + "================================\r\n\r\n"
                + "Configure endpoint: QS3DMCPSETTINGS\r\n"
                + "Transport check: QS3DMCPCHECK\r\n"
                + "Dashboard: QS3DAIDASHBOARD\r\n\r\n"
                + "Configuration priority:\r\n"
                + "1. Environment variable QS3D_MCP_ENDPOINT\r\n"
                + "2. %APPDATA%\\QS3D\\mcp-endpoint.txt\r\n\r\n"
                + "Accepted endpoint schemes: http, https, tcp. TCP requires an explicit port.\r\n"
                + "The connection check tests only TCP reachability. It does NOT claim MCP protocol or model health.\r\n"
                + "No credentials are written by these commands.\r\n";
            File.WriteAllText(path, text, new UTF8Encoding(false));
            return path;
        }

        private static string ConfigPath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", ConfigFileName);
    }
}
