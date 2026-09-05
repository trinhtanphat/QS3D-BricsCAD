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

                    var hasAuthoritativeOwner = TryResolveAuthoritativeOwner(
                        document,
                        transaction,
                        element,
                        issues,
                        out var authoritativeOwnerId);

                    // Preserve every persisted slot. Empty/malformed tokens must not collapse the
                    // extension/bubble/text positional contract before validation.
                    var handles = (raw ?? string.Empty)
                        .Split(new[] { ';' }, StringSplitOptions.None)
                        .ToList();

                    for (var index = 0; index < handles.Count; index++)
                        InspectHandle(document, transaction, project, element, handles[index], index, hasAuthoritativeOwner, authoritativeOwnerId, issues);

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

        private static bool TryResolveAuthoritativeOwner(
            Document document,
            Transaction transaction,
            ProjectElement element,
            ICollection<ModelHealthIssue> issues,
            out ObjectId authoritativeOwnerId)
        {
            authoritativeOwnerId = ObjectId.Null;
            var sources = element.SourceHandles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sources.Count != 1)
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_SOURCE_HANDLE_COUNT",
                    HealthSeverity.Error,
                    "Grid annotation runtime health cần đúng một authoritative Grid source Handle; hiện có " + sources.Count + ".",
                    element.Id));
                return false;
            }

            var canonical = CadHandleService.NormalizeHexHandle(sources[0]);
            if (canonical == null ||
                !long.TryParse(canonical, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ||
                value <= 0L)
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_SOURCE_HANDLE_INVALID",
                    HealthSeverity.Error,
                    "Authoritative Grid source Handle không hợp lệ cho runtime owner-space health: " + sources[0] + ".",
                    element.Id));
                return false;
            }

            ObjectId sourceId;
            try { sourceId = document.Database.GetObjectId(false, new Handle(value), 0); }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                AddSourceMissing(element, canonical, issues);
                return false;
            }

            if (sourceId.IsNull || !sourceId.IsValid)
            {
                AddSourceMissing(element, canonical, issues);
                return false;
            }

            Entity? source;
            try { source = transaction.GetObject(sourceId, OpenMode.ForRead, true) as Entity; }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                AddSourceMissing(element, canonical, issues);
                return false;
            }

            if (source == null || source.IsErased)
            {
                AddSourceMissing(element, canonical, issues);
                return false;
            }

            authoritativeOwnerId = source.OwnerId;
            if (authoritativeOwnerId.IsNull || !authoritativeOwnerId.IsValid)
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_SOURCE_OWNER_INVALID",
                    HealthSeverity.Error,
                    "Authoritative Grid source không có live owner space/layout hợp lệ: " + canonical + ".",
                    element.Id));
                authoritativeOwnerId = ObjectId.Null;
                return false;
            }

            DBObject? owner;
            try { owner = transaction.GetObject(authoritativeOwnerId, OpenMode.ForRead, true); }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                owner = null;
            }
            if (!(owner is BlockTableRecord))
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_SOURCE_OWNER_INVALID",
                    HealthSeverity.Error,
                    "Authoritative Grid source owner không phải BlockTableRecord live cho annotation lifecycle: " + canonical + ".",
                    element.Id));
                authoritativeOwnerId = ObjectId.Null;
                return false;
            }

            return true;
        }

        private static void InspectHandle(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            string handle,
            int index,
            bool hasAuthoritativeOwner,
            ObjectId authoritativeOwnerId,
            ICollection<ModelHealthIssue> issues)
        {
            var canonicalHandle = CadHandleService.NormalizeHexHandle(handle);
            if (canonicalHandle == null)
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_HANDLE_INVALID",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle không phải CAD hex dương hợp lệ: " + handle + ".",
                    element.Id));
                return;
            }
            if (!string.Equals(handle, canonicalHandle, StringComparison.Ordinal))
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_HANDLE_NON_CANONICAL",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle phải dùng đúng CAD hex canonical: " + canonicalHandle + ".",
                    element.Id));
                return;
            }
            if (!long.TryParse(canonicalHandle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value <= 0L)
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_HANDLE_INVALID",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle không thể resolve theo canonical CAD hex contract: " + canonicalHandle + ".",
                    element.Id));
                return;
            }

            ObjectId id;
            try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                AddMissing(element, canonicalHandle, issues);
                return;
            }

            if (id.IsNull || !id.IsValid)
            {
                AddMissing(element, canonicalHandle, issues);
                return;
            }

            Entity? entity;
            try { entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity; }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                AddMissing(element, canonicalHandle, issues);
                return;
            }

            if (entity == null || entity.IsErased)
            {
                AddMissing(element, canonicalHandle, issues);
                return;
            }

            if (!MatchesExpectedType(entity, index))
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_TYPE_MISMATCH",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle " + canonicalHandle + " có type " + entity.GetType().Name +
                    ", expected " + ExpectedTypeName(index) + " tại slot " + index + ".",
                    element.Id));
            }

            if (hasAuthoritativeOwner && entity.OwnerId != authoritativeOwnerId)
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_OWNER_SPACE_MISMATCH",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle " + canonicalHandle + " đã drift sang owner space/layout khác authoritative Grid source.",
                    element.Id));
            }

            if (!GeneratedGeometryService.HasMatchingOwnership(entity, project, element))
            {
                issues.Add(new ModelHealthIssue(
                    "GRID_ANNOTATION_CAD_OWNERSHIP_MISMATCH",
                    HealthSeverity.Error,
                    "Generated Grid annotation Handle " + canonicalHandle + " còn sống nhưng QS3D XData ownership không khớp project/Grid hiện tại.",
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

        private static void AddSourceMissing(ProjectElement element, string handle, ICollection<ModelHealthIssue> issues)
        {
            issues.Add(new ModelHealthIssue(
                "GRID_ANNOTATION_SOURCE_MISSING",
                HealthSeverity.Error,
                "Authoritative Grid source Handle không còn resolve tới live CAD entity cho runtime owner-space health: " + handle + ".",
                element.Id));
        }

        private static bool IsRecoverableDiagnosticFailure(Exception exception)
        {
            return !(exception is OutOfMemoryException) &&
                   !(exception is StackOverflowException) &&
                   !(exception is AccessViolationException);
        }
    }
}
