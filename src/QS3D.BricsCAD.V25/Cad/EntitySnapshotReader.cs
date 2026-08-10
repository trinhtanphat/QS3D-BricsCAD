using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Model;
using Teigha.DatabaseServices;
namespace QS3D.BricsCAD.V25.Cad
{
    internal static class EntitySnapshotReader
    {
        public static IReadOnlyList<EntitySnapshot> ReadCurrentSelection(Document document) => ReadSelection(document, true);
        public static IReadOnlyList<EntitySnapshot> ReadImpliedSelection(Document document) => ReadSelection(document, false);
        private static IReadOnlyList<EntitySnapshot> ReadSelection(Document document, bool promptIfEmpty)
        {
            if (document == null) throw new ArgumentNullException(nameof(document)); var editor = document.Editor; var selection = editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) { if (!promptIfEmpty) return Array.Empty<EntitySnapshot>(); var prompt = editor.GetSelection(); if (prompt.Status != PromptStatus.OK || prompt.Value == null) return Array.Empty<EntitySnapshot>(); selection = prompt; }
            var result = new List<EntitySnapshot>(); using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction()) { foreach (var id in selection.Value.GetObjectIds()) { var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; if (entity == null) continue; var snapshot = new EntitySnapshot(entity.Handle.ToString(), entity.GetType().Name, entity.Layer); TryPopulateCommonMetrics(entity, snapshot); result.Add(snapshot); } transaction.Commit(); } return result;
        }
        private static void TryPopulateCommonMetrics(Entity entity, EntitySnapshot snapshot)
        {
            if (entity is Curve curve) { try { var start = curve.StartParam; var end = curve.EndParam; var length = curve.GetDistanceAtParameter(end) - curve.GetDistanceAtParameter(start); if (!double.IsNaN(length) && !double.IsInfinity(length) && length >= 0d) snapshot.LengthDrawingUnits = length; } catch { } }
            if (entity is Polyline polyline && polyline.Closed) { try { var area = Math.Abs(polyline.Area); if (!double.IsNaN(area) && !double.IsInfinity(area)) snapshot.AreaDrawingUnitsSquared = area; } catch { } }
        }
    }
}
