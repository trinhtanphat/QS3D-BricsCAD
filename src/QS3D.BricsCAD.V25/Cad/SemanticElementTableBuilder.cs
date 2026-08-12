using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticElementTableBuilder
    {
        internal const string DocumentId = "SemanticElementSchedule";
        internal const string HandleKey = "GeneratedSemanticElementTableHandle";
        internal const string OwnerProjectKey = "GeneratedSemanticElementTableOwnerProjectId";
        internal const string OwnershipVersionKey = "GeneratedSemanticElementTableOwnershipVersion";
        internal const string FingerprintKey = "GeneratedSemanticElementTableFingerprint";
        internal const string PositionXKey = "GeneratedSemanticElementTablePositionX";
        internal const string PositionYKey = "GeneratedSemanticElementTablePositionY";
        internal const string PositionZKey = "GeneratedSemanticElementTablePositionZ";
        internal const string RowCountKey = "GeneratedSemanticElementTableRowCount";
        internal const string ColumnCountKey = "GeneratedSemanticElementTableColumnCount";

        private const string RegAppName = "QS3DDOC";
        private const string OwnershipVersion = "1";
        private const string ProjectIdentityTokenPrefix = "p1:";
        private const string DocumentKind = "SemanticElementTable";
        private const double TextHeightM = 0.0035d;
        private const double RowHeightM = 0.008d;
        private const double ColumnWidthM = 0.035d;

        private static readonly SemanticDocumentationColumn[] Columns =
        {
            new SemanticDocumentationColumn("Id", "{Id}"),
            new SemanticDocumentationColumn("Category", "{Category}"),
            new SemanticDocumentationColumn("Family", "{Family}"),
            new SemanticDocumentationColumn("Floor", "{Floor}"),
            new SemanticDocumentationColumn("Zone", "{Zone}")
        };

        public static string Build(Document document, ProjectState project, Point3d position)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic element Table yêu cầu DWG đích vẫn là MdiActiveDocument.");
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Semantic element Table P0 chỉ hỗ trợ ModelSpace.");
            RequireFinite(position);

            var semanticTable = BuildSnapshot(project);
            if (semanticTable.Rows.Count == 0)
                throw new InvalidOperationException("Project chưa có semantic element để tạo native element schedule.");
            var fingerprint = ComputeFingerprint(semanticTable);
            ValidatePersistedState(project);

            var snapshot = ProjectStateSnapshot.Capture(project);
            var committed = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var modelSpace = OpenModelSpace(document.Database, transaction, OpenMode.ForWrite);
                    ErasePrevious(document, transaction, project);

                    var table = new Table();
                    table.SetDatabaseDefaults(document.Database);
                    table.Position = position;
                    table.SetSize(semanticTable.Rows.Count + 2, semanticTable.Headers.Count);

                    var textHeight = CadUnitService.MetersToDrawingUnits(document, TextHeightM);
                    var rowHeight = CadUnitService.MetersToDrawingUnits(document, RowHeightM);
                    var columnWidth = CadUnitService.MetersToDrawingUnits(document, ColumnWidthM);
                    RequirePositiveFinite(textHeight, "table text height");
                    RequirePositiveFinite(rowHeight, "table row height");
                    RequirePositiveFinite(columnWidth, "table column width");

                    table.SetRowHeight(rowHeight);
                    table.SetColumnWidth(columnWidth);
                    table.SetTextString(0, 0, semanticTable.Title);
                    table.SetTextHeight(0, 0, textHeight);
                    for (var column = 1; column < semanticTable.Headers.Count; column++)
                        table.SetTextString(0, column, string.Empty);
                    for (var column = 0; column < semanticTable.Headers.Count; column++)
                    {
                        table.SetTextString(1, column, semanticTable.Headers[column]);
                        table.SetTextHeight(1, column, textHeight);
                    }
                    for (var row = 0; row < semanticTable.Rows.Count; row++)
                    {
                        for (var column = 0; column < semanticTable.Headers.Count; column++)
                        {
                            table.SetTextString(row + 2, column, semanticTable.Rows[row].Cells[column]);
                            table.SetTextHeight(row + 2, column, textHeight);
                        }
                    }
                    table.GenerateLayout();
                    modelSpace.AppendEntity(table);
                    transaction.AddNewlyCreatedDBObject(table, true);
                    MarkOwned(document.Database, transaction, table, project.ProjectId, fingerprint);

                    project.Metadata[HandleKey] = table.Handle.ToString();
                    project.Metadata[OwnerProjectKey] = project.ProjectId;
                    project.Metadata[OwnershipVersionKey] = OwnershipVersion;
                    project.Metadata[FingerprintKey] = fingerprint;
                    project.Metadata[PositionXKey] = Format(position.X);
                    project.Metadata[PositionYKey] = Format(position.Y);
                    project.Metadata[PositionZKey] = Format(position.Z);
                    project.Metadata[RowCountKey] = semanticTable.Rows.Count.ToString(CultureInfo.InvariantCulture);
                    project.Metadata[ColumnCountKey] = semanticTable.Headers.Count.ToString(CultureInfo.InvariantCulture);
                    AuditTrail.ForProject(project).Record("BuildSemanticElementTable", string.Empty, "Generated native Table " + table.Handle + " from " + semanticTable.Rows.Count.ToString(CultureInfo.InvariantCulture) + " semantic elements.");

                    transaction.Commit();
                    committed = true;
                    return table.Handle.ToString();
                }
            }
            catch (Exception operationError)
            {
                if (!committed)
                {
                    try { snapshot.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Semantic element Table build failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static void Remove(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Semantic element Table remove yêu cầu DWG đích vẫn là MdiActiveDocument.");
            ValidatePersistedState(project);
            if (!project.Metadata.ContainsKey(HandleKey)) return;

            var snapshot = ProjectStateSnapshot.Capture(project);
            var committed = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    ErasePrevious(document, transaction, project);
                    foreach (var key in StateKeys) project.Metadata.Remove(key);
                    AuditTrail.ForProject(project).Record("RemoveSemanticElementTable", string.Empty, "Removed project-owned native semantic element Table metadata/entity.");
                    transaction.Commit();
                    committed = true;
                }
            }
            catch (Exception operationError)
            {
                if (!committed)
                {
                    try { snapshot.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Semantic element Table removal failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static SemanticDocumentationTable BuildSnapshot(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var ids = project.Elements
                .Select(x => x?.Id ?? throw new InvalidOperationException("Project contains a null semantic element."))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return SemanticDocumentationTableBuilder.Build(project, "QS3D Semantic Element Schedule", ids, Columns);
        }

        public static Point3d StoredPosition(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidatePersistedState(project);
            if (!project.Metadata.ContainsKey(HandleKey)) throw new InvalidOperationException("Project chưa có generated semantic element Table.");
            return new Point3d(
                ParseFinite(project.Metadata[PositionXKey], PositionXKey),
                ParseFinite(project.Metadata[PositionYKey], PositionYKey),
                ParseFinite(project.Metadata[PositionZKey], PositionZKey));
        }

        public static string ExpectedFingerprint(ProjectState project) => ComputeFingerprint(BuildSnapshot(project));

        public static IReadOnlyList<string> ValidateRuntime(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<string>();
            var hasAny = StateKeys.Any(project.Metadata.ContainsKey);
            if (!hasAny) return issues.AsReadOnly();

            try { ValidatePersistedState(project); }
            catch (Exception ex)
            {
                issues.Add("SEMANTIC_ELEMENT_TABLE_METADATA_INVALID: " + ex.Message);
                return issues.AsReadOnly();
            }

            var storedFingerprint = project.Metadata[FingerprintKey].Trim();
            try
            {
                if (!string.Equals(storedFingerprint, ExpectedFingerprint(project), StringComparison.OrdinalIgnoreCase))
                    issues.Add("SEMANTIC_ELEMENT_TABLE_STALE: semantic table content no longer matches the generated snapshot.");
            }
            catch (Exception ex) { issues.Add("SEMANTIC_ELEMENT_TABLE_RENDER_INVALID: " + ex.Message); }

            var handle = project.Metadata[HandleKey].Trim();
            if (!TryResolve(document.Database, handle, out var id))
            {
                issues.Add("SEMANTIC_ELEMENT_TABLE_MISSING: generated Table handle " + handle + " is not live.");
                return issues.AsReadOnly();
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                    issues.Add("SEMANTIC_ELEMENT_TABLE_MISSING: generated Table handle " + handle + " is erased or unavailable.");
                else if (!(entity is Table table))
                    issues.Add("SEMANTIC_ELEMENT_TABLE_WRONG_TYPE: generated handle " + handle + " is not a Table.");
                else if (!HasMatchingOwnership(table, project.ProjectId, storedFingerprint))
                    issues.Add("SEMANTIC_ELEMENT_TABLE_OWNERSHIP_MISMATCH: native Table QS3DDOC XData does not match project metadata.");
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static readonly string[] StateKeys =
        {
            HandleKey, OwnerProjectKey, OwnershipVersionKey, FingerprintKey,
            PositionXKey, PositionYKey, PositionZKey, RowCountKey, ColumnCountKey
        };

        private static void ValidatePersistedState(ProjectState project)
        {
            var present = StateKeys.Count(project.Metadata.ContainsKey);
            if (present == 0) return;
            if (present != StateKeys.Length)
                throw new InvalidOperationException("Generated semantic element table metadata is partial. Refusing destructive replacement.");
            foreach (var key in StateKeys)
                if (string.IsNullOrWhiteSpace(project.Metadata[key])) throw new InvalidOperationException(key + " is empty.");
            if (!string.Equals(project.Metadata[OwnerProjectKey].Trim(), (project.ProjectId ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Generated semantic element table owner project does not match the active project.");
            if (!string.Equals(project.Metadata[OwnershipVersionKey].Trim(), OwnershipVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported semantic element table ownership version: " + project.Metadata[OwnershipVersionKey]);
            _ = ParseFinite(project.Metadata[PositionXKey], PositionXKey);
            _ = ParseFinite(project.Metadata[PositionYKey], PositionYKey);
            _ = ParseFinite(project.Metadata[PositionZKey], PositionZKey);
            if (!int.TryParse(project.Metadata[RowCountKey], NumberStyles.None, CultureInfo.InvariantCulture, out var rowCount) || rowCount < 0)
                throw new InvalidOperationException(RowCountKey + " is invalid.");
            if (!int.TryParse(project.Metadata[ColumnCountKey], NumberStyles.None, CultureInfo.InvariantCulture, out var columnCount) || columnCount != Columns.Length)
                throw new InvalidOperationException(ColumnCountKey + " is invalid.");
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project)
        {
            if (!project.Metadata.TryGetValue(HandleKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var handle = raw.Trim();
            if (!TryResolve(document.Database, handle, out var id)) return;
            var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
            if (entity == null || entity.IsErased) return;
            if (!(entity is Table table))
                throw new InvalidOperationException("Generated semantic element table handle " + handle + " resolves to a live non-Table object. Refusing replacement.");
            var storedFingerprint = project.Metadata[FingerprintKey].Trim();
            if (!HasMatchingOwnership(table, project.ProjectId, storedFingerprint))
                throw new InvalidOperationException("Refusing to erase Table " + handle + " because QS3DDOC ownership/fingerprint does not match persisted project metadata.");
            table.Erase();
        }

        private static BlockTableRecord OpenModelSpace(Database database, Transaction transaction, OpenMode mode)
        {
            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], mode);
        }

        private static void MarkOwned(Database database, Transaction transaction, Table table, string projectId, string fingerprint)
        {
            EnsureRegApp(database, transaction);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ProjectIdentityToken(projectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, DocumentId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, DocumentKind),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, fingerprint)))
                table.XData = marker;
        }

        private static bool HasMatchingOwnership(Table table, string projectId, string fingerprint)
        {
            using (var marker = table.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                return values.Length >= 6 &&
                    string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal) &&
                    MatchesProjectIdentity(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), projectId) &&
                    string.Equals(Convert.ToString(values[3].Value, CultureInfo.InvariantCulture), DocumentId, StringComparison.Ordinal) &&
                    string.Equals(Convert.ToString(values[4].Value, CultureInfo.InvariantCulture), DocumentKind, StringComparison.Ordinal) &&
                    string.Equals(Convert.ToString(values[5].Value, CultureInfo.InvariantCulture), fingerprint, StringComparison.OrdinalIgnoreCase);
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

        private static bool TryResolve(Database database, string handle, out ObjectId id)
        {
            id = ObjectId.Null;
            if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return false;
            try
            {
                id = database.GetObjectId(false, new Handle(value), 0);
                return !id.IsNull && id.IsValid;
            }
            catch { return false; }
        }

        private static string ProjectIdentityToken(string projectId)
        {
            var normalized = (projectId ?? string.Empty).Trim();
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                var result = new StringBuilder(ProjectIdentityTokenPrefix.Length + hash.Length * 2);
                result.Append(ProjectIdentityTokenPrefix);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static bool MatchesProjectIdentity(string storedIdentity, string projectId)
        {
            var normalized = (projectId ?? string.Empty).Trim();
            return string.Equals(storedIdentity, ProjectIdentityToken(normalized), StringComparison.Ordinal) ||
                string.Equals(storedIdentity, normalized, StringComparison.Ordinal);
        }

        private static string ComputeFingerprint(SemanticDocumentationTable table)
        {
            var builder = new StringBuilder();
            Append(builder, table.Title);
            foreach (var header in table.Headers) Append(builder, header);
            foreach (var row in table.Rows)
            {
                Append(builder, row.ElementId);
                foreach (var cell in row.Cells) Append(builder, cell);
            }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var result = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static void Append(StringBuilder builder, string value)
        {
            var text = value ?? string.Empty;
            builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append(';');
        }

        private static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Generated table position must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static double ParseFinite(string raw, string key)
        {
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(key + " is not a finite invariant number.");
            return value;
        }

        private static void RequireFinite(Point3d point)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) ||
                double.IsNaN(point.Y) || double.IsInfinity(point.Y) ||
                double.IsNaN(point.Z) || double.IsInfinity(point.Z))
                throw new InvalidOperationException("Generated table placement must be finite.");
        }

        private static void RequirePositiveFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new InvalidOperationException(label + " must be positive and finite.");
        }
    }
}
