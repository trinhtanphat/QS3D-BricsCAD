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
    /// one command-specific EXTRUDE fallback grammar. It is also the published extension point
    /// for bounded direct CAD view/status tools. Mutation entries confirmation-gate, re-check the
    /// shared emergency stop before CAD dispatch and immediately before mutation, and write
    /// bounded diagnostics.
    /// </summary>
    internal static class McpCadDirectModelRuntime
    {
        private const int DbmodPersistentContentMask = 1 | 4 | 32;

        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "cad_create_box",
            "cad_extrude",
            "cad_boolean_union",
            "cad_boolean_subtract",
            "cad_boolean_intersect",
            "cad_save",
            "cad_save_as"
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

        internal static bool IsTool(string? tool)
        {
            return Tools.Contains(tool ?? string.Empty)
                   || McpCadLayerStateRuntime.IsTool(tool)
                   || McpCadViewStatusRuntime.IsTool(tool);
        }

        internal static bool RequiresMutation(string? tool)
        {
            if (McpCadLayerStateRuntime.IsTool(tool)) return McpCadLayerStateRuntime.RequiresMutation(tool);
            if (McpCadViewStatusRuntime.IsTool(tool)) return McpCadViewStatusRuntime.RequiresMutation(tool);
            return Tools.Contains(tool ?? string.Empty);
        }

        internal static bool CanHandleCadCommandSequence(string arguments)
        {
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            var command = NormalizeCommandToken(McpTopLevelJson.ExtractString(body, "command"));
            if (string.Equals(command, "EXTRUDE", StringComparison.Ordinal)
                || string.Equals(command, "QSAVE", StringComparison.Ordinal))
                return true;
            string layoutAction;
            string layoutName;
            return TryParseDirectLayoutCommand(command, McpTopLevelJson.ExtractString(body, "inputs"), out layoutAction, out layoutName);
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
                "Synchronously save the active rooted DWG and report success after persistent DBMOD content is clean; window/view bits may remain.",
                ConfirmProperty(),
                "\"confirmMutation\"");
            yield return Descriptor(
                "cad_save_as",
                "Safely save the active drawing to an absolute writable .dwg path after overwrite and protected-directory checks.",
                "\"path\":{\"type\":\"string\",\"maxLength\":1024},\"overwrite\":{\"type\":\"boolean\"}," + ConfirmProperty(),
                "\"path\",\"confirmMutation\"");
            foreach (var descriptor in McpCadLayerStateRuntime.ToolDescriptors()) yield return descriptor;
            foreach (var descriptor in McpCadViewStatusRuntime.ToolDescriptors()) yield return descriptor;
        }

        internal static string Call(string tool, string arguments)
        {
            if (!IsTool(tool)) throw new InvalidOperationException("Unknown direct MCP CAD model tool: " + tool);
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            if (McpCadLayerStateRuntime.IsTool(tool))
            {
                var mutation = McpCadLayerStateRuntime.RequiresMutation(tool);
                if (mutation)
                {
                    RequireConfirmedMutation(body, tool);
                    EnsureAutomationRunning();
                }
                return McpDiagnosticHub.InvokeInCadContext(() =>
                {
                    if (mutation) EnsureAutomationRunning();
                    return McpCadLayerStateRuntime.CallInCadContext(tool, body);
                });
            }
            if (McpCadViewStatusRuntime.IsTool(tool))
            {
                var mutation = McpCadViewStatusRuntime.RequiresMutation(tool);
                if (mutation)
                {
                    RequireConfirmedMutation(body, tool);
                    EnsureAutomationRunning();
                }
                return McpDiagnosticHub.InvokeInCadContext(() =>
                {
                    if (mutation) EnsureAutomationRunning();
                    return McpCadViewStatusRuntime.CallInCadContext(tool, body);
                });
            }

            RequireConfirmedMutation(body, tool);
            EnsureAutomationRunning();
            try
            {
                if (string.Equals(tool, "cad_save", StringComparison.Ordinal)) return Save();
                if (string.Equals(tool, "cad_save_as", StringComparison.Ordinal)) return SaveAs(body);
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
                        default: throw new InvalidOperationException("Unknown direct MCP CAD model tool: " + tool);
                    }
                    return result;
                });
            }
            catch (Exception ex)
            {
                RecordDirectMutationFailure(tool, ex);
                throw;
            }
        }

        internal static string CallCadCommandSequence(string arguments)
        {
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            RequireConfirmedMutation(body, "cad_command_sequence");
            var command = NormalizeCommandToken(McpTopLevelJson.ExtractString(body, "command"));
            var rawInputs = McpTopLevelJson.ExtractString(body, "inputs");
            string layoutAction;
            string layoutName;
            var directLayout = TryParseDirectLayoutCommand(command, rawInputs, out layoutAction, out layoutName);
            EnsureAutomationRunning();
            if (!string.Equals(command, "QSAVE", StringComparison.Ordinal)
                && !string.Equals(command, "EXTRUDE", StringComparison.Ordinal)
                && !directLayout)
                throw new InvalidOperationException("Direct multi-stage command grammar currently supports EXTRUDE, synchronous QSAVE, and bounded LAYOUT/-LAYOUT NEW/SET/DELETE only.");
            var inputs = string.Equals(command, "EXTRUDE", StringComparison.Ordinal)
                ? NormalizeExtrudeInputs(rawInputs)
                : string.Empty;
            if (string.Equals(command, "QSAVE", StringComparison.Ordinal)) return SaveCadCommandSequence();
            return McpDiagnosticHub.InvokeInCadContext(() =>
            {
                EnsureAutomationRunning();
                if (directLayout)
                    return ExecuteDirectLayoutCommand(command, layoutAction, layoutName);
                var document = RequireDocument();
                var script = "_.EXTRUDE\n" + inputs;
                if (!script.EndsWith("\n", StringComparison.Ordinal)) script += "\n";
                McpCadMutationCoordinator.QueueNativeCommand(
                    document,
                    command,
                    () => document.SendStringToExecute(script, true, false, true),
                    detail => McpCadAgentRuntime.AuditDomainMutation("cad_command_sequence", detail));
                McpDiagnosticHub.Record("mcp", "info", "cad-command-sequence", "command=EXTRUDE; boundedMultiStage=true; inputChars=" + inputs.Length.ToString(CultureInfo.InvariantCulture), document);
                return "{\"accepted\":true,\"command\":\"EXTRUDE\",\"multiStage\":true,\"inputChars\":" + inputs.Length.ToString(CultureInfo.InvariantCulture) + "}";
            });
        }

        private static string SaveCadCommandSequence()
        {
            Save();
            return "{\"accepted\":true,\"completed\":true,\"saved\":true,\"command\":\"QSAVE\",\"inputChars\":0}";
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
                var profileClone = source.Clone() as Curve;
                if (profileClone == null)
                    throw new InvalidOperationException("Could not clone the extrusion source Curve for database-resident kernel evaluation.");
                var solid = new Solid3d();
                var profileAppended = false;
                try
                {
                    var model = ModelSpace(transaction, document.Database, OpenMode.ForWrite);
                    model.AppendEntity(profileClone);
                    profileAppended = true;
                    transaction.AddNewlyCreatedDBObject(profileClone, true);
                    solid.SetDatabaseDefaults(document.Database);
                    solid.CreateExtrudedSolid(profileClone, new Vector3d(0d, 0d, height), new SweepOptions());
                    ApplyLayer(transaction, document.Database, solid, string.IsNullOrWhiteSpace(requestedLayer) ? source.Layer : requestedLayer);
                    var id = model.AppendEntity(solid);
                    transaction.AddNewlyCreatedDBObject(solid, true);
                    if (!profileClone.IsErased) profileClone.Erase();
                    transaction.Commit();
                    var resultHandle = id.Handle.ToString();
                    RecordMutation(document, "cad-extrude", "handle=" + resultHandle + "; sourceHandle=" + handle + "; kernelSource=database-resident-profile-clone");
                    return "{\"created\":true,\"handle\":\"" + Escape(resultHandle) + "\",\"type\":\"Solid3d\",\"sourceHandle\":\"" + Escape(handle) + "\"}";
                }
                catch
                {
                    if (solid.ObjectId.IsNull) solid.Dispose();
                    if (!profileAppended) profileClone.Dispose();
                    throw;
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
                var targetWorking = target.Clone() as Solid3d;
                if (targetWorking == null)
                    throw new InvalidOperationException("Could not clone the boolean target Solid3d for database-resident kernel evaluation.");
                var operandWorking = operand.Clone() as Solid3d;
                if (operandWorking == null)
                {
                    targetWorking.Dispose();
                    throw new InvalidOperationException("Could not clone the boolean tool Solid3d for database-resident kernel evaluation.");
                }
                var targetAppended = false;
                var operandAppended = false;
                var handedOver = false;
                Solid3d? resultClone = null;
                try
                {
                    var model = ModelSpace(transaction, document.Database, OpenMode.ForWrite);
                    model.AppendEntity(targetWorking);
                    targetAppended = true;
                    transaction.AddNewlyCreatedDBObject(targetWorking, true);
                    model.AppendEntity(operandWorking);
                    operandAppended = true;
                    transaction.AddNewlyCreatedDBObject(operandWorking, true);
                    EnsureAutomationRunning();
                    targetWorking.BooleanOperation(operation, operandWorking);
                    resultClone = targetWorking.Clone() as Solid3d;
                    if (resultClone == null)
                        throw new InvalidOperationException("Could not clone the boolean result for target identity handover.");
                    target.HandOverTo(resultClone, true, true);
                    handedOver = true;
                    if (!targetWorking.IsErased) targetWorking.Erase();
                    if (!operandWorking.IsErased) operandWorking.Erase();
                    if (!operand.IsErased) operand.Erase();
                    transaction.Commit();
                    RecordMutation(document, "cad-boolean", "targetHandle=" + targetHandle + "; consumedHandle=" + toolHandle + "; operation=" + operationName + "; kernelInputs=database-resident-working-clones; result=handed-over");
                    return "{\"updated\":true,\"resultHandle\":\"" + Escape(targetHandle) + "\",\"consumedHandle\":\"" + Escape(toolHandle) + "\",\"operation\":\"" + operationName + "\"}";
                }
                finally
                {
                    if (!targetAppended) targetWorking.Dispose();
                    if (!operandAppended) operandWorking.Dispose();
                    if (!handedOver && resultClone != null) resultClone.Dispose();
                }
            }
        }

        private static string Save()
        {
            EnsureAutomationRunning();
            var result = McpNativeCurrentDocumentSave.SaveCurrentDocument(
                EnsureAutomationRunning,
                detail => McpCadAgentRuntime.AuditDomainMutation("cad_save", detail));
            return "{\"saved\":true,\"completed\":true,\"fileName\":\"" + Escape(result.FileName)
                   + "\",\"route\":\"native-QSAVE-current-document\",\"dbmodAfterSave\":"
                   + result.DbmodAfterSave.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string SaveAs(string body)
        {
            var requested = McpTopLevelJson.ExtractString(body, "path");
            var fullPath = ValidateSaveAsPath(requested);
            var overwrite = McpTopLevelJson.ExtractBoolean(body, "overwrite");
            var existed = File.Exists(fullPath);
            if (existed && !overwrite)
                throw new InvalidOperationException("SaveAs target already exists. Set overwrite=true explicitly to replace it.");
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            EnsureWritableDirectory(directory);
            EnsureAutomationRunning();
            Document? document = null;
            McpDiagnosticHub.InvokeInCadContext(() =>
            {
                EnsureAutomationRunning();
                document = RequireDocument();
                var current = document.Database.Filename ?? string.Empty;
                if (Path.IsPathRooted(current)
                    && string.Equals(Path.GetFullPath(current), fullPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("cad_save_as target is the active drawing path. Use cad_save instead.");
                RequireIdle();
                using (document.LockDocument()) document.Database.SaveAs(fullPath, DwgVersion.Current);
                var actual = document.Database.Filename ?? string.Empty;
                if (!Path.IsPathRooted(actual)
                    || !string.Equals(Path.GetFullPath(actual), fullPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("BricsCAD SaveAs returned but the active database path did not match the requested target.");
                return string.Empty;
            });
            var saveResult = McpNativeCurrentDocumentSave.SaveCurrentDocument(
                EnsureAutomationRunning,
                detail => McpCadAgentRuntime.AuditDomainMutation("cad_save_as", detail));
            var leaf = SafeLeaf(fullPath);
            if (document != null)
                RecordMutation(document, "cad-save-as", "completed=true; fileName=" + leaf + "; overwrite=" + overwrite
                    + "; route=Database.SaveAs+native-QSAVE; dbmodAfterSave=" + saveResult.DbmodAfterSave.ToString(CultureInfo.InvariantCulture));
            return "{\"saved\":true,\"completed\":true,\"saveAs\":true,\"fileName\":\"" + Escape(leaf)
                   + "\",\"overwroteExisting\":" + (existed ? "true" : "false")
                   + ",\"route\":\"Database.SaveAs+native-QSAVE\",\"dbmodAfterSave\":"
                   + saveResult.DbmodAfterSave.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static bool TryParseDirectLayoutCommand(string command, string input, out string action, out string layoutName)
        {
            action = string.Empty;
            layoutName = string.Empty;
            if (!string.Equals(command, "-LAYOUT", StringComparison.Ordinal)
                && !string.Equals(command, "LAYOUT", StringComparison.Ordinal))
                return false;
            var value = (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (value.Length == 0 || value.Length > 2048 || value.IndexOf('\0') >= 0 || value.IndexOf('\u001b') >= 0 || value.IndexOf('\u0003') >= 0)
                return false;
            var parts = new List<string>();
            foreach (var raw in value.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = raw.Trim();
                if (part.Length > 0) parts.Add(part);
            }
            if (parts.Count != 2) return false;
            var option = NormalizeCommandToken(parts[0]);
            if (option == "N" || option == "NEW") action = "NEW";
            else if (option == "S" || option == "SET") action = "SET";
            else if (option == "D" || option == "DELETE") action = "DELETE";
            else return false;
            layoutName = parts[1].Trim();
            if (layoutName.Length == 0 || layoutName.Length > 255) return false;
            foreach (var ch in layoutName) if (ch < 32) return false;
            return true;
        }

        private static string ExecuteDirectLayoutCommand(string command, string action, string layoutName)
        {
            var document = RequireDocument();
            RequireIdle();
            EnsureAutomationRunning();
            using (document.LockDocument())
            {
                EnsureAutomationRunning();
                if (string.Equals(action, "NEW", StringComparison.Ordinal))
                {
                    if (LayoutExists(document.Database, layoutName))
                        throw new InvalidOperationException("Layout already exists: " + layoutName + ".");
                    LayoutManager.Current.CreateLayout(layoutName);
                }
                else if (string.Equals(action, "SET", StringComparison.Ordinal))
                {
                    if (!LayoutExists(document.Database, layoutName))
                        throw new InvalidOperationException("Layout does not exist: " + layoutName + ".");
                    LayoutManager.Current.CurrentLayout = layoutName;
                }
                else if (string.Equals(action, "DELETE", StringComparison.Ordinal))
                {
                    if (string.Equals(layoutName, "Model", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The Model layout cannot be deleted.");
                    if (!LayoutExists(document.Database, layoutName))
                        throw new InvalidOperationException("Layout does not exist: " + layoutName + ".");
                    if (string.Equals(LayoutManager.Current.CurrentLayout, layoutName, StringComparison.OrdinalIgnoreCase))
                        LayoutManager.Current.CurrentLayout = "Model";
                    LayoutManager.Current.DeleteLayout(layoutName);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported direct layout action.");
                }
            }
            var currentLayout = LayoutManager.Current.CurrentLayout ?? string.Empty;
            RecordMutation(document, "cad-layout", "completed=true; command=" + command + "; action=" + action + "; layout=" + layoutName + "; route=LayoutManager-direct");
            return "{\"accepted\":true,\"completed\":true,\"command\":\"" + Escape(command)
                   + "\",\"action\":\"" + Escape(action) + "\",\"layout\":\"" + Escape(layoutName)
                   + "\",\"currentLayout\":\"" + Escape(currentLayout) + "\",\"route\":\"LayoutManager-direct\"}";
        }

        private static bool LayoutExists(Database database, string layoutName)
        {
            using (var transaction = database.TransactionManager.StartOpenCloseTransaction())
            {
                var dictionary = (DBDictionary)transaction.GetObject(database.LayoutDictionaryId, OpenMode.ForRead);
                return dictionary.Contains(layoutName);
            }
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

        private static int WaitForSavedContentDbmod()
        {
            var deadline = DateTime.UtcNow.AddSeconds(2);
            var lastDbmod = -1;
            string raw;
            int dbmod;
            do
            {
                raw = Convert.ToString(Application.GetSystemVariable("DBMOD"), CultureInfo.InvariantCulture) ?? string.Empty;
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out dbmod))
                {
                    lastDbmod = dbmod;
                    // BricsCAD tracks window/view state separately; window/view DBMOD bits may remain after save.
                    if ((dbmod & DbmodPersistentContentMask) == 0)
                        return dbmod;
                }
                Thread.Sleep(25);
            }
            while (DateTime.UtcNow < deadline);
            throw new InvalidOperationException(
                "BricsCAD save returned but persistent-content DBMOD bits did not settle within 2 seconds; save completion was not confirmed; dbmod="
                + (lastDbmod >= 0 ? lastDbmod.ToString(CultureInfo.InvariantCulture) : "unavailable") + ".");
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

        private static void RecordDirectMutationFailure(string tool, Exception ex)
        {
            Document? document = null;
            try { document = Application.DocumentManager.MdiActiveDocument; } catch { }
            McpDiagnosticHub.Record(
                "mcp",
                "error",
                "cad-mutation-failed",
                "tool=" + (tool ?? string.Empty) + "; exception=" + ex.GetType().Name + "; reason=" + ex.Message,
                document);
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
