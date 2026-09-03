using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Direct native layer-state reads and atomic writes for the embedded MCP surface.
    /// Mutation admission/serialization is owned by McpCadAgentRuntime via
    /// McpCadDirectModelRuntime.RequiresMutation; this type owns only document-scoped native work.
    /// </summary>
    internal static class McpCadLayerStateRuntime
    {
        private const int MaxSnapshotLayers = 4096;
        private const int MaxSnapshotTokenLength = 512 * 1024;
        private const string SnapshotVersion = "QS3D-LAYER-STATE-V1";

        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "cad_layer_state",
            "cad_layer_set_state",
            "cad_layer_snapshot",
            "cad_layer_restore"
        };

        private static readonly HashSet<string> MutationTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "cad_layer_set_state",
            "cad_layer_restore"
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
                "cad_layer_state",
                "Read native ON/OFF, frozen and locked state for one existing layer.",
                "\"name\":{\"type\":\"string\",\"minLength\":1,\"maxLength\":255}",
                "name");
            yield return Tool(
                "cad_layer_set_state",
                "Atomically set native ON/OFF, frozen and/or locked state for one existing layer. The current layer cannot be turned off or frozen.",
                "\"name\":{\"type\":\"string\",\"minLength\":1,\"maxLength\":255},"
                + "\"on\":{\"type\":\"boolean\"},\"frozen\":{\"type\":\"boolean\"},\"locked\":{\"type\":\"boolean\"},"
                + ConfirmMutationProperty(),
                "name", "confirmMutation");
            yield return Tool(
                "cad_layer_snapshot",
                "Capture an opaque bounded snapshot of every native layer ON/OFF, frozen and locked state plus the current-layer identity.",
                string.Empty);
            yield return Tool(
                "cad_layer_restore",
                "Atomically restore a snapshot captured by cad_layer_snapshot. Unknown/missing layers or a snapshot that would turn off/freeze the current layer fail closed before any write.",
                "\"snapshot\":{\"type\":\"string\",\"minLength\":1,\"maxLength\":" + MaxSnapshotTokenLength.ToString(CultureInfo.InvariantCulture) + "},"
                + ConfirmMutationProperty(),
                "snapshot", "confirmMutation");
        }

        internal static string CallInCadContext(string tool, string body)
        {
            if (!IsTool(tool)) throw new InvalidOperationException("Unknown MCP CAD layer-state tool: " + tool);
            var args = string.IsNullOrWhiteSpace(body) ? "{}" : body;
            switch (tool)
            {
                case "cad_layer_state": return ReadLayerState(args);
                case "cad_layer_set_state": return SetLayerState(args);
                case "cad_layer_snapshot": return CaptureSnapshot();
                case "cad_layer_restore": return RestoreSnapshot(args);
                default: throw new InvalidOperationException("Unknown MCP CAD layer-state tool: " + tool);
            }
        }

        private static string ReadLayerState(string body)
        {
            var name = RequireLayerName(McpTopLevelJson.ExtractString(body, "name"));
            var document = RequireDocument();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var record = RequireLayer(transaction, document.Database, name, OpenMode.ForRead);
                return LayerStateJson(record, record.ObjectId == document.Database.Clayer);
            }
        }

        private static string SetLayerState(string body)
        {
            var name = RequireLayerName(McpTopLevelJson.ExtractString(body, "name"));
            var hasOn = McpTopLevelJson.HasProperty(body, "on");
            var hasFrozen = McpTopLevelJson.HasProperty(body, "frozen");
            var hasLocked = McpTopLevelJson.HasProperty(body, "locked");
            if (!hasOn && !hasFrozen && !hasLocked)
                throw new InvalidOperationException("cad_layer_set_state requires at least one of on, frozen or locked.");

            var requestedOn = hasOn && McpTopLevelJson.ExtractBoolean(body, "on");
            var requestedFrozen = hasFrozen && McpTopLevelJson.ExtractBoolean(body, "frozen");
            var requestedLocked = hasLocked && McpTopLevelJson.ExtractBoolean(body, "locked");
            var document = RequireDocument();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var table = (LayerTable)transaction.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                if (!table.Has(name)) throw new InvalidOperationException("Layer does not exist: " + name);
                var id = table[name];
                var isCurrent = id == document.Database.Clayer;
                if (isCurrent && ((hasOn && !requestedOn) || (hasFrozen && requestedFrozen)))
                    throw new InvalidOperationException("The current layer cannot be turned off or frozen: " + name);

                var record = (LayerTableRecord)transaction.GetObject(id, OpenMode.ForWrite);
                if (hasOn) record.IsOff = !requestedOn;
                if (hasFrozen) record.IsFrozen = requestedFrozen;
                if (hasLocked) record.IsLocked = requestedLocked;
                transaction.Commit();
                McpCadAgentRuntime.AuditDomainMutation(
                    "cad_layer_set_state",
                    "name=" + name + "; on=" + (!record.IsOff ? "true" : "false")
                    + "; frozen=" + (record.IsFrozen ? "true" : "false")
                    + "; locked=" + (record.IsLocked ? "true" : "false"));
                return LayerStateJson(record, isCurrent);
            }
        }

        private static string CaptureSnapshot()
        {
            var document = RequireDocument();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (LayerTable)transaction.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                var entries = new List<LayerSnapshotEntry>();
                foreach (ObjectId id in table)
                {
                    if (entries.Count >= MaxSnapshotLayers)
                        throw new InvalidOperationException("Layer snapshot exceeds the bounded " + MaxSnapshotLayers.ToString(CultureInfo.InvariantCulture) + " layer limit.");
                    var record = (LayerTableRecord)transaction.GetObject(id, OpenMode.ForRead);
                    entries.Add(new LayerSnapshotEntry(record.Name, !record.IsOff, record.IsFrozen, record.IsLocked));
                }
                entries.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
                var current = (LayerTableRecord)transaction.GetObject(document.Database.Clayer, OpenMode.ForRead);
                var token = EncodeSnapshot(current.Name, entries);
                return "{\"captured\":true,\"layerCount\":" + entries.Count.ToString(CultureInfo.InvariantCulture)
                       + ",\"currentLayer\":\"" + Escape(current.Name) + "\",\"snapshot\":\"" + Escape(token) + "\"}";
            }
        }

        private static string RestoreSnapshot(string body)
        {
            var token = McpTopLevelJson.ExtractString(body, "snapshot");
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("snapshot is required.");
            if (token.Length > MaxSnapshotTokenLength) throw new InvalidOperationException("snapshot exceeds the bounded token size.");
            var snapshot = DecodeSnapshot(token);
            var document = RequireDocument();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var table = (LayerTable)transaction.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                var current = (LayerTableRecord)transaction.GetObject(document.Database.Clayer, OpenMode.ForRead);

                // Validate the complete restore set before opening any layer for write. A stale or
                // malformed snapshot therefore cannot partially mutate the drawing.
                var ids = new Dictionary<string, ObjectId>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in snapshot.Entries)
                {
                    if (!table.Has(entry.Name)) throw new InvalidOperationException("Snapshot layer no longer exists: " + entry.Name);
                    var id = table[entry.Name];
                    if (ids.ContainsKey(entry.Name)) throw new InvalidOperationException("Snapshot contains a duplicate layer: " + entry.Name);
                    if (id == document.Database.Clayer && (!entry.On || entry.Frozen))
                        throw new InvalidOperationException("Snapshot would turn off or freeze the current layer: " + entry.Name);
                    ids.Add(entry.Name, id);
                }
                if (!string.Equals(snapshot.CurrentLayer, current.Name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Snapshot current-layer identity does not match the active current layer. Expected "
                        + snapshot.CurrentLayer + " but found " + current.Name + ".");

                foreach (var entry in snapshot.Entries)
                {
                    var record = (LayerTableRecord)transaction.GetObject(ids[entry.Name], OpenMode.ForWrite);
                    record.IsOff = !entry.On;
                    record.IsFrozen = entry.Frozen;
                    record.IsLocked = entry.Locked;
                }
                transaction.Commit();
                McpCadAgentRuntime.AuditDomainMutation(
                    "cad_layer_restore",
                    "layerCount=" + snapshot.Entries.Count.ToString(CultureInfo.InvariantCulture)
                    + "; currentLayer=" + snapshot.CurrentLayer);
                return "{\"restored\":true,\"layerCount\":" + snapshot.Entries.Count.ToString(CultureInfo.InvariantCulture)
                       + ",\"currentLayer\":\"" + Escape(snapshot.CurrentLayer) + "\"}";
            }
        }

        private static string EncodeSnapshot(string currentLayer, List<LayerSnapshotEntry> entries)
        {
            var builder = new StringBuilder();
            builder.Append(SnapshotVersion).Append('\n');
            builder.Append(ToBase64(currentLayer)).Append('\n');
            foreach (var entry in entries)
            {
                builder.Append(ToBase64(entry.Name)).Append('|')
                    .Append(entry.On ? '1' : '0').Append('|')
                    .Append(entry.Frozen ? '1' : '0').Append('|')
                    .Append(entry.Locked ? '1' : '0').Append('\n');
            }
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(builder.ToString()));
            if (token.Length > MaxSnapshotTokenLength)
                throw new InvalidOperationException("Layer snapshot exceeds the bounded token size.");
            return token;
        }

        private static LayerSnapshot DecodeSnapshot(string token)
        {
            string text;
            try { text = Encoding.UTF8.GetString(Convert.FromBase64String(token)); }
            catch (Exception ex) { throw new InvalidOperationException("snapshot is not a valid layer-state token.", ex); }
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length < 2 || !string.Equals(lines[0], SnapshotVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("snapshot has an unsupported layer-state version.");
            var currentLayer = FromBase64(lines[1], "snapshot current layer");
            var entries = new List<LayerSnapshotEntry>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 2; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;
                if (entries.Count >= MaxSnapshotLayers)
                    throw new InvalidOperationException("snapshot exceeds the bounded layer count.");
                var parts = lines[i].Split('|');
                if (parts.Length != 4 || !Bit(parts[1]) || !Bit(parts[2]) || !Bit(parts[3]))
                    throw new InvalidOperationException("snapshot contains a malformed layer entry.");
                var name = RequireLayerName(FromBase64(parts[0], "snapshot layer"));
                if (!names.Add(name)) throw new InvalidOperationException("snapshot contains a duplicate layer: " + name);
                entries.Add(new LayerSnapshotEntry(name, parts[1] == "1", parts[2] == "1", parts[3] == "1"));
            }
            if (entries.Count == 0) throw new InvalidOperationException("snapshot contains no layer entries.");
            return new LayerSnapshot(RequireLayerName(currentLayer), entries);
        }

        private static bool Bit(string value) { return value == "0" || value == "1"; }
        private static string ToBase64(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty)); }
        private static string FromBase64(string value, string label)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
            catch (Exception ex) { throw new InvalidOperationException(label + " is malformed.", ex); }
        }

        private static LayerTableRecord RequireLayer(Transaction transaction, Database database, string name, OpenMode mode)
        {
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (!table.Has(name)) throw new InvalidOperationException("Layer does not exist: " + name);
            return (LayerTableRecord)transaction.GetObject(table[name], mode);
        }

        private static string RequireLayerName(string value)
        {
            var name = (value ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > 255)
                throw new InvalidOperationException("Layer name must contain 1 through 255 characters.");
            if (name.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
                throw new InvalidOperationException("Layer name contains invalid control characters.");
            return name;
        }

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document.");
            return document;
        }

        private static string LayerStateJson(LayerTableRecord record, bool isCurrent)
        {
            return "{\"name\":\"" + Escape(record.Name) + "\",\"on\":" + (!record.IsOff ? "true" : "false")
                   + ",\"frozen\":" + (record.IsFrozen ? "true" : "false")
                   + ",\"locked\":" + (record.IsLocked ? "true" : "false")
                   + ",\"current\":" + (isCurrent ? "true" : "false") + "}";
        }

        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }
        private static string ConfirmMutationProperty() { return "\"confirmMutation\":{\"type\":\"boolean\",\"const\":true}"; }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + Escape(name) + "\",\"description\":\"" + Escape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + properties + "}"
                   + requiredJson + ",\"additionalProperties\":false}}";
        }

        private sealed class LayerSnapshot
        {
            internal LayerSnapshot(string currentLayer, List<LayerSnapshotEntry> entries)
            {
                CurrentLayer = currentLayer;
                Entries = entries;
            }
            internal string CurrentLayer { get; private set; }
            internal List<LayerSnapshotEntry> Entries { get; private set; }
        }

        private sealed class LayerSnapshotEntry
        {
            internal LayerSnapshotEntry(string name, bool on, bool frozen, bool locked)
            {
                Name = name;
                On = on;
                Frozen = frozen;
                Locked = locked;
            }
            internal string Name { get; private set; }
            internal bool On { get; private set; }
            internal bool Frozen { get; private set; }
            internal bool Locked { get; private set; }
        }
    }
}
