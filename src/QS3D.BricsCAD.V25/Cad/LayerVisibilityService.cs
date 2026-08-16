using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class LayerVisibilityService
    {
        public static int SetVisible(Document document, IEnumerable<string> names, bool visible)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (names == null) throw new ArgumentNullException(nameof(names));
            var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            if (wanted.Count == 0) return 0;
            var count = 0;
            var changed = 0;
            using (document.LockDocument())
            using (var tr = document.Database.TransactionManager.StartTransaction())
            {
                var table = (LayerTable)tr.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var layer = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    if (layer == null || !wanted.Contains(layer.Name)) continue;
                    count++;

                    var targetIsOff = !visible;
                    var requiresThaw = visible && layer.IsFrozen;
                    if (layer.IsOff == targetIsOff && !requiresThaw) continue;

                    layer.UpgradeOpen();
                    if (layer.IsOff != targetIsOff) layer.IsOff = targetIsOff;
                    if (requiresThaw) layer.IsFrozen = false;
                    changed++;
                }
                tr.Commit();
            }
            if (changed > 0) document.Editor.Regen();
            return count;
        }

        public static int SetLocked(Document document, IEnumerable<string> names, bool locked)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (names == null) throw new ArgumentNullException(nameof(names));
            var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            if (wanted.Count == 0) return 0;
            var count = 0;
            var changed = 0;
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
                    changed++;
                }
                tr.Commit();
            }
            if (changed > 0) document.Editor.Regen();
            return count;
        }
    }
}
