using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticTagRemovalService
    {
        public static int Remove(Document document, ProjectState project, ProjectElement element)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic tag remove yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var unique = project.Elements.Where(x => string.Equals(x.Id, element.Id, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if (unique.Count != 1 || !ReferenceEquals(unique[0], element))
                throw new InvalidOperationException("Semantic tag element id không unique trong project: " + element.Id + ".");

            if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                return 0;

            var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (handles.Length == 0)
                throw new InvalidOperationException("GeneratedSemanticTagHandles không có handle hợp lệ để remove cho " + element.Id + ".");

            var ownership = GeneratedHandleOwnershipIndex.Build(project);
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            var erased = 0;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    foreach (var handle in handles)
                    {
                        EnsureOwnedBySemanticTag(ownership, element, handle);
                        var id = ResolveHandle(document.Database, handle, allowMissing: true);
                        if (id.IsNull || !id.IsValid) continue;

                        var entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
                        if (entity == null || entity.IsErased) continue;
                        if (!(entity is MText))
                            throw new InvalidOperationException("Generated semantic tag handle " + handle + " là live CAD nhưng không phải MText. Refusing destructive remove.");
                        GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "remove semantic tag " + handle);
                        entity.Erase();
                        erased++;
                    }

                    ClearGeneratedTagMetadata(element);
                    AuditTrail.ForProject(project).Record(
                        "documentation.semantic-tag.remove",
                        element.Id,
                        erased.ToString(CultureInfo.InvariantCulture) + " live MText erased; tag ownership metadata cleared");
                    project.Touch();
                    transaction.Commit();
                    cadCommitted = true;
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
                            "Semantic tag remove failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return erased;
        }

        private static void EnsureOwnedBySemanticTag(GeneratedHandleOwnershipIndex ownership, ProjectElement element, string handle)
        {
            if (!ownership.TryFindOwner(handle, out var owner, out var slot) || owner == null ||
                !ReferenceEquals(owner, element) ||
                !string.Equals(
                    GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot),
                    GeneratedSemanticTagHealthService.HandlesKey,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Refusing semantic tag remove because generated handle ownership is not " +
                    element.Id + "/" + GeneratedSemanticTagHealthService.HandlesKey + ": " + handle + ".");
        }

        private static void ClearGeneratedTagMetadata(ProjectElement element)
        {
            var keys = element.Properties.Keys
                .Where(x => x.StartsWith("GeneratedSemanticTag", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var key in keys) element.Properties.Remove(key);
        }

        private static ObjectId ResolveHandle(Database database, string text, bool allowMissing)
        {
            if (!long.TryParse((text ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException("Generated semantic tag Handle không hợp lệ: " + text + ".");
            try
            {
                var id = database.GetObjectId(false, new Handle(value), 0);
                if (!id.IsNull && id.IsValid) return id;
            }
            catch
            {
                if (!allowMissing) throw;
            }
            if (allowMissing) return ObjectId.Null;
            throw new InvalidOperationException("Không resolve được generated semantic tag Handle: " + text + ".");
        }
    }
}
