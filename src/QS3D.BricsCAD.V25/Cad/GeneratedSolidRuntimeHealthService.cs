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
            AddProviderSafely(
                issues,
                "BbsNativeTableBuilder",
                () => BbsNativeTableBuilder.Inspect(document, project));
            return issues.AsReadOnly();
        }

        private static IReadOnlyList<ModelHealthIssue> InspectGeneratedSolidOwnership(Document document, ProjectState project)
        {
            var issues = new List<ModelHealthIssue>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements)
                {
                    if (element == null) continue;
                    if (!element.Properties.TryGetValue(HandleKey, out var rawHandle) || string.IsNullOrWhiteSpace(rawHandle)) continue;
                    var handle = rawHandle.Trim();
                    if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_HANDLE_INVALID",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle không phải handle hex hợp lệ. Health chỉ báo lỗi và không sửa metadata/project.",
                            element.Id));
                        continue;
                    }

                    ObjectId id;
                    try
                    {
                        id = document.Database.GetObjectId(false, new Handle(value), 0);
                    }
                    catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_HANDLE_UNRESOLVED",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle không resolve được tới đối tượng CAD hiện tại: " + ex.Message,
                            element.Id));
                        continue;
                    }

                    if (id.IsNull || !id.IsValid)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_HANDLE_UNRESOLVED",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle không resolve được tới ObjectId hợp lệ trong database hiện tại.",
                            element.Id));
                        continue;
                    }

                    DBObject? dbObject;
                    try
                    {
                        dbObject = transaction.GetObject(id, OpenMode.ForRead, true);
                    }
                    catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_ENTITY_UNREADABLE",
                            HealthSeverity.Error,
                            "Đối tượng CAD được GeneratedSolidHandle tham chiếu không thể đọc trong health inspection: " + ex.Message,
                            element.Id));
                        continue;
                    }

                    if (dbObject == null)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_ENTITY_UNREADABLE",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle resolve được ObjectId nhưng không đọc được đối tượng CAD tương ứng.",
                            element.Id));
                        continue;
                    }

                    if (dbObject.IsErased)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_ENTITY_ERASED",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle đang trỏ tới đối tượng CAD đã bị erase. Health chỉ báo lỗi và không tự sửa/xóa metadata.",
                            element.Id));
                        continue;
                    }

                    if (!(dbObject is Solid3d entity))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GENERATED_SOLID_ENTITY_TYPE_MISMATCH",
                            HealthSeverity.Error,
                            "GeneratedSolidHandle đang trỏ tới đối tượng CAD không phải Solid3d.",
                            element.Id));
                        continue;
                    }

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
