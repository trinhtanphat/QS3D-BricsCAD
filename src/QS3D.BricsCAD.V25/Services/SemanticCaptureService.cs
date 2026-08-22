using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Recognition;
using QS3D.Core.Services;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class SemanticCaptureService
    {
        public static int Capture(Document document, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            if (snapshots.Count == 0) return 0;
            EnsureCapturePreflight(document, snapshots, category);
            var projectExistedBeforeCapture = ProjectContextCoordinator.TryGetReadOnly(document, out _);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                var count = 0;
                foreach (var snapshot in snapshots) if (CaptureSnapshotCore(document, project, snapshot, category)) count++;
                return count;
            }
            catch (Exception operationError)
            {
                RestoreCaptureOrThrow(document, project, rollback, projectExistedBeforeCapture, operationError, "Semantic capture batch");
                throw;
            }
        }

        public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            EnsureCapturePreflight(document, new[] { snapshot }, category);
            var projectExistedBeforeCapture = ProjectContextCoordinator.TryGetReadOnly(document, out _);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                return CaptureSnapshotCore(document, project, snapshot, category);
            }
            catch (Exception operationError)
            {
                RestoreCaptureOrThrow(document, project, rollback, projectExistedBeforeCapture, operationError, "Semantic capture");
                throw;
            }
        }

        private static void EnsureCapturePreflight(
            Document document,
            IReadOnlyList<EntitySnapshot> snapshots,
            ElementCategory category)
        {
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            foreach (var snapshot in snapshots)
            {
                if (snapshot == null)
                    throw new ArgumentException("Semantic capture selection cannot contain a null snapshot.", nameof(snapshots));
                EntitySnapshotCaptureEligibility.EnsureReady(snapshot, category);
            }

            if (!CadUnitService.TryGetPolicy(document, out _, out _))
                throw new InvalidOperationException("Drawing units are unresolved. Run QS3DUNITS before semantic capture.");
        }

        private static bool CaptureSnapshotCore(Document document, ProjectState project, EntitySnapshot snapshot, ElementCategory category)
        {
            EntitySnapshotCaptureEligibility.EnsureReady(snapshot, category);
            if (!CadUnitService.TryGetPolicy(document, out var units, out var unitResolution))
                throw new InvalidOperationException("Drawing units are unresolved. Run QS3DUNITS before semantic capture.");
            DrawingUnitResolutionPolicy.BindQuantityUnit(
                project.Metadata,
                project.Elements.Count > 0,
                unitResolution.Unit,
                unitResolution.Source);

            if (GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle, out var generatedOwner, out var generatedSlot))
                throw new InvalidOperationException("CAD handle " + snapshot.Handle + " là output do QS3D sinh từ " + generatedOwner!.Id + " (" + generatedSlot + ") và không thể dùng làm semantic source. Hãy chọn CAD source gốc.");

            var id = category.ToString().ToUpperInvariant() + "-" + snapshot.Handle;
            var element = SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, snapshot.Handle, category, id);
            ProjectFamily family;
            if (element == null)
            {
                family = ResolveFamily(project, category);
                element = new ProjectElement(id, category, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                project.Elements.Add(element);
            }
            else
            {
                var existingFamily = project.FindFamily(element.FamilyId);
                if (existingFamily == null || existingFamily.Category != category)
                {
                    family = ResolveFamily(project, category);
                    element.FamilyId = family.Id;
                }
                else family = existingFamily;
            }
            element.Category = category;
            element.SourceHandles.Clear();
            element.SourceHandles.Add(snapshot.Handle);
            element.DrawingFingerprint = project.DrawingFingerprint;
            element.Properties["Layer"] = snapshot.Layer;
            foreach (var key in element.Properties.Keys.Where(x => x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)).ToList()) element.Properties.Remove(key);
            foreach (var item in snapshot.Metadata) element.Properties["CAD." + item.Key] = item.Value ?? string.Empty;

            ReplaceSourceMetric(element, "LengthM", snapshot.LengthDrawingUnits.HasValue ? units.ToMeters(snapshot.LengthDrawingUnits.Value) : (double?)null);
            ReplaceSourceMetric(element, "AreaM2", snapshot.AreaDrawingUnitsSquared.HasValue ? units.AreaToSquareMeters(snapshot.AreaDrawingUnitsSquared.Value) : (double?)null);
            ReplaceSourceMetric(element, MeasuredSolidQuantityPolicy.SurfaceAreaProperty, snapshot.SurfaceAreaDrawingUnitsSquared.HasValue ? units.AreaToSquareMeters(snapshot.SurfaceAreaDrawingUnitsSquared.Value) : (double?)null);
            ReplaceSourceMetric(element, MeasuredSolidQuantityPolicy.VolumeProperty, snapshot.VolumeDrawingUnitsCubed.HasValue ? units.VolumeToCubicMeters(snapshot.VolumeDrawingUnitsCubed.Value) : (double?)null);
            element.Properties.Remove("VolumeM3");
            if (snapshot.SurfaceAreaDrawingUnitsSquared.HasValue || snapshot.VolumeDrawingUnitsCubed.HasValue)
                element.Properties["CAD.SolidMetricSource"] = "Solid3d.MassProperties";
            else element.Properties.Remove("CAD.SolidMetricSource");
            ApplyFamilyDefaults(element, family);
            element.MarkDirty(ElementDirtyFlags.All);
            Regenerate(project, element);
            MeasuredSolidQuantityPolicy.Apply(element);
            project.Touch();
            return true;
        }

        private static void RestoreCaptureOrThrow(
            Document document,
            ProjectState project,
            ProjectStateSnapshot rollback,
            bool projectExistedBeforeCapture,
            Exception operationError,
            string operation)
        {
            Exception? restoreError = null;
            try
            {
                rollback.Restore(project);
            }
            catch (Exception error)
            {
                restoreError = error;
            }

            if (!projectExistedBeforeCapture) ProjectContextCoordinator.Forget(document);
            if (restoreError != null)
                throw new InvalidOperationException(operation + " failed and project rollback also failed.", new AggregateException(operationError, restoreError));
        }

        private static void RestoreOrThrow(ProjectState project, ProjectStateSnapshot rollback, Exception operationError, string operation)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(operation + " failed and project rollback also failed.", new AggregateException(operationError, restoreError));
            }
        }

        private static void ReplaceSourceMetric(ProjectElement element, string key, double? value)
        {
            if (!value.HasValue)
            {
                element.Properties.Remove(key);
                return;
            }
            if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) throw new InvalidOperationException("CAD source metric must be finite: " + key + ".");
            element.Properties[key] = value.Value.ToString("R", CultureInfo.InvariantCulture);
        }

        public static int GenerateRoomFinishes(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            if (snapshots.Count == 0) return 0;
            var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
            var project = ExistingProjectMutationContext.Require(document, "Room finish generation");
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                var rooms = project.Elements
                    .Where(x => x.Category == ElementCategory.Room && !AutoRoomLifecycle.IsStaleAutoRoom(x) && SemanticReferenceHandles.Intersects(x, handles))
                    .ToList();
                var created = 0;
                foreach (var room in rooms)
                {
                    foreach (var category in RoomFinishSynchronizationService.Categories)
                    {
                        var finish = RoomFinishIdentityService.FindExisting(project, room, category);
                        if (finish == null)
                        {
                            var family = ResolveFamily(project, category);
                            finish = new ProjectElement(RoomFinishIdentityService.CanonicalId(room.Id, category), category, family.Id, room.FloorId, room.ZoneId);
                            project.Elements.Add(finish);
                            created++;
                        }
                        RoomFinishSynchronizationService.Synchronize(project, room, finish);
                        Regenerate(project, finish);
                    }
                }
                if (rooms.Count > 0) project.Touch();
                return created;
            }
            catch (Exception operationError)
            {
                RestoreOrThrow(project, rollback, operationError, "Room finish generation");
                throw;
            }
        }

        public static int SyncExistingRoomFinishes(ProjectState project, ProjectElement room)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (room.Category != ElementCategory.Room) throw new ArgumentException("Source element must be a Room.", nameof(room));
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                var finishes = RoomFinishSynchronizationService.SynchronizeExisting(project, room);
                foreach (var finish in finishes) Regenerate(project, finish);
                if (finishes.Count > 0) project.Touch();
                return finishes.Count;
            }
            catch (Exception operationError)
            {
                RestoreOrThrow(project, rollback, operationError, "Room finish synchronization");
                throw;
            }
        }

        private static ProjectFamily ResolveFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == category) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == category) ?? CreateFamily(project, category);
        }

        private static void Regenerate(ProjectState project, ProjectElement element)
        {
            IElementRegenerator? regenerator = null;
            if (element.Category == ElementCategory.ArchitecturalWall || element.Category == ElementCategory.GlassWall || element.Category == ElementCategory.WallPier) regenerator = new WallRegenerator();
            else if (element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) regenerator = new OpeningRegenerator();
            else
            {
                var structural = new StructuralRegenerator();
                if (structural.CanRegenerate(element.Category)) regenerator = structural;
                else
                {
                    var takeoff = new GenericTakeoffRegenerator();
                    regenerator = takeoff.CanRegenerate(element.Category) ? (IElementRegenerator)takeoff : new RoomRegenerator();
                }
            }
            if (regenerator.CanRegenerate(element.Category)) regenerator.Regenerate(project, element);
            element.MarkClean(ElementGeometryPolicy.SemanticCleanFlags(element.Category));
        }

        private static ProjectFamily CreateFamily(ProjectState project, ElementCategory category)
        {
            var family = new ProjectFamily("auto-" + category.ToString().ToLowerInvariant(), DefaultName(category), category);
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                    family.Properties["ThicknessM"] = "0.2";
                    family.Properties["HeightM"] = "3.6";
                    family.Properties["AxisLeftOffsetM"] = "0";
                    family.Properties["AxisRightOffsetM"] = "0";
                    family.Properties["Material"] = "Gạch";
                    break;
                case ElementCategory.GlassWall:
                    family.Properties["ThicknessM"] = "0.012";
                    family.Properties["HeightM"] = "3.6";
                    family.Properties["AxisLeftOffsetM"] = "0";
                    family.Properties["AxisRightOffsetM"] = "0";
                    family.Properties["Material"] = "Kính";
                    family.Properties["CurtainMaxPanelWidthM"] = "1.2";
                    family.Properties["CurtainMaxPanelHeightM"] = "1.5";
                    family.Properties["CurtainPerimeterFrameWidthM"] = "0.05";
                    family.Properties["CurtainMullionWidthM"] = "0.05";
                    family.Properties["CurtainTransomWidthM"] = "0.05";
                    family.Properties["CurtainFrameDepthM"] = "0.05";
                    family.Properties["CurtainFrameMaterial"] = "Nhôm";
                    break;
                case ElementCategory.WallPier:
                    family.Properties["ThicknessM"] = "0.2";
                    family.Properties["HeightM"] = "3.6";
                    family.Properties["AxisLeftOffsetM"] = "0";
                    family.Properties["AxisRightOffsetM"] = "0";
                    family.Properties["Material"] = "Gạch";
                    family.Properties["WallPierProfileMode"] = "Rectangular";
                    family.Properties["WallPierChamferM"] = "0.02";
                    break;
                case ElementCategory.StructuralWall: family.Properties["ThicknessM"] = "0.2"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Beam: family.Properties["WidthM"] = "0.3"; family.Properties["HeightM"] = "0.5"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Slab: family.Properties["ThicknessM"] = "0.12"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Column: family.Properties["WidthM"] = "0.4"; family.Properties["DepthM"] = "0.4"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Foundation: family.Properties["ThicknessM"] = "0.5"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Stair: family.Properties["ThicknessM"] = "0.15"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Railing: family.Properties["Material"] = "Thép"; break;
                case ElementCategory.Earthwork: family.Properties["DepthM"] = "1"; break;
            }
            if (category == ElementCategory.Room || category == ElementCategory.WallFinish) family.Properties["HeightM"] = "3.6";
            if (category == ElementCategory.WallOpening || category == ElementCategory.Door) family.Properties["HeightM"] = "2.2";
            project.Families.Add(family);
            return family;
        }

        private static string DefaultName(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Room: return "Phòng";
                case ElementCategory.ArchitecturalWall: return "Tường Gạch";
                case ElementCategory.GlassWall: return "Vách Kính";
                case ElementCategory.WallPier: return "Trụ Tường";
                case ElementCategory.StructuralWall: return "Vách BTCT";
                case ElementCategory.Beam: return "Dầm BTCT";
                case ElementCategory.Slab: return "Sàn BTCT";
                case ElementCategory.Column: return "Cột BTCT";
                case ElementCategory.Foundation: return "Móng BTCT";
                case ElementCategory.Stair: return "Cầu thang";
                case ElementCategory.Railing: return "Lan can";
                case ElementCategory.Earthwork: return "Đào đất";
                case ElementCategory.WallOpening: return "Lỗ Mở Vách";
                case ElementCategory.Door: return "Cửa Đi";
                case ElementCategory.FloorFinish: return "Sàn Hoàn Thiện";
                case ElementCategory.Waterproofing: return "Chống Thấm";
                case ElementCategory.Skirting: return "Chân Tường";
                case ElementCategory.WallFinish: return "Hoàn Thiện Tường";
                case ElementCategory.CeilingFinish: return "Trần Hoàn Thiện";
                case ElementCategory.CustomQuantity: return "Quick Takeoff";
                default: return category.ToString();
            }
        }

        private static void ApplyFamilyDefaults(ProjectElement element, ProjectFamily family)
        {
            foreach (var property in family.Properties)
                if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
            if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && !element.Properties.ContainsKey("WidthM"))
                element.Properties["WidthM"] = element.Properties.TryGetValue("LengthM", out var length) ? length : "0.9";
            if (element.Category == ElementCategory.Room && element.Properties.TryGetValue("LengthM", out var perimeter)) element.Properties["PerimeterM"] = perimeter;
            if ((element.Category == ElementCategory.Slab || element.Category == ElementCategory.Foundation || element.Category == ElementCategory.Stair || element.Category == ElementCategory.Earthwork) && element.Properties.TryGetValue("LengthM", out var outline) && !element.Properties.ContainsKey("PerimeterM")) element.Properties["PerimeterM"] = outline;
        }
    }
}