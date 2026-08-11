using System;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedCurtainPanelNativeOwnershipService
    {
        private const string RegAppName = "QS3D_CURTAIN_PANEL";
        private const string OwnershipVersion = "1";
        private const string HandlesKey = "GeneratedCurtainPanelHandles";

        public static void MarkGenerated(Document document, Transaction transaction, Entity entity, ProjectState project, ProjectElement element)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            EnsureRegApp(document.Database, transaction);
            var ownerSlot = GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(HandlesKey);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Project(project.ProjectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, GeneratedOwnershipIdentityToken.Element(element.Id)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ownerSlot)))
                entity.XData = marker;
        }

        public static void RequireMatchingOwnership(Entity entity, ProjectState project, ProjectElement element, string operation)
        {
            if (HasMatchingOwnership(entity, project, element)) return;
            throw new InvalidOperationException(
                "Refusing to " + (string.IsNullOrWhiteSpace(operation) ? "modify generated curtain panel" : operation.Trim()) +
                " because its QS3D curtain-panel ownership marker does not match project " + project.ProjectId +
                ", element " + element.Id + ", owner slot " + GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(HandlesKey) + ".");
        }

        public static bool HasMatchingOwnership(Entity entity, ProjectState project, ProjectElement element)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            var ownerSlot = GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(HandlesKey);
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
