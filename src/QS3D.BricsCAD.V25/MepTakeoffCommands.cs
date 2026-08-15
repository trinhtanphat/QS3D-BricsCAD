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
    /// Read-only MEP takeoff and coordination adapter.
    ///
    /// Native selection and metrics stay on the document thread, classification is delegated to the
    /// host-neutral configurable Core recognition profile, and quantity/clash math is delegated to
    /// QS3D.Core. Unknown or ambiguous recognition results fail closed instead of being guessed.
    /// </summary>
    public sealed class MepTakeoffCommands
    {
        private const string DefaultRegion = "DRAWING";
        private const int MaxLocateReviewPairs = 200;
        private static readonly MepRecognitionProfile RecognitionProfile = MepRecognitionProfiles.CreateDefault();

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
                        "\nQS3DMEPTAKEOFF: không có entity nào được profile nhận diện rõ là MEP; unknown/ambiguous đều bị bỏ qua an toàn.");
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

                if (!TryPromptClearance(document, out var clearanceDrawingUnits)) return;
                var units = CadUnitService.GetPolicy(document);
                var clearanceM = units.ToMeters(clearanceDrawingUnits);
                var relevant = DetectRelevantClashes(document, snapshots, units, clearanceM, out var candidateCount, out var skipped);
                if (candidateCount < 2)
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPCLASH: cần ít nhất hai entity có classification rõ và geometric extents hợp lệ; skipped=" + skipped + ".");
                    return;
                }

                document.Editor.WriteMessage(
                    "\nQS3DMEPCLASH: candidates=" + candidateCount +
                    " • clashes=" + relevant.Count +
                    " • skipped=" + skipped +
                    " • clearance=" + clearanceM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");

                for (var i = 0; i < relevant.Count; i++)
                    WriteClashRow(document, relevant[i], null);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPCLASH lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DMEPCLASHLOCATE", CommandFlags.UsePickSet)]
        public void MepClashLocate()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count < 2)
                {
                    document.Editor.WriteMessage("\nQS3DMEPCLASHLOCATE: chọn tập entity cần review clash trước.");
                    return;
                }

                if (!TryPromptClearance(document, out var clearanceDrawingUnits)) return;
                var units = CadUnitService.GetPolicy(document);
                var clearanceM = units.ToMeters(clearanceDrawingUnits);
                var relevant = DetectRelevantClashes(document, snapshots, units, clearanceM, out var candidateCount, out var skipped);
                if (candidateCount < 2 || relevant.Count == 0)
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPCLASHLOCATE: không có clash MEP phù hợp để locate; candidates=" + candidateCount + " • skipped=" + skipped + ".");
                    return;
                }

                var reviewCount = Math.Min(relevant.Count, MaxLocateReviewPairs);
                document.Editor.WriteMessage(
                    "\nQS3DMEPCLASHLOCATE: clashes=" + relevant.Count +
                    " • hiển thị=" + reviewCount +
                    (relevant.Count > reviewCount ? " (hãy thu hẹp selection để review phần còn lại)." : "."));
                for (var i = 0; i < reviewCount; i++)
                    WriteClashRow(document, relevant[i], i + 1);

                var prompt = new PromptIntegerOptions(
                    "\nChọn số clash cần Locate [1-" + reviewCount.ToString(CultureInfo.InvariantCulture) + "]: ")
                {
                    AllowNegative = false,
                    AllowZero = false,
                    AllowNone = false,
                    LowerLimit = 1,
                    UpperLimit = reviewCount
                };
                var selected = document.Editor.GetInteger(prompt);
                if (selected.Status != PromptStatus.OK) return;

                var clash = relevant[selected.Value - 1];
                var selectedCount = CadHandleService.SelectIfAny(document, new[] { clash.LeftElementId, clash.RightElementId });
                document.Editor.WriteMessage(
                    "\nQS3DMEPCLASHLOCATE: selected=" + selectedCount + "/2 • " +
                    clash.LeftElementId + " ↔ " + clash.RightElementId +
                    (selectedCount == 2 ? "." : " • một hoặc nhiều Handle không còn live."));
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPCLASHLOCATE lỗi: " + ex.Message);
            }
        }

        private static bool TryPromptClearance(Document document, out double clearanceDrawingUnits)
        {
            var prompt = new PromptDistanceOptions("\nQS3D MEP Clash - nhập clearance kiểm tra (drawing units; 0 = hard clash only): ")
            {
                AllowNegative = false,
                AllowZero = true,
                AllowNone = false
            };
            var result = document.Editor.GetDistance(prompt);
            if (result.Status != PromptStatus.OK)
            {
                clearanceDrawingUnits = 0d;
                return false;
            }
            clearanceDrawingUnits = result.Value;
            return true;
        }

        private static IReadOnlyList<ClashResult> DetectRelevantClashes(
            Document document,
            IReadOnlyList<EntitySnapshot> snapshots,
            ProjectUnitPolicy units,
            double clearanceM,
            out int candidateCount,
            out int skipped)
        {
            var selectedByHandle = BuildSnapshotIndex(snapshots);
            var coordination = ReadCoordinationElements(document, selectedByHandle, units, out skipped);
            candidateCount = coordination.Count;
            if (coordination.Count < 2) return Array.Empty<ClashResult>();

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
            return new ReadOnlyCollection<ClashResult>(relevant.ToArray());
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
            var recognition = Recognize(snapshot);
            if (recognition.Status != MepRecognitionStatus.Matched ||
                recognition.Discipline != MepRecognitionDiscipline.Mep ||
                !recognition.MepKind.HasValue)
            {
                element = null!;
                return false;
            }

            var kind = recognition.MepKind.Value;
            var lengthM = snapshot.LengthDrawingUnits.HasValue
                ? units.ToMeters(snapshot.LengthDrawingUnits.Value)
                : 0d;
            var areaSource = snapshot.SurfaceAreaDrawingUnitsSquared ?? snapshot.AreaDrawingUnitsSquared;
            var areaM2 = areaSource.HasValue ? units.AreaToSquareMeters(areaSource.Value) : 0d;
            var volumeM3 = snapshot.VolumeDrawingUnitsCubed.HasValue
                ? units.VolumeToCubicMeters(snapshot.VolumeDrawingUnitsCubed.Value)
                : 0d;
            var system = CanonicalOrFallback(snapshot.Layer, recognition.Category ?? kind.ToString());
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
            var recognition = Recognize(snapshot);
            if (recognition.Status != MepRecognitionStatus.Matched ||
                !recognition.Discipline.HasValue ||
                string.IsNullOrWhiteSpace(recognition.Category))
            {
                discipline = string.Empty;
                category = string.Empty;
                system = string.Empty;
                return false;
            }

            discipline = DisciplineText(recognition.Discipline.Value);
            category = recognition.Category!;
            system = CanonicalOrFallback(snapshot.Layer, category);
            return true;
        }

        private static MepRecognitionResult Recognize(EntitySnapshot snapshot)
        {
            snapshot.Metadata.TryGetValue("BlockName", out var blockName);
            return RecognitionProfile.Recognize(snapshot.Layer, blockName);
        }

        private static string SnapshotSpecification(EntitySnapshot snapshot)
        {
            if (snapshot.Metadata.TryGetValue("BlockName", out var blockName) && !string.IsNullOrWhiteSpace(blockName))
                return blockName.Trim();
            return CanonicalOrFallback(snapshot.EntityType, "Entity");
        }

        private static string DisciplineText(MepRecognitionDiscipline discipline)
        {
            switch (discipline)
            {
                case MepRecognitionDiscipline.Mep: return "MEP";
                case MepRecognitionDiscipline.Structure: return "STRUCTURE";
                case MepRecognitionDiscipline.Architecture: return "ARCHITECTURE";
                default: throw new ArgumentOutOfRangeException(nameof(discipline));
            }
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

        private static void WriteClashRow(Document document, ClashResult clash, int? index)
        {
            document.Editor.WriteMessage(
                "\n  " + (index.HasValue ? index.Value.ToString(CultureInfo.InvariantCulture) + ". " : string.Empty) +
                clash.Kind + " • " + clash.LeftElementId + " ↔ " + clash.RightElementId +
                " • gap=" + clash.SeparationM.ToString("0.###", CultureInfo.InvariantCulture) + " m" +
                " • overlap=" + clash.OverlapXM.ToString("0.###", CultureInfo.InvariantCulture) + "×" +
                clash.OverlapYM.ToString("0.###", CultureInfo.InvariantCulture) + "×" +
                clash.OverlapZM.ToString("0.###", CultureInfo.InvariantCulture) + " m");
        }
    }
}
