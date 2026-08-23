using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal readonly struct SemanticMLeaderBatchItem
    {
        public SemanticMLeaderBatchItem(ProjectElement element, Point3d targetPoint, Point3d textPoint)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            TargetPoint = targetPoint;
            TextPoint = textPoint;
        }

        public ProjectElement Element { get; }
        public Point3d TargetPoint { get; }
        public Point3d TextPoint { get; }
    }

    internal static class SemanticMLeaderBuilder
    {
        private const int MaxBatchItems = 256;
        private const double DefaultTextHeightM = 0.18d;
        private const double MaxTextHeightM = 10d;

        public static string Build(
            Document document,
            ProjectState project,
            ProjectElement element,
            Point3d targetPoint,
            Point3d textPoint)
        {
            var handles = BuildBatch(
                document,
                project,
                new[] { new SemanticMLeaderBatchItem(element, targetPoint, textPoint) });
            if (handles.Count != 1)
                throw new InvalidOperationException("Semantic MLeader single build did not return exactly one generated handle.");
            return handles[0];
        }

        public static IReadOnlyList<string> BuildBatch(
            Document document,
            ProjectState project,
            IEnumerable<SemanticMLeaderBatchItem> items)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic MLeader yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var materialized = MaterializeBounded(items);
            if (materialized.Count == 0)
                throw new InvalidOperationException("Semantic MLeader batch không có item để tạo.");

            var ownership = GeneratedHandleOwnershipIndex.Build(project);
            var prepared = Prepare(document, project, materialized, ownership);
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            var generatedHandles = new List<string>(prepared.Count);

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    foreach (var item in prepared)
                    {
                        var sourceId = ResolveHandle(document.Database, item.SourceHandle, "semantic MLeader source " + item.Element.Id);
                        var source = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Entity;
                        if (source == null || source.IsErased)
                            throw new InvalidOperationException("Semantic MLeader source không còn live: " + item.SourceHandle + ".");
                        var owner = transaction.GetObject(source.OwnerId, OpenMode.ForWrite, false) as BlockTableRecord;
                        if (owner == null)
                            throw new InvalidOperationException("Không mở được owner space của semantic MLeader source " + item.Element.Id + ".");

                        ErasePrevious(transaction, project, item.Element, item.Previous);

                        var mtext = new MText
                        {
                            Location = item.TextPoint,
                            TextHeight = item.TextHeightDrawing,
                            Contents = EncodePlainMText(item.Rendered),
                            Attachment = AttachmentPoint.MiddleLeft,
                            Normal = Vector3d.ZAxis,
                            Rotation = 0d
                        };
                        mtext.SetDatabaseDefaults(document.Database);

                        var leader = new MLeader();
                        leader.SetDatabaseDefaults(document.Database);
                        leader.ContentType = ContentType.MTextContent;
                        leader.MText = mtext;
                        leader.TextHeight = item.TextHeightDrawing;
                        leader.TextLocation = item.TextPoint;
                        try { leader.LayerId = source.LayerId; } catch { }

                        var leaderIndex = leader.AddLeader();
                        var lineIndex = leader.AddLeaderLine(leaderIndex);
                        leader.AddFirstVertex(lineIndex, item.TargetPoint);
                        leader.AddLastVertex(lineIndex, item.TextPoint);

                        owner.AppendEntity(leader);
                        transaction.AddNewlyCreatedDBObject(leader, true);
                        GeneratedGeometryService.MarkGenerated(
                            document,
                            transaction,
                            leader,
                            project.ProjectId,
                            item.Element.Id,
                            item.Element.Category);

                        var generatedHandle = leader.Handle.ToString();
                        generatedHandles.Add(generatedHandle);
                        WriteMetadata(item, project, generatedHandle);

                        AuditTrail.ForProject(project).Record(
                            "documentation.semantic-tag.mleader.replace",
                            item.Element.Id,
                            generatedHandle + " • target=" + item.SourceHandle + " • template=" + item.Template);
                    }

                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Semantic MLeader batch failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return generatedHandles.AsReadOnly();
        }

        public static Point3d StoredTargetWorldPosition(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return new Point3d(
                RequiredFinite(element, GeneratedSemanticTagHealthService.LeaderTargetXKey),
                RequiredFinite(element, GeneratedSemanticTagHealthService.LeaderTargetYKey),
                RequiredFinite(element, GeneratedSemanticTagHealthService.LeaderTargetZKey));
        }

        public static Point3d StoredTextWorldPosition(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return new Point3d(
                RequiredFinite(element, GeneratedSemanticTagHealthService.LeaderTextXKey),
                RequiredFinite(element, GeneratedSemanticTagHealthService.LeaderTextYKey),
                RequiredFinite(element, GeneratedSemanticTagHealthService.LeaderTextZKey));
        }

        public static Point3d ReadSourceAnchor(Document document, string sourceHandle)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var id = ResolveHandle(document.Database, sourceHandle, "semantic MLeader source anchor");
                var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (source == null || source.IsErased)
                    throw new InvalidOperationException("Semantic MLeader source không còn live: " + sourceHandle + ".");
                Extents3d extents;
                try { extents = source.GeometricExtents; }
                catch (Exception error)
                {
                    throw new InvalidOperationException("Không đọc được GeometricExtents cho semantic MLeader source " + sourceHandle + ".", error);
                }
                var point = new Point3d(
                    (extents.MinPoint.X + extents.MaxPoint.X) * 0.5d,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) * 0.5d,
                    (extents.MinPoint.Z + extents.MaxPoint.Z) * 0.5d);
                ValidatePoint(point, "semantic MLeader source anchor");
                transaction.Commit();
                return point;
            }
        }

        private static List<SemanticMLeaderBatchItem> MaterializeBounded(IEnumerable<SemanticMLeaderBatchItem> items)
        {
            var result = new List<SemanticMLeaderBatchItem>();
            using (var enumerator = items.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxBatchItems)
                        throw new InvalidOperationException("Semantic MLeader batch supports at most " + MaxBatchItems + " items.");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        private static IReadOnlyList<PreparedItem> Prepare(
            Document document,
            ProjectState project,
            IReadOnlyList<SemanticMLeaderBatchItem> items,
            GeneratedHandleOwnershipIndex ownership)
        {
            var result = new List<PreparedItem>(items.Count);
            var seenElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                var element = item.Element;
                if (!seenElements.Add(element.Id))
                    throw new InvalidOperationException("Semantic MLeader batch contains duplicate semantic element id: " + element.Id + ".");
                var unique = project.Elements.Where(x => string.Equals(x.Id, element.Id, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
                if (unique.Count != 1 || !ReferenceEquals(unique[0], element))
                    throw new InvalidOperationException("Semantic MLeader element id không unique trong project: " + element.Id + ".");

                ValidatePoint(item.TargetPoint, element.Id + "/leader target");
                ValidatePoint(item.TextPoint, element.Id + "/leader text position");
                var sourceHandle = RequireSingleSourceHandle(element);
                var family = string.IsNullOrWhiteSpace(element.FamilyId) ? null : project.FindFamily(element.FamilyId);
                var template = Text(element, family, SemanticTagBuilder.TemplatePropertyKey, "{Id}");
                var rendered = SemanticTagRenderer.Render(project, element, template);
                var textHeightM = CadGeometryGuard.Positive(
                    CadGeometryGuard.Number(element, family, SemanticTagBuilder.TextHeightPropertyKey, DefaultTextHeightM),
                    element.Id + "/" + SemanticTagBuilder.TextHeightPropertyKey);
                if (textHeightM > MaxTextHeightM)
                    throw new InvalidOperationException(SemanticTagBuilder.TextHeightPropertyKey + " vượt giới hạn " + MaxTextHeightM.ToString("R", CultureInfo.InvariantCulture) + " m cho " + element.Id + ".");
                var textHeightDrawing = CadGeometryGuard.Positive(
                    CadGeometryGuard.ToDrawingUnits(document, textHeightM, element.Id + "/semantic MLeader text height"),
                    element.Id + "/semantic MLeader text height drawing");
                var previous = ValidatePrevious(document.Database, project, element, ownership);
                result.Add(new PreparedItem(
                    element,
                    sourceHandle,
                    item.TargetPoint,
                    item.TextPoint,
                    template,
                    rendered,
                    textHeightM,
                    textHeightDrawing,
                    previous));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<KeyValuePair<string, ObjectId>> ValidatePrevious(
            Database database,
            ProjectState project,
            ProjectElement element,
            GeneratedHandleOwnershipIndex ownership)
        {
            var result = new List<KeyValuePair<string, ObjectId>>();
            if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                return result;

            var seenCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var handle = token.Trim();
                if (handle.Length == 0) continue;
                var canonical = CadHandleService.NormalizeHexHandle(handle);
                if (canonical == null)
                    throw new InvalidOperationException("Generated semantic tag handle không hợp lệ cho " + element.Id + ": " + handle + ". Refusing destructive MLeader replacement.");
                if (!seenCanonical.Add(canonical)) continue;
                if (!ownership.TryFindOwner(handle, out var owner, out var slot) || owner == null ||
                    !ReferenceEquals(owner, element) ||
                    !string.Equals(GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot), GeneratedSemanticTagHealthService.HandlesKey, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Refusing semantic MLeader replacement because generated handle ownership is not " + element.Id + "/" + GeneratedSemanticTagHealthService.HandlesKey + ": " + handle + ".");
                result.Add(new KeyValuePair<string, ObjectId>(handle, ResolveHandle(database, handle, "generated semantic tag " + element.Id)));
            }

            using (var validation = database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var item in result)
                {
                    var entity = validation.GetObject(item.Value, OpenMode.ForRead, false) as Entity;
                    RequireSupportedSemanticTag(entity, item.Key, "validate semantic MLeader replacement");
                    GeneratedGeometryService.RequireMatchingOwnership(entity!, project, element, "validate semantic MLeader replacement " + item.Key);
                }
                validation.Commit();
            }
            return result.AsReadOnly();
        }

        private static void ErasePrevious(
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            IReadOnlyList<KeyValuePair<string, ObjectId>> previous)
        {
            foreach (var item in previous)
            {
                var entity = transaction.GetObject(item.Value, OpenMode.ForWrite, false) as Entity;
                RequireSupportedSemanticTag(entity, item.Key, "erase semantic MLeader replacement");
                GeneratedGeometryService.RequireMatchingOwnership(entity!, project, element, "erase semantic tag " + item.Key);
                entity!.Erase();
            }
        }

        private static void RequireSupportedSemanticTag(Entity? entity, string handle, string operation)
        {
            if (entity == null || entity.IsErased)
                throw new InvalidOperationException("Generated semantic tag handle " + handle + " is missing or erased during " + operation + ".");
            if (!(entity is MText) && !(entity is MLeader))
                throw new InvalidOperationException("Generated semantic tag handle " + handle + " is live but is neither MText nor MLeader during " + operation + ".");
        }

        private static void WriteMetadata(PreparedItem item, ProjectState project, string generatedHandle)
        {
            ClearLeaderMetadata(item.Element);
            item.Element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = generatedHandle;
            item.Element.Properties[GeneratedSemanticTagHealthService.TemplateKey] = item.Template;
            item.Element.Properties[GeneratedSemanticTagHealthService.TextKey] = item.Rendered;
            item.Element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = project.ProjectId;
            item.Element.Properties[GeneratedSemanticTagHealthService.OwnerElementKey] = item.Element.Id;
            item.Element.Properties[GeneratedSemanticTagHealthService.OwnershipVersionKey] = GeneratedSemanticTagHealthService.OwnershipVersion;
            item.Element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = item.TextHeightM.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;
            item.Element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = item.TextPoint.X.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.PositionYKey] = item.TextPoint.Y.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.PositionZKey] = item.TextPoint.Z.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.RotationKey] = 0d.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.ArtifactKindKey] = GeneratedSemanticTagHealthService.MLeaderArtifactKind;
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTargetHandleKey] = item.SourceHandle;
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTargetXKey] = item.TargetPoint.X.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTargetYKey] = item.TargetPoint.Y.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTargetZKey] = item.TargetPoint.Z.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTextXKey] = item.TextPoint.X.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTextYKey] = item.TextPoint.Y.ToString("R", CultureInfo.InvariantCulture);
            item.Element.Properties[GeneratedSemanticTagHealthService.LeaderTextZKey] = item.TextPoint.Z.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void ClearLeaderMetadata(ProjectElement element)
        {
            var keys = element.Properties.Keys
                .Where(x => x.StartsWith("GeneratedSemanticTagLeader", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var key in keys) element.Properties.Remove(key);
        }

        private static string RequireSingleSourceHandle(ProjectElement element)
        {
            var sources = element.SourceHandles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sources.Count != 1)
                throw new InvalidOperationException("Semantic MLeader yêu cầu đúng một authoritative source Handle cho " + element.Id + "; hiện có " + sources.Count + ".");
            if (CadHandleService.NormalizeHexHandle(sources[0]) == null)
                throw new InvalidOperationException("Semantic MLeader authoritative source Handle không hợp lệ cho " + element.Id + ": " + sources[0] + ".");
            return sources[0];
        }

        private static ObjectId ResolveHandle(Database database, string text, string label)
        {
            var canonical = CadHandleService.NormalizeHexHandle(text);
            if (canonical == null || !long.TryParse(canonical, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException(label + " Handle không hợp lệ: " + text + ".");
            try
            {
                var id = database.GetObjectId(false, new Handle(value), 0);
                if (!id.IsNull && id.IsValid) return id;
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("Không resolve được " + label + " Handle: " + text + ".", error);
            }
            throw new InvalidOperationException("Không resolve được " + label + " Handle: " + text + ".");
        }

        private static string EncodePlainMText(string value)
        {
            var text = value ?? string.Empty;
            var output = new StringBuilder(text.Length + 16);
            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                if (ch == '\r')
                {
                    if (index + 1 < text.Length && text[index + 1] == '\n') index++;
                    output.Append("\\P");
                }
                else if (ch == '\n') output.Append("\\P");
                else if (ch == '\\') output.Append("\\\\");
                else if (ch == '{') output.Append("\\{");
                else if (ch == '}') output.Append("\\}");
                else output.Append(ch);
            }
            return output.ToString();
        }

        private static string Text(ProjectElement element, ProjectFamily? family, string key, string fallback)
        {
            if (element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)) return own.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return fallback;
        }

        private static double RequiredFinite(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(key + " không hợp lệ cho " + element.Id + ".");
            return value;
        }

        private static void ValidatePoint(Point3d point, string label)
        {
            CadGeometryGuard.Finite(point.X, label + "/X");
            CadGeometryGuard.Finite(point.Y, label + "/Y");
            CadGeometryGuard.Finite(point.Z, label + "/Z");
        }

        private sealed class PreparedItem
        {
            public PreparedItem(
                ProjectElement element,
                string sourceHandle,
                Point3d targetPoint,
                Point3d textPoint,
                string template,
                string rendered,
                double textHeightM,
                double textHeightDrawing,
                IReadOnlyList<KeyValuePair<string, ObjectId>> previous)
            {
                Element = element;
                SourceHandle = sourceHandle;
                TargetPoint = targetPoint;
                TextPoint = textPoint;
                Template = template;
                Rendered = rendered;
                TextHeightM = textHeightM;
                TextHeightDrawing = textHeightDrawing;
                Previous = previous;
            }

            public ProjectElement Element { get; }
            public string SourceHandle { get; }
            public Point3d TargetPoint { get; }
            public Point3d TextPoint { get; }
            public string Template { get; }
            public string Rendered { get; }
            public double TextHeightM { get; }
            public double TextHeightDrawing { get; }
            public IReadOnlyList<KeyValuePair<string, ObjectId>> Previous { get; }
        }
    }
}