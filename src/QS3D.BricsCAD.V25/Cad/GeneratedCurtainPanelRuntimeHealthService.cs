using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedCurtainPanelRuntimeHealthService
    {
        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue("GeneratedCurtainPanelHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0))
                    {
                        var canonical = CadHandleService.NormalizeHexHandle(token);
                        if (canonical == null) continue;
                        var ids = CadHandleService.Resolve(document, new[] { canonical });
                        if (ids.Count != 1) continue;
                        var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
                        if (!(entity is Solid3d solid) || solid.IsErased) continue;
                        if (!GeneratedCurtainPanelNativeOwnershipService.HasMatchingOwnership(solid, project, element))
                            issues.Add(new ModelHealthIssue(
                                "CURTAIN_PANEL_NATIVE_OWNERSHIP_MISMATCH",
                                HealthSeverity.Error,
                                "Live curtain panel Solid3d is missing the matching QS3D_CURTAIN_PANEL project/element/owner-slot marker: " + canonical + ".",
                                element.Id));
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }
    }
}
