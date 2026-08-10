using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedSolidRuntimeHealthService
    {
        private const string HandleKey = "GeneratedSolidHandle";

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            AddProviderSafely(
                issues,
                "GeneratedSolidOwnershipRuntimeHealth",
                () => InspectGeneratedSolidOwnership(document, project));
            AddProviderSafely(
                issues,
                "GeneratedGridAnnotationRuntimeHealthService",
                () => GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project));
            AddProviderSafely(
                issues,
                "GeneratedSemanticTagRuntimeHealthService",
                () => GeneratedSemanticTagRuntimeHealthService.Inspect(document, project));
            AddProviderSafely(
                issues,
                "GeneratedSemanticElementTableRuntimeHealthService",
                () => GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project));
            AddProviderSafely(
                issues,
                "DoorOpeningNativeTableBuilder",
                () => DoorOpeningNativeTableBuilder.Inspect(document, project));
            AddProviderSafely(
                issues,
                "RoomFinishNativeTableBuilder",
                () => RoomFinishNativeTableBuilder.Inspect(document, project));
            AddProviderSafely(
                issues,
                "MaterialUsageNativeTableBuilder",
                () => MaterialUsageNativeTableBuilder.Inspect(document, project));
            AddProviderSafely(
                issues,
                "BqNativeTableBuilder",
                () => BqNativeTableBuilder.Inspect(document, project));
            return issues.AsReadOnly();
        }

        private static IReadOnlyList<ModelHealthIssue> InspectGeneratedSolidOwnership(Document document, ProjectState project)
        {
            var issues = new List<ModelHealthIssue>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue(HandleKey, out var rawHandle) || string.IsNullOrWhiteSpace(rawHandle)) continue;
                    var handle = rawHandle.Trim();
                    if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;

                    ObjectId id;
                    try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                    catch { continue; }
                    if (id.IsNull || !id.IsValid) continue;

                    Entity? entity;
                    try { entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; }
                    catch { continue; }
                    if (entity == null || entity.IsErased || !(entity is Solid3d)) continue;

                    if (!GeneratedGeometryService.HasMatchingOwnership(entity, project, element))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_OWNERSHIP_MISMATCH",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle trỏ tới Solid3d còn sống nhưng XData ownership không khớp project/element/category hiện tại. Health chỉ báo lỗi và không sửa/xóa đối tượng CAD này.",
                            element.Id));
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static void AddProviderSafely(
            ICollection<ModelHealthIssue> target,
            string providerName,
            Func<IReadOnlyList<ModelHealthIssue>> provider)
        {
            try
            {
                foreach (var issue in provider())
                    if (issue != null) target.Add(issue);
            }
            catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                target.Add(new ModelHealthIssue(
                    "RUNTIME_HEALTH_PROVIDER_FAILED",
                    HealthSeverity.Error,
                    providerName + " không thể hoàn tất native diagnostic: " + ex.Message));
            }
        }

        private static bool IsRecoverableDiagnosticFailure(System.Exception exception)
        {
            return !(exception is OutOfMemoryException) &&
                   !(exception is StackOverflowException) &&
                   !(exception is AccessViolationException);
        }
    }
}
