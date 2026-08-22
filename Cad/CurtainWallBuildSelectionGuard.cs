using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class CurtainWallBuildSelection
    {
        internal CurtainWallBuildSelection(
            IEnumerable<ObjectId> lineSourceIds,
            IEnumerable<ObjectId> pathSourceIds,
            IEnumerable<ObjectId> allSourceIds)
        {
            LineSourceIds = new List<ObjectId>(lineSourceIds).AsReadOnly();
            PathSourceIds = new List<ObjectId>(pathSourceIds).AsReadOnly();
            AllSourceIds = new List<ObjectId>(allSourceIds).AsReadOnly();
        }

        internal IReadOnlyList<ObjectId> LineSourceIds { get; }
        internal IReadOnlyList<ObjectId> PathSourceIds { get; }
        internal IReadOnlyList<ObjectId> AllSourceIds { get; }
    }

    internal static class CurtainWallBuildSelectionGuard
    {
        public static CurtainWallBuildSelection Validate(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
                return new CurtainWallBuildSelection(Array.Empty<ObjectId>(), Array.Empty<ObjectId>(), Array.Empty<ObjectId>());
            var selectedOwners = new Dictionary<string, ObjectId>(StringComparer.OrdinalIgnoreCase);
            var lineSourceIds = new List<ObjectId>();
            var pathSourceIds = new List<ObjectId>();
            var allSourceIds = new List<ObjectId>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline))) continue;
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.GlassWall && x.SourceHandles.Any(h => SameHandle(h, source.Handle.ToString())))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("Curtain source " + source.Handle + " belongs to multiple GlassWall elements.");
                    var element = matches[0];
                    if (selectedOwners.ContainsKey(element.Id))
                        throw new InvalidOperationException("GlassWall " + element.Id + " has more than one selected LINE/POLYLINE source. Curtain 3D requires exactly one canonical source.");
                    selectedOwners.Add(element.Id, id);
                    if (source.OwnerId != modelSpaceId)
                        throw new InvalidOperationException("GlassWall " + element.Id + " source must be a Model Space LINE/POLYLINE.");

                    var canonicalMetadata = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var raw in element.SourceHandles)
                    {
                        var canonical = CadHandleService.NormalizeHexHandle(raw);
                        if (canonical == null) throw new InvalidOperationException("GlassWall " + element.Id + " contains an invalid source handle.");
                        canonicalMetadata.Add(canonical);
                    }
                    if (canonicalMetadata.Count != 1)
                        throw new InvalidOperationException("GlassWall " + element.Id + " must own exactly one canonical source handle before Curtain 3D build.");
                    var liveSources = CadHandleService.Resolve(document, canonicalMetadata);
                    if (liveSources.Count != 1 || liveSources[0] != id)
                        throw new InvalidOperationException("GlassWall " + element.Id + " canonical source is missing, ambiguous, or differs from the selected entity.");
                    if (source is Line) lineSourceIds.Add(id);
                    else pathSourceIds.Add(id);
                    allSourceIds.Add(id);
                }
                transaction.Commit();
            }
            return new CurtainWallBuildSelection(lineSourceIds, pathSourceIds, allSourceIds);
        }

        private static bool SameHandle(string? left, string? right)
        {
            var a = CadHandleService.NormalizeHexHandle(left);
            var b = CadHandleService.NormalizeHexHandle(right);
            return a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
