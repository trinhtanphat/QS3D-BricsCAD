using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using QS3D.Core.Agent;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// BricsCAD-side runtime behind the embedded MCP transport. This class owns all CAD
    /// database/editor work, bounded command dispatch, BricsCAD-process-only SendInput,
    /// emergency stop state and local mutation audit evidence.
    /// </summary>
    internal static class McpCadAgentRuntime
    {
        private const int CadDispatchTimeoutMilliseconds = 15000;
        private const long MaxAuditBytes = 4L * 1024L * 1024L;
        private const string AuditFileName = "mcp-agent-audit.jsonl";
        private const int CadWorkQueued = 0;
        private const int CadWorkRunning = 1;
        private const int CadWorkCancelledBeforeStart = 2;
        internal const string Qs3dCommandPattern = "^QS3D[A-Za-z0-9_]*$";

        private static readonly object AuditSync = new object();
        private static readonly AsyncLocal<int?> MutationEpoch = new AsyncLocal<int?>();
        private static readonly HashSet<string> AllowedCadCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
        private static readonly HashSet<string> NoInputCadCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "REGEN", "REGENALL", "QSAVE", "REDO", "UNISOLATEOBJECTS"
        };
        private static readonly HashSet<string> ReadableSystemVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CMDACTIVE", "INSUNITS", "CLAYER", "CTAB", "TILEMODE", "DWGNAME", "CVPORT", "ORTHOMODE", "OSMODE"
        };

        private static volatile bool _automationStopped;
        private static int _automationEpoch;

        public static bool AutomationStopped { get { return _automationStopped; } }
        public static string AuditFilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", AuditFileName); }
        }

        public static void ResetForServerStart()
        {
            Interlocked.Increment(ref _automationEpoch);
            _automationStopped = false;
            McpQs3dDomainRuntime.ResetForServerStart();
        }

        public static void StopAutomation()
        {
            _automationStopped = true;
            Interlocked.Increment(ref _automationEpoch);
        }

        public static string Call(string toolName, string arguments)
        {
            var tool = (toolName ?? string.Empty).Trim();
            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            var executionMode = McpToolCapabilityContract.ResolveExecutionMode(
                McpTopLevelJson.ExtractString(args, "executionMode"),
                McpTopLevelJson.ExtractString(args, "execution_mode"));
            McpToolCapabilityContract.EnsureAllowed(tool, executionMode, ToolRequiresMutation(tool));
            switch (tool)
            {
                case "mcp_status": return InvokeCad(() => BuildMcpStatusJson(executionMode));
                case "bricscad_status": return InvokeCad(BuildBricscadStatusJson);
                case "qs3d_status": return InvokeCad(() => McpQs3dDomainRuntime.BuildStatusJson(true));
                case "qs3d_domain_status": return InvokeCad(() => McpQs3dDomainRuntime.BuildStatusJson(false));
                case "cad_active_document": return InvokeCad(BuildActiveDocumentJson);
                case "cad_selection": return InvokeCad(BuildSelectionJson);
                case "cad_database_snapshot": return InvokeCad(() => BuildDatabaseSnapshotJson(Integer(args, "limit", 250, 1, 1000)));
                case "cad_entity_inspect": return InspectEntity(args);
                case "cad_view_state": return InvokeCad(BuildViewStateJson);
                case "cad_wait_idle": return WaitUntilIdle(Integer(args, "timeoutMs", 10000, 100, 30000));
                case "cad_sysvar": return ReadSystemVariable(args);
                case "cad_create_line": return Mutation(args, tool, () => CreateLine(args));
                case "cad_create_circle": return Mutation(args, tool, () => CreateCircle(args));
                case "cad_create_arc": return Mutation(args, tool, () => CreateArc(args));
                case "cad_create_polyline": return Mutation(args, tool, () => CreatePolyline(args));
                case "cad_create_text": return Mutation(args, tool, () => CreateText(args));
                case "cad_create_mtext": return Mutation(args, tool, () => CreateMText(args));
                case "cad_entity_transform": return Mutation(args, tool, () => TransformEntity(args));
                case "cad_entity_delete": return Mutation(args, tool, () => DeleteEntity(args));
                case "cad_entity_set_layer": return Mutation(args, tool, () => SetEntityLayer(args));
                case "cad_layer": return Mutation(args, tool, () => LayerAction(args));
                case "cad_command_catalog": return CommandCatalogJson();
                case "cad_command_sequence":
                    return Mutation(args, tool, () =>
                        McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)
                            ? McpCadDirectModelRuntime.CallCadCommandSequence(args)
                            : RunCadCommandSequence(args));
                case "qs3d_run_command": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));
                case "qs3d_place_single_footing": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));
                case "cad_ui_click": return Mutation(args, tool, () => UiClick(args));
                case "cad_ui_type": return Mutation(args, tool, () => UiType(args));
                case "cad_ui_key": return Mutation(args, tool, () => UiKey(args));
                case "cad_agent_stop": return EmergencyStop();
                case "cad_agent_resume": return ResumeAgent(args);
                case "cad_audit_tail": return ReadAuditTail(Integer(args, "limit", 25, 1, 100));
                case "cad_cancel_command": return CancelCurrentCommand();
                default:
                    if (McpCadDirectModelRuntime.IsTool(tool))
                        return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));
                    if (McpDesktopAutomationRuntime.IsTool(tool))
                    {
                        if (McpDesktopAutomationRuntime.RequiresMutation(tool))
                            return Mutation(args, tool, () => McpDesktopAutomationRuntime.Call(
                                tool, args, EnsureCurrentMutationRunning, detail => Audit(tool, detail)));
                        return McpDesktopAutomationRuntime.Call(tool, args, null, detail => Audit(tool, detail));
                    }
                    throw new InvalidOperationException("Unknown MCP CAD tool: " + tool);
            }
        }

        private static bool ToolRequiresMutation(string? tool)
        {
            var normalizedTool = tool ?? string.Empty;
            switch (normalizedTool)
            {
                case "cad_create_line":
                case "cad_create_circle":
                case "cad_create_arc":
                case "cad_create_polyline":
                case "cad_create_text":
                case "cad_create_mtext":
                case "cad_entity_transform":
                case "cad_entity_delete":
                case "cad_entity_set_layer":
                case "cad_layer":
                case "cad_command_sequence":
                case "cad_ui_click":
                case "cad_ui_type":
                case "cad_ui_key":
                case "cad_agent_stop":
                case "cad_agent_resume":
                case "cad_cancel_command":
                    return true;
            }
            if (McpQs3dDomainRuntime.IsTool(normalizedTool)) return McpQs3dDomainRuntime.RequiresMutation(normalizedTool);
            if (McpCadDirectModelRuntime.IsTool(normalizedTool)) return McpCadDirectModelRuntime.RequiresMutation(normalizedTool);
            if (McpDesktopAutomationRuntime.IsTool(normalizedTool)) return McpDesktopAutomationRuntime.RequiresMutation(normalizedTool);
            return false;
        }

        private static string Mutation(string body, string tool, Func<string> action)
        {
            EnsureAutomationRunning();
            if (!McpTopLevelJson.ExtractBoolean(body, "confirmMutation"))
                throw new InvalidOperationException("confirmMutation=true is required for " + tool + ".");

            var epoch = Volatile.Read(ref _automationEpoch);
            EnsureAutomationRunning(epoch);
            var previousEpoch = MutationEpoch.Value;
            MutationEpoch.Value = epoch;
            try { return action(); }
            finally { MutationEpoch.Value = previousEpoch; }
        }

        private static void EnsureAutomationRunning()
        {
            if (_automationStopped)
                throw new InvalidOperationException("Automation is emergency-stopped. Call cad_agent_resume with confirmMutation=true first.");
        }

        private static void EnsureAutomationRunning(int expectedEpoch)
        {
            if (_automationStopped || Volatile.Read(ref _automationEpoch) != expectedEpoch)
                throw new InvalidOperationException("Automation was stopped or restarted before this mutation could continue. Submit a new confirmed mutation after resume.");
        }

        internal static void EnsureCurrentMutationRunning()
        {
            var epoch = MutationEpoch.Value;
            if (!epoch.HasValue)
                throw new InvalidOperationException("Mutation execution context is unavailable.");
            EnsureAutomationRunning(epoch.Value);
        }

        internal static void AuditDomainMutation(string tool, string detail)
        {
            Audit(tool, detail);
        }

        private static string CreateLine(string body)
        {
            var entity = new Line(
                new Point3d(NumberRequired(body, "x1"), NumberRequired(body, "y1"), NumberOptional(body, "z1", 0d)),
                new Point3d(NumberRequired(body, "x2"), NumberRequired(body, "y2"), NumberOptional(body, "z2", 0d)));
            return InvokeCadMutation(() => AddEntity(entity, LayerOptional(body), "cad_create_line"));
        }

        private static string CreateCircle(string body)
        {
            var radius = NumberRequired(body, "radius");
            if (!(radius > 0d)) throw new InvalidOperationException("radius must be > 0.");
            var entity = new Circle(
                new Point3d(NumberRequired(body, "x"), NumberRequired(body, "y"), NumberOptional(body, "z", 0d)),
                Vector3d.ZAxis,
                radius);
            return InvokeCadMutation(() => AddEntity(entity, LayerOptional(body), "cad_create_circle"));
        }

        private static string CreateArc(string body)
        {
            var radius = NumberRequired(body, "radius");
            if (!(radius > 0d)) throw new InvalidOperationException("radius must be > 0.");
            var start = NumberRequired(body, "startAngleDeg") * Math.PI / 180d;
            var end = NumberRequired(body, "endAngleDeg") * Math.PI / 180d;
            if (Math.Abs(end - start) < 1e-12) throw new InvalidOperationException("Arc start/end angles must define a non-zero sweep.");
            var entity = new Arc(
                new Point3d(NumberRequired(body, "x"), NumberRequired(body, "y"), NumberOptional(body, "z", 0d)),
                radius,
                start,
                end);
            return InvokeCadMutation(() => AddEntity(entity, LayerOptional(body), "cad_create_arc"));
        }

        private static string CreatePolyline(string body)
        {
            var raw = McpTopLevelJson.ExtractString(body, "points");
            if (raw.Length > 16000) throw new InvalidOperationException("points exceeds 16000 characters.");
            var points = ParsePoints2d(raw);
            if (points.Count < 2) throw new InvalidOperationException("Polyline requires at least two x,y points.");
            if (points.Count > 2048) throw new InvalidOperationException("Polyline exceeds 2048 vertices.");
            var closed = McpTopLevelJson.ExtractBoolean(body, "closed");
            var elevation = NumberOptional(body, "elevation", 0d);
            return InvokeCadMutation(() =>
            {
                var entity = new Polyline(points.Count);
                for (var i = 0; i < points.Count; i++) entity.AddVertexAt(i, points[i], 0d, 0d, 0d);
                entity.Closed = closed;
                entity.Elevation = elevation;
                return AddEntity(entity, LayerOptional(body), "cad_create_polyline");
            });
        }

        private static string CreateText(string body)
        {
            var text = RequiredText(body, "text", 4000, true);
            var height = NumberRequired(body, "height");
            if (!(height > 0d)) throw new InvalidOperationException("height must be > 0.");
            var entity = new DBText
            {
                TextString = text,
                Position = new Point3d(NumberRequired(body, "x"), NumberRequired(body, "y"), NumberOptional(body, "z", 0d)),
                Height = height,
                Rotation = NumberOptional(body, "rotationDeg", 0d) * Math.PI / 180d
            };
            return InvokeCadMutation(() => AddEntity(entity, LayerOptional(body), "cad_create_text"));
        }

        private static string CreateMText(string body)
        {
            var text = RequiredText(body, "text", 16000, false);
            var height = NumberRequired(body, "height");
            if (!(height > 0d)) throw new InvalidOperationException("height must be > 0.");
            var width = NumberOptional(body, "width", 0d);
            if (width < 0d) throw new InvalidOperationException("width must be >= 0.");
            return InvokeCadMutation(() =>
            {
                var entity = new MText
                {
                    Location = new Point3d(NumberRequired(body, "x"), NumberRequired(body, "y"), NumberOptional(body, "z", 0d)),
                    TextHeight = height,
                    Contents = text,
                    Normal = Vector3d.ZAxis,
                    Rotation = NumberOptional(body, "rotationDeg", 0d) * Math.PI / 180d
                };
                if (width > 0d) entity.Width = width;
                return AddEntity(entity, LayerOptional(body), "cad_create_mtext");
            });
        }

        private static string AddEntity(Entity entity, string layer, string auditTool)
        {
            var document = RequireDocument();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                entity.SetDatabaseDefaults(document.Database);
                EnsureLayer(transaction, document.Database, layer, entity);
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var id = model.AppendEntity(entity);
                transaction.AddNewlyCreatedDBObject(entity, true);
                transaction.Commit();
                var handle = id.Handle.ToString();
                Audit(auditTool, "handle=" + handle);
                return "{\"created\":true,\"handle\":\"" + Escape(handle) + "\",\"type\":\"" + Escape(entity.GetType().Name) + "\"}";
            }
        }

        private static string TransformEntity(string body)
        {
            var handle = Handle(body);
            var action = McpTopLevelJson.ExtractString(body, "action").Trim().ToLowerInvariant();
            return InvokeCadMutation(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var entity = OpenEntity(transaction, document.Database, handle, OpenMode.ForWrite);
                    if (action == "move")
                    {
                        entity.TransformBy(Matrix3d.Displacement(new Vector3d(
                            NumberOptional(body, "dx", 0d), NumberOptional(body, "dy", 0d), NumberOptional(body, "dz", 0d))));
                    }
                    else if (action == "rotate")
                    {
                        entity.TransformBy(Matrix3d.Rotation(NumberRequired(body, "angleDeg") * Math.PI / 180d, Vector3d.ZAxis, EntityCenter(entity)));
                    }
                    else if (action == "scale")
                    {
                        var factor = NumberRequired(body, "factor");
                        if (!(factor > 0d)) throw new InvalidOperationException("factor must be > 0.");
                        entity.TransformBy(Matrix3d.Scaling(factor, EntityCenter(entity)));
                    }
                    else throw new InvalidOperationException("action must be move, rotate or scale.");
                    transaction.Commit();
                    Audit("cad_entity_transform", "handle=" + handle + "; action=" + action);
                    return "{\"updated\":true,\"handle\":\"" + Escape(handle) + "\",\"action\":\"" + Escape(action) + "\"}";
                }
            });
        }

        private static string DeleteEntity(string body)
        {
            var handle = Handle(body);
            return InvokeCadMutation(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    OpenEntity(transaction, document.Database, handle, OpenMode.ForWrite).Erase();
                    transaction.Commit();
                    Audit("cad_entity_delete", "handle=" + handle);
                    return "{\"erased\":true,\"handle\":\"" + Escape(handle) + "\"}";
                }
            });
        }

        private static string SetEntityLayer(string body)
        {
            var handle = Handle(body);
            var layer = LayerRequired(body, "layer");
            return InvokeCadMutation(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var entity = OpenEntity(transaction, document.Database, handle, OpenMode.ForWrite);
                    EnsureLayer(transaction, document.Database, layer, entity);
                    transaction.Commit();
                    Audit("cad_entity_set_layer", "handle=" + handle + "; layer=" + layer);
                    return "{\"updated\":true,\"handle\":\"" + Escape(handle) + "\",\"layer\":\"" + Escape(layer) + "\"}";
                }
            });
        }

        private static string LayerAction(string body)
        {
            var action = McpTopLevelJson.ExtractString(body, "action").Trim().ToLowerInvariant();
            var name = LayerRequired(body, "name");
            if (action != "create" && action != "set_current") throw new InvalidOperationException("action must be create or set_current.");
            return InvokeCadMutation(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var id = EnsureLayerRecord(transaction, document.Database, name);
                    if (action == "set_current") document.Database.Clayer = id;
                    transaction.Commit();
                    Audit("cad_layer", "action=" + action + "; name=" + name);
                    return "{\"ok\":true,\"action\":\"" + Escape(action) + "\",\"name\":\"" + Escape(name) + "\"}";
                }
            });
        }

        private static string InspectEntity(string body)
        {
            var handle = Handle(body);
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var entity = OpenEntity(transaction, document.Database, handle, OpenMode.ForRead);
                    return DescribeEntity(entity, true, true);
                }
            });
        }

        private static string BuildBricscadStatusJson()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var layer = SafeSystemVariable("CLAYER");
            return "{\"product\":\"BricsCAD\",\"connected\":true,\"bricscadVersion\":\"" + Escape(Convert.ToString(Application.Version) ?? string.Empty)
                   + "\",\"activeDocument\":\"" + Escape(document == null ? string.Empty : SafeDocumentName(document))
                   + "\",\"currentLayer\":\"" + Escape(layer)
                   + "\",\"automationStopped\":" + (_automationStopped ? "true" : "false") + "}";
        }

        private static string BuildMcpStatusJson(McpExecutionMode executionMode)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var documentName = document == null ? string.Empty : SafeDocumentName(document);
            return "{\"executionMode\":\"" + McpToolCapabilityContract.ModeName(executionMode)
                   + "\",\"bricscad\":{\"connected\":true,\"activeDocument\":\"" + Escape(documentName) + "\"}"
                   + ",\"cadDirect\":{\"available\":" + (document == null ? "false" : "true") + "}"
                   + ",\"desktopAutomation\":{\"available\":true,\"consent\":\"runtime-gated\"}"
                   + ",\"qs3dDomain\":" + McpQs3dDomainRuntime.BuildStatusJson(false) + "}";
        }

        private static string BuildActiveDocumentJson()
        {
            var document = RequireDocument();
            var filename = document.Database.Filename ?? string.Empty;
            var hasLocalPath = Path.IsPathRooted(filename);
            var modified = SafeInteger(SafeSystemVariable("DBMOD")) != "0";
            return "{\"name\":\"" + Escape(SafeDocumentName(document))
                   + "\",\"saved\":" + (hasLocalPath && !modified ? "true" : "false")
                   + ",\"hasLocalPath\":" + (hasLocalPath ? "true" : "false")
                   + ",\"modified\":" + (modified ? "true" : "false") + "}";
        }

        private static string BuildSelectionJson()
        {
            var document = RequireDocument();
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return "[]";
            var builder = new StringBuilder("[");
            var written = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    if (id.IsNull) continue;
                    try
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity == null) continue;
                        if (written++ > 0) builder.Append(',');
                        builder.Append(DescribeEntity(entity, false, false));
                    }
                    catch { }
                }
            }
            return builder.Append(']').ToString();
        }

        private static string BuildDatabaseSnapshotJson(int limit)
        {
            var document = RequireDocument();
            var builder = new StringBuilder("{\"limit\":").Append(limit).Append(",\"entities\":[");
            var count = 0;
            var more = false;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in model)
                {
                    if (id.IsNull) continue;
                    Entity? entity;
                    try { entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; }
                    catch { continue; }
                    if (entity == null) continue;
                    if (count >= limit) { more = true; break; }
                    if (count++ > 0) builder.Append(',');
                    builder.Append(DescribeEntity(entity, true, false));
                }
            }
            return builder.Append("],\"count\":").Append(count).Append(",\"truncated\":").Append(more ? "true" : "false").Append('}').ToString();
        }

        private static string BuildViewStateJson()
        {
            var document = RequireDocument();
            using (var view = document.Editor.GetCurrentView())
            {
                RECT rect;
                var hwnd = CurrentProcessWindow();
                var hasRect = GetClientRect(hwnd, out rect);
                return "{\"commandActive\":" + SafeInteger(SafeSystemVariable("CMDACTIVE"))
                       + ",\"center\":{\"x\":" + JsonNumber(view.CenterPoint.X) + ",\"y\":" + JsonNumber(view.CenterPoint.Y) + "}"
                       + ",\"width\":" + JsonNumber(view.Width) + ",\"height\":" + JsonNumber(view.Height)
                       + ",\"clientWidth\":" + (hasRect ? (rect.Right - rect.Left).ToString(CultureInfo.InvariantCulture) : "null")
                       + ",\"clientHeight\":" + (hasRect ? (rect.Bottom - rect.Top).ToString(CultureInfo.InvariantCulture) : "null") + "}";
            }
        }

        private static string ReadSystemVariable(string body)
        {
            var name = McpTopLevelJson.ExtractString(body, "name").Trim().ToUpperInvariant();
            if (!ReadableSystemVariables.Contains(name))
                throw new InvalidOperationException("System variable is not in the read-only MCP allowlist.");
            return InvokeCad(() =>
            {
                object value = Application.GetSystemVariable(name);
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (name == "DWGNAME") text = Path.GetFileName(text) ?? string.Empty;
                return "{\"name\":\"" + Escape(name) + "\",\"value\":\"" + Escape(text) + "\"}";
            });
        }

        private static string WaitUntilIdle(int timeoutMs)
        {
            var started = DateTime.UtcNow;
            while ((DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)
            {
                var active = InvokeCad(() => SafeSystemVariable("CMDACTIVE"));
                int value;
                if (int.TryParse(active, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value == 0)
                    return "{\"idle\":true,\"elapsedMs\":" + ((int)(DateTime.UtcNow - started).TotalMilliseconds).ToString(CultureInfo.InvariantCulture) + "}";
                Thread.Sleep(100);
            }
            return "{\"idle\":false,\"timeoutMs\":" + timeoutMs.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string RunCadCommandSequence(string body)
        {
            var command = NormalizeCadCommandToken(McpTopLevelJson.ExtractString(body, "command"));
            if (!AllowedCadCommands.Contains(command)) throw new InvalidOperationException("Command is not in the QS3D MCP CAD allowlist. Use cad_command_catalog.");
            var inputs = NormalizeCommandInputs(McpTopLevelJson.ExtractString(body, "inputs"), command);
            return InvokeCadMutation(() =>
            {
                var document = RequireDocument();
                if (command == "QSAVE") return SaveActiveDocument(document);
                var script = "_." + command + "\n" + inputs;
                if (!script.EndsWith("\n", StringComparison.Ordinal)) script += "\n";
                document.SendStringToExecute(script, true, false, true);
                Audit("cad_command_sequence", "command=" + command + "; inputChars=" + inputs.Length.ToString(CultureInfo.InvariantCulture));
                return "{\"accepted\":true,\"command\":\"" + Escape(command) + "\",\"inputChars\":" + inputs.Length.ToString(CultureInfo.InvariantCulture) + "}";
            });
        }

        private static string SaveActiveDocument(Document document)
        {
            var filename = document.Database.Filename ?? string.Empty;
            if (!Path.IsPathRooted(filename))
                throw new InvalidOperationException("Active drawing has no existing local path. Use SAVEAS before QSAVE.");
            if (SafeInteger(SafeSystemVariable("CMDACTIVE")) != "0")
                throw new InvalidOperationException("Cannot save while a BricsCAD command is active. Wait for idle or cancel the active command before retrying.");

            EnsureCurrentMutationRunning();
            using (document.LockDocument())
            {
                document.Database.Save();
            }

            var dbmod = SafeInteger(SafeSystemVariable("DBMOD"));
            if (dbmod != "0")
                throw new InvalidOperationException("BricsCAD save returned but DBMOD is still non-zero; save completion was not confirmed.");

            Audit("cad_command_sequence", "command=QSAVE; inputChars=0; completed=true");
            return "{\"accepted\":true,\"completed\":true,\"saved\":true,\"command\":\"QSAVE\",\"inputChars\":0}";
        }

        private static string CommandCatalogJson()
        {
            var commands = new List<string>(AllowedCadCommands);
            commands.Sort(StringComparer.OrdinalIgnoreCase);
            var builder = new StringBuilder("{\"commands\":[");
            for (var i = 0; i < commands.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append('"').Append(Escape(commands[i])).Append('"');
            }
            return builder.Append("],\"guard\":\"one allowlisted command; bounded prompt lines; no known command chaining after terminators\"}").ToString();
        }
        private static string NormalizeCadCommandToken(string value)
        {
            var token = (value ?? string.Empty).Trim();
            var index = 0;
            while (index < token.Length && (token[index] == '_' || token[index] == '.')) index++;
            return token.Substring(index).ToUpperInvariant();
        }

        private static string NormalizeCommandInputs(string input, string command)
        {
            var value = (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (value.Length > 16000 || value.IndexOf('\0') >= 0 || value.IndexOf('\u001b') >= 0 || value.IndexOf('\u0003') >= 0)
                throw new InvalidOperationException("inputs exceeds bounds or contains forbidden control characters.");
            if (NoInputCadCommands.Contains(command) && value.Trim().Length != 0)
                throw new InvalidOperationException(command + " does not accept MCP command-sequence inputs.");
            var lines = value.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length > 64) throw new InvalidOperationException("inputs exceeds 64 prompt lines.");
            var terminated = false;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 1024) throw new InvalidOperationException("one command input line exceeds 1024 characters.");
                foreach (var ch in lines[i]) if (ch < 32 && ch != '\t') throw new InvalidOperationException("inputs contains forbidden control characters.");
                var trimmed = lines[i].Trim();
                if (trimmed.Length == 0) { if (i < lines.Length - 1) terminated = true; continue; }
                if (terminated) throw new InvalidOperationException("inputs may not continue after a blank command terminator.");
                var commandLike = NormalizeCadCommandToken(trimmed);
                if (AllowedCadCommands.Contains(commandLike) || commandLike.StartsWith("QS3D", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("inputs may not inject another CAD/QS3D command.");
            }
            return value;
        }

        private static string EmergencyStop()
        {
            StopAutomation();
            Audit("cad_agent_stop", "emergency stop");
            try
            {
                return InvokeCad(() =>
                {
                    RequireDocument().SendStringToExecute("\u001b\u001b", true, false, true);
                    return "{\"stopped\":true,\"escapeCount\":2,\"delivery\":\"cad-context\"}";
                });
            }
            catch (Exception ex)
            {
                if (TrySendEscapeFallback())
                {
                    Audit("cad_agent_stop", "foreground ESC fallback after cad-context failure");
                    return "{\"stopped\":true,\"escapeCount\":2,\"delivery\":\"foreground-fallback\",\"cadContextError\":\"" + Escape(ex.Message) + "\"}";
                }
                throw new InvalidOperationException("Automation stopped, but ESC delivery failed: " + ex.Message, ex);
            }
        }

        private static string ResumeAgent(string body)
        {
            if (!McpTopLevelJson.ExtractBoolean(body, "confirmMutation"))
                throw new InvalidOperationException("confirmMutation=true is required before resuming automation.");
            Interlocked.Increment(ref _automationEpoch);
            _automationStopped = false;
            Audit("cad_agent_resume", "resume");
            return "{\"stopped\":false}";
        }

        private static string CancelCurrentCommand()
        {
            try
            {
                return InvokeCad(() =>
                {
                    RequireDocument().SendStringToExecute("\u001b\u001b", true, false, true);
                    Audit("cad_cancel_command", "escapeCount=2; delivery=cad-context");
                    return "{\"accepted\":true,\"escapeCount\":2,\"delivery\":\"cad-context\"}";
                });
            }
            catch (Exception ex)
            {
                if (TrySendEscapeFallback())
                {
                    Audit("cad_cancel_command", "escapeCount=2; delivery=foreground-fallback");
                    return "{\"accepted\":true,\"escapeCount\":2,\"delivery\":\"foreground-fallback\",\"cadContextError\":\"" + Escape(ex.Message) + "\"}";
                }
                throw;
            }
        }

        private static string UiClick(string body)
        {
            var x = Integer(body, "x", -1, -1, 100000);
            var y = Integer(body, "y", -1, -1, 100000);
            var button = McpTopLevelJson.ExtractString(body, "button").Trim().ToLowerInvariant();
            var count = Integer(body, "count", 1, 1, 3);
            var hwnd = RequireForegroundCadWindow();
            RECT rect;
            if (!GetClientRect(hwnd, out rect)) throw new InvalidOperationException("Could not read active BricsCAD client rectangle.");
            if (x < 0 || y < 0 || x >= rect.Right - rect.Left || y >= rect.Bottom - rect.Top)
                throw new InvalidOperationException("Click coordinates must stay inside the active BricsCAD-process window.");
            EnsureCurrentMutationRunning();
            POINT point = new POINT { X = x, Y = y };
            if (!ClientToScreen(hwnd, ref point) || !SetCursorPos(point.X, point.Y))
                throw new InvalidOperationException("Could not position cursor inside BricsCAD.");
            uint down;
            uint up;
            if (button == "left") { down = 0x0002; up = 0x0004; }
            else if (button == "right") { down = 0x0008; up = 0x0010; }
            else if (button == "middle") { down = 0x0020; up = 0x0040; }
            else throw new InvalidOperationException("button must be left, right or middle.");
            for (var i = 0; i < count; i++)
            {
                EnsureCurrentMutationRunning();
                RequireSameForegroundCadWindow(hwnd);
                SendMouse(down);
                SendMouse(up);
                Thread.Sleep(40);
            }
            Audit("cad_ui_click", "x=" + x + "; y=" + y + "; button=" + button + "; count=" + count);
            return "{\"clicked\":true,\"x\":" + x + ",\"y\":" + y + ",\"button\":\"" + Escape(button) + "\",\"count\":" + count + "}";
        }

        private static string UiType(string body)
        {
            var text = RequiredText(body, "text", 8000, true);
            var hwnd = RequireForegroundCadWindow();
            EnsureCurrentMutationRunning();
            SendUnicodeText(hwnd, text);
            var enter = McpTopLevelJson.ExtractBoolean(body, "pressEnter");
            if (enter)
            {
                EnsureCurrentMutationRunning();
                RequireSameForegroundCadWindow(hwnd);
                SendVirtualKey(0x0D, false, false, false);
            }
            Audit("cad_ui_type", "chars=" + text.Length.ToString(CultureInfo.InvariantCulture) + "; enter=" + enter);
            return "{\"typed\":true,\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + ",\"enter\":" + (enter ? "true" : "false") + "}";
        }

        private static string UiKey(string body)
        {
            var key = McpTopLevelJson.ExtractString(body, "key").Trim().ToUpperInvariant();
            var ctrl = McpTopLevelJson.ExtractBoolean(body, "ctrl");
            var alt = McpTopLevelJson.ExtractBoolean(body, "alt");
            var shift = McpTopLevelJson.ExtractBoolean(body, "shift");
            if (alt && key == "F4") throw new InvalidOperationException("Alt+F4 is blocked from MCP UI automation.");
            var hwnd = RequireForegroundCadWindow();
            EnsureCurrentMutationRunning();
            RequireSameForegroundCadWindow(hwnd);
            SendVirtualKey(VirtualKey(key), ctrl, alt, shift);
            Audit("cad_ui_key", "key=" + key + "; ctrl=" + ctrl + "; alt=" + alt + "; shift=" + shift);
            return "{\"pressed\":true,\"key\":\"" + Escape(key) + "\"}";
        }

        private static bool TrySendEscapeFallback()
        {
            try
            {
                var hwnd = RequireForegroundCadWindow();
                RequireSameForegroundCadWindow(hwnd);
                SendVirtualKey(0x1B, false, false, false);
                Thread.Sleep(25);
                RequireSameForegroundCadWindow(hwnd);
                SendVirtualKey(0x1B, false, false, false);
                return true;
            }
            catch { return false; }
        }

        private sealed class CadWorkItem
        {
            public Func<string> Action = null!;
            public string Result = string.Empty;
            public Exception? Error;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public int DispatchState = CadWorkQueued;
            public int Abandoned;
        }

        private static string InvokeCadMutation(Func<string> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var epoch = MutationEpoch.Value;
            if (!epoch.HasValue) throw new InvalidOperationException("Mutation execution context is unavailable.");
            return InvokeCad(() =>
            {
                EnsureAutomationRunning(epoch.Value);
                return action();
            });
        }

        private static string InvokeCad(Func<string> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var item = new CadWorkItem { Action = action };
            Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadWork, item);
            if (!item.Done.Wait(CadDispatchTimeoutMilliseconds))
            {
                Interlocked.Exchange(ref item.Abandoned, 1);
                var cancelled = Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued) == CadWorkQueued;
                if (cancelled)
                    throw new TimeoutException("Timed out waiting for BricsCAD application context; queued work was cancelled before start.");
                throw new TimeoutException("Timed out after CAD work started; completion is uncertain. Do not retry automatically; inspect drawing/audit state first.");
            }
            try
            {
                if (item.Error != null) throw new InvalidOperationException(item.Error.Message, item.Error);
                return item.Result;
            }
            finally { item.Done.Dispose(); }
        }

        private static void ExecuteCadWork(object state)
        {
            var item = (CadWorkItem)state;
            try
            {
                if (Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued) != CadWorkQueued) return;
                item.Result = item.Action();
            }
            catch (Exception ex) { item.Error = ex; }
            finally
            {
                try { item.Done.Set(); }
                finally
                {
                    if (Volatile.Read(ref item.Abandoned) != 0)
                    {
                        try { item.Done.Dispose(); } catch (ObjectDisposedException) { }
                    }
                }
            }
        }

        private static string DescribeEntity(Entity entity, bool extents, bool details)
        {
            var builder = new StringBuilder();
            var boundedSolidInspect = extents && details && entity is Solid3d;
            builder.Append("{\"handle\":\"").Append(Escape(entity.Handle.ToString())).Append("\",\"type\":\"")
                .Append(Escape(entity.GetType().Name)).Append("\",\"layer\":\"").Append(Escape(entity.Layer)).Append('"');
            if (extents)
            {
                builder.Append(",\"extents\":");
                if (boundedSolidInspect) builder.Append("null");
                else try { builder.Append(ExtentsJson(entity.GeometricExtents)); } catch { builder.Append("null"); }
                if (boundedSolidInspect) builder.Append(",\"extentsDeferred\":true");
            }
            if (details)
            {
                var line = entity as Line;
                var circle = entity as Circle;
                var arc = entity as Arc;
                var polyline = entity as Polyline;
                var text = entity as DBText;
                var mtext = entity as MText;
                if (line != null) builder.Append(",\"start\":").Append(PointJson(line.StartPoint)).Append(",\"end\":").Append(PointJson(line.EndPoint));
                else if (circle != null) builder.Append(",\"center\":").Append(PointJson(circle.Center)).Append(",\"radius\":").Append(JsonNumber(circle.Radius));
                else if (arc != null) builder.Append(",\"center\":").Append(PointJson(arc.Center)).Append(",\"radius\":").Append(JsonNumber(arc.Radius)).Append(",\"startAngleRad\":").Append(JsonNumber(arc.StartAngle)).Append(",\"endAngleRad\":").Append(JsonNumber(arc.EndAngle));
                else if (polyline != null) builder.Append(",\"vertexCount\":").Append(polyline.NumberOfVertices).Append(",\"closed\":").Append(polyline.Closed ? "true" : "false");
                else if (text != null) builder.Append(",\"text\":\"").Append(Escape(Truncate(text.TextString, 4000))).Append("\"");
                else if (mtext != null) builder.Append(",\"text\":\"").Append(Escape(Truncate(mtext.Contents, 16000))).Append("\"");
            }
            return builder.Append('}').ToString();
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
            if (entity == null) throw new InvalidOperationException("Object handle is not a readable live entity.");
            return entity;
        }

        private static string Handle(string body)
        {
            var value = McpTopLevelJson.ExtractString(body, "handle").Trim();
            if (value.Length == 0 || value.Length > 32 || !Regex.IsMatch(value, "^[0-9A-Fa-f]+$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("handle must be a hexadecimal entity handle up to 32 characters.");
            return value;
        }

        private static void EnsureLayer(Transaction transaction, Database database, string layer, Entity entity)
        {
            if (string.IsNullOrWhiteSpace(layer)) return;
            EnsureLayerRecord(transaction, database, layer);
            entity.Layer = layer;
        }

        private static ObjectId EnsureLayerRecord(Transaction transaction, Database database, string layer)
        {
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (table.Has(layer)) return table[layer];
            table.UpgradeOpen();
            var record = new LayerTableRecord { Name = layer };
            var id = table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
            return id;
        }

        private static string LayerOptional(string body)
        {
            var value = McpTopLevelJson.ExtractString(body, "layer").Trim();
            return ValidateLayer(value, true);
        }

        private static string LayerRequired(string body, string property)
        {
            return ValidateLayer(McpTopLevelJson.ExtractString(body, property).Trim(), false);
        }

        private static string ValidateLayer(string value, bool optional)
        {
            if (value.Length == 0 && optional) return string.Empty;
            if (value.Length == 0) throw new InvalidOperationException("Layer name is required.");
            if (value.Length > 255) throw new InvalidOperationException("Layer name exceeds 255 characters.");
            foreach (var ch in value) if (ch < 32) throw new InvalidOperationException("Layer name contains control characters.");
            return value;
        }

        private static string RequiredText(string body, string property, int maximum, bool rejectControls)
        {
            var value = McpTopLevelJson.ExtractString(body, property);
            if (string.IsNullOrEmpty(value)) throw new InvalidOperationException(property + " is required.");
            if (value.Length > maximum) throw new InvalidOperationException(property + " exceeds " + maximum.ToString(CultureInfo.InvariantCulture) + " characters.");
            foreach (var ch in value)
                if (ch == '\0' || ch == '\u001b' || (rejectControls && ch < 32))
                    throw new InvalidOperationException(property + " contains forbidden control characters.");
            return value;
        }

        private static double NumberRequired(string body, string property)
        {
            double value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractDouble(body, property, out value, out found, out error)) throw new InvalidOperationException(error);
            if (!found) throw new InvalidOperationException(property + " must be a finite number.");
            return value;
        }

        private static double NumberOptional(string body, string property, double fallback)
        {
            double value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractDouble(body, property, out value, out found, out error)) throw new InvalidOperationException(error);
            return found ? value : fallback;
        }

        private static int Integer(string body, string property, int fallback, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error)) throw new InvalidOperationException(error);
            if (!found) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        private static List<Point2d> ParsePoints2d(string raw)
        {
            var result = new List<Point2d>();
            foreach (var part in (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split(',');
                double x;
                double y;
                if (pair.Length != 2 || !double.TryParse(pair[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    || !double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                    || !Finite(x) || !Finite(y))
                    throw new InvalidOperationException("points must use finite invariant x,y;x,y format.");
                result.Add(new Point2d(x, y));
            }
            return result;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static Point3d EntityCenter(Entity entity)
        {
            try
            {
                var e = entity.GeometricExtents;
                return new Point3d((e.MinPoint.X + e.MaxPoint.X) / 2d, (e.MinPoint.Y + e.MaxPoint.Y) / 2d, (e.MinPoint.Z + e.MaxPoint.Z) / 2d);
            }
            catch { return Point3d.Origin; }
        }

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document.");
            return document;
        }

        private static string SafeDocumentName(Document document)
        {
            try
            {
                var name = document.Name ?? string.Empty;
                var leaf = Path.GetFileName(name);
                return string.IsNullOrWhiteSpace(leaf) ? name : leaf;
            }
            catch { return string.Empty; }
        }

        private static string SafeSystemVariable(string name)
        {
            try { return Convert.ToString(Application.GetSystemVariable(name), CultureInfo.InvariantCulture) ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string SafeInteger(string value)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed.ToString(CultureInfo.InvariantCulture) : "0";
        }

        private static string ExtentsJson(Extents3d e) { return "{\"min\":" + PointJson(e.MinPoint) + ",\"max\":" + PointJson(e.MaxPoint) + "}"; }
        private static string PointJson(Point3d p) { return "{\"x\":" + JsonNumber(p.X) + ",\"y\":" + JsonNumber(p.Y) + ",\"z\":" + JsonNumber(p.Z) + "}"; }
        private static string JsonNumber(double value) { return Finite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : "null"; }
        private static string Truncate(string value, int max) { var text = value ?? string.Empty; return text.Length <= max ? text : text.Substring(0, max); }
        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }

        private static IntPtr CurrentProcessWindow()
        {
            var handle = Process.GetCurrentProcess().MainWindowHandle;
            if (handle == IntPtr.Zero || !IsCurrentProcessWindow(handle)) throw new InvalidOperationException("BricsCAD main window handle is unavailable.");
            return handle;
        }

        private static IntPtr RequireForegroundCadWindow()
        {
            var foreground = GetForegroundWindow();
            if (foreground != IntPtr.Zero && IsCurrentProcessWindow(foreground)) return foreground;
            var main = CurrentProcessWindow();
            if (!SetForegroundWindow(main)) throw new InvalidOperationException("Could not focus the BricsCAD window; UI input was not sent.");
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(25);
                foreground = GetForegroundWindow();
                if (foreground != IntPtr.Zero && IsCurrentProcessWindow(foreground)) return foreground;
            }
            throw new InvalidOperationException("BricsCAD did not become foreground; UI input was not sent.");
        }

        private static void RequireSameForegroundCadWindow(IntPtr expected)
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero || foreground != expected || !IsCurrentProcessWindow(foreground))
                throw new InvalidOperationException("BricsCAD foreground window changed; UI input stopped before injection.");
        }

        private static bool IsCurrentProcessWindow(IntPtr hwnd)
        {
            uint processId;
            return hwnd != IntPtr.Zero && GetWindowThreadProcessId(hwnd, out processId) != 0 && processId == (uint)Process.GetCurrentProcess().Id;
        }

        private static void SendUnicodeText(IntPtr hwnd, string text)
        {
            foreach (var ch in text)
            {
                EnsureCurrentMutationRunning();
                RequireSameForegroundCadWindow(hwnd);
                var input = new[]
                {
                    new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = 0x0004 } } },
                    new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = 0x0004 | 0x0002 } } }
                };
                if (SendInput((uint)input.Length, input, Marshal.SizeOf(typeof(INPUT))) != (uint)input.Length)
                    throw new InvalidOperationException("Windows SendInput rejected Unicode keyboard input.");
            }
        }

        private static void SendVirtualKey(ushort key, bool ctrl, bool alt, bool shift)
        {
            var list = new List<INPUT>();
            if (ctrl) list.Add(KeyInput(0x11, false));
            if (alt) list.Add(KeyInput(0x12, false));
            if (shift) list.Add(KeyInput(0x10, false));
            list.Add(KeyInput(key, false)); list.Add(KeyInput(key, true));
            if (shift) list.Add(KeyInput(0x10, true));
            if (alt) list.Add(KeyInput(0x12, true));
            if (ctrl) list.Add(KeyInput(0x11, true));
            var input = list.ToArray();
            if (SendInput((uint)input.Length, input, Marshal.SizeOf(typeof(INPUT))) != (uint)input.Length)
                throw new InvalidOperationException("Windows SendInput rejected keyboard input.");
        }

        private static void SendMouse(uint flags)
        {
            var input = new[] { new INPUT { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } } } };
            if (SendInput(1, input, Marshal.SizeOf(typeof(INPUT))) != 1) throw new InvalidOperationException("Windows SendInput rejected mouse input.");
        }

        private static INPUT KeyInput(ushort key, bool up)
        {
            return new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = up ? 0x0002u : 0u } } };
        }

        private static ushort VirtualKey(string key)
        {
            switch (key)
            {
                case "ENTER": return 0x0D; case "ESC": case "ESCAPE": return 0x1B; case "TAB": return 0x09;
                case "BACKSPACE": return 0x08; case "DELETE": return 0x2E; case "SPACE": return 0x20;
                case "LEFT": return 0x25; case "UP": return 0x26; case "RIGHT": return 0x27; case "DOWN": return 0x28;
                case "HOME": return 0x24; case "END": return 0x23; case "PAGEUP": return 0x21; case "PAGEDOWN": return 0x22;
                case "F1": return 0x70; case "F2": return 0x71; case "F3": return 0x72; case "F4": return 0x73; case "F5": return 0x74;
                case "F6": return 0x75; case "F7": return 0x76; case "F8": return 0x77; case "F9": return 0x78; case "F10": return 0x79;
                case "F11": return 0x7A; case "F12": return 0x7B;
            }
            if (key.Length == 1)
            {
                var ch = char.ToUpperInvariant(key[0]);
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) return ch;
            }
            throw new InvalidOperationException("Unsupported key name.");
        }

        private static void Audit(string tool, string detail)
        {
            try
            {
                lock (AuditSync)
                {
                    var path = AuditFilePath;
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                    RotateAudit(path);
                    var line = "{\"utc\":\"" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\",\"tool\":\"" + Escape(tool)
                               + "\",\"detail\":\"" + Escape(SanitizeAudit(detail)) + "\"}" + Environment.NewLine;
                    File.AppendAllText(path, line, new UTF8Encoding(false));
                }
            }
            catch { }
        }

        private static void RotateAudit(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length < MaxAuditBytes) return;
                var previous = path + ".1";
                try { if (File.Exists(previous)) File.Delete(previous); } catch { }
                try { File.Move(path, previous); } catch { File.WriteAllText(path, string.Empty, new UTF8Encoding(false)); }
            }
            catch { }
        }

        private static string SanitizeAudit(string value)
        {
            var text = value ?? string.Empty;
            if (text.Length > 1024) text = text.Substring(0, 1024);
            var builder = new StringBuilder(text.Length);
            foreach (var ch in text) builder.Append(ch < 32 ? ' ' : ch);
            return builder.ToString();
        }

        private static string ReadAuditTail(int limit)
        {
            lock (AuditSync)
            {
                if (!File.Exists(AuditFilePath)) return "{\"entries\":[]}";
                var lines = File.ReadAllLines(AuditFilePath, Encoding.UTF8);
                var start = Math.Max(0, lines.Length - limit);
                var builder = new StringBuilder("{\"entries\":[");
                var written = 0;
                for (var i = start; i < lines.Length; i++)
                {
                    if (!lines[i].StartsWith("{", StringComparison.Ordinal) || !lines[i].EndsWith("}", StringComparison.Ordinal)) continue;
                    if (written++ > 0) builder.Append(',');
                    builder.Append(lines[i]);
                }
                return builder.Append("]}").ToString();
            }
        }

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
        [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    }
}