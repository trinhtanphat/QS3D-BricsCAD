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
        public short ColorIndex { get; set; }
    }

    internal sealed class DrawingReferenceSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsXref { get; set; }
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
                    result.Add(new LayerSnapshot { Name = layer.Name, IsVisible = !layer.IsOff && !layer.IsFrozen, ColorIndex = layer.Color.ColorIndex });
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
            using (var tr = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = (BlockTable)tr.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    var record = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                    if (record == null || !record.IsFromExternalReference) continue;
                    result.Add(new DrawingReferenceSnapshot { Name = record.Name, Path = record.PathName ?? string.Empty, IsXref = true });
                }
                tr.Commit();
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }
    }
}
