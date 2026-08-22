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

            issues.AddRange(GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project));
            issues.AddRange(GeneratedSemanticTagRuntimeHealthService.Inspect(document, project));
            return issues.AsReadOnly();
        }
    }
}
