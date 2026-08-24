using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedRebarRegionOwnershipService
    {
        private const string RegAppName = "QS3D_REBAR_REGION";
        private const string OwnershipVersion = "1";
        private const int MaxRegionIdLength = 160;

        public static void MarkGenerated(
            Document document,
            Transaction transaction,
            Entity entity,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            string regionId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            var normalizedRegionId = NormalizeRegionId(regionId);
            EnsureRegApp(document.Database, transaction);
            var ownerSlot = GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim());
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Project(project.ProjectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Element(element.Id)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ownerSlot),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, normalizedRegionId)))
                entity.XData = marker;
        }

        public static void MarkFreshGeneratedHandles(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            string regionId,
            IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            var normalizedRegionId = NormalizeRegionId(regionId);
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
                MarkGenerated(document, transaction, entity, project, element, propertyKey, normalizedRegionId);
            }
        }

        public static void RequireMatchingOwnership(
            Entity entity,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            string regionId,
            string operation)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            var normalizedRegionId = NormalizeRegionId(regionId);
            if (HasMatchingOwnership(entity, project, element, propertyKey, normalizedRegionId)) return;
            throw new InvalidOperationException(
                "Refusing to " + (string.IsNullOrWhiteSpace(operation) ? "modify generated rebar" : operation.Trim()) +
                " because its QS3D region ownership marker does not match project " + project.ProjectId +
                ", element " + element.Id + ", owner slot " + GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim()) +
                ", region " + normalizedRegionId + ". Region-owned generated rebar must not be destructively erased by handle alone.");
        }

        public static bool HasMatchingOwnership(
            Entity entity,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            string regionId)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));

            var ownerSlot = GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(propertyKey.Trim());
            var normalizedRegionId = NormalizeRegionId(regionId);
            using (var marker = entity.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                return values.Length >= 6 &&
                    string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal) &&
                    GeneratedOwnershipIdentityToken.MatchesProject(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), project.ProjectId) &&
                    GeneratedOwnershipIdentityToken.MatchesElement(Convert.ToString(values[3].Value, CultureInfo.InvariantCulture), element.Id) &&
                    string.Equals(Convert.ToString(values[4].Value, CultureInfo.InvariantCulture), ownerSlot, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[5].Value, CultureInfo.InvariantCulture), normalizedRegionId, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string NormalizeRegionId(string regionId)
        {
            var normalized = (regionId ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Generated rebar regionId is required.", nameof(regionId));
            if (normalized.Length > MaxRegionIdLength)
                throw new ArgumentException("Generated rebar regionId exceeds the supported " + MaxRegionIdLength + " character limit.", nameof(regionId));
            for (var index = 0; index < normalized.Length; index++)
            {
                if (char.IsControl(normalized[index]))
                    throw new ArgumentException("Generated rebar regionId contains control characters.", nameof(regionId));
            }
            return normalized.ToUpperInvariant();
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
