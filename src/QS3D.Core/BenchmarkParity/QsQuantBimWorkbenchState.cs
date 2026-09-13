using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class QuantBimWorkbenchStateSnapshot
    {
        public QuantBimWorkbenchStateSnapshot(
            string documentPath,
            string revision,
            string entity,
            string storey,
            string type,
            string classification,
            string selectionName,
            IEnumerable<string> selectedGuids)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Entity = QsModelElementSnapshot.Optional(entity);
            Storey = QsModelElementSnapshot.Optional(storey);
            Type = QsModelElementSnapshot.Optional(type);
            Classification = QsModelElementSnapshot.Optional(classification);
            SelectionName = QsModelElementSnapshot.Require(selectionName, "selectionName");
            SelectedGuids = new ReadOnlyCollection<string>((selectedGuids ?? throw new ArgumentNullException("selectedGuids"))
                .Select(x => QsModelElementSnapshot.Require(x, "selectedGuid"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList());
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public string Entity { get; private set; }
        public string Storey { get; private set; }
        public string Type { get; private set; }
        public string Classification { get; private set; }
        public string SelectionName { get; private set; }
        public IReadOnlyList<string> SelectedGuids { get; private set; }

        public IfcWorkbenchFilter ToFilter()
        {
            return new IfcWorkbenchFilter(Entity, Storey, Type, Classification);
        }

        public IfcSelectionSet ToSelection()
        {
            return new IfcSelectionSet(SelectionName, SelectedGuids);
        }
    }

    public sealed class QuantBimWorkbenchRestoredState
    {
        public QuantBimWorkbenchRestoredState(IfcWorkbenchFilter filter, IfcSelectionSet selection)
        {
            Filter = filter ?? throw new ArgumentNullException("filter");
            Selection = selection ?? throw new ArgumentNullException("selection");
        }

        public IfcWorkbenchFilter Filter { get; private set; }
        public IfcSelectionSet Selection { get; private set; }
    }

    public sealed class QuantBimWorkbenchStateCoordinator
    {
        public QuantBimWorkbenchStateSnapshot Capture(IfcStandaloneDocument document, IfcWorkbenchFilter filter, IfcSelectionSet selection)
        {
            Validate(document, filter, selection);
            return new QuantBimWorkbenchStateSnapshot(
                document.Path,
                document.Revision,
                filter.Entity,
                filter.Storey,
                filter.Type,
                filter.Classification,
                selection.Name,
                selection.Guids);
        }

        public QuantBimWorkbenchRestoredState Restore(IfcStandaloneDocument document, QuantBimWorkbenchStateSnapshot snapshot)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (!string.Equals(document.Path, snapshot.DocumentPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QuantBIM workbench state belongs to a different IFC document path.");
            if (!string.Equals(document.Revision, snapshot.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM workbench state is stale for the current IFC document revision.");

            var filter = snapshot.ToFilter();
            var selection = snapshot.ToSelection();
            Validate(document, filter, selection);
            return new QuantBimWorkbenchRestoredState(filter, selection);
        }

        private static void Validate(IfcStandaloneDocument document, IfcWorkbenchFilter filter, IfcSelectionSet selection)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (filter == null) throw new ArgumentNullException("filter");
            if (selection == null) throw new ArgumentNullException("selection");

            var byGuid = new Dictionary<string, IfcStandaloneElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in document.Elements)
            {
                if (element == null) throw new InvalidOperationException("IFC document contains a null element.");
                if (!byGuid.TryAdd(element.Guid, element))
                    throw new InvalidOperationException("IFC document contains duplicate element GUID: " + element.Guid + ".");
            }

            foreach (var guid in selection.Guids)
            {
                IfcStandaloneElement element;
                if (!byGuid.TryGetValue(guid, out element))
                    throw new InvalidOperationException("QuantBIM selection references an element that is not present in the current IFC document: " + guid + ".");
                if (!Matches(filter.Entity, element.Entity) ||
                    !Matches(filter.Storey, element.Storey) ||
                    !Matches(filter.Type, element.Type) ||
                    !Matches(filter.Classification, element.Classification))
                    throw new InvalidOperationException("QuantBIM selection contains an element hidden by the active workbench filter: " + guid + ".");
            }
        }

        private static bool Matches(string filter, string value)
        {
            return filter.Length == 0 || string.Equals(filter, value, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class QuantBimWorkbenchStateCodec
    {
        private const string Header = "QS3D-QUANTBIM-WORKBENCH-STATE/1";
        private static readonly string[] FixedKeys =
        {
            "DocumentPath", "Revision", "Entity", "Storey", "Type", "Classification", "SelectionName"
        };

        public static string Encode(QuantBimWorkbenchStateSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var builder = new StringBuilder();
            builder.Append(Header).Append('\n');
            Append(builder, "DocumentPath", snapshot.DocumentPath);
            Append(builder, "Revision", snapshot.Revision);
            Append(builder, "Entity", snapshot.Entity);
            Append(builder, "Storey", snapshot.Storey);
            Append(builder, "Type", snapshot.Type);
            Append(builder, "Classification", snapshot.Classification);
            Append(builder, "SelectionName", snapshot.SelectionName);
            foreach (var guid in snapshot.SelectedGuids) Append(builder, "Guid", guid);
            return builder.ToString();
        }

        public static QuantBimWorkbenchStateSnapshot Decode(string encoded)
        {
            if (encoded == null) throw new ArgumentNullException("encoded");
            var lines = encoded.Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length < 8 || !string.Equals(lines[0], Header, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported QuantBIM workbench state format.");

            var values = new string[FixedKeys.Length];
            for (var i = 0; i < FixedKeys.Length; i++)
            {
                values[i] = Read(lines[i + 1], FixedKeys[i]);
            }

            var guids = new List<string>();
            for (var i = 1 + FixedKeys.Length; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                {
                    if (i == lines.Length - 1) continue;
                    throw new InvalidOperationException("Unexpected blank line in QuantBIM workbench state.");
                }
                guids.Add(Read(lines[i], "Guid"));
            }

            return new QuantBimWorkbenchStateSnapshot(
                values[0], values[1], values[2], values[3], values[4], values[5], values[6], guids);
        }

        private static void Append(StringBuilder builder, string key, string value)
        {
            builder.Append(key).Append('=').Append(ToBase64(value ?? string.Empty)).Append('\n');
        }

        private static string Read(string line, string expectedKey)
        {
            var prefix = expectedKey + "=";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid QuantBIM workbench state field; expected " + expectedKey + ".");
            var payload = line.Substring(prefix.Length);
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Invalid base64 payload for QuantBIM workbench state field " + expectedKey + ".", ex);
            }
        }

        private static string ToBase64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }
    }
}
