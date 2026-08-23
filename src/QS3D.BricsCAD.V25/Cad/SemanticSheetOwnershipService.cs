using System;
using System.Globalization;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticSheetOwnershipService
    {
        internal const string RegAppName = "QS3D_SHEET";
        internal const string OwnershipVersion = "1";
        internal const string ArtifactLayout = "Layout";
        internal const string ArtifactPaperSpace = "PaperSpace";
        internal const string ArtifactViewport = "Viewport";
        internal const string ArtifactTitleBlock = "TitleBlock";

        private const string ProjectPrefix = "Project:";
        private const string SheetPrefix = "Sheet:";
        private const string ArtifactPrefix = "Artifact:";
        private const string ViewPrefix = "View:";

        public static void Mark(
            Database database,
            Transaction transaction,
            DBObject target,
            string projectId,
            string sheetId,
            string artifactKind,
            string? viewId = null)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var project = Required(projectId, nameof(projectId));
            var sheet = Required(sheetId, nameof(sheetId));
            var artifact = RequiredArtifact(artifactKind);
            var view = Optional(viewId);
            if (string.Equals(artifact, ArtifactViewport, StringComparison.Ordinal) && view == null)
                throw new InvalidOperationException("Semantic sheet viewport ownership requires a view id.");
            if (!string.Equals(artifact, ArtifactViewport, StringComparison.Ordinal) && view != null)
                throw new InvalidOperationException("Only semantic sheet viewport ownership may carry a view id.");

            EnsureRegApp(database, transaction);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ProjectPrefix + project),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, SheetPrefix + sheet),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ArtifactPrefix + artifact),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ViewPrefix + (view ?? string.Empty))))
                target.XData = marker;
        }

        public static bool HasMatching(
            DBObject target,
            string projectId,
            string sheetId,
            string artifactKind,
            string? viewId = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var expectedProject = Required(projectId, nameof(projectId));
            var expectedSheet = Required(sheetId, nameof(sheetId));
            var expectedArtifact = RequiredArtifact(artifactKind);
            var expectedView = Optional(viewId) ?? string.Empty;

            using (var marker = target.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                if (values.Length != 6) return false;
                return String(values[0]).Equals(RegAppName, StringComparison.OrdinalIgnoreCase) &&
                       String(values[1]).Equals(OwnershipVersion, StringComparison.Ordinal) &&
                       String(values[2]).Equals(ProjectPrefix + expectedProject, StringComparison.OrdinalIgnoreCase) &&
                       String(values[3]).Equals(SheetPrefix + expectedSheet, StringComparison.OrdinalIgnoreCase) &&
                       String(values[4]).Equals(ArtifactPrefix + expectedArtifact, StringComparison.Ordinal) &&
                       String(values[5]).Equals(ViewPrefix + expectedView, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool TryRead(
            DBObject target,
            out string projectId,
            out string sheetId,
            out string artifactKind,
            out string viewId)
        {
            projectId = string.Empty;
            sheetId = string.Empty;
            artifactKind = string.Empty;
            viewId = string.Empty;
            if (target == null) return false;

            using (var marker = target.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                if (values.Length != 6 ||
                    !String(values[0]).Equals(RegAppName, StringComparison.OrdinalIgnoreCase) ||
                    !String(values[1]).Equals(OwnershipVersion, StringComparison.Ordinal))
                    return false;
                if (!TryStrip(String(values[2]), ProjectPrefix, out projectId) ||
                    !TryStrip(String(values[3]), SheetPrefix, out sheetId) ||
                    !TryStrip(String(values[4]), ArtifactPrefix, out artifactKind) ||
                    !TryStrip(String(values[5]), ViewPrefix, out viewId))
                    return false;
                if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(sheetId)) return false;
                try { RequiredArtifact(artifactKind); }
                catch (InvalidOperationException) { return false; }
                if (string.Equals(artifactKind, ArtifactViewport, StringComparison.Ordinal) != !string.IsNullOrWhiteSpace(viewId))
                    return false;
                return true;
            }
        }

        public static void RequireMatching(
            DBObject target,
            string projectId,
            string sheetId,
            string artifactKind,
            string operation,
            string? viewId = null)
        {
            if (HasMatching(target, projectId, sheetId, artifactKind, viewId)) return;
            throw new InvalidOperationException(
                "Refusing to " + (string.IsNullOrWhiteSpace(operation) ? "modify semantic sheet artifact" : operation.Trim()) +
                " because QS3D_SHEET ownership does not match project " + projectId + ", sheet " + sheetId +
                ", artifact " + artifactKind + (string.IsNullOrWhiteSpace(viewId) ? string.Empty : ", view " + viewId) + ".");
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

        private static string Required(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            return value!.Trim();
        }

        private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();

        private static string RequiredArtifact(string? value)
        {
            var artifact = Required(value, nameof(value));
            if (string.Equals(artifact, ArtifactLayout, StringComparison.Ordinal) ||
                string.Equals(artifact, ArtifactPaperSpace, StringComparison.Ordinal) ||
                string.Equals(artifact, ArtifactViewport, StringComparison.Ordinal) ||
                string.Equals(artifact, ArtifactTitleBlock, StringComparison.Ordinal))
                return artifact;
            throw new InvalidOperationException("Unsupported semantic sheet artifact kind: " + artifact + ".");
        }

        private static string String(TypedValue value) => Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;

        private static bool TryStrip(string value, string prefix, out string result)
        {
            result = string.Empty;
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            result = value.Substring(prefix.Length);
            return true;
        }
    }
}
