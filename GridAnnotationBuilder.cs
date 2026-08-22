using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GridAnnotationBuilder
    {
        internal const string HandlesKey = "GeneratedGridAnnotationHandles";
        internal const string LabelKey = "GeneratedGridAnnotationLabel";
        internal const string OwnerProjectKey = "GeneratedGridAnnotationOwnerProjectId";
        internal const string OwnerElementKey = "GeneratedGridAnnotationOwnerElementId";
        internal const string OwnershipVersionKey = "GeneratedGridAnnotationOwnershipVersion";
        internal const string BubbleRadiusKey = "GridBubbleRadiusM";
        internal const string TextHeightKey = "GridTextHeightM";
        internal const string OwnershipVersion = "1";
        private const int MaxBatch = 2000;
        private const int MaxLabelLength = 64;
        private const double DefaultBubbleRadiusM = 0.25d;
        private const double DefaultTextHeightM = 0.18d;
        private const double GeometryTolerance = 1e-9d;

        public static int Build(Document document, ProjectState project, IReadOnlyList<ProjectElement> elements)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (elements.Count == 0) return 0;
            if (elements.Count > MaxBatch) throw new InvalidOperationException("Grid annotation batch vượt giới hạn " + MaxBatch + ".");
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Grid annotation yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var distinctIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element == null) throw new InvalidOperationException("Grid annotation batch chứa element null.");
                if (element.Category != ElementCategory.Grid) throw new InvalidOperationException("Grid annotation chỉ nhận ElementCategory.Grid: " + element.Id + ".");
                if (!distinctIds.Add(element.Id)) throw new InvalidOperationException("Grid annotation batch chứa Grid trùng: " + element.Id + ".");
            }
            RequireCanonicalElements(project, elements, "Grid annotation build");

            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    foreach (var element in elements) ReplaceOne(document, transaction, project, element);
                    transaction.Commit();
                }
            }
            catch (Exception operationError)
            {
                try { rollback.Restore(project); }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        "Grid annotation thất bại và semantic rollback cũng thất bại.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }

            try { document.Editor.Regen(); } catch { }
            return elements.Count;
        }

        internal static void RebuildInTransaction(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Grid annotation rebuild yêu cầu DWG đích vẫn là MdiActiveDocument.");
            if (element.Category != ElementCategory.Grid)
                throw new InvalidOperationException("Grid annotation rebuild chỉ nhận ElementCategory.Grid: " + element.Id + ".");
            RequireCanonicalElements(project, new[] { element }, "Grid annotation rebuild");

            ReplaceOne(document, transaction, project, element);
        }

        private static void RequireCanonicalElements(ProjectState project, IReadOnlyList<ProjectElement> elements, string operation)
        {
            var canonical = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in project.Elements)
            {
                if (candidate == null)
                    throw new InvalidOperationException(operation + " refused a project containing a null semantic element.");
                if (candidate.Category != ElementCategory.Grid) continue;
                if (canonical.ContainsKey(candidate.Id))
                    throw new InvalidOperationException(operation + " refused duplicate semantic Grid Id: " + candidate.Id + ".");
                canonical.Add(candidate.Id, candidate);
            }

            foreach (var element in elements)
            {
                if (!canonical.TryGetValue(element.Id, out var current))
                    throw new InvalidOperationException(operation + " target is no longer present in the current project: " + element.Id + ".");
                if (!ReferenceEquals(current, element))
                    throw new InvalidOperationException(operation + " refused a stale/detached Grid instance: " + element.Id + ". Re-resolve it from the current project and retry.");
            }
        }

        private static void ReplaceOne(Document document, Transaction transaction, ProjectState project, ProjectElement element)
        {
            var label = Property(element, GridNamingService.GridLabelKey);
            if (label.Length == 0) throw new InvalidOperationException("Grid " + element.Id + " chưa có GridLabel. Chạy QS3DGRIDNUMBER trước.");
            if (label.Length > MaxLabelLength) throw new InvalidOperationException("GridLabel vượt " + MaxLabelLength + " ký tự: " + element.Id + ".");

            var sources = element.SourceHandles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sources.Count != 1)
                throw new InvalidOperationException("Grid " + element.Id + " phải có đúng một authoritative source Handle để tạo annotation; hiện có " + sources.Count + ".");

            var sourceId = ResolveHandle(document.Database, sources[0], "Grid source " + element.Id);
            var source = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Entity;
            if (source == null || source.IsErased) throw new InvalidOperationException("Grid source không còn live: " + sources[0] + ".");

            Point3d start;
            Point3d end;
            Vector3d annotationNormal;
            if (source is Line line)
            {
                start = line.StartPoint;
                end = line.EndPoint;
                if (Math.Abs(start.Z - end.Z) > GeometryTolerance)
                    throw new InvalidOperationException("Grid LINE 3D nghiêng chưa có annotation plane xác định; source phải nằm trên mặt phẳng WCS-XY tại một cao độ: " + element.Id + ".");
                annotationNormal = Vector3d.ZAxis;
            }
            else if (source is Arc arc)
            {
                start = arc.StartPoint;
                end = arc.EndPoint;
                annotationNormal = arc.Normal;
                ValidateVector(annotationNormal, element.Id + "/arc normal");
                if (annotationNormal.Length <= GeometryTolerance)
                    throw new InvalidOperationException("Grid ARC có normal suy biến: " + element.Id + ".");
                annotationNormal = annotationNormal.GetNormal();
            }
            else
            {
                throw new InvalidOperationException("Grid annotation chỉ hỗ trợ LINE/ARC source; nhận " + source.GetType().Name + " cho " + element.Id + ".");
            }

            ValidatePoint(start, element.Id + "/start");
            ValidatePoint(end, element.Id + "/end");
            if (start.DistanceTo(end) <= GeometryTolerance) throw new InvalidOperationException("Grid source có endpoints trùng nhau: " + element.Id + ".");

            var family = string.IsNullOrWhiteSpace(element.FamilyId) ? null : project.FindFamily(element.FamilyId);
            var radiusM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, BubbleRadiusKey, DefaultBubbleRadiusM), element.Id + "/" + BubbleRadiusKey);
            var textHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, TextHeightKey, DefaultTextHeightM), element.Id + "/" + TextHeightKey);
            var radius = CadGeometryGuard.ToDrawingUnits(document, radiusM, element.Id + "/bubble radius");
            var textHeight = CadGeometryGuard.ToDrawingUnits(document, textHeightM, element.Id + "/text height");

            if (textHeight > radius * 1.8d)
                throw new InvalidOperationException("GridTextHeightM quá lớn so với GridBubbleRadiusM cho " + element.Id + ".");

            var previous = ValidatePrevious(document.Database, transaction, project, element);
            ErasePrevious(transaction, project, element, previous);

            var owner = transaction.GetObject(source.OwnerId, OpenMode.ForWrite, false) as BlockTableRecord;
            if (owner == null) throw new InvalidOperationException("Không mở được owner space của Grid source " + element.Id + ".");

            var centers = BubbleCenters(source, start, end, radius);
            var generatedHandles = new List<string>(6);
            AddEndpointAnnotation(document, transaction, owner, source, project, element, start, centers.Item1, annotationNormal, radius, textHeight, label, generatedHandles);
            AddEndpointAnnotation(document, transaction, owner, source, project, element, end, centers.Item2, annotationNormal, radius, textHeight, label, generatedHandles);

            element.Properties[HandlesKey] = string.Join(";", generatedHandles);
            element.Properties[LabelKey] = label;
            element.Properties[OwnerProjectKey] = project.ProjectId;
            element.Properties[OwnerElementKey] = element.Id;
            element.Properties[OwnershipVersionKey] = OwnershipVersion;
            element.Properties[BubbleRadiusKey] = radiusM.ToString("R", CultureInfo.InvariantCulture);
            element.Properties[TextHeightKey] = textHeightM.ToString("R", CultureInfo.InvariantCulture);

            AuditTrail.ForProject(project).Record(
                "grid.annotation.replace",
                element.Id,
                label + " • " + generatedHandles.Count.ToString(CultureInfo.InvariantCulture) + " CAD entities");
        }

        private static Tuple<Point3d, Point3d> BubbleCenters(Entity source, Point3d start, Point3d end, double radius)
        {
            if (source is Line)
            {
                var direction = end - start;
                if (direction.Length <= GeometryTolerance) return Tuple.Create(start, end);
                direction = direction.GetNormal();
                var offset = direction * (radius * 1.5d);
                return Tuple.Create(start - offset, end + offset);
            }

            if (source is Arc arc)
            {
                var first = start - arc.Center;
                var second = end - arc.Center;
                if (first.Length > GeometryTolerance) first = first.GetNormal() * (radius * 1.5d);
                if (second.Length > GeometryTolerance) second = second.GetNormal() * (radius * 1.5d);
                return Tuple.Create(start + first, end + second);
            }

            return Tuple.Create(start, end);
        }

        private static void AddEndpointAnnotation(
            Document document,
            Transaction transaction,
            BlockTableRecord owner,
            Entity source,
            ProjectState project,
            ProjectElement element,
            Point3d endpoint,
            Point3d center,
            Vector3d normal,
            double radius,
            double textHeight,
            string label,
            ICollection<string> handles)
        {
            var extensionVector = center - endpoint;
            if (extensionVector.Length > GeometryTolerance)
            {
                var extension = new Line(endpoint, center);
                PrepareEntity(document, transaction, owner, source, project, element, extension, handles);
            }

            var circle = new Circle(center, normal, radius);
            PrepareEntity(document, transaction, owner, source, project, element, circle, handles);

            var text = new DBText
            {
                TextString = label,
                Height = textHeight,
                Position = center,
                AlignmentPoint = center,
                Justify = AttachmentPoint.MiddleCenter,
                Normal = normal
            };
            PrepareEntity(document, transaction, owner, source, project, element, text, handles);
            try { text.AdjustAlignment(document.Database); } catch { }
        }

        private static void PrepareEntity(
            Document document,
            Transaction transaction,
            BlockTableRecord owner,
            Entity source,
            ProjectState project,
            ProjectElement element,
            Entity generated,
            ICollection<string> handles)
        {
            generated.SetDatabaseDefaults(document.Database);
            try { generated.LayerId = source.LayerId; } catch { }
            owner.AppendEntity(generated);
            transaction.AddNewlyCreatedDBObject(generated, true);
            GeneratedGeometryService.MarkGenerated(document, transaction, generated, project.ProjectId, element.Id, ElementCategory.Grid);
            handles.Add(generated.Handle.ToString());
        }

        private static IReadOnlyList<KeyValuePair<string, ObjectId>> ValidatePrevious(
            Database database,
            Transaction transaction,
            ProjectState project,
            ProjectElement element)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<KeyValuePair<string, ObjectId>>();

            var expected = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var canonical = CadHandleService.NormalizeHexHandle(token);
                if (canonical == null)
                    throw new InvalidOperationException(
                        "Generated Grid annotation metadata chứa Handle không hợp lệ. Refusing destructive replacement before any Grid annotation is erased: " + token + ".");
                if (seen.Add(canonical)) expected.Add(canonical);
            }

            if (expected.Count == 0)
                throw new InvalidOperationException(
                    "Generated Grid annotation metadata không chứa Handle hợp lệ. Refusing destructive replacement before any Grid annotation is erased.");

            var result = new List<KeyValuePair<string, ObjectId>>(expected.Count);
            foreach (var handle in expected)
            {
                var id = ResolveHandle(database, handle, "generated Grid annotation " + element.Id);
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated Grid annotation không còn live. Refusing destructive replacement before any Grid annotation is erased: " + handle + ".");
                if (!(entity is Line) && !(entity is Circle) && !(entity is DBText))
                    throw new InvalidOperationException(
                        "Generated Grid annotation có loại CAD object không hợp lệ. Refusing destructive replacement before any Grid annotation is erased: " + handle + "/" + entity.GetType().Name + ".");
                GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "validate Grid annotation " + handle);
                result.Add(new KeyValuePair<string, ObjectId>>(handle, id));
            }

            if (result.Count != expected.Count)
                throw new InvalidOperationException(
                    "Generated Grid annotation live-handle set không đầy đủ. Refusing destructive replacement before any Grid annotation is erased.");
            return result;
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
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated Grid annotation changed after validation. Refusing partial destructive replacement: " + item.Key + ".");
                if (!(entity is Line) && !(entity is Circle) && !(entity is DBText))
                    throw new InvalidOperationException(
                        "Generated Grid annotation type changed after validation. Refusing partial destructive replacement: " + item.Key + ".");
                GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "erase Grid annotation " + item.Key);
                entity.Erase();
            }
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Không resolve được " + label + " Handle: " + text + ".", ex);
            }
            throw new InvalidOperationException("Không resolve được " + label + " Handle: " + text + ".");
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

        private static void ValidatePoint(Point3d point, string label)
        {
            CadGeometryGuard.Finite(point.X, label + "/X");
            CadGeometryGuard.Finite(point.Y, label + "/Y");
            CadGeometryGuard.Finite(point.Z, label + "/Z");
        }

        private static void ValidateVector(Vector3d vector, string label)
        {
            CadGeometryGuard.Finite(vector.X, label + "/X");
            CadGeometryGuard.Finite(vector.Y, label + "/Y");
            CadGeometryGuard.Finite(vector.Z, label + "/Z");
        }
    }
}
