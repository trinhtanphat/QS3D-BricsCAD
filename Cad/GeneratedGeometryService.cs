using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedGeometryService
    {
        private const string RegAppName = "QS3D";
        private const string HandleKey = "GeneratedSolidHandle";
        private const string CategoryKey = "GeneratedSolidCategory";
        private const string OwnerProjectKey = "GeneratedSolidOwnerProjectId";
        private const string OwnerElementKey = "GeneratedSolidOwnerElementId";
        private const string OwnershipVersionKey = "GeneratedSolidOwnershipVersion";
        private const string OwnershipVersion = "1";

        public static string PrepareReplacement(Document document, Transaction transaction, ProjectState project, ProjectElement element)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!element.Properties.TryGetValue(HandleKey, out var text) || string.IsNullOrWhiteSpace(text)) return string.Empty;

            var normalized = text.Trim();
            if (!long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException("GeneratedSolidHandle is invalid for " + element.Id + ": " + normalized);

            ObjectId id;
            try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
            catch { return normalized; }

            if (id.IsNull || !id.IsValid) return normalized;
            var entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
            if (entity == null || entity.IsErased) return normalized;
            var solid = entity as Solid3d;
            if (solid == null)
                throw new InvalidOperationException("GeneratedSolidHandle " + normalized + " for " + element.Id + " resolves to a live non-Solid3d object. Refusing to orphan or overwrite generated geometry ownership.");
            RequireMatchingOwnership(solid, project, element, "erase Solid3d " + normalized);
            solid.Erase();
            return normalized;
        }

        public static void MarkGenerated(Document document, Transaction transaction, Entity entity, string projectId, string elementId, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("Project id is required.", nameof(projectId));
            if (string.IsNullOrWhiteSpace(elementId)) throw new ArgumentException("Element id is required.", nameof(elementId));

            EnsureRegApp(document.Database, transaction);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Project(projectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Element(elementId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, category.ToString())))
                entity.XData = marker;
        }

        public static IReadOnlyList<string> FindMatchingOwnedHandles(Document document, string projectId, string elementId, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("Project id is required.", nameof(projectId));
            if (string.IsNullOrWhiteSpace(elementId)) throw new ArgumentException("Element id is required.", nameof(elementId));

            var normalizedProjectId = projectId.Trim();
            var normalizedElementId = elementId.Trim();
            var result = new List<string>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in modelSpace)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    if (HasMatchingOwnership(entity, normalizedProjectId, normalizedElementId, category))
                        result.Add(entity.Handle.ToString());
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        public static bool HasMatchingOwnership(Entity entity, ProjectState project, ProjectElement element)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            return HasMatchingOwnership(entity, project.ProjectId, element.Id, element.Category);
        }

        public static void RequireMatchingOwnership(Entity entity, ProjectState project, ProjectElement element, string operation)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (HasMatchingOwnership(entity, project, element)) return;
            throw new InvalidOperationException(
                "Refusing to " + (string.IsNullOrWhiteSpace(operation) ? "modify generated geometry" : operation.Trim()) +
                " because its QS3D ownership marker does not match project " + project.ProjectId + ", element " + element.Id + ".");
        }

        public static void CommitReplacement(ProjectState project, ProjectElement element, string previousHandle, string generatedHandle, ElementCategory category)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(generatedHandle)) throw new ArgumentException("Generated solid handle is required.", nameof(generatedHandle));

            RemoveFromSourceHandles(element, previousHandle);
            RemoveFromSourceHandles(element, generatedHandle);
            element.Properties[HandleKey] = generatedHandle.Trim();
            element.Properties[CategoryKey] = category.ToString();
            element.Properties[OwnerProjectKey] = project.ProjectId;
            element.Properties[OwnerElementKey] = element.Id;
            element.Properties[OwnershipVersionKey] = OwnershipVersion;
            element.ClearGeneratedSolidStale();
            element.MarkClean(ElementDirtyFlags.Geometry);
        }

        private static bool HasMatchingOwnership(Entity entity, string projectId, string elementId, ElementCategory category)
        {
            using (var marker = entity.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                return values.Length >= 5 &&
                    string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal) &&
                    GeneratedOwnershipIdentityToken.MatchesProject(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), projectId) &&
                    GeneratedOwnershipIdentityToken.MatchesElement(Convert.ToString(values[3].Value, CultureInfo.InvariantCulture), elementId) &&
                    string.Equals(Convert.ToString(values[4].Value, CultureInfo.InvariantCulture), category.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void EnsureRegApp(Database database, Transaction transaction)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        private static void RemoveFromSourceHandles(ProjectElement element, string? handle)
        {
            if (string.IsNullOrWhiteSpace(handle)) return;
            for (var index = element.SourceHandles.Count - 1; index >= 0; index--)
                if (string.Equals(element.SourceHandles[index], handle, StringComparison.OrdinalIgnoreCase)) element.SourceHandles.RemoveAt(index);
        }
    }
}
