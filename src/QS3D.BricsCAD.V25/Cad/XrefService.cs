using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class XrefService
    {
        public static int SelectInstances(Document document, string xrefName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var ids = new List<ObjectId>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var xrefId = FindRecord(document.Database, transaction, xrefName);
                if (xrefId.IsNull) return 0;
                var currentSpace = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                if (currentSpace != null)
                    foreach (ObjectId id in currentSpace)
                    {
                        var reference = transaction.GetObject(id, OpenMode.ForRead, false) as BlockReference;
                        if (reference != null && reference.BlockTableRecord == xrefId) ids.Add(id);
                    }
                transaction.Commit();
            }
            document.Editor.SetImpliedSelection(ids.ToArray()); return ids.Count;
        }

        public static int SetInstanceLayersLocked(Document document, string xrefName, bool locked)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(xrefName)) throw new ArgumentException("Xref name is required.", nameof(xrefName));

            var affectedLayers = 0;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var xrefId = FindRecord(document.Database, transaction, xrefName);
                if (xrefId.IsNull) throw new InvalidOperationException("Không tìm thấy Xref: " + xrefName);

                var currentSpace = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (currentSpace == null)
                {
                    transaction.Commit();
                    return 0;
                }

                var layerIds = new HashSet<ObjectId>();
                foreach (ObjectId id in currentSpace)
                {
                    var reference = transaction.GetObject(id, OpenMode.ForRead, false) as BlockReference;
                    if (reference == null || reference.IsErased || reference.BlockTableRecord != xrefId) continue;
                    if (!reference.LayerId.IsNull) layerIds.Add(reference.LayerId);
                }

                foreach (var layerId in layerIds)
                {
                    var layer = transaction.GetObject(layerId, OpenMode.ForWrite, false) as LayerTableRecord;
                    if (layer == null) continue;
                    layer.IsLocked = locked;
                    affectedLayers = checked(affectedLayers + 1);
                }

                transaction.Commit();
            }

            if (affectedLayers > 0) document.Editor.Regen();
            return affectedLayers;
        }

        public static void Reload(Document document, string xrefName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            ObjectId xrefId;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction()) { xrefId = FindRecord(document.Database, transaction, xrefName); transaction.Commit(); }
            if (xrefId.IsNull) throw new InvalidOperationException("Không tìm thấy Xref: " + xrefName);
            var ids = new ObjectIdCollection(); ids.Add(xrefId);
            using (document.LockDocument()) document.Database.ReloadXrefs(ids);
            document.Editor.Regen();
        }

        public static void Detach(Document document, string xrefName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            ObjectId xrefId;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction()) { xrefId = FindRecord(document.Database, transaction, xrefName); transaction.Commit(); }
            if (xrefId.IsNull) throw new InvalidOperationException("Không tìm thấy Xref: " + xrefName);
            using (document.LockDocument()) document.Database.DetachXref(xrefId);
            document.Editor.Regen();
        }

        private static ObjectId FindRecord(Database database, Transaction transaction, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return ObjectId.Null;
            var table = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId id in table)
            {
                var record = transaction.GetObject(id, OpenMode.ForRead, false) as BlockTableRecord;
                if (record != null && record.IsFromExternalReference && string.Equals(record.Name, name, StringComparison.OrdinalIgnoreCase)) return id;
            }
            return ObjectId.Null;
        }
    }
}
