using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Legacy;
using QS3D.Core.Model;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Clean-room legacy BLT3D compatibility commands. Source CAD objects are opened
    /// read-only. Explode() is used only against transient DBObjectCollection output;
    /// no source entity is erased, rewritten, converted or redrawn.
    /// </summary>
    public sealed class BltLegacyCommands
    {
        [CommandMethod("QS3DBLTPROBE", CommandFlags.UsePickSet)]
        public void Probe()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var snapshots = BltLegacyCadInspector.ReadSelection(document, true);
                if (snapshots.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D BLT Probe: không có đối tượng được chọn.");
                    return;
                }

                var candidates = snapshots.Select(BltLegacyEntityAdapter.Adapt).ToList();
                var path = BltLegacyProbeReport.Write(candidates);
                document.Editor.WriteMessage(
                    "\nQS3D BLT Probe: " + candidates.Count.ToString(CultureInfo.InvariantCulture) +
                    " đối tượng • kết quả: " + path);
            }
            catch (Exception error)
            {
                Report(document, "QS3DBLTPROBE", error);
            }
        }

        [CommandMethod("QS3DBLTSCAN", CommandFlags.Modal)]
        public void Scan()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var candidates = BltLegacyCadInspector.ReadCurrentSpace(document)
                    .Select(BltLegacyEntityAdapter.Adapt)
                    .Where(x => x.HasLegacySignal)
                    .ToList();
                WriteSummary(document, "BLT Scan", candidates);
            }
            catch (Exception error)
            {
                Report(document, "QS3DBLTSCAN", error);
            }
        }

        [CommandMethod("QS3DBLTAUDIT", CommandFlags.Modal)]
        public void Audit()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var candidates = BltLegacyCadInspector.ReadCurrentSpace(document)
                    .Select(BltLegacyEntityAdapter.Adapt)
                    .Where(x => x.HasLegacySignal)
                    .ToList();
                WriteSummary(document, "BLT Audit", candidates);
                var blocked = candidates.Where(x => !x.CanImport).Take(20).ToList();
                foreach (var item in blocked)
                {
                    document.Editor.WriteMessage(
                        "\n  - Handle " + item.Snapshot.Handle + " • " +
                        (item.Category.HasValue ? item.Category.Value.ToString() : "Unknown") +
                        " • " + item.Reason);
                }
                if (candidates.Count(x => !x.CanImport) > blocked.Count)
                    document.Editor.WriteMessage("\n  ... xem QS3DBLTPROBE trên các object đại diện để lấy schema chi tiết.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DBLTAUDIT", error);
            }
        }

        [CommandMethod("QS3DBLTIMPORT", CommandFlags.Modal)]
        public void Import()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DBLTIMPORT")) return;
                var candidates = BltLegacyCadInspector.ReadCurrentSpace(document)
                    .Select(BltLegacyEntityAdapter.Adapt)
                    .Where(x => x.HasLegacySignal)
                    .ToList();
                var ready = candidates.Where(x => x.CanImport && x.Category.HasValue).ToList();
                if (ready.Count == 0)
                {
                    WriteSummary(document, "BLT Import", candidates);
                    document.Editor.WriteMessage("\nQS3D BLT Import: chưa có object đủ evidence để import; chạy QS3DBLTPROBE/QS3DBLTAUDIT.");
                    return;
                }

                var imported = 0;
                foreach (var candidate in ready)
                {
                    var category = candidate.Category;
                    if (!category.HasValue) continue;
                    if (!SemanticCaptureService.CaptureSnapshot(document, candidate.Snapshot, category.Value)) continue;
                    ApplyLegacyEvidence(document, candidate);
                    imported++;
                }

                document.Editor.WriteMessage(
                    "\nQS3D BLT Import: " + imported.ToString(CultureInfo.InvariantCulture) +
                    "/" + candidates.Count.ToString(CultureInfo.InvariantCulture) +
                    " legacy object đã được upsert semantic; source Handle giữ nguyên. " +
                    "Chạy QS3DQUANTITYENGINE2 rồi QS3DEXCEL. Object chưa đủ evidence vẫn bị bỏ qua, không fabricate BT/VK.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DBLTIMPORT", error);
            }
        }

        private static void ApplyLegacyEvidence(Document document, BltLegacyElementCandidate candidate)
        {
            var project = ExistingProjectMutationContext.Require(document, "BLT legacy semantic import");
            var element = project.Elements.FirstOrDefault(x =>
                x.SourceHandles.Any(handle => string.Equals(handle, candidate.Snapshot.Handle, StringComparison.OrdinalIgnoreCase)));
            if (element == null)
                throw new InvalidOperationException("BLT semantic import could not resolve the captured source Handle " + candidate.Snapshot.Handle + ".");

            element.SetProperty("CAD.BLT.SourceSystem", "BLT3D");
            element.SetProperty("CAD.BLT.EvidenceMode", candidate.EvidenceMode.ToString());
            element.SetProperty("CAD.BLT.CategoryEvidence", candidate.CategoryEvidence);

            if (candidate.LegacyConcreteM3.HasValue)
            {
                var concrete = candidate.LegacyConcreteM3.Value;
                element.SetQuantity("GrossVolumeM3", concrete);
                element.SetQuantity("NetVolumeM3", concrete);
                element.SetQuantity("MeasuredSolidVolumeM3", concrete);
                element.SetProperty("CAD.BLT.LegacyConcreteM3", concrete.ToString("R", CultureInfo.InvariantCulture));
            }

            if (candidate.LegacyFormworkM2.HasValue)
            {
                var formwork = candidate.LegacyFormworkM2.Value;
                element.SetQuantity("FormworkM2", formwork);
                element.SetProperty("CAD.BLT.LegacyFormworkM2", formwork.ToString("R", CultureInfo.InvariantCulture));
                element.SetProperty("CAD.BLT.FormworkStatus", "ExactLegacyQuantity");
            }
            else
            {
                // Structural defaults are not evidence for an arbitrary legacy/proxy body.
                // Keep VK blank rather than exporting an invented default-family value.
                element.RemoveQuantity("FormworkM2");
                element.SetProperty("CAD.BLT.FormworkStatus", "PENDING_EXACT_EVIDENCE");
            }

            if (!string.IsNullOrWhiteSpace(candidate.ElementNameHint))
                element.SetProperty("Name", candidate.ElementNameHint);
            if (!string.IsNullOrWhiteSpace(candidate.MaterialHint))
                element.SetProperty("Material", candidate.MaterialHint);

            if (!string.IsNullOrWhiteSpace(candidate.FloorHint))
            {
                var floor = project.Floors.FirstOrDefault(x =>
                    string.Equals(x.Id, candidate.FloorHint, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Name, candidate.FloorHint, StringComparison.OrdinalIgnoreCase));
                if (floor != null) element.FloorId = floor.Id;
                else element.SetProperty("CAD.BLT.UnresolvedFloorHint", candidate.FloorHint);
            }

            if (!string.IsNullOrWhiteSpace(candidate.FamilyHint))
            {
                var family = project.Families.FirstOrDefault(x =>
                    x.Category == element.Category &&
                    (string.Equals(x.Id, candidate.FamilyHint, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.Name, candidate.FamilyHint, StringComparison.OrdinalIgnoreCase)));
                if (family != null) element.FamilyId = family.Id;
                else element.SetProperty("CAD.BLT.UnresolvedFamilyHint", candidate.FamilyHint);
            }

            project.Touch();
        }

        private static void WriteSummary(Document document, string operation, IReadOnlyList<BltLegacyElementCandidate> candidates)
        {
            var ready = candidates.Count(x => x.CanImport);
            var unknown = candidates.Count(x => !x.Category.HasValue);
            document.Editor.WriteMessage(
                "\nQS3D " + operation + ": legacy=" + candidates.Count.ToString(CultureInfo.InvariantCulture) +
                " • ready=" + ready.ToString(CultureInfo.InvariantCulture) +
                " • blocked=" + (candidates.Count - ready).ToString(CultureInfo.InvariantCulture) +
                " • unknown-category=" + unknown.ToString(CultureInfo.InvariantCulture) + ".");

            foreach (var group in candidates.Where(x => x.Category.HasValue)
                         .GroupBy(x => x.Category.GetValueOrDefault())
                         .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
                document.Editor.WriteMessage("\n  " + group.Key + ": " + group.Count().ToString(CultureInfo.InvariantCulture));
        }
        private static void Report(Document document, string operation, Exception error)
        {
            try { document.Editor.WriteMessage("\nQS3D " + operation + " lỗi: " + error.GetBaseException().Message); }
            catch { }
        }
    }

    internal static class BltLegacyCadInspector
    {
        private const int MaxScannedEntities = 250000;
        private const int MaxProxyExplodedParts = 4096;
        private const int MaxMetadataValues = 512;
        private const int MaxTypedValues = 256;
        private const int MaxMetadataValueLength = 512;
        private const long MaxRetainedSnapshotBudgetBytes = 64L * 1024L * 1024L;
        private const int EstimatedSnapshotOverheadBytes = 512;
        private const int EstimatedMetadataEntryOverheadBytes = 128;

        public static IReadOnlyList<EntitySnapshot> ReadCurrentSpace(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new List<EntitySnapshot>();
            long retainedSnapshotBytes = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var space = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;
                if (space == null) return result.AsReadOnly();
                var scanned = 0;
                foreach (ObjectId id in space)
                {
                    if (scanned++ >= MaxScannedEntities)
                        throw new InvalidOperationException("BLT legacy scan exceeds guarded Current Space limit of " + MaxScannedEntities + " entities.");
                    TryAdd(transaction, id, result, ref retainedSnapshotBytes);
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        public static IReadOnlyList<EntitySnapshot> ReadSelection(Document document, bool promptIfEmpty)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                if (!promptIfEmpty) return Array.Empty<EntitySnapshot>();
                selection = document.Editor.GetSelection();
            }
            if (selection.Status != PromptStatus.OK || selection.Value == null) return Array.Empty<EntitySnapshot>();
            if (selection.Value.Count > MaxScannedEntities)
                throw new InvalidOperationException("BLT legacy selection exceeds guarded limit of " + MaxScannedEntities + " entities.");

            var result = new List<EntitySnapshot>();
            long retainedSnapshotBytes = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds()) TryAdd(transaction, id, result, ref retainedSnapshotBytes);
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private static void TryAdd(Transaction transaction, ObjectId id, ICollection<EntitySnapshot> result, ref long retainedSnapshotBytes)
        {
            if (id.IsNull || id.IsErased) return;
            EntitySnapshot snapshot;
            try
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null) return;
                snapshot = new EntitySnapshot(entity.Handle.ToString(), entity.GetType().Name, entity.Layer);
                PopulateDirectMetrics(entity, snapshot);
                PopulateRuntimeMetadata(entity, snapshot);
                PopulateXData(entity, snapshot);
                PopulateExtensionDictionary(transaction, entity, snapshot);
                PopulateProxyExplodeMetrics(entity, snapshot);
            }
            catch
            {
                // One malformed/proprietary object must not prevent probing the rest of a legacy drawing.
                return;
            }

            var snapshotBytes = EstimateRetainedSnapshotBytes(snapshot);
            if (snapshotBytes > MaxRetainedSnapshotBudgetBytes ||
                retainedSnapshotBytes > MaxRetainedSnapshotBudgetBytes - snapshotBytes)
                throw new InvalidOperationException(
                    "BLT legacy scan exceeds guarded retained snapshot budget of " +
                    MaxRetainedSnapshotBudgetBytes.ToString(CultureInfo.InvariantCulture) + " bytes.");
            retainedSnapshotBytes += snapshotBytes;
            result.Add(snapshot);
        }

        private static long EstimateRetainedSnapshotBytes(EntitySnapshot snapshot)
        {
            long total = EstimatedSnapshotOverheadBytes;
            total = AddEstimatedBytes(total, Encoding.UTF8.GetByteCount(snapshot.Handle ?? string.Empty));
            total = AddEstimatedBytes(total, Encoding.UTF8.GetByteCount(snapshot.EntityType ?? string.Empty));
            total = AddEstimatedBytes(total, Encoding.UTF8.GetByteCount(snapshot.Layer ?? string.Empty));
            foreach (var pair in snapshot.Metadata)
            {
                total = AddEstimatedBytes(total, EstimatedMetadataEntryOverheadBytes);
                total = AddEstimatedBytes(total, Encoding.UTF8.GetByteCount(pair.Key ?? string.Empty));
                total = AddEstimatedBytes(total, Encoding.UTF8.GetByteCount(pair.Value ?? string.Empty));
            }
            return total;
        }

        private static long AddEstimatedBytes(long current, long addition)
        {
            if (addition < 0 || current > MaxRetainedSnapshotBudgetBytes - addition)
                return MaxRetainedSnapshotBudgetBytes + 1L;
            return current + addition;
        }

        private static void PopulateDirectMetrics(Entity entity, EntitySnapshot snapshot)
        {
            if (entity is Curve curve)
            {
                try
                {
                    var length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                    if (FiniteNonNegative(length)) snapshot.LengthDrawingUnits = length;
                }
                catch { }
            }
            if (entity is Polyline polyline && polyline.Closed)
            {
                try { var area = Math.Abs(polyline.Area); if (FiniteNonNegative(area)) snapshot.AreaDrawingUnitsSquared = area; }
                catch { }
            }
            else if (entity is Circle circle)
            {
                try { var area = Math.PI * circle.Radius * circle.Radius; if (FiniteNonNegative(area)) snapshot.AreaDrawingUnitsSquared = area; }
                catch { }
            }
            else if (entity is Region region)
            {
                try { var area = Math.Abs(region.Area); if (FiniteNonNegative(area)) snapshot.AreaDrawingUnitsSquared = area; }
                catch { }
            }
            else if (entity is Hatch hatch)
            {
                try { var area = Math.Abs(hatch.Area); if (FiniteNonNegative(area)) snapshot.AreaDrawingUnitsSquared = area; }
                catch { }
            }

            if (entity is Solid3d solid)
            {
                try { var area = Math.Abs(solid.Area); if (FiniteNonNegative(area)) snapshot.SurfaceAreaDrawingUnitsSquared = area; }
                catch { }
                try { var volume = Math.Abs(solid.MassProperties.Volume); if (FiniteNonNegative(volume)) snapshot.VolumeDrawingUnitsCubed = volume; }
                catch { }
                snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();
            }
        }

        private static void PopulateRuntimeMetadata(Entity entity, EntitySnapshot snapshot)
        {
            Put(snapshot, "LegacyProbe.RuntimeType", entity.GetType().FullName ?? entity.GetType().Name);
            Put(snapshot, "LegacyProbe.Proxy", entity is ProxyEntity ? "true" : "false");
            if (!(entity is ProxyEntity)) return;

            PutReflectedString(entity, snapshot, "OriginalClassName", "LegacyProbe.ProxyOriginalClass");
            PutReflectedString(entity, snapshot, "OriginalDxfName", "LegacyProbe.ProxyOriginalDxfName");
            PutReflectedString(entity, snapshot, "ApplicationDescription", "LegacyProbe.ProxyApplicationDescription");
            try
            {
                var extents = entity.GeometricExtents;
                Put(snapshot, "LegacyProbe.ExtentsMin", Point(extents.MinPoint.X, extents.MinPoint.Y, extents.MinPoint.Z));
                Put(snapshot, "LegacyProbe.ExtentsMax", Point(extents.MaxPoint.X, extents.MaxPoint.Y, extents.MaxPoint.Z));
            }
            catch { }
        }

        private static void PutReflectedString(object target, EntitySnapshot snapshot, string propertyName, string metadataKey)
        {
            try
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.GetIndexParameters().Length != 0) return;
                var value = property.GetValue(target, null) as string;
                if (string.IsNullOrWhiteSpace(value)) return;
                Put(snapshot, metadataKey, value ?? string.Empty);
            }
            catch { }
        }

        private static void PopulateXData(Entity entity, EntitySnapshot snapshot)
        {
            try
            {
                using (var data = entity.XData)
                {
                    if (data == null) return;
                    PutTypedValues(snapshot, "LegacyProbe.XData", data.AsArray());
                }
            }
            catch { }
        }

        private static void PopulateExtensionDictionary(Transaction transaction, Entity entity, EntitySnapshot snapshot)
        {
            try
            {
                if (entity.ExtensionDictionary.IsNull) return;
                var dictionary = transaction.GetObject(entity.ExtensionDictionary, OpenMode.ForRead, false) as DBDictionary;
                if (dictionary == null) return;
                var index = 0;
                foreach (DBDictionaryEntry entry in dictionary)
                {
                    if (index >= MaxMetadataValues) break;
                    var prefix = "LegacyProbe.Extension." + index.ToString("D3", CultureInfo.InvariantCulture);
                    Put(snapshot, prefix + ".Key", entry.Key);
                    try
                    {
                        var record = transaction.GetObject(entry.Value, OpenMode.ForRead, false) as Xrecord;
                        if (record != null)
                        {
                            using (var data = record.Data)
                            {
                                if (data != null)
                                    PutTypedValues(snapshot, prefix + ".Data", data.AsArray());
                            }
                        }
                    }
                    catch { }
                    index++;
                }
                Put(snapshot, "LegacyProbe.Extension.CountObserved", index.ToString(CultureInfo.InvariantCulture));
            }
            catch { }
        }

        private static void PutTypedValues(EntitySnapshot snapshot, string prefix, TypedValue[] values)
        {
            if (values == null) return;
            var count = Math.Min(values.Length, MaxTypedValues);
            for (var index = 0; index < count; index++)
            {
                if (snapshot.Metadata.Count >= MaxMetadataValues) break;
                var item = values[index];
                var slot = prefix + "." + index.ToString("D3", CultureInfo.InvariantCulture);
                Put(snapshot, slot + ".TypeCode", item.TypeCode.ToString(CultureInfo.InvariantCulture));
                Put(snapshot, slot + ".Value", SafeValue(item.Value));
            }
            Put(snapshot, prefix + ".CountObserved", count.ToString(CultureInfo.InvariantCulture));
        }

        private static void PopulateProxyExplodeMetrics(Entity entity, EntitySnapshot snapshot)
        {
            if (!(entity is ProxyEntity)) return;
            var exploded = new DBObjectCollection();
            try
            {
                entity.Explode(exploded);
                Put(snapshot, "LegacyProbe.ProxyExplodedPartCount", exploded.Count.ToString(CultureInfo.InvariantCulture));
                if (exploded.Count == 0) return;
                if (exploded.Count > MaxProxyExplodedParts)
                {
                    Put(snapshot, "LegacyProbe.ProxyExplodeLimitExceeded", "true");
                    return;
                }

                var allSolids = true;
                var volume = 0d;
                var surface = 0d;
                foreach (DBObject item in exploded)
                {
                    if (!(item is Solid3d solid))
                    {
                        allSolids = false;
                        continue;
                    }
                    try
                    {
                        var partVolume = Math.Abs(solid.MassProperties.Volume);
                        var partSurface = Math.Abs(solid.Area);
                        if (!FiniteNonNegative(partVolume) || !FiniteNonNegative(partSurface))
                        {
                            allSolids = false;
                            continue;
                        }
                        volume = AddFinite(volume, partVolume);
                        surface = AddFinite(surface, partSurface);
                    }
                    catch { allSolids = false; }
                }

                if (!allSolids || volume <= 0d) return;
                snapshot.VolumeDrawingUnitsCubed = volume;
                if (surface > 0d) snapshot.SurfaceAreaDrawingUnitsSquared = surface;
                snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();
                Put(snapshot, "LegacyProbe.ProxyExplodeEvidence", "all-top-level-parts-are-Solid3d");
            }
            catch (Exception error)
            {
                Put(snapshot, "LegacyProbe.ProxyExplodeError", error.GetType().Name);
            }
            finally
            {
                foreach (DBObject item in exploded) item.Dispose();
            }
        }

        private static double AddFinite(double left, double right)
        {
            var value = left + right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException("Legacy proxy metric total is not finite.");
            return value;
        }

        private static bool FiniteNonNegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;

        private static string Point(double x, double y, double z) =>
            x.ToString("R", CultureInfo.InvariantCulture) + "," +
            y.ToString("R", CultureInfo.InvariantCulture) + "," +
            z.ToString("R", CultureInfo.InvariantCulture);

        private static string SafeValue(object value)
        {
            if (value == null) return string.Empty;
            try { return Bound(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty); }
            catch { return "<" + value.GetType().Name + ">"; }
        }

        private static void Put(EntitySnapshot snapshot, string key, string value)
        {
            if (snapshot.Metadata.Count >= MaxMetadataValues && !snapshot.Metadata.ContainsKey(key)) return;
            snapshot.Metadata[key] = Bound(value ?? string.Empty);
        }

        private static string Bound(string value) => value.Length <= MaxMetadataValueLength ? value : value.Substring(0, MaxMetadataValueLength);
    }

    internal static class BltLegacyProbeReport
    {
        private const long MaxProbeReportBytes = 64L * 1024L * 1024L;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public static string Write(IReadOnlyList<BltLegacyElementCandidate> candidates)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            var directory = Path.Combine(Path.GetTempPath(), "QS3D-BLT-Probe");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "qs3d-blt-probe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + ".json");
            var tempPath = path + ".partial-" + Guid.NewGuid().ToString("N");
            var published = false;
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.SequentialScan))
                using (var writer = new StreamWriter(stream, Utf8NoBom, 65536))
                {
                    long bytesWritten = 0;
                    WriteJson(writer, candidates, ref bytesWritten);
                    writer.Flush();
                    stream.Flush();
                }

                File.Move(tempPath, path);
                published = true;
                return path;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (!published && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { }
                }
            }
        }

        private static void WriteJson(StreamWriter writer, IReadOnlyList<BltLegacyElementCandidate> candidates, ref long bytesWritten)
        {
            WritePart(writer, "{\n  \"schema\": \"QS3D_BLT_LEGACY_PROBE_V1\",\n  \"objects\": [\n", ref bytesWritten);
            for (var index = 0; index < candidates.Count; index++)
            {
                if (index > 0) WritePart(writer, ",\n", ref bytesWritten);
                var item = candidates[index];
                WritePart(writer, "    {\n", ref bytesWritten);
                Property(writer, "handle", item.Snapshot.Handle, true, 6, ref bytesWritten);
                Property(writer, "entityType", item.Snapshot.EntityType, true, 6, ref bytesWritten);
                Property(writer, "layer", item.Snapshot.Layer, true, 6, ref bytesWritten);
                Property(writer, "legacySignal", item.HasLegacySignal ? "true" : "false", true, 6, ref bytesWritten, true);
                Property(writer, "category", item.Category.HasValue ? item.Category.Value.ToString() : string.Empty, true, 6, ref bytesWritten);
                Property(writer, "categoryEvidence", item.CategoryEvidence, true, 6, ref bytesWritten);
                Property(writer, "evidenceMode", item.EvidenceMode.ToString(), true, 6, ref bytesWritten);
                Property(writer, "canImport", item.CanImport ? "true" : "false", true, 6, ref bytesWritten, true);
                NumberProperty(writer, "lengthDrawingUnits", item.Snapshot.LengthDrawingUnits, true, 6, ref bytesWritten);
                NumberProperty(writer, "areaDrawingUnitsSquared", item.Snapshot.AreaDrawingUnitsSquared, true, 6, ref bytesWritten);
                NumberProperty(writer, "surfaceAreaDrawingUnitsSquared", item.Snapshot.SurfaceAreaDrawingUnitsSquared, true, 6, ref bytesWritten);
                NumberProperty(writer, "volumeDrawingUnitsCubed", item.Snapshot.VolumeDrawingUnitsCubed, true, 6, ref bytesWritten);
                NumberProperty(writer, "legacyConcreteM3", item.LegacyConcreteM3, true, 6, ref bytesWritten);
                NumberProperty(writer, "legacyFormworkM2", item.LegacyFormworkM2, true, 6, ref bytesWritten);
                Property(writer, "reason", item.Reason, true, 6, ref bytesWritten);
                WritePart(writer, "      \"metadata\": {\n", ref bytesWritten);
                var metadata = item.Snapshot.Metadata.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
                for (var metadataIndex = 0; metadataIndex < metadata.Count; metadataIndex++)
                {
                    var pair = metadata[metadataIndex];
                    WritePart(writer, "        \"" + Escape(pair.Key) + "\": \"" + Escape(pair.Value) + "\"", ref bytesWritten);
                    if (metadataIndex + 1 < metadata.Count) WritePart(writer, ",", ref bytesWritten);
                    WritePart(writer, "\n", ref bytesWritten);
                }
                WritePart(writer, "      }\n    }", ref bytesWritten);
            }
            WritePart(writer, "\n  ]\n}\n", ref bytesWritten);
        }

        private static void Property(StreamWriter writer, string name, string value, bool comma, int indent, ref long bytesWritten, bool raw = false)
        {
            var text = new string(' ', indent) + "\"" + name + "\": " +
                (raw ? value : "\"" + Escape(value ?? string.Empty) + "\"") +
                (comma ? "," : string.Empty) + "\n";
            WritePart(writer, text, ref bytesWritten);
        }

        private static void NumberProperty(StreamWriter writer, string name, double? value, bool comma, int indent, ref long bytesWritten)
        {
            var text = new string(' ', indent) + "\"" + name + "\": " +
                (value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : "null") +
                (comma ? "," : string.Empty) + "\n";
            WritePart(writer, text, ref bytesWritten);
        }

        private static void WritePart(StreamWriter writer, string value, ref long bytesWritten)
        {
            var byteCount = Utf8NoBom.GetByteCount(value ?? string.Empty);
            if (byteCount > MaxProbeReportBytes || bytesWritten > MaxProbeReportBytes - byteCount)
                throw new InvalidOperationException("BLT legacy probe report exceeds guarded output limit of " + MaxProbeReportBytes.ToString(CultureInfo.InvariantCulture) + " bytes.");
            writer.Write(value);
            bytesWritten += byteCount;
        }

        private static string Escape(string value)
        {
            var builder = new StringBuilder(value.Length + 16);
            foreach (var character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (char.IsControl(character)) builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}