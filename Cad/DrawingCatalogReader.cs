using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class LayerSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public short ColorIndex { get; set; }
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
    }

    internal sealed class DrawingReferenceSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsXref { get; set; }
        public int InstanceCount { get; set; }
        public int LockedInstanceCount { get; set; }
        public string LockState { get; set; } = "—";
    }

    internal static class DrawingCatalogReader
    {
        public static IReadOnlyList<LayerSnapshot> ReadLayers(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new List<LayerSnapshot>();
            using (var tr = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (LayerTable)tr.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var layer = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    if (layer == null) continue;
                    var color = layer.Color;
                    result.Add(new LayerSnapshot
                    {
                        Name = layer.Name,
                        IsVisible = !layer.IsOff && !layer.IsFrozen,
                        IsLocked = layer.IsLocked,
                        ColorIndex = color.ColorIndex,
                        Red = color.Red,
                        Green = color.Green,
                        Blue = color.Blue
                    });
                }
                tr.Commit();
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        public static IReadOnlyList<DrawingReferenceSnapshot> ReadReferences(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new List<DrawingReferenceSnapshot>();
            var byRecord = new Dictionary<ObjectId, DrawingReferenceSnapshot>();
            using (var tr = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (BlockTable)tr.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var record = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                    if (record == null || !record.IsFromExternalReference) continue;
                    var snapshot = new DrawingReferenceSnapshot
                    {
                        Name = record.Name,
                        Path = record.PathName ?? string.Empty,
                        IsXref = true
                    };
                    result.Add(snapshot);
                    byRecord[id] = snapshot;
                }

                var currentSpace = tr.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (currentSpace != null)
                {
                    foreach (ObjectId id in currentSpace)
                    {
                        var reference = tr.GetObject(id, OpenMode.ForRead, false) as BlockReference;
                        if (reference == null || reference.IsErased || !byRecord.TryGetValue(reference.BlockTableRecord, out var snapshot)) continue;
                        snapshot.InstanceCount = checked(snapshot.InstanceCount + 1);
                        var layer = tr.GetObject(reference.LayerId, OpenMode.ForRead, false) as LayerTableRecord;
                        if (layer != null && layer.IsLocked) snapshot.LockedInstanceCount = checked(snapshot.LockedInstanceCount + 1);
                    }
                }

                foreach (var snapshot in result)
                {
                    if (snapshot.InstanceCount == 0) snapshot.LockState = "—";
                    else if (snapshot.LockedInstanceCount == 0) snapshot.LockState = "Mở";
                    else if (snapshot.LockedInstanceCount == snapshot.InstanceCount) snapshot.LockState = "Khóa";
                    else snapshot.LockState = "Hỗn hợp";
                }
                tr.Commit();
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }
    }
}
