using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticSelectionResolver
    {
        public static IReadOnlyList<ProjectElement> ResolveImplied(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return Array.Empty<ProjectElement>();
            var objectIds = selection.Value.GetObjectIds();
            if (objectIds.Length == 0) return Array.Empty<ProjectElement>();

            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var objectId in objectIds)
                {
                    var entity = transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var handle = entity.Handle.ToString();
                    if (!string.IsNullOrWhiteSpace(handle)) selectedHandles.Add(handle.Trim());
                }
            }
            return SemanticHandleOwnershipResolver.Resolve(project, selectedHandles);
        }
    }
}
