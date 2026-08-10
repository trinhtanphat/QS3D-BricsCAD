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
    internal static class GeneratedGridAnnotationRuntimeHealthService
    {
        private const string HandlesKey = "GeneratedGridAnnotationHandles";
        private const int ExpectedEntityCount = 6;

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements)
                {
                    if (element.Category != ElementCategory.Grid) continue;
                    if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;

                    var handles = raw
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .ToList();

                    for (var index = 0; index < handles.Count; index++)
                        InspectHandle(document, transaction, project, element, handles[index], index, issues);

                    if (handles.Count != ExpectedEntityCount)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_ANNOTATION_CAD_ENTITY_COUNT",
                            HealthSeverity.Warning,
                            "Live Grid annotation kỳ vọng " + ExpectedEntityCount + " tracked entities nhưng metadata hiện có " + handles.Count + ".",
                            element.Id));
                    }
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
            int index,
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

            Entity? entity;
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

            if (!MatchesExpectedType(entity, index))
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_TYPE_MISMATCH",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle " + handle + " có type " + entity.GetType().Name +
                    ", expected " + ExpectedTypeName(index) + " tại slot " + index + ".",
                    element.Id));
            }

            if (!GeneratedGeometryService.HasMatchingOwnership(entity, project, element))
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_OWNERSHIP_MISMATCH",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle " + handle + " còn sống nhưng QS3D XData ownership không khớp project/Grid hiện tại.",
                    element.Id));
            }

            if (entity is DBText text)
            {
                var currentLabel = element.Properties.TryGetValue(GridNamingService.GridLabelKey, out var rawLabel)
                    ? (rawLabel ?? string.Empty).Trim()
                    : string.Empty;
                var cadLabel = (text.TextString ?? string.Empty).Trim();
                if (!string.Equals(currentLabel, cadLabel, StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_ANNOTATION_CAD_TEXT_STALE",
                        HealthSeverity.Error,
                        "Grid annotation DBText không khớp semantic GridLabel. CAD=" + cadLabel + ", semantic=" + currentLabel + ".",
                        element.Id));
                }
            }
        }

        private static bool MatchesExpectedType(Entity entity, int index)
        {
            switch (index % 3)
            {
                case 0: return entity is Line;
                case 1: return entity is Circle;
                default: return entity is DBText;
            }
        }

        private static string ExpectedTypeName(int index)
        {
            switch (index % 3)
            {
                case 0: return nameof(Line);
                case 1: return nameof(Circle);
                default: return nameof(DBText);
            }
        }

        private static void AddMissing(ProjectElement element, string handle, ICollection<ModelHealthIssue> issues)
        {
            issues.Add(new ModelHealthIssue(
                "GRID_ANNOTATION_CAD_MISSING",
                HealthSeverity.Error,
                "Generated Grid annotation Handle không còn resolve tới live CAD entity: " + handle + ".",
                element.Id));
        }
    }
}
