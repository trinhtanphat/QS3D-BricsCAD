using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Coordination;
using QS3D.Core.Mep;
using QS3D.Core.Model;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Read-only first-wave MEP takeoff and coordination adapter.
    ///
    /// It deliberately does not create a QS3D project, persist semantic elements, mutate the DWG,
    /// infer length from bounding boxes, or guess unclassified layers/blocks. Native selection and
    /// metrics are captured on the document thread, converted to meters through the canonical CAD
    /// unit policy, and then passed to host-neutral QS3D.Core services.
    /// </summary>
    public sealed class MepTakeoffCommands
    {
        private const string DefaultRegion = "DRAWING";

        [CommandMethod("QS3DMEPTAKEOFF", CommandFlags.UsePickSet)]
        public void MepTakeoff()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DMEPTAKEOFF: chọn các entity MEP cần bóc khối lượng.");
                    return;
                }

                var units = CadUnitService.GetPolicy(document);
                var captured = new List<MepElement>();
                var skipped = 0;
                foreach (var snapshot in snapshots)
                {
                    if (!TryCreateMepElement(snapshot, units, out var element))
                    {
                        skipped++;
                        continue;
                    }
                    captured.Add(element);
                }

                if (captured.Count == 0)
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPTAKEOFF: không có entity nào được phân loại MEP. " +
                        "Dùng layer/block name có token rõ như DUCT, PIPE, CABLETRAY, CONDUIT, CABLE, EQUIP, FIXTURE, FITTING, VALVE hoặc DAMPER.");
                    return;
                }

                var rows = new MepQuantityService().Aggregate(captured);
                document.Editor.WriteMessage(
                    "\nQS3DMEPTAKEOFF: recognized=" + captured.Count +
                    " • groups=" + rows.Count +
                    " • skipped=" + skipped +
                    " • units=" + CadUnitService.Describe(document) + ".");

                for (var i = 0; i < rows.Count; i++)
                    WriteTakeoffRow(document, rows[i]);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPTAKEOFF lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DMEPCLASH", CommandFlags.UsePickSet)]
        public void MepClash()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count < 2)
                {
                    document.Editor.WriteMessage("\nQS3DMEPCLASH: chọn ít nhất hai entity MEP/Structure/Architecture cần kiểm tra.");
                    return;
                }

                var clearancePrompt = new PromptDistanceOptions("\nQS3D MEP Clash - nhập clearance kiểm tra (drawing units; 0 = hard clash only): ")
                {
                    AllowNegative = false,
                    AllowZero = true,
                    AllowNone = false
                };
                var clearanceResult = document.Editor.GetDistance(clearancePrompt);
                if (clearanceResult.Status != PromptStatus.OK) return;

                var units = CadUnitService.GetPolicy(document);
                var clearanceM = units.ToMeters(clearanceResult.Value);
                var selectedByHandle = BuildSnapshotIndex(snapshots);
                var coordination = ReadCoordinationElements(document, selectedByHandle, units, out var skipped);
                if (coordination.Count < 2)
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPCLASH: cần ít nhất hai entity có classification rõ và geometric extents hợp lệ; skipped=" + skipped + ".");
                    return;
                }

                var disciplineById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < coordination.Count; i++)
                    disciplineById[coordination[i].ElementId] = coordination[i].Discipline;

                var detected = new ClashDetectionService().Detect(coordination, clearanceM, includeSameDiscipline: true);
                var relevant = new List<ClashResult>();
                for (var i = 0; i < detected.Count; i++)
                {
                    var clash = detected[i];
                    if (IsMep(disciplineById, clash.LeftElementId) || IsMep(disciplineById, clash.RightElementId))
                        relevant.Add(clash);
                }

                document.Editor.WriteMessage(
                    "\nQS3DMEPCLASH: candidates=" + coordination.Count +
                    " • clashes=" + relevant.Count +
                    " • skipped=" + skipped +
                    " • clearance=" + clearanceM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");

                for (var i = 0; i < relevant.Count; i++)
                {
                    var clash = relevant[i];
                    document.Editor.WriteMessage(
                        "\n  " + clash.Kind + " • " + clash.LeftElementId + " ↔ " + clash.RightElementId +
                        " • gap=" + clash.SeparationM.ToString("0.###", CultureInfo.InvariantCulture) + " m" +
                        " • overlap=" + clash.OverlapXM.ToString("0.###", CultureInfo.InvariantCulture) + "×" +
                        clash.OverlapYM.ToString("0.###", CultureInfo.InvariantCulture) + "×" +
                        clash.OverlapZM.ToString("0.###", CultureInfo.InvariantCulture) + " m");
                }
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPCLASH lỗi: " + ex.Message);
            }
        }

        private static IReadOnlyList<CoordinationElement> ReadCoordinationElements(
            Document document,
            IReadOnlyDictionary<string, EntitySnapshot> selectedByHandle,
            ProjectUnitPolicy units,
            out int skipped)
        {
            var result = new List<CoordinationElement>();
            skipped = 0;
            var ids = CadHandleService.Resolve(document, selectedByHandle.Keys);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity == null || entity.IsErased)
                        {
                            skipped++;
                            continue;
                        }

                        var handle = entity.Handle.ToString();
                        if (!selectedByHandle.TryGetValue(handle, out var snapshot) ||
                            !TryClassifyCoordination(snapshot, out var discipline, out var category, out var system))
                        {
                            skipped++;
                            continue;
                        }

                        var extents = entity.GeometricExtents;
                        var bounds = new AxisAlignedBox(
                            units.ToMeters(extents.MinPoint.X),
                            units.ToMeters(extents.MinPoint.Y),
                            units.ToMeters(extents.MinPoint.Z),
                            units.ToMeters(extents.MaxPoint.X),
                            units.ToMeters(extents.MaxPoint.Y),
                            units.ToMeters(extents.MaxPoint.Z));
                        result.Add(new CoordinationElement(handle, discipline, category, system, DefaultRegion, bounds));
                    }
                    catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
                    {
                        skipped++;
                    }
                }
                transaction.Commit();
            }

            result.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId));
            return new ReadOnlyCollection<CoordinationElement>(result.ToArray());
        }

        private static IReadOnlyDictionary<string, EntitySnapshot> BuildSnapshotIndex(IReadOnlyList<EntitySnapshot> snapshots)
        {
            var result = new Dictionary<string, EntitySnapshot>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (!result.ContainsKey(snapshot.Handle)) result.Add(snapshot.Handle, snapshot);
            }
            return new ReadOnlyDictionary<string, EntitySnapshot>(result);
        }

        private static bool TryCreateMepElement(EntitySnapshot snapshot, ProjectUnitPolicy units, out MepElement element)
        {
            if (!TryClassifyMep(snapshot, out var kind))
            {
                element = null!;
                return false;
            }

            var lengthM = snapshot.LengthDrawingUnits.HasValue
                ? units.ToMeters(snapshot.LengthDrawingUnits.Value)
                : 0d;
            var areaSource = snapshot.SurfaceAreaDrawingUnitsSquared ?? snapshot.AreaDrawingUnitsSquared;
            var areaM2 = areaSource.HasValue ? units.AreaToSquareMeters(areaSource.Value) : 0d;
            var volumeM3 = snapshot.VolumeDrawingUnitsCubed.HasValue
                ? units.VolumeToCubicMeters(snapshot.VolumeDrawingUnitsCubed.Value)
                : 0d;
            var system = CanonicalOrFallback(snapshot.Layer, kind.ToString());
            var specification = SnapshotSpecification(snapshot);
            element = new MepElement(snapshot.Handle, kind, system, specification, DefaultRegion, 1, lengthM, areaM2, volumeM3);
            return true;
        }

        private static bool TryClassifyCoordination(
            EntitySnapshot snapshot,
            out string discipline,
            out string category,
            out string system)
        {
            if (TryClassifyMep(snapshot, out var kind))
            {
                discipline = "MEP";
                category = kind.ToString();
                system = CanonicalOrFallback(snapshot.Layer, kind.ToString());
                return true;
            }

            var text = ClassificationText(snapshot);
            if (ContainsAny(text, "STRUCT", "BEAM", "COLUMN", "FOOTING", "FOUNDATION", "PILE", "RC_", "RC-"))
            {
                discipline = "STRUCTURE";
                category = StructuralCategory(text);
                system = CanonicalOrFallback(snapshot.Layer, category);
                return true;
            }
            if (ContainsAny(text, "ARCH", "WALL", "SLAB", "CEILING", "FLOOR", "ROOF"))
            {
                discipline = "ARCHITECTURE";
                category = ArchitecturalCategory(text);
                system = CanonicalOrFallback(snapshot.Layer, category);
                return true;
            }

            discipline = string.Empty;
            category = string.Empty;
            system = string.Empty;
            return false;
        }

        private static bool TryClassifyMep(EntitySnapshot snapshot, out MepElementKind kind)
        {
            var text = ClassificationText(snapshot);
            if (ContainsAny(text, "CABLETRAY", "CABLE_TRAY", "CABLE-TRAY", "TRAY")) { kind = MepElementKind.CableTray; return true; }
            if (ContainsAny(text, "CONDUIT")) { kind = MepElementKind.Conduit; return true; }
            if (ContainsAny(text, "DUCT")) { kind = MepElementKind.Duct; return true; }
            if (ContainsAny(text, "PIPE", "PIPING")) { kind = MepElementKind.Pipe; return true; }
            if (ContainsAny(text, "CABLE", "WIRE")) { kind = MepElementKind.Cable; return true; }
            if (ContainsAny(text, "FITTING", "ELBOW", "REDUCER", "COUPLING", "TEE_", "TEE-")) { kind = MepElementKind.Fitting; return true; }
            if (ContainsAny(text, "VALVE", "DAMPER", "ACCESSORY")) { kind = MepElementKind.Accessory; return true; }
            if (ContainsAny(text, "EQUIP", "AHU", "FCU", "PUMP", "FAN", "CHILLER", "BOILER")) { kind = MepElementKind.Equipment; return true; }
            if (ContainsAny(text, "FIXTURE", "LUMINAIRE", "LIGHTING", "LIGHT_", "LIGHT-", "SOCKET", "OUTLET", "SWITCH", "SANITARY", "SPRINKLER")) { kind = MepElementKind.Fixture; return true; }
            kind = default(MepElementKind);
            return false;
        }

        private static string ClassificationText(EntitySnapshot snapshot)
        {
            snapshot.Metadata.TryGetValue("BlockName", out var blockName);
            return (snapshot.Layer + "|" + (blockName ?? string.Empty)).ToUpperInvariant();
        }

        private static string SnapshotSpecification(EntitySnapshot snapshot)
        {
            if (snapshot.Metadata.TryGetValue("BlockName", out var blockName) && !string.IsNullOrWhiteSpace(blockName))
                return blockName.Trim();
            return CanonicalOrFallback(snapshot.EntityType, "Entity");
        }

        private static string StructuralCategory(string text)
        {
            if (ContainsAny(text, "BEAM")) return "Beam";
            if (ContainsAny(text, "COLUMN")) return "Column";
            if (ContainsAny(text, "FOOTING", "FOUNDATION", "PILE")) return "Foundation";
            return "Structure";
        }

        private static string ArchitecturalCategory(string text)
        {
            if (ContainsAny(text, "WALL")) return "Wall";
            if (ContainsAny(text, "SLAB", "FLOOR")) return "Slab";
            if (ContainsAny(text, "CEILING")) return "Ceiling";
            if (ContainsAny(text, "ROOF")) return "Roof";
            return "Architecture";
        }

        private static bool ContainsAny(string source, params string[] tokens)
        {
            for (var i = 0; i < tokens.Length; i++)
                if (source.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string CanonicalOrFallback(string? value, string fallback)
        {
            var text = (value ?? string.Empty).Trim();
            return text.Length == 0 ? fallback : text;
        }

        private static bool IsMep(IReadOnlyDictionary<string, string> disciplineById, string elementId) =>
            disciplineById.TryGetValue(elementId, out var discipline) &&
            StringComparer.OrdinalIgnoreCase.Equals(discipline, "MEP");

        private static bool IsRecoverableEntityFailure(System.Exception exception) =>
            !(exception is OutOfMemoryException) &&
            !(exception is StackOverflowException) &&
            !(exception is AccessViolationException);

        private static void WriteTakeoffRow(Document document, MepQuantityGroup row)
        {
            document.Editor.WriteMessage(
                "\n  " + row.Region + " • " + row.System + " • " + row.Specification + " • " + row.Kind +
                " • entities=" + row.ElementCount +
                " • count=" + row.QuantityCount +
                " • L=" + row.LengthM.ToString("0.###", CultureInfo.InvariantCulture) + " m" +
                " • A=" + row.AreaM2.ToString("0.###", CultureInfo.InvariantCulture) + " m²" +
                " • V=" + row.VolumeM3.ToString("0.###", CultureInfo.InvariantCulture) + " m³");
        }
    }
}
