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
    internal static class SemanticTagBuilder
    {
        internal const string TemplatePropertyKey = "SemanticTagTemplate";
        internal const string TextHeightPropertyKey = "SemanticTagTextHeightM";
        private const double DefaultTextHeightM = 0.18d;
        private const double MaxTextHeightM = 10d;

        public static string Build(
            Document document,
            ProjectState project,
            ProjectElement element,
            Point3d worldPosition,
            double rotationRadians)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic tag yêu cầu DWG đích vẫn là MdiActiveDocument.");
            ValidatePoint(worldPosition, element.Id + "/tag position");
            if (double.IsNaN(rotationRadians) || double.IsInfinity(rotationRadians))
                throw new InvalidOperationException("Semantic tag rotation phải hữu hạn.");

            var unique = project.Elements.Where(x => string.Equals(x.Id, element.Id, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if (unique.Count != 1 || !ReferenceEquals(unique[0], element))
                throw new InvalidOperationException("Semantic tag element id không unique trong project: " + element.Id + ".");

            var sourceHandle = RequireSingleSourceHandle(element);
            var family = string.IsNullOrWhiteSpace(element.FamilyId) ? null : project.FindFamily(element.FamilyId);
            var template = Text(element, family, TemplatePropertyKey, "{Id}");
            var rendered = SemanticTagRenderer.Render(project, element, template);
            var textHeightM = CadGeometryGuard.Positive(
                CadGeometryGuard.Number(element, family, TextHeightPropertyKey, DefaultTextHeightM),
                element.Id + "/" + TextHeightPropertyKey);
            if (textHeightM > MaxTextHeightM)
                throw new InvalidOperationException(TextHeightPropertyKey + " vượt giới hạn " + MaxTextHeightM.ToString("R", CultureInfo.InvariantCulture) + " m cho " + element.Id + ".");
            var textHeight = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, textHeightM, element.Id + "/semantic tag text height"),
                element.Id + "/semantic tag text height drawing");

            var ownership = GeneratedHandleOwnershipIndex.Build(project);
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            string generatedHandle;
            try
            {
                using (document.LockDocument())
                {
                    var previous = ValidatePrevious(document.Database, project, element, ownership);
                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        var sourceId = ResolveHandle(document.Database, sourceHandle, "semantic tag source " + element.Id);
                        var source = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Entity;
                        if (source == null || source.IsErased)
                            throw new InvalidOperationException("Semantic tag source không còn live: " + sourceHandle + ".");
                        var owner = transaction.GetObject(source.OwnerId, OpenMode.ForWrite, false) as BlockTableRecord;
                        if (owner == null)
                            throw new InvalidOperationException("Không mở được owner space của semantic source " + element.Id + ".");

                        ErasePrevious(transaction, project, element, previous);

                        var tag = new MText
                        {
                            Location = worldPosition,
                            TextHeight = textHeight,
                            Contents = EncodePlainMText(rendered),
                            Attachment = AttachmentPoint.MiddleCenter,
                            Normal = Vector3d.ZAxis,
                            Rotation = rotationRadians
                        };
                        tag.SetDatabaseDefaults(document.Database);
                        try { tag.LayerId = source.LayerId; } catch { }
                        owner.AppendEntity(tag);
                        transaction.AddNewlyCreatedDBObject(tag, true);
                        GeneratedGeometryService.MarkGenerated(document, transaction, tag, project.ProjectId, element.Id, element.Category);
                        generatedHandle = tag.Handle.ToString();

                        element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = generatedHandle;
                        element.Properties[GeneratedSemanticTagHealthService.TemplateKey] = template;
                        element.Properties[GeneratedSemanticTagHealthService.TextKey] = rendered;
                        element.Properties[GeneratedSemanticTagHealthService.OwnerProjectKey] = project.ProjectId;
                        element.Properties[GeneratedSemanticTagHealthService.OwnerElementKey] = element.Id;
                        element.Properties[GeneratedSemanticTagHealthService.OwnershipVersionKey] = GeneratedSemanticTagHealthService.OwnershipVersion;
                        element.Properties[GeneratedSemanticTagHealthService.TextHeightKey] = textHeightM.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;
                        element.Properties[GeneratedSemanticTagHealthService.PositionXKey] = worldPosition.X.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties[GeneratedSemanticTagHealthService.PositionYKey] = worldPosition.Y.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties[GeneratedSemanticTagHealthService.PositionZKey] = worldPosition.Z.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["GeneratedSemanticTagRotationRad"] = rotationRadians.ToString("R", CultureInfo.InvariantCulture);

                        AuditTrail.ForProject(project).Record(
                            "documentation.semantic-tag.replace",
                            element.Id,
                            generatedHandle + " • template=" + template);
                        transaction.Commit();
                        cadCommitted = true;
                    }
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
                            "Semantic tag replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return generatedHandle;
        }

        public static Point3d StoredWorldPosition(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!string.Equals(Property(element, GeneratedSemanticTagHealthService.PositionScopeKey), GeneratedSemanticTagHealthService.DrawingLocalWcs, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic tag chưa có drawing-local WCS position hợp lệ: " + element.Id + ".");
            return new Point3d(
                RequiredFinite(element, GeneratedSemanticTagHealthService.PositionXKey),
                RequiredFinite(element, GeneratedSemanticTagHealthService.PositionYKey),
                RequiredFinite(element, GeneratedSemanticTagHealthService.PositionZKey));
        }

        public static double StoredRotation(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return RequiredFinite(element, "GeneratedSemanticTagRotationRad");
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
                    throw new InvalidOperationException(
                        "Generated semantic tag handle không hợp lệ cho " + element.Id + ": " + handle + ". Refusing destructive replacement.");
                if (!seenCanonical.Add(canonical)) continue;

                if (!ownership.TryFindOwner(handle, out var owner, out var slot) || owner == null ||
                    !ReferenceEquals(owner, element) ||
                    !string.Equals(GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot), GeneratedSemanticTagHealthService.HandlesKey, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Refusing semantic tag replacement because generated handle ownership is not " +
                        element.Id + "/" + GeneratedSemanticTagHealthService.HandlesKey + ": " + handle + ".");

                result.Add(new KeyValuePair<string, ObjectId>(
                    handle,
                    ResolveHandle(database, handle, "generated semantic tag " + element.Id)));
            }

            if (result.Count == 0)
                throw new InvalidOperationException(
                    "GeneratedSemanticTagHandles không có handle hợp lệ để replace cho " + element.Id + ".");

            using (var validation = database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var item in result)
                {
                    var entity = validation.GetObject(item.Value, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException(
                            "Generated semantic tag handle " + item.Key +
                            " is missing or erased. Refusing destructive replacement before any semantic tag is erased.");
                    if (!(entity is MText))
                        throw new InvalidOperationException(
                            "Generated semantic tag handle " + item.Key + " is live but is not MText. Refusing destructive replacement.");
                    GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "validate semantic tag replacement " + item.Key);
                }
                validation.Commit();
            }

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
                        "Generated semantic tag handle " + item.Key + " is no longer live. Refusing partial destructive replacement.");
                if (!(entity is MText))
                    throw new InvalidOperationException(
                        "Generated semantic tag handle " + item.Key + " is live but is not MText. Refusing destructive replacement.");
                GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "erase semantic tag " + item.Key);
                entity.Erase();
            }
        }

        private static string RequireSingleSourceHandle(ProjectElement element)
        {
            var sources = element.SourceHandles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sources.Count != 1)
                throw new InvalidOperationException("Semantic tag P0 yêu cầu đúng một authoritative source Handle cho " + element.Id + "; hiện có " + sources.Count + ".");
            return sources[0];
        }

        private static ObjectId ResolveHandle(Database database, string text, string label)
        {
            var canonical = CadHandleService.NormalizeHexHandle(text);
            if (canonical == null ||
                !long.TryParse(canonical, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
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

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

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
    }
}
