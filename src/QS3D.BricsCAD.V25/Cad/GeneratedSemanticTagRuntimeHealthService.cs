using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedSemanticTagRuntimeHealthService
    {
        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    var handles = raw
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    foreach (var handle in handles)
                        InspectHandle(document, transaction, project, element, handle, issues);
                }
                transaction.Commit();
            }

            return issues.AsReadOnly();
        }

        private static void InspectHandle(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            string handle,
            ICollection<ModelHealthIssue> issues)
        {
            if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return;

            ObjectId id;
            try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
            catch
            {
                AddMissing(element, handle, issues);
                return;
            }

            if (id.IsNull || !id.IsValid)
            {
                AddMissing(element, handle, issues);
                return;
            }

            Entity entity;
            try { entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity; }
            catch
            {
                AddMissing(element, handle, issues);
                return;
            }

            if (entity == null || entity.IsErased)
            {
                AddMissing(element, handle, issues);
                return;
            }

            if (!(entity is MText tag))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_CAD_TYPE_MISMATCH",
                    HealthSeverity.Error,
                    "Generated semantic tag Handle " + handle + " có type " + entity.GetType().Name + ", expected MText.",
                    element.Id));
                return;
            }

            if (!GeneratedGeometryService.HasMatchingOwnership(tag, project, element))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_TAG_CAD_OWNERSHIP_MISMATCH",
                    HealthSeverity.Error,
                    "Generated semantic tag Handle " + handle + " còn sống nhưng QS3D XData ownership không khớp project/element/category hiện tại.",
                    element.Id));
            }

            var builtText = Property(element, GeneratedSemanticTagHealthService.TextKey);
            if (builtText.Length > 0)
            {
                var expectedContents = SemanticTagBuilder.EncodePlainMText(builtText);
                if (!string.Equals(tag.Contents ?? string.Empty, expectedContents, StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue(
                        "SEMANTIC_TAG_CAD_TEXT_STALE",
                        HealthSeverity.Warning,
                        "Generated semantic tag MText đã bị sửa trực tiếp hoặc không còn khớp text đã build; chạy QS3DTAGREFRESH.",
                        element.Id));
                }
            }
        }

        private static void AddMissing(ProjectElement element, string handle, ICollection<ModelHealthIssue> issues)
        {
            issues.Add(new ModelHealthIssue(
                "SEMANTIC_TAG_CAD_MISSING",
                HealthSeverity.Error,
                "Generated semantic tag Handle không còn resolve tới live MText: " + handle + ". Chạy QS3DTAGREFRESH để rebuild.",
                element.Id));
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;
    }
}
