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
        private const int MaxCurrentSpaceEntities = 250000;

        public static IReadOnlyList<EntitySnapshot> ReadCurrentSelection(Document document) => ReadSelection(document, true);
        public static IReadOnlyList<EntitySnapshot> ReadImpliedSelection(Document document) => ReadSelection(document, false);

        public static IReadOnlyList<EntitySnapshot> ReadCurrentSpace(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new List<EntitySnapshot>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (space != null)
                {
                    var scanned = 0;
                    foreach (ObjectId id in space)
                    {
                        if (scanned++ >= MaxCurrentSpaceEntities) throw new InvalidOperationException("QS3DB4D Current Space exceeds the guarded limit of " + MaxCurrentSpaceEntities + " entities.");
                        try { AddSnapshot(transaction, id, result); } catch { }
                    }
                }
                transaction.Commit();
            }
            return result;
        }

        public static IReadOnlyList<EntitySnapshot> ReadHandles(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            var objectIds = CadHandleService.Resolve(document, handles);
            if (objectIds.Count == 0) return Array.Empty<EntitySnapshot>();

            var result = new List<EntitySnapshot>(objectIds.Count);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in objectIds)
                    AddSnapshot(transaction, id, result);
                transaction.Commit();
            }
            return result;
        }

        private static IReadOnlyList<EntitySnapshot> ReadSelection(Document document, bool promptIfEmpty)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var editor = document.Editor;
            var selection = editor.SelectImplied();
            var restoreInteractiveSelection = false;
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                if (!promptIfEmpty) return Array.Empty<EntitySnapshot>();
                var prompt = editor.GetSelection();
                if (prompt.Status != PromptStatus.OK || prompt.Value == null) return Array.Empty<EntitySnapshot>();
                selection = prompt;
                restoreInteractiveSelection = true;
            }

            var objectIds = selection.Value.GetObjectIds();
            if (objectIds.Length == 0) return Array.Empty<EntitySnapshot>();

            // GetSelection() is interactive and is not guaranteed to become the persistent PICKFIRST set.
            // Restore only that interactive result so native QS3D builders consuming SelectImplied() see the
            // same source ids. Never call SetImpliedSelection while merely reading an existing implied
            // selection: doing so from ImpliedSelectionChanged can recursively generate more selection events.
            if (restoreInteractiveSelection) editor.SetImpliedSelection(objectIds);

            var result = new List<EntitySnapshot>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in objectIds)
                    AddSnapshot(transaction, id, result);
                transaction.Commit();
            }
            return result;
        }

        private static void AddSnapshot(Transaction transaction, ObjectId id, ICollection<EntitySnapshot> result)
        {
            if (id.IsNull || id.IsErased) return;
            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null) return;
            var snapshot = new EntitySnapshot(entity.Handle.ToString(), entity.GetType().Name, entity.Layer)
            {
                HasQs3dGeneratedOwnershipMarker = GeneratedNativeSourceGuard.HasKnownOwnershipMarker(entity)
            };
            PopulateMetrics(entity, snapshot);
            PopulateMetadata(transaction, entity, snapshot);
            result.Add(snapshot);
        }

        private static void PopulateMetrics(Entity entity, EntitySnapshot snapshot)
        {
            if (entity is Curve curve)
            {
                try { var length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam); if (!double.IsNaN(length) && !double.IsInfinity(length) && length >= 0d) snapshot.LengthDrawingUnits = length; } catch { }
            }
            if (entity is Polyline polyline && polyline.Closed)
            {
                try { var area = Math.Abs(polyline.Area); if (!double.IsNaN(area) && !double.IsInfinity(area)) snapshot.AreaDrawingUnitsSquared = area; } catch { }
            }
            if (entity is Region region)
            {
                try { var area = Math.Abs(region.Area); if (!double.IsNaN(area) && !double.IsInfinity(area)) snapshot.AreaDrawingUnitsSquared = area; } catch { }
            }
            if (entity is Hatch hatch)
            {
                try { var area = Math.Abs(hatch.Area); if (!double.IsNaN(area) && !double.IsInfinity(area)) snapshot.AreaDrawingUnitsSquared = area; } catch { }
            }
            if (entity is Solid3d solid)
            {
                try { var area = Math.Abs(solid.Area); if (!double.IsNaN(area) && !double.IsInfinity(area)) snapshot.SurfaceAreaDrawingUnitsSquared = area; } catch { }
                try { var volume = Math.Abs(solid.MassProperties.Volume); if (!double.IsNaN(volume) && !double.IsInfinity(volume)) snapshot.VolumeDrawingUnitsCubed = volume; } catch { }
            }
        }

        private static void PopulateMetadata(Transaction transaction, Entity entity, EntitySnapshot snapshot)
        {
            if (entity is DBText dbText && !string.IsNullOrWhiteSpace(dbText.TextString)) snapshot.Metadata["Text"] = dbText.TextString;
            if (entity is MText mText && !string.IsNullOrWhiteSpace(mText.Contents)) snapshot.Metadata["Text"] = mText.Contents;
            if (entity is BlockReference block)
            {
                try
                {
                    var record = transaction.GetObject(block.BlockTableRecord, OpenMode.ForRead, false) as BlockTableRecord;
                    if (record != null && !string.IsNullOrWhiteSpace(record.Name)) snapshot.Metadata["BlockName"] = record.Name;
                }
                catch { }
            }
        }
    }
}
