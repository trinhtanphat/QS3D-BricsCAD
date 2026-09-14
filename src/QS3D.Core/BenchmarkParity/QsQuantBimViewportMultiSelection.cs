using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum QuantBimSelectionInteractionMode
    {
        Replace,
        Add,
        Toggle
    }

    /// <summary>
    /// Renderer-neutral reducer for Windows standalone viewport selection interaction.
    /// It only manages IFC GUID selection identity; picking, property/QTO/BOQ generation and
    /// quantity evidence remain owned by the existing standalone session/workbench pipeline.
    /// </summary>
    public sealed class QuantBimViewportMultiSelection
    {
        public IfcSelectionSet Apply(
            IfcSelectionSet current,
            QuantBimViewportSelectionResult viewportResult,
            QuantBimSelectionInteractionMode mode,
            bool clearOnMiss)
        {
            if (viewportResult == null) throw new ArgumentNullException("viewportResult");
            var guid = viewportResult.Hit == null ? null : viewportResult.Hit.Guid;
            return ApplyGuid(current, guid, mode, clearOnMiss);
        }

        public IfcSelectionSet ApplyGuid(
            IfcSelectionSet current,
            string? pickedGuid,
            QuantBimSelectionInteractionMode mode,
            bool clearOnMiss)
        {
            if (current == null) throw new ArgumentNullException("current");
            if (!Enum.IsDefined(typeof(QuantBimSelectionInteractionMode), mode))
                throw new ArgumentOutOfRangeException("mode");

            var selected = new HashSet<string>(current.Guids, StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(pickedGuid))
                return clearOnMiss ? new IfcSelectionSet(current.Name, Array.Empty<string>()) : Snapshot(current.Name, selected);

            pickedGuid = QsModelElementSnapshot.Require(pickedGuid, "pickedGuid");
            switch (mode)
            {
                case QuantBimSelectionInteractionMode.Replace:
                    selected.Clear();
                    selected.Add(pickedGuid);
                    break;
                case QuantBimSelectionInteractionMode.Add:
                    selected.Add(pickedGuid);
                    break;
                case QuantBimSelectionInteractionMode.Toggle:
                    if (!selected.Remove(pickedGuid)) selected.Add(pickedGuid);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }

            return Snapshot(current.Name, selected);
        }

        private static IfcSelectionSet Snapshot(string name, IEnumerable<string> guids)
        {
            return new IfcSelectionSet(name, guids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal));
        }
    }
}
