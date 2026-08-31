using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Deterministic MCP mutations that use native BricsCAD/Teigha APIs. This runtime is
    /// deliberately narrow: it owns direct solids/saves plus bounded QS3D authoring bridges and
    /// one command-specific EXTRUDE fallback grammar. Every entry point confirmation-gates,
    /// re-checks the shared emergency stop before CAD dispatch and immediately before mutation,
    /// and writes bounded diagnostics.
    /// </summary>
    internal static class McpCadDirectModelRuntime
    {
        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "cad_create_box",
            "cad_extrude",
            "cad_boolean_union",
            "cad_boolean_subtract",
            "cad_boolean_intersect",
            "cad_save",
            "cad_save_as",
            "qs3d_place_single_footing"
        };

        private static readonly HashSet<string> KnownCommandTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LINE", "PLINE", "3DPOLY", "CIRCLE", "ARC", "RECTANG", "POLYGON", "ELLIPSE", "SPLINE", "POINT",
            "HATCH", "-HATCH", "BOUNDARY", "REGION", "BOX", "CYLINDER", "SPHERE", "CONE", "WEDGE", "TORUS",
            "EXTRUDE", "PRESSPULL", "REVOLVE", "SWEEP", "LOFT", "UNION", "SUBTRACT", "INTERSECT", "SLICE",
            "MOVE", "COPY", "ROTATE", "SCALE", "MIRROR", "OFFSET", "TRIM", "EXTEND", "FILLET", "CHAMFER",
            "STRETCH", "ARRAY", "ERASE", "EXPLODE", "JOIN", "PEDIT", "MATCHPROP", "CHPROP", "PROPERTIES",
            "LAYER", "-LAYER", "LINETYPE", "-LINETYPE", "COLOR", "STYLE", "-STYLE", "TEXT", "DTEXT", "MTEXT",
            "DIM", "DIMLINEAR", "DIMALIGNED", "DIMANGULAR", "DIMRADIUS", "DIMDIAMETER", "DIMSTYLE", "-DIMSTYLE",
            "LEADER", "MLEADER", "BLOCK", "-BLOCK", "WBLOCK", "INSERT", "-INSERT", "XREF", "-XREF", "IMAGEATTACH",
            "LAYOUT", "-LAYOUT", "MVIEW", "MSPACE", "PSPACE", "PLOT", "-PLOT", "PAGESETUP", "ZOOM", "PAN",
            "REGEN", "REGENALL", "UCS", "PLAN", "VPOINT", "VIEW", "-VIEW", "SELECT", "QSELECT", "ISOLATEOBJECTS",
            "UNISOLATEOBJECTS", "UNDO", "REDO", "QSAVE", "SAVEAS", "OPEN", "NEW", "CLOSE", "PURGE", "-PURGE",
            "AUDIT", "OVERKILL"
        };

        internal static bool IsTool(string tool)
        {
            return Tools.Contains(tool ?? string.Empty);
        }

        internal static bool CanHandleCadCommandSequence(string arguments)
        {
            var command = NormalizeCommandToken(McpTopLevelJson.ExtractString(arguments ?? "{}", "command"));
            return string.Equals(command, "EXTRUDE", StringComparison.Ordinal)
                   || string.Equals(command, "QSAVE", StringComparison.Ordinal);
        }

        internal static IEnumerable<string> ToolDescriptors()
        {
            yield return Descriptor(
                "cad_create_box",
                "Create a native Solid3d box centered at x,y,z using direct BricsCAD database APIs.",
                Numeric("x", "y", "z", "length", "width", "height") + LayerAndConfirm(),
                "\"x\",\"y\",\"z\",\"length\",\"width\",\"height\",\"confirmMutation\"");
            yield return Descriptor(
                "cad_extrude",
                "Extrude one closed planar curve vertically into a native Solid3d without prompt scripting.",
                "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"height\":{\"type\":\"number\"}" + LayerAndConfirm(),
                "\"handle\",\"height\",\"confirmMutation\"");
            yield return BooleanDescriptor("cad_boolean_union", "Union target Solid3d with tool Solid3d; the tool solid is consumed after success.");
            yield return BooleanDescriptor("cad_boolean_subtract", "Subtract tool Solid3d from target Solid3d; the tool solid is consumed after success.");
            yield return BooleanDescriptor("cad_boolean_intersect", "Intersect target Solid3d with tool Solid3d; the tool solid is consumed after success.");
            yield return Descriptor(
                "cad_save",
                "Synchronously save the active rooted DWG and report success only after DBMOD is clean.",
                ConfirmProperty(),
                "\"confirmMutation\"");
            yield return Descriptor(
                "cad_save_as",
                "Safely save the active drawing to an absolute writable .dwg path after overwrite and protected-directory checks.",
                "\"path\":{\"type\":\"string\",\"maxLength\":1024},\"overwrite\":{\"type\":\"boolean\"}," + ConfirmProperty(),
                "\"path\",\"confirmMutation\"");
            yield return Descriptor(
                "qs3d_place_single_footing",
                "Place the active QS3D Móng đơn Family at drawing x,y. Active Floor elevation is resolved by the shared Móng đơn authoring workflow.",
                Numeric("x", "y") + "," + ConfirmProperty(),
                "\"x\",\"y\",\"confirmMutation\"");
        }

        internal static string Call(string tool, string arguments)
        {
            if (!IsTool(tool)) throw new InvalidOperationException("Unknown direct MCP CAD model tool: " + tool);
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            RequireConfirmedMutation(body, tool);
            EnsureAutomationRunning();
            return McpDiagnosticHub.InvokeInCadContext(() =>
            {
                EnsureAutomationRunning();
                string result;
                switch (tool)
                {
                    case "cad_create_box": result = CreateBox(body); break;
                    case "cad_extrude": result = Extrude(body); break;
                    case "cad_boolean_union": result = Boolean(body, BooleanOperationType.BoolUnite, "union"); break;
                    case "cad_boolean_subtract": result = Boolean(body, BooleanOperationType.BoolSubtract, "subtract"); break;
                    case "cad_boolean_intersect": result = Boolean(body, BooleanOperationType.BoolIntersect, "intersect"); break;
                    case "cad_save": result = Save(); break;
                    case "cad_save_as": result = SaveAs(body); break;
                    case "qs3d_place_single_footing": result = PlaceSingleFooting(body); break;
                    default: throw new InvalidOperationException("Unknown direct MCP CAD model tool: " + tool);
                }
                return result;
            });
        }

        internal static string CallCadCommandSequence(string arguments)
        {
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            RequireConfirmedMutation(body, "cad_command_sequence");
            var command = NormalizeCommandToken(McpTopLevelJson.ExtractString(body, "command"));
            EnsureAutomationRunning();
            if (string.Equals(command, "QSAVE", StringComparison.Ordinal))
            {
                Save();
                return "{\"accepted\":true,\"completed\":true,\"saved\":true,\"command\":\"QSAVE\",\"inputChars\":0}";
            }
            if (!string.Equals(command, "EXTRUDE", StringComparison.Ordinal))
                throw new InvalidOperationException("Direct multi-stage command grammar currently supports EXTRUDE and synchronous QSAVE only.");
            var inputs = NormalizeExtrudeInputs(McpTopLevelJson.ExtractString(body, "inputs"));
            return McpDiagnosticHub.InvokeInCadContext(() =>
            {
                EnsureAutomationRunning();
                var document = RequireDocument();
                var script = "_.EXTRUDE\n" + inputs;
                if (!script.EndsWith("\n", StringComparison.Ordinal)) script += "\n";
                document.SendStringToExecute(script, true, false, true);
                McpDiagnosticHub.Record("mcp", "info", "cad-command-sequence", "command=EXTRUDE; boundedMultiStage=true; inputChars=" + inputs.Length.ToString(CultureInfo.InvariantCulture), document);
                return "{\"accepted\":true,\"command\":\"EXTRUDE\",\"multiStage\":true,\"inputChars\":" + inputs.Length.ToString(CultureInfo.InvariantCulture) + "}";
            });
        }

        private static string CreateBox(string body)
        {
            var x = NumberRequired(body, "x");
            var y = NumberRequired(body, "y");
            var z = NumberRequired(body, "z");
            var length = Positive(body, "length");
            var width = Positive(body, "width");
            var height = Positive(body, "height");
            var layer = LayerOptional(body);
            var document = RequireDocument();
            EnsureAutomationRunning();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var solid = new Solid3d();
                try
                {
                    solid.SetDatabaseDefaults(document.Database);
                    solid.CreateBox(length, width, height);
                    solid.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, z)));
                    ApplyLayer(transaction, document.Database, solid, layer);
                    var model = ModelSpace(transaction, document.Database, OpenMode.ForWrite);
                    var id = model.AppendEntity(solid);
                    transaction.AddNewlyCreatedDBObject(solid, true);
                    transaction.Commit();
                    var handle = id.Handle.ToString();
                    RecordMutation(document, "cad-create-box", "handle=" + handle);
                    return "{\"created\":true,\"handle\":\"" + Escape(handle) + "\",\"type\":\"Solid3d\",\"primitive\":\"box\"}";
                }
                catch
                {
                    if (solid.ObjectId.IsNull) solid.Dispose();
                    throw;
                }
            }
        }

        private static string Extrude(string body)
        {
            var handle = Handle(body, "handle");
            var height = NumberRequired(body, "height");
            if (Math.Abs(height) <= 1e-12) throw new InvalidOperationException("height must be non-zero.");
            var requestedLayer = LayerOptional(body);
            var document = RequireDocument();
            EnsureAutomationRunning();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var source = OpenEntity(transaction, document.Database, handle, OpenMode.ForRead) as Curve;
                if (source == null) throw new InvalidOperationException("cad_extrude requires a curve entity handle.");
                Curve? clone = null;
                Region? region = null;
                var solid = new Solid3d();
                try
                {
                    clone = source.Clone() as Curve;
                    if (clone == null) throw new InvalidOperationException("Could not clone the source curve for safe region construction.");
                    var regions = Region.CreateFromCurves(new DBObjectCollection { clone });
                    if (regions == null || regions.Count != 1 || !(regions[0] is Region generatedRegion))
                    {
                        if (regions != null)
                            foreach (DBObject item in regions) item.Dispose();
                        throw new InvalidOperationException("Source curve must form exactly one closed planar region for cad_extrude.");
                    }
                    region = generatedRegion;
                    solid.SetDatabaseDefaults(document.Database);
                    solid.CreateExtrudedSolid(region, new Vector3d(0d, 0d, height), new SweepOptions());
                    ApplyLayer(transaction, document.Database, solid, string.IsNullOrWhiteSpace(requestedLayer) ? source.Layer : requestedLayer);
                    var model = ModelSpace(transaction, document.Database, OpenMode.ForWrite);
                    var id = model.AppendEntity(solid);
                    transaction.AddNewlyCreatedDBObject(solid, true);
                    transaction.Commit();
                    var resultHandle = id.Handle.ToString();
                    RecordMutation(document, "cad-extrude", "handle=" + resultHandle + "; sourceHandle=" + handle);
                    return "{\"created\":true,\"handle\":\"" + Escape(resultHandle) + "\",\"type\":\"Solid3d\",\"sourceHandle\":\"" + Escape(handle) + "\"}";
                }
                catch
                {
                    if (solid.ObjectId.IsNull) solid.Dispose();
                    throw;
                }
                finally
                {
                    region?.Dispose();
                    clone?.Dispose();
                }
            }
        }

        private static string Boolean(string body, BooleanOperationType operation, string operationName)
        {
            var targetHandle = Handle(body, "targetHandle");
            var toolHandle = Handle(body, "toolHandle");
            if (string.Equals(targetHandle, toolHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("targetHandle and toolHandle must identify different Solid3d entities.");
            var document = RequireDocument();
            EnsureAutomationRunning();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var target = OpenEntity(transaction, document.Database, targetHandle, OpenMode.ForWrite) as Solid3d;
                var operand = OpenEntity(transaction, document.Database, toolHandle, OpenMode.ForWrite) as Solid3d;
                if (target == null || operand == null)
                    throw new InvalidOperationException("Boolean operations require two live Solid3d entity handles.");
                EnsureAutomationRunning();
                target.BooleanOperation(operation, operand);
                if (!operand.IsErased) operand.Erase();
                transaction.Commit();
                RecordMutation(document, "cad-boolean", "targetHandle=" + targetHandle + "; consumedHandle=" + toolHandle + "; operation=" + operationName);
                return "{\"updated\":true,\"resultHandle\":\"" + Escape(targetHandle) + "\",\"consumedHandle\":\"" + Escape(toolHandle) + "\",\"operation\":\"" + operationName + "\"}";
            }
        }

        private static string PlaceSingleFooting(string body)
        {
            var x = NumberRequired(body, "x");
            var y = NumberRequired(body, "y");
            var document = RequireDocument();
            EnsureAutomationRunning();
            var handle = SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d));
            RecordMutation(document, "qs3d-place-single-footing", "handle=" + handle);
            return "{\"created\":true,\"handle\":\"" + Escape(handle) + "\",\"type\":\"SingleFooting\",\"elevationPolicy\":\"active-floor\"}";
        }

        private static string Save()
        {
            var document = RequireDocument();
            var filename = document.Database.Filename ?? string.Empty;
            if (!Path.IsPathRooted(filename))
                throw new InvalidOperationException("Active drawing has no existing local path. Use cad_save_as first.");
            RequireIdle();
            EnsureAutomationRunning();
            using (document.LockDocument()) document.Database.SaveAs(filename, DwgVersion.Current);
            WaitForCleanDbmod();
            RecordMutation(document, "cad-save", "completed=true; fileName=" + SafeLeaf(filename) + "; route=SaveAs-current-path");
            return "{\"saved\":true,\"completed\":true,\"fileName\":\"" + Escape(SafeLeaf(filename)) + "\"}";
        }

        private static string SaveAs(string body)
        {
            var requested = McpTopLevelJson.ExtractString(body, "path");
            var fullPath = ValidateSaveAsPath(requested);
            var overwrite = McpTopLevelJson.ExtractBoolean(body, "overwrite");
            var document = RequireDocument();
            var current = document.Database.Filename ?? string.Empty;
            if (Path.IsPathRooted(current)
                && string.Equals(Path.GetFullPath(current), fullPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("cad_save_as target is the active drawing path. Use cad_save instead.");
            var existed = File.Exists(fullPath);
            if (existed && !overwrite)
                throw new InvalidOperationException("SaveAs target already exists. Set overwrite=true explicitly to replace it.");
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            EnsureWritableDirectory(directory);
            RequireIdle();
            EnsureAutomationRunning();
            using (document.LockDocument()) document.Database.SaveAs(fullPath, DwgVersion.Current);
            var actual = document.Database.Filename ?? string.Empty;
            if (!Path.IsPathRooted(actual)
                || !string.Equals(Path.GetFullPath(actual), fullPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("BricsCAD SaveAs returned but the active database path did not match the requested target.");
            WaitForCleanDbmod();
            var leaf = SafeLeaf(fullPath);
            RecordMutation(document, "cad-save-as", "completed=true; fileName=" + leaf + "; overwrite=" + overwrite);
            return "{\"saved\":true,\"completed\":true,\"saveAs\":true,\"fileName\":\"" + Escape(leaf)
                   + "\",\"overwroteExisting\":" + (existed ? "true" : "false") + "}";
        }

        private static string NormalizeExtrudeInputs(string input)
        {
            var value = (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (value.Length > 16000 || value.IndexOf('\0') >= 0 || value.IndexOf('\u001b') >= 0 || value.IndexOf('\u0003') >= 0)
                throw new InvalidOperationException("inputs exceeds bounds or contains forbidden control characters.");
            var lines = value.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length > 64) throw new InvalidOperationException("inputs exceeds 64 prompt lines.");
            var afterSelectionTerminator = false;
            var postSelectionValues = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length > 1024) throw new InvalidOperationException("one command input line exceeds 1024 characters.");
                foreach (var ch in line) if (ch < 32 && ch != '\t') throw new InvalidOperationException("inputs contains forbidden control characters.");
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    if (i < lines.Length - 1) afterSelectionTerminator = true;
                    continue;
                }
                var token = NormalizeCommandToken(trimmed);
                if (KnownCommandTokens.Contains(token) || token.StartsWith("QS3D", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("inputs may not inject another CAD/QS3D command.");
                if (!afterSelectionTerminator) continue;
                double numeric;
                if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out numeric)
                    || double.IsNaN(numeric) || double.IsInfinity(numeric))
                    throw new InvalidOperationException("EXTRUDE post-selection input is limited to finite numeric height/taper values.");
                postSelectionValues++;
                if (postSelectionValues > 2)
                    throw new InvalidOperationException("EXTRUDE accepts at most two bounded post-selection numeric values.");
            }
            return value;
        }

        private static string ValidateSaveAsPath(string requested)
        {
            var value = (requested ?? string.Empty).Trim();
            if (value.Length == 0 || value.Length > 1024 || value.IndexOf('\0') >= 0)
                throw new InvalidOperationException("path must be an absolute .dwg path up to 1024 characters.");
            if (!Path.IsPathRooted(value)) throw new InvalidOperationException("cad_save_as requires an absolute path.");
            string fullPath;
            try { fullPath = Path.GetFullPath(value); }
            catch (Exception ex) { throw new InvalidOperationException("cad_save_as path is invalid.", ex); }
            if (!string.Equals(Path.GetExtension(fullPath), ".dwg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("cad_save_as only accepts .dwg targets.");
            if (IsProtectedInstallationPath(fullPath))
                throw new InvalidOperationException("cad_save_as refuses protected Windows/application installation directories. Choose a writable project or user document folder.");
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new InvalidOperationException("cad_save_as destination directory does not exist.");
            return fullPath;
        }

        private static bool IsProtectedInstallationPath(string path)
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
                Environment.GetEnvironmentVariable("WINDIR") ?? string.Empty
            };
            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                string normalized;
                try { normalized = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
                catch { continue; }
                if (string.Equals(path, normalized, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(normalized + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(normalized + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void EnsureWritableDirectory(string directory)
        {
            var probe = Path.Combine(directory, ".qs3d-mcp-write-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.None))
                    stream.WriteByte(0);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("cad_save_as destination directory is not writable.", ex);
            }
            finally
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
            }
        }

        private static void RequireIdle()
        {
            var raw = Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? string.Empty;
            int active;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out active) || active != 0)
                throw new InvalidOperationException("Cannot save while a BricsCAD command is active. Wait for idle or cancel the active command before retrying.");
        }

        private static void WaitForCleanDbmod()
        {
            var deadline = DateTime.UtcNow.AddSeconds(2);
            string raw;
            int dbmod;
            do
            {
                raw = Convert.ToString(Application.GetSystemVariable("DBMOD"), CultureInfo.InvariantCulture) ?? string.Empty;
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out dbmod) && dbmod == 0)
                    return;
                Thread.Sleep(25);
            }
            while (DateTime.UtcNow < deadline);
            throw new InvalidOperationException("BricsCAD save returned but DBMOD did not settle to zero within 2 seconds; save completion was not confirmed.");
        }

        private static void RequireConfirmedMutation(string body, string tool)
        {
            if (!McpTopLevelJson.ExtractBoolean(body, "confirmMutation"))
                throw new InvalidOperationException("confirmMutation=true is required for " + tool + ".");
        }

        private static void EnsureAutomationRunning()
        {
            if (McpCadAgentRuntime.AutomationStopped)
                throw new InvalidOperationException("Automation is emergency-stopped. Resume the MCP CAD agent before mutating the drawing.");
            McpCadAgentRuntime.EnsureCurrentMutationRunning();
        }

        private static void RecordMutation(Document document, string eventName, string detail)
        {
            McpDiagnosticHub.Record("mcp", "info", eventName, detail, document);
        }

        private static BlockTableRecord ModelSpace(Transaction transaction, Database database, OpenMode mode)
        {
            var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], mode);
        }

        private static Entity OpenEntity(Transaction transaction, Database database, string handleText, OpenMode mode)
        {
            long value;
            if (!long.TryParse(handleText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new InvalidOperationException("Invalid entity handle.");
            ObjectId id;
            try { id = database.GetObjectId(false, new Handle(value), 0); }
            catch (Exception ex) { throw new InvalidOperationException("Entity handle was not found.", ex); }
            if (id.IsNull) throw new InvalidOperationException("Entity handle was not found.");
            var entity = transaction.GetObject(id, mode, false) as Entity;
            if (entity == null || entity.IsErased) throw new InvalidOperationException("Object handle is not a live entity.");
            return entity;
        }

        private static string Handle(string body, string property)
        {
            var value = McpTopLevelJson.ExtractString(body, property).Trim();
            if (value.Length == 0 || value.Length > 32 || !Regex.IsMatch(value, "^[0-9A-Fa-f]+$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException(property + " must be a hexadecimal entity handle up to 32 characters.");
            return value;
        }

        private static double Positive(string body, string property)
        {
            var value = NumberRequired(body, property);
            if (!(value > 0d)) throw new InvalidOperationException(property + " must be > 0.");
            return value;
        }

        private static double NumberRequired(string body, string property)
        {
            double value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractDouble(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) throw new InvalidOperationException(property + " must be a finite number.");
            return value;
        }

        private static string LayerOptional(string body)
        {
            var value = McpTopLevelJson.ExtractString(body, "layer").Trim();
            if (value.Length == 0) return string.Empty;
            if (value.Length > 255) throw new InvalidOperationException("layer exceeds 255 characters.");
            foreach (var ch in value) if (ch < 32) throw new InvalidOperationException("layer contains control characters.");
            return value;
        }

        private static void ApplyLayer(Transaction transaction, Database database, Entity entity, string layer)
        {
            if (string.IsNullOrWhiteSpace(layer)) return;
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (!table.Has(layer))
            {
                table.UpgradeOpen();
                var record = new LayerTableRecord { Name = layer };
                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);
            }
            entity.Layer = layer;
        }

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document.");
            return document;
        }

        private static string NormalizeCommandToken(string value)
        {
            var token = (value ?? string.Empty).Trim();
            var index = 0;
            while (index < token.Length && (token[index] == '_' || token[index] == '.')) index++;
            return token.Substring(index).ToUpperInvariant();
        }

        private static string SafeLeaf(string path)
        {
            try { return Path.GetFileName(path) ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string Escape(string value)
        {
            return McpEmbeddedServer.JsonEscape(value ?? string.Empty);
        }

        private static string BooleanDescriptor(string name, string description)
        {
            return Descriptor(
                name,
                description,
                "\"targetHandle\":{\"type\":\"string\",\"maxLength\":32},\"toolHandle\":{\"type\":\"string\",\"maxLength\":32}," + ConfirmProperty(),
                "\"targetHandle\",\"toolHandle\",\"confirmMutation\"");
        }

        private static string Descriptor(string name, string description, string properties, string required)
        {
            return "{\"name\":\"" + name + "\",\"description\":\"" + description
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + properties
                   + "},\"required\":[" + required + "],\"additionalProperties\":false}}";
        }

        private static string Numeric(params string[] names)
        {
            var parts = new List<string>();
            foreach (var name in names) parts.Add("\"" + name + "\":{\"type\":\"number\"}");
            return string.Join(",", parts);
        }

        private static string LayerAndConfirm()
        {
            return ",\"layer\":{\"type\":\"string\",\"maxLength\":255}," + ConfirmProperty();
        }

        private static string ConfirmProperty()
        {
            return "\"confirmMutation\":{\"type\":\"boolean\",\"const\":true}";
        }
    }
}
