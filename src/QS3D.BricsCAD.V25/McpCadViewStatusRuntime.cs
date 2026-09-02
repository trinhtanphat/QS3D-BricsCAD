using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Bounded direct BricsCAD view control plus privacy-safe agent/command state.
    /// Mutation dispatch is owned by McpCadDirectModelRuntime so the shared emergency-stop
    /// epoch and confirmMutation boundary remain authoritative. No command-line history,
    /// prompt text, shell or process execution is exposed here.
    /// </summary>
    internal static class McpCadViewStatusRuntime
    {
        private const int MaxFitHandles = 100;
        private const int MaxHandlesCsvLength = 1800;
        private const int MaxTrackedCommandDepth = 16;
        private const int MaxCommandNameLength = 96;
        private const double MinViewSize = 1e-6;
        private const double MaxViewSize = 1e12;
        private const double MinDirectionLength = 1e-9;
        private const double TwoPi = Math.PI * 2d;
        private static readonly object CommandGate = new object();
        private static readonly Dictionary<Document, CommandTracker> CommandTrackers =
            new Dictionary<Document, CommandTracker>();

        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "cad_view_zoom_extents",
            "cad_view_fit_entities",
            "cad_view_set",
            "agent_status",
            "cad_command_state"
        };

        private static readonly HashSet<string> MutationTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "cad_view_zoom_extents",
            "cad_view_fit_entities",
            "cad_view_set"
        };

        internal static bool IsTool(string? tool)
        {
            return Tools.Contains(tool ?? string.Empty);
        }

        internal static bool RequiresMutation(string? tool)
        {
            return MutationTools.Contains(tool ?? string.Empty);
        }

        internal static IEnumerable<string> ToolDescriptors()
        {
            yield return Tool(
                "cad_view_zoom_extents",
                "Directly fit the active BricsCAD view to current drawing extents with bounded padding. Requires confirmMutation=true.",
                "\"padding\":{\"type\":\"number\",\"minimum\":1.0,\"maximum\":2.0}," + ConfirmMutationProperty(),
                "confirmMutation");
            yield return Tool(
                "cad_view_fit_entities",
                "Directly fit the active BricsCAD view to up to 100 exact hexadecimal entity handles supplied as a comma-separated string. Requires confirmMutation=true.",
                "\"handlesCsv\":{\"type\":\"string\",\"maxLength\":1800},\"padding\":{\"type\":\"number\",\"minimum\":1.0,\"maximum\":2.0}," + ConfirmMutationProperty(),
                "handlesCsv", "confirmMutation");
            yield return Tool(
                "cad_view_set",
                "Directly set active BricsCAD view center/width/height and optionally a bounded view direction/twist. Requires confirmMutation=true.",
                "\"centerX\":{\"type\":\"number\"},\"centerY\":{\"type\":\"number\"},\"width\":{\"type\":\"number\",\"exclusiveMinimum\":0},\"height\":{\"type\":\"number\",\"exclusiveMinimum\":0},"
                + "\"directionX\":{\"type\":\"number\"},\"directionY\":{\"type\":\"number\"},\"directionZ\":{\"type\":\"number\"},\"twistRadians\":{\"type\":\"number\",\"minimum\":-6.283185307179586,\"maximum\":6.283185307179586},"
                + ConfirmMutationProperty(),
                "centerX", "centerY", "width", "height", "confirmMutation");
            yield return Tool(
                "agent_status",
                "Read bounded ChatGPT-facing agent execution status: current action, Action ID, next step, last error, terminal state, duration and update time. No typed, clipboard or screenshot content.",
                string.Empty);
            yield return Tool(
                "cad_command_state",
                "Read CMDACTIVE/CMDNAMES plus bounded in-memory active/last BricsCAD command lifecycle metadata. Does not expose command-line history or prompt contents.",
                string.Empty);
        }

        /// <summary>
        /// Called only by McpCadDirectModelRuntime after that runtime has entered the BricsCAD
        /// application context. Mutation tools additionally arrive through its shared epoch gate.
        /// </summary>
        internal static string CallInCadContext(string tool, string body)
        {
            if (!IsTool(tool)) throw new InvalidOperationException("Unknown MCP CAD view/status tool: " + tool);
            var args = string.IsNullOrWhiteSpace(body) ? "{}" : body;
            switch (tool)
            {
                case "cad_view_zoom_extents": return ZoomExtents(args);
                case "cad_view_fit_entities": return FitEntities(args);
                case "cad_view_set": return SetView(args);
                case "agent_status": return AgentStatusJson();
                case "cad_command_state": return CommandStateJsonInCadContext();
                default: throw new InvalidOperationException("Unknown MCP CAD view/status tool: " + tool);
            }
        }

        private static string ZoomExtents(string body)
        {
            var padding = NumberOptional(body, "padding", 1.08d, 1d, 2d);
            var document = RequireDocument();
            RequireViewMutationIdle();
            using (document.LockDocument())
            {
                RequireViewMutationIdle();
                document.Database.UpdateExt(false);
                var extents = RequireFiniteExtents(
                    new Extents3d(document.Database.Extmin, document.Database.Extmax),
                    "drawing extents");
                return ApplyExtents(document, extents, padding, "drawing_extents", 0);
            }
        }

        private static string FitEntities(string body)
        {
            var handles = ParseHandlesCsv(McpTopLevelJson.ExtractString(body, "handlesCsv"));
            var padding = NumberOptional(body, "padding", 1.12d, 1d, 2d);
            var document = RequireDocument();
            RequireViewMutationIdle();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var hasExtents = false;
                var combined = new Extents3d();
                foreach (var handle in handles)
                {
                    ObjectId id;
                    try { id = document.Database.GetObjectId(false, handle, 0); }
                    catch (Exception ex) { throw new InvalidOperationException("Entity handle was not found: " + HandleText(handle), ex); }
                    if (id.IsNull || id.IsErased)
                        throw new InvalidOperationException("Entity handle is invalid or erased: " + HandleText(handle));
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Handle does not identify a live entity: " + HandleText(handle));
                    Extents3d extents;
                    try { extents = RequireFiniteExtents(entity.GeometricExtents, "entity " + HandleText(handle)); }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Entity has no usable geometric extents: " + HandleText(handle) + ". " + ex.Message,
                            ex);
                    }
                    if (!hasExtents) { combined = extents; hasExtents = true; }
                    else combined.AddExtents(extents);
                }
                if (!hasExtents) throw new InvalidOperationException("No usable entity extents were supplied.");
                return ApplyExtents(document, combined, padding, "entities", handles.Count);
            }
        }

        private static string SetView(string body)
        {
            var centerX = NumberRequired(body, "centerX");
            var centerY = NumberRequired(body, "centerY");
            var width = PositiveViewSize(NumberRequired(body, "width"), "width");
            var height = PositiveViewSize(NumberRequired(body, "height"), "height");
            var hasDirection = McpTopLevelJson.HasProperty(body, "directionX")
                               || McpTopLevelJson.HasProperty(body, "directionY")
                               || McpTopLevelJson.HasProperty(body, "directionZ");
            Vector3d? direction = null;
            if (hasDirection)
            {
                if (!McpTopLevelJson.HasProperty(body, "directionX")
                    || !McpTopLevelJson.HasProperty(body, "directionY")
                    || !McpTopLevelJson.HasProperty(body, "directionZ"))
                    throw new InvalidOperationException("directionX, directionY and directionZ must be supplied together.");
                var vector = new Vector3d(
                    NumberRequired(body, "directionX"),
                    NumberRequired(body, "directionY"),
                    NumberRequired(body, "directionZ"));
                if (!IsFinite(vector.X) || !IsFinite(vector.Y) || !IsFinite(vector.Z) || vector.Length < MinDirectionLength)
                    throw new InvalidOperationException("View direction must be finite and non-zero.");
                direction = vector.GetNormal();
            }
            var hasTwist = McpTopLevelJson.HasProperty(body, "twistRadians");
            var twist = hasTwist ? NumberRequired(body, "twistRadians") : 0d;
            if (hasTwist && (twist < -TwoPi || twist > TwoPi))
                throw new InvalidOperationException("twistRadians must be between -2π and 2π.");

            var document = RequireDocument();
            RequireViewMutationIdle();
            using (document.LockDocument())
            using (var view = document.Editor.GetCurrentView())
            {
                view.CenterPoint = new Point2d(centerX, centerY);
                view.Width = width;
                view.Height = height;
                if (direction.HasValue) view.ViewDirection = direction.Value;
                if (hasTwist) view.ViewTwist = twist;
                RequireViewMutationIdle();
                document.Editor.SetCurrentView(view);
            }
            return CurrentViewJson(document, "set");
        }

        private static string ApplyExtents(Document document, Extents3d worldExtents, double padding, string source, int entityCount)
        {
            RequireViewMutationIdle();
            using (var view = document.Editor.GetCurrentView())
            {
                var worldToEye = Matrix3d.PlaneToWorld(view.ViewDirection);
                worldToEye = Matrix3d.Displacement(view.Target - Point3d.Origin) * worldToEye;
                worldToEye = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * worldToEye;
                worldToEye = worldToEye.Inverse();
                var eye = TransformExtents(worldExtents, worldToEye);
                var rawWidth = Math.Max(MinViewSize, eye.MaxPoint.X - eye.MinPoint.X);
                var rawHeight = Math.Max(MinViewSize, eye.MaxPoint.Y - eye.MinPoint.Y);
                view.CenterPoint = new Point2d(
                    (eye.MinPoint.X + eye.MaxPoint.X) / 2d,
                    (eye.MinPoint.Y + eye.MaxPoint.Y) / 2d);
                view.Width = PositiveViewSize(rawWidth * padding, "computed width");
                view.Height = PositiveViewSize(rawHeight * padding, "computed height");
                RequireViewMutationIdle();
                document.Editor.SetCurrentView(view);
            }
            var result = CurrentViewJson(document, source);
            if (entityCount <= 0) return result;
            return result.Substring(0, result.Length - 1)
                   + ",\"entityCount\":" + entityCount.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static Extents3d TransformExtents(Extents3d extents, Matrix3d transform)
        {
            var min = extents.MinPoint;
            var max = extents.MaxPoint;
            var points = new[]
            {
                new Point3d(min.X, min.Y, min.Z), new Point3d(max.X, min.Y, min.Z),
                new Point3d(min.X, max.Y, min.Z), new Point3d(max.X, max.Y, min.Z),
                new Point3d(min.X, min.Y, max.Z), new Point3d(max.X, min.Y, max.Z),
                new Point3d(min.X, max.Y, max.Z), new Point3d(max.X, max.Y, max.Z)
            };
            var first = points[0].TransformBy(transform);
            var result = new Extents3d(first, first);
            for (var i = 1; i < points.Length; i++) result.AddPoint(points[i].TransformBy(transform));
            return RequireFiniteExtents(result, "transformed view extents");
        }

        private static string CurrentViewJson(Document document, string source)
        {
            using (var view = document.Editor.GetCurrentView())
            {
                return "{\"updated\":true,\"source\":\"" + Escape(source) + "\",\"center\":{" 
                       + "\"x\":" + JsonNumber(view.CenterPoint.X) + ",\"y\":" + JsonNumber(view.CenterPoint.Y) + "},"
                       + "\"width\":" + JsonNumber(view.Width) + ",\"height\":" + JsonNumber(view.Height)
                       + ",\"direction\":{" + "\"x\":" + JsonNumber(view.ViewDirection.X)
                       + ",\"y\":" + JsonNumber(view.ViewDirection.Y) + ",\"z\":" + JsonNumber(view.ViewDirection.Z) + "}"
                       + ",\"twistRadians\":" + JsonNumber(view.ViewTwist) + "}";
            }
        }

        private static string AgentStatusJson()
        {
            var now = DateTime.UtcNow;
            var currentAction = McpAgentExperience.CurrentAction ?? string.Empty;
            var updated = McpAgentExperience.UpdatedUtc;
            var duration = currentAction.Length > 0
                ? Math.Max(0L, (long)(now - updated).TotalMilliseconds)
                : Math.Max(0L, McpAgentExperience.LastDurationMilliseconds);
            return "{\"active\":" + (currentAction.Length > 0 ? "true" : "false")
                   + ",\"currentAction\":\"" + Escape(Bound(currentAction, 1200)) + "\""
                   + ",\"actionId\":\"" + Escape(Bound(McpAgentExperience.LastActionId, 128)) + "\""
                   + ",\"nextStep\":\"" + Escape(Bound(McpAgentExperience.NextStep, 1200)) + "\""
                   + ",\"lastError\":\"" + Escape(Bound(McpAgentExperience.LastError, 1200)) + "\""
                   + ",\"terminalState\":\"" + Escape(Bound(McpAgentExperience.LastTerminalState, 64)) + "\""
                   + ",\"durationMs\":" + duration.ToString(CultureInfo.InvariantCulture)
                   + ",\"updatedUtc\":\"" + updated.ToString("o", CultureInfo.InvariantCulture) + "\"}";
        }

        private static string CommandStateJsonInCadContext()
        {
            var document = RequireDocument();
            EnsureCommandTracking(document);
            var cmdActiveText = Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? "0";
            int cmdActive;
            if (!int.TryParse(cmdActiveText, NumberStyles.Integer, CultureInfo.InvariantCulture, out cmdActive)) cmdActive = 0;
            var cmdNames = SafeCommandNames(Convert.ToString(Application.GetSystemVariable("CMDNAMES"), CultureInfo.InvariantCulture) ?? string.Empty);
            var lifecycle = CommandSnapshot(document);
            var activeCommand = lifecycle.ActiveCommand;
            if (activeCommand.Length == 0 && cmdNames.Length > 0) activeCommand = cmdNames;
            return "{\"document\":\"" + Escape(SafeDocumentName(document)) + "\""
                   + ",\"cmdActive\":" + cmdActive.ToString(CultureInfo.InvariantCulture)
                   + ",\"active\":" + (cmdActive != 0 ? "true" : "false")
                   + ",\"activeCommand\":\"" + Escape(activeCommand) + "\""
                   + ",\"lastCommand\":\"" + Escape(lifecycle.LastCommand) + "\""
                   + ",\"lastPhase\":\"" + Escape(lifecycle.LastPhase) + "\""
                   + ",\"updatedUtc\":\"" + lifecycle.UpdatedUtc.ToString("o", CultureInfo.InvariantCulture) + "\"}";
        }

        private static void EnsureCommandTracking(Document document)
        {
            lock (CommandGate)
            {
                if (CommandTrackers.ContainsKey(document)) return;
                if (CommandTrackers.Count >= 32)
                {
                    Document? removable = null;
                    foreach (var pair in CommandTrackers)
                    {
                        if (!ReferenceEquals(pair.Key, document)) { removable = pair.Key; break; }
                    }
                    if (removable != null)
                    {
                        try { CommandTrackers[removable].Dispose(); } catch { }
                        CommandTrackers.Remove(removable);
                    }
                }
                var tracker = new CommandTracker(document);
                tracker.Subscribe();
                CommandTrackers[document] = tracker;
            }
        }

        private static CommandLifecycleSnapshot CommandSnapshot(Document document)
        {
            lock (CommandGate)
            {
                CommandTracker tracker;
                if (!CommandTrackers.TryGetValue(document, out tracker))
                    return new CommandLifecycleSnapshot(string.Empty, string.Empty, string.Empty, DateTime.MinValue);
                return tracker.Snapshot();
            }
        }

        private static void TrackCommand(Document document, string phase, CommandEventArgs args)
        {
            var command = SafeCommandName(args == null ? string.Empty : args.GlobalCommandName);
            if (command.Length == 0) return;
            lock (CommandGate)
            {
                CommandTracker tracker;
                if (!CommandTrackers.TryGetValue(document, out tracker)) return;
                tracker.Track(command, phase);
            }
        }

        private static string SafeCommandName(string value)
        {
            var source = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (source.Length > MaxCommandNameLength) source = source.Substring(0, MaxCommandNameLength);
            var chars = new List<char>(source.Length);
            foreach (var ch in source)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.' || ch == '$') chars.Add(ch);
            }
            return new string(chars.ToArray());
        }

        private static string SafeCommandNames(string value)
        {
            var raw = (value ?? string.Empty).Split(new[] { '\'', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var names = new List<string>();
            foreach (var item in raw)
            {
                var name = SafeCommandName(item);
                if (name.Length == 0 || names.Contains(name)) continue;
                names.Add(name);
                if (names.Count >= MaxTrackedCommandDepth) break;
            }
            return string.Join("'", names);
        }

        private static List<Handle> ParseHandlesCsv(string raw)
        {
            var text = (raw ?? string.Empty).Trim();
            if (text.Length == 0) throw new InvalidOperationException("handlesCsv is required.");
            if (text.Length > MaxHandlesCsvLength)
                throw new InvalidOperationException("handlesCsv exceeds the bounded input length.");
            var result = new List<Handle>();
            var seen = new HashSet<long>();
            foreach (var part in text.Split(','))
            {
                var token = part.Trim();
                if (token.Length == 0 || token.Length > 16)
                    throw new InvalidOperationException("Each handlesCsv item must be a non-zero hexadecimal handle up to 16 characters.");
                long value;
                if (!long.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value <= 0)
                    throw new InvalidOperationException("Invalid hexadecimal entity handle: " + token);
                if (seen.Add(value)) result.Add(new Handle(value));
                if (result.Count > MaxFitHandles)
                    throw new InvalidOperationException("cad_view_fit_entities permits at most 100 distinct handles.");
            }
            if (result.Count == 0) throw new InvalidOperationException("handlesCsv must contain at least one handle.");
            return result;
        }

        private static Extents3d RequireFiniteExtents(Extents3d extents, string label)
        {
            var min = extents.MinPoint;
            var max = extents.MaxPoint;
            if (!IsFinite(min.X) || !IsFinite(min.Y) || !IsFinite(min.Z)
                || !IsFinite(max.X) || !IsFinite(max.Y) || !IsFinite(max.Z)
                || min.X > max.X || min.Y > max.Y || min.Z > max.Z)
                throw new InvalidOperationException(label + " are invalid or non-finite.");
            return extents;
        }

        private static double PositiveViewSize(double value, string label)
        {
            if (!IsFinite(value) || value < MinViewSize || value > MaxViewSize)
                throw new InvalidOperationException(label + " must be finite and between "
                    + MinViewSize.ToString("R", CultureInfo.InvariantCulture) + " and "
                    + MaxViewSize.ToString("R", CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static double NumberRequired(string body, string property)
        {
            double value; bool found; string error;
            if (!McpTopLevelJson.TryExtractDouble(body ?? "{}", property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found || !IsFinite(value))
                throw new InvalidOperationException(property + " is required and must be finite.");
            return value;
        }

        private static double NumberOptional(string body, string property, double fallback, double minimum, double maximum)
        {
            double value; bool found; string error;
            if (!McpTopLevelJson.TryExtractDouble(body ?? "{}", property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            if (!IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException(property + " must be finite and between "
                    + minimum.ToString("R", CultureInfo.InvariantCulture) + " and "
                    + maximum.ToString("R", CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static void RequireViewMutationIdle()
        {
            var raw = Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? string.Empty;
            int active;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out active) && active == 0) return;
            var commandNames = SafeCommandNames(
                Convert.ToString(Application.GetSystemVariable("CMDNAMES"), CultureInfo.InvariantCulture) ?? string.Empty);
            throw new InvalidOperationException(
                "BricsCAD view update is busy while another command is active. Wait for cad_wait_idle/CMDACTIVE=0 before retrying the view mutation."
                + (commandNames.Length == 0 ? string.Empty : " Active command: " + commandNames + "."));
        }

        private static Document RequireDocument()
        {
            return Application.DocumentManager.MdiActiveDocument
                   ?? throw new InvalidOperationException("No active BricsCAD document.");
        }

        private static string SafeDocumentName(Document document)
        {
            try { return System.IO.Path.GetFileName(document.Name ?? string.Empty) ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static string JsonNumber(double value) { return IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : "null"; }
        private static string HandleText(Handle handle) { return handle.Value.ToString("X", CultureInfo.InvariantCulture); }
        private static string Bound(string value, int maximum) { var text = value ?? string.Empty; return text.Length <= maximum ? text : text.Substring(0, maximum); }
        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }
        private static string ConfirmMutationProperty() { return "\"confirmMutation\":{\"type\":\"boolean\",\"const\":true}"; }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + Escape(name) + "\",\"description\":\"" + Escape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + (properties ?? string.Empty)
                   + "},\"additionalProperties\":false" + requiredJson + "}}";
        }

        private sealed class CommandLifecycleSnapshot
        {
            internal CommandLifecycleSnapshot(string activeCommand, string lastCommand, string lastPhase, DateTime updatedUtc)
            {
                ActiveCommand = activeCommand ?? string.Empty;
                LastCommand = lastCommand ?? string.Empty;
                LastPhase = lastPhase ?? string.Empty;
                UpdatedUtc = updatedUtc;
            }
            internal string ActiveCommand { get; private set; }
            internal string LastCommand { get; private set; }
            internal string LastPhase { get; private set; }
            internal DateTime UpdatedUtc { get; private set; }
        }

        private sealed class CommandTracker : IDisposable
        {
            private readonly Document _document;
            private readonly List<string> _active = new List<string>();
            private string _lastCommand = string.Empty;
            private string _lastPhase = string.Empty;
            private DateTime _updatedUtc = DateTime.MinValue;
            private readonly CommandEventHandler _willStart;
            private readonly CommandEventHandler _ended;
            private readonly CommandEventHandler _cancelled;
            private readonly CommandEventHandler _failed;

            internal CommandTracker(Document document)
            {
                _document = document;
                _willStart = (sender, args) => TrackCommand(_document, "start", args);
                _ended = (sender, args) => TrackCommand(_document, "end", args);
                _cancelled = (sender, args) => TrackCommand(_document, "cancelled", args);
                _failed = (sender, args) => TrackCommand(_document, "failed", args);
            }

            internal void Subscribe()
            {
                _document.CommandWillStart += _willStart;
                _document.CommandEnded += _ended;
                _document.CommandCancelled += _cancelled;
                _document.CommandFailed += _failed;
            }

            internal void Track(string command, string phase)
            {
                _lastCommand = command;
                _lastPhase = phase ?? string.Empty;
                _updatedUtc = DateTime.UtcNow;
                if (string.Equals(phase, "start", StringComparison.Ordinal))
                {
                    _active.Add(command);
                    while (_active.Count > MaxTrackedCommandDepth) _active.RemoveAt(0);
                    return;
                }
                for (var i = _active.Count - 1; i >= 0; i--)
                {
                    if (!string.Equals(_active[i], command, StringComparison.OrdinalIgnoreCase)) continue;
                    _active.RemoveAt(i);
                    break;
                }
            }

            internal CommandLifecycleSnapshot Snapshot()
            {
                var active = _active.Count == 0 ? string.Empty : _active[_active.Count - 1];
                return new CommandLifecycleSnapshot(active, _lastCommand, _lastPhase, _updatedUtc);
            }

            public void Dispose()
            {
                try { _document.CommandWillStart -= _willStart; } catch { }
                try { _document.CommandEnded -= _ended; } catch { }
                try { _document.CommandCancelled -= _cancelled; } catch { }
                try { _document.CommandFailed -= _failed; } catch { }
            }
        }
    }
}