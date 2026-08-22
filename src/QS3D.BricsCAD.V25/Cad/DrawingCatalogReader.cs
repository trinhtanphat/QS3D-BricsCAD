using System;
using System.Collections.Generic;
using System.Globalization;
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
        public bool HasScale { get; set; }
        public bool MixedScale { get; set; }
        public double ScaleX { get; set; } = 1d;
        public double ScaleY { get; set; } = 1d;
        public double ScaleZ { get; set; } = 1d;
        public string ScaleText { get; set; } = "—";
    }

    internal static class DrawingCatalogReader
    {
        private const double ScaleTolerance = 1e-9;

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

                        var scale = reference.ScaleFactors;
                        if (!snapshot.HasScale)
                        {
                            snapshot.HasScale = true;
                            snapshot.ScaleX = scale.X;
                            snapshot.ScaleY = scale.Y;
                            snapshot.ScaleZ = scale.Z;
                        }
                        else if (!SameScale(snapshot.ScaleX, scale.X) ||
                                 !SameScale(snapshot.ScaleY, scale.Y) ||
                                 !SameScale(snapshot.ScaleZ, scale.Z))
                        {
                            snapshot.MixedScale = true;
                        }
                    }
                }

                foreach (var snapshot in result)
                {
                    if (snapshot.InstanceCount == 0) snapshot.LockState = "—";
                    else if (snapshot.LockedInstanceCount == 0) snapshot.LockState = "Mở";
                    else if (snapshot.LockedInstanceCount == snapshot.InstanceCount) snapshot.LockState = "Khóa";
                    else snapshot.LockState = "Hỗn hợp";

                    snapshot.ScaleText = snapshot.InstanceCount == 0 || !snapshot.HasScale
                        ? "—"
                        : snapshot.MixedScale
                            ? "Hỗn hợp"
                            : FormatScale(snapshot.ScaleX, snapshot.ScaleY, snapshot.ScaleZ);
                }
                tr.Commit();
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        private static bool SameScale(double left, double right)
        {
            if (double.IsNaN(left) || double.IsNaN(right) || double.IsInfinity(left) || double.IsInfinity(right))
                return left.Equals(right);
            var magnitude = Math.Max(1d, Math.Max(Math.Abs(left), Math.Abs(right)));
            return Math.Abs(left - right) <= ScaleTolerance * magnitude;
        }

        private static string FormatScale(double x, double y, double z)
        {
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z)) return "Không hợp lệ";
            var uniform = SameScale(x, y) && SameScale(y, z);
            if (uniform && x > 0d)
            {
                if (SameScale(x, 1d)) return "1:1";
                return x < 1d
                    ? "1:" + FormatScaleNumber(1d / x)
                    : FormatScaleNumber(x) + ":1";
            }

            return "X/Y/Z " + FormatScaleNumber(x) + "/" + FormatScaleNumber(y) + "/" + FormatScaleNumber(z);
        }

        private static string FormatScaleNumber(double value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
