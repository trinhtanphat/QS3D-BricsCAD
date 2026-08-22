using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedRebarNativeOwnershipService
    {
        private const string RegAppName = "QS3D_REBAR";
        private const string OwnershipVersion = "1";

        public static void MarkGenerated(
            Document document,
            Transaction transaction,
            Entity entity,
            ProjectState project,
            ProjectElement element,
            string propertyKey)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            EnsureRegApp(document.Database, transaction);
            var ownerSlot = GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim());
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Project(project.ProjectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Element(element.Id)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ownerSlot)))
                entity.XData = marker;
        }

        public static void MarkFreshGeneratedHandles(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawHandle in handles)
            {
                var handle = (rawHandle ?? string.Empty).Trim();
                if (handle.Length == 0 || !seen.Add(handle)) continue;
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count != 1)
                    throw new InvalidOperationException("Fresh generated rebar handle " + handle + " must resolve to exactly one CAD object before commit.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased || !entity.IsNewObject)
                    throw new InvalidOperationException("Fresh generated rebar handle " + handle + " must identify a new live Entity in the current CAD transaction.");
                MarkGenerated(document, transaction, entity, project, element, propertyKey);
            }
        }

        public static void RequireMatchingOwnership(
            Entity entity,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            string operation)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            if (HasMatchingOwnership(entity, project, element, propertyKey)) return;
            throw new InvalidOperationException(
                "Refusing to " + (string.IsNullOrWhiteSpace(operation) ? "modify generated rebar" : operation.Trim()) +
                " because its QS3D rebar ownership marker does not match project " + project.ProjectId +
                ", element " + element.Id + ", owner slot " + GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim()) + ". " +
                "Legacy/unmarked generated rebar must not be destructively erased by handle alone.");
        }

        public static bool HasMatchingOwnership(Entity entity, ProjectState project, ProjectElement element, string propertyKey)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            var ownerSlot = GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim());
            using (var marker = entity.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                return values.Length >= 5 &&
                    string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal) &&
                    GeneratedOwnershipIdentityToken.MatchesProject(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), project.ProjectId) &&
                    GeneratedOwnershipIdentityToken.MatchesElement(Convert.ToString(values[3].Value, CultureInfo.InvariantCulture), element.Id) &&
                    string.Equals(Convert.ToString(values[4].Value, CultureInfo.InvariantCulture), ownerSlot, StringComparison.OrdinalIgnoreCase);
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
    }
}
