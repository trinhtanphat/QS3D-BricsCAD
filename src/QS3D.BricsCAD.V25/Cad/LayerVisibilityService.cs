using System;
using System.Collections;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class LayerVisibilityService
    {
        private const int MaxRequestedLayerNames = 10000;

        public static int SetVisible(Document document, IEnumerable<string> names, bool visible)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (names == null) throw new ArgumentNullException(nameof(names));
            var wanted = BuildWantedNames(names);
            if (wanted.Count == 0) return 0;
            var count = 0;
            var mutationCount = 0;
            using (document.LockDocument())
            using (var tr = document.Database.TransactionManager.StartTransaction())
            {
                var table = (LayerTable)tr.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var layer = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    if (layer == null || !wanted.Contains(layer.Name)) continue;
                    count++;

                    var desiredOff = !visible;
                    var changeOffState = layer.IsOff != desiredOff;
                    var thawLayer = visible && layer.IsFrozen;
                    if (!changeOffState && !thawLayer) continue;

                    layer.UpgradeOpen();
                    if (changeOffState) layer.IsOff = desiredOff;
                    if (thawLayer) layer.IsFrozen = false;
                    mutationCount++;
                }
                tr.Commit();
            }
            if (mutationCount > 0) document.Editor.Regen();
            return count;
        }

        public static int SetLocked(Document document, IEnumerable<string> names, bool locked)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (names == null) throw new ArgumentNullException(nameof(names));
            var wanted = BuildWantedNames(names);
            if (wanted.Count == 0) return 0;
            var count = 0;
            var mutationCount = 0;
            using (document.LockDocument())
            using (var tr = document.Database.TransactionManager.StartTransaction())
            {
                var table = (LayerTable)tr.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var layer = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    if (layer == null || !wanted.Contains(layer.Name)) continue;
                    count++;
                    if (layer.IsLocked == locked) continue;

                    layer.UpgradeOpen();
                    layer.IsLocked = locked;
                    mutationCount++;
                }
                tr.Commit();
            }
            if (mutationCount > 0) document.Editor.Regen();
            return count;
        }

        private static HashSet<string> BuildWantedNames(IEnumerable<string> names)
        {
            var countedNames = names as ICollection<string>;
            var readOnlyCountedNames = names as IReadOnlyCollection<string>;
            var nonGenericCountedNames = names as ICollection;
            if ((countedNames != null && countedNames.Count > MaxRequestedLayerNames) ||
                (readOnlyCountedNames != null && readOnlyCountedNames.Count > MaxRequestedLayerNames) ||
                (nonGenericCountedNames != null && nonGenericCountedNames.Count > MaxRequestedLayerNames))
            {
                throw LayerSelectionLimitError();
            }

            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var enumerated = 0;
            foreach (var name in names)
            {
                enumerated++;
                if (enumerated > MaxRequestedLayerNames)
                    throw LayerSelectionLimitError();
                wanted.Add(name);
            }
            return wanted;
        }

        private static ArgumentException LayerSelectionLimitError()
        {
            return new ArgumentException(
                "Layer selection exceeds the supported limit of " + MaxRequestedLayerNames + " entries.",
                "names");
        }
    }
}
