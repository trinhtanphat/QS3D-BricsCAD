using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Mep;
using QS3D.Core.Model;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Read-only exact hard-clash narrow phase for selected native Solid3d entities.
    /// Classification is delegated to the Core MEP recognition profile. Native DBObjects never leave
    /// the document thread/transaction and the command never clones, modifies, appends or erases CAD.
    /// </summary>
    public sealed class MepExactClashCommands
    {
        private const int MaxRecognizedSolids = 500;
        private const int MaxBroadPhasePairs = 100000;
        private static readonly MepRecognitionProfile RecognitionProfile = MepRecognitionProfiles.CreateDefault();

        [CommandMethod("QS3DMEPEXACTCLASH", CommandFlags.UsePickSet)]
        public void ExactMepClash()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count < 2)
                {
                    document.Editor.WriteMessage("\nQS3DMEPEXACTCLASH: chọn ít nhất hai Solid3d MEP/Structure/Architecture cần kiểm tra.");
                    return;
                }

                var snapshotByHandle = BuildSnapshotIndex(snapshots);
                var ids = CadHandleService.Resolve(document, snapshotByHandle.Keys);
                var clashes = DetectExact(document, ids, snapshotByHandle, out var recognizedSolids, out var skipped, out var broadPhasePairs);

                document.Editor.WriteMessage(
                    "\nQS3DMEPEXACTCLASH: solids=" + recognizedSolids +
                    " • broad-phase=" + broadPhasePairs +
                    " • exact-clashes=" + clashes.Count +
                    " • skipped=" + skipped + ".");

                for (var i = 0; i < clashes.Count; i++)
                {
                    var clash = clashes[i];
                    document.Editor.WriteMessage("\n  ExactHard • " + clash.LeftHandle + " ↔ " + clash.RightHandle);
                }
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPEXACTCLASH lỗi: " + ex.Message);
            }
        }

        private static IReadOnlyList<ExactClashPair> DetectExact(
            Document document,
            IReadOnlyList<ObjectId> ids,
            IReadOnlyDictionary<string, EntitySnapshot> snapshotByHandle,
            out int recognizedSolids,
            out int skipped,
            out int broadPhasePairs)
        {
            var results = new List<ExactClashPair>();
            var candidates = new List<SolidCandidate>();
            skipped = 0;
            broadPhasePairs = 0;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                for (var i = 0; i < ids.Count; i++)
                {
                    try
                    {
                        var solid = transaction.GetObject(ids[i], OpenMode.ForRead, false) as Solid3d;
                        if (solid == null || solid.IsErased)
                        {
                            skipped++;
                            continue;
                        }

                        var handle = solid.Handle.ToString();
                        if (!snapshotByHandle.TryGetValue(handle, out var snapshot) ||
                            !TryRecognize(snapshot, out var discipline))
                        {
                            skipped++;
                            continue;
                        }

                        var extents = solid.GeometricExtents;
                        if (!HasFiniteExtents(extents))
                        {
                            skipped++;
                            continue;
                        }

                        candidates.Add(new SolidCandidate(handle, discipline, solid, extents));
                    }
                    catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
                    {
                        skipped++;
                    }

                    if (candidates.Count > MaxRecognizedSolids)
                        throw new InvalidOperationException(
                            "QS3DMEPEXACTCLASH giới hạn " + MaxRecognizedSolids +
                            " Solid3d đã nhận diện mỗi lần; hãy thu hẹp selection.");
                }

                candidates.Sort(CompareCandidates);
                recognizedSolids = candidates.Count;
                for (var i = 0; i < candidates.Count; i++)
                {
                    for (var j = i + 1; j < candidates.Count; j++)
                    {
                        var left = candidates[i];
                        var right = candidates[j];
                        if (left.Discipline != MepRecognitionDiscipline.Mep &&
                            right.Discipline != MepRecognitionDiscipline.Mep)
                            continue;
                        if (!ExtentsMayIntersect(left.Extents, right.Extents)) continue;

                        broadPhasePairs++;
                        if (broadPhasePairs > MaxBroadPhasePairs)
                            throw new InvalidOperationException(
                                "QS3DMEPEXACTCLASH vượt " + MaxBroadPhasePairs +
                                " broad-phase pair; hãy thu hẹp selection.");

                        try
                        {
                            if (left.Solid.CheckInterference(right.Solid))
                                results.Add(new ExactClashPair(left.Handle, right.Handle));
                        }
                        catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
                        {
                            skipped++;
                        }
                    }
                }
                transaction.Commit();
            }

            return new ReadOnlyCollection<ExactClashPair>(results.ToArray());
        }

        private static bool TryRecognize(EntitySnapshot snapshot, out MepRecognitionDiscipline discipline)
        {
            snapshot.Metadata.TryGetValue("BlockName", out var blockName);
            var recognition = RecognitionProfile.Recognize(snapshot.Layer, blockName);
            if (recognition.Status != MepRecognitionStatus.Matched || !recognition.Discipline.HasValue)
            {
                discipline = default(MepRecognitionDiscipline);
                return false;
            }
            discipline = recognition.Discipline.Value;
            return true;
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

        private static bool HasFiniteExtents(Extents3d extents) =>
            IsFinite(extents.MinPoint.X) && IsFinite(extents.MinPoint.Y) && IsFinite(extents.MinPoint.Z) &&
            IsFinite(extents.MaxPoint.X) && IsFinite(extents.MaxPoint.Y) && IsFinite(extents.MaxPoint.Z) &&
            extents.MaxPoint.X >= extents.MinPoint.X &&
            extents.MaxPoint.Y >= extents.MinPoint.Y &&
            extents.MaxPoint.Z >= extents.MinPoint.Z;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool ExtentsMayIntersect(Extents3d left, Extents3d right) =>
            left.MaxPoint.X >= right.MinPoint.X && right.MaxPoint.X >= left.MinPoint.X &&
            left.MaxPoint.Y >= right.MinPoint.Y && right.MaxPoint.Y >= left.MinPoint.Y &&
            left.MaxPoint.Z >= right.MinPoint.Z && right.MaxPoint.Z >= left.MinPoint.Z;

        private static int CompareCandidates(SolidCandidate left, SolidCandidate right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Handle, right.Handle);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Handle, right.Handle);
        }

        private static bool IsRecoverableEntityFailure(System.Exception exception) =>
            !(exception is OutOfMemoryException) &&
            !(exception is StackOverflowException) &&
            !(exception is AccessViolationException);

        private sealed class SolidCandidate
        {
            internal SolidCandidate(
                string handle,
                MepRecognitionDiscipline discipline,
                Solid3d solid,
                Extents3d extents)
            {
                Handle = handle;
                Discipline = discipline;
                Solid = solid;
                Extents = extents;
            }

            internal string Handle { get; }
            internal MepRecognitionDiscipline Discipline { get; }
            internal Solid3d Solid { get; }
            internal Extents3d Extents { get; }
        }

        private sealed class ExactClashPair
        {
            internal ExactClashPair(string leftHandle, string rightHandle)
            {
                LeftHandle = leftHandle;
                RightHandle = rightHandle;
            }

            internal string LeftHandle { get; }
            internal string RightHandle { get; }
        }
    }
}
