using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class NativeDocumentationTableSnapshot
    {
        public NativeDocumentationTableSnapshot(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            Title = title ?? string.Empty;
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }

        public string Title { get; }
        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
    }

    internal sealed class ProjectOwnedNativeTableDefinition
    {
        public ProjectOwnedNativeTableDefinition(string documentId, string documentKind, string metadataPrefix, double textHeightM, double rowHeightM, double columnWidthM)
        {
            DocumentId = Required(documentId, nameof(documentId), 96);
            DocumentKind = Required(documentKind, nameof(documentKind), 96);
            MetadataPrefix = Required(metadataPrefix, nameof(metadataPrefix), 96);
            TextHeightM = Positive(textHeightM, nameof(textHeightM));
            RowHeightM = Positive(rowHeightM, nameof(rowHeightM));
            ColumnWidthM = Positive(columnWidthM, nameof(columnWidthM));
        }

        public string DocumentId { get; }
        public string DocumentKind { get; }
        public string MetadataPrefix { get; }
        public double TextHeightM { get; }
        public double RowHeightM { get; }
        public double ColumnWidthM { get; }

        public string HandleKey => MetadataPrefix + "Handle";
        public string OwnerProjectKey => MetadataPrefix + "OwnerProjectId";
        public string OwnershipVersionKey => MetadataPrefix + "OwnershipVersion";
        public string FingerprintKey => MetadataPrefix + "Fingerprint";
        public string PositionXKey => MetadataPrefix + "PositionX";
        public string PositionYKey => MetadataPrefix + "PositionY";
        public string PositionZKey => MetadataPrefix + "PositionZ";
        public string RowCountKey => MetadataPrefix + "RowCount";
        public string ColumnCountKey => MetadataPrefix + "ColumnCount";

        public IReadOnlyList<string> StateKeys => new[]
        {
            HandleKey, OwnerProjectKey, OwnershipVersionKey, FingerprintKey,
            PositionXKey, PositionYKey, PositionZKey, RowCountKey, ColumnCountKey
        };

        private static string Required(string value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }

        private static double Positive(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new ArgumentOutOfRangeException(name, "Value must be finite and > 0.");
            return value;
        }
    }

    internal static class ProjectOwnedNativeTableArtifactService
    {
        private const string RegAppName = "QS3DDOC";
        private const string OwnershipVersion = "1";
        private const string ProjectIdentityTokenPrefix = "p1:";
        private const int MaxRows = 5000;
        private const int MaxColumns = 32;
        private const int MaxCellLength = 4096;
        private const int MaxDetailedCellIssues = 32;

        public static string Build(
            Document document,
            ProjectState project,
            ProjectOwnedNativeTableDefinition definition,
            NativeDocumentationTableSnapshot snapshot,
            Point3d position)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Native documentation Table yêu cầu DWG đích vẫn là MdiActiveDocument.");
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Native documentation Table P0 chỉ hỗ trợ ModelSpace.");
            ValidatePoint(position);
            ValidateSnapshot(snapshot);
            ValidatePersistedState(project, definition);
            var fingerprint = ComputeFingerprint(snapshot);

            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var modelSpace = OpenModelSpace(document.Database, transaction, OpenMode.ForWrite);
                    ErasePrevious(document, transaction, project, definition);

                    var table = new Table();
                    table.SetDatabaseDefaults(document.Database);
                    table.Position = position;
                    table.SetSize(snapshot.Rows.Count + 2, snapshot.Headers.Count);

                    var textHeight = PositiveDrawing(CadUnitService.MetersToDrawingUnits(document, definition.TextHeightM), "table text height");
                    var rowHeight = PositiveDrawing(CadUnitService.MetersToDrawingUnits(document, definition.RowHeightM), "table row height");
                    var columnWidth = PositiveDrawing(CadUnitService.MetersToDrawingUnits(document, definition.ColumnWidthM), "table column width");
                    table.SetRowHeight(rowHeight);
                    table.SetColumnWidth(columnWidth);

                    table.SetTextString(0, 0, snapshot.Title);
                    table.SetTextHeight(0, 0, textHeight);
                    for (var column = 1; column < snapshot.Headers.Count; column++) table.SetTextString(0, column, string.Empty);
                    for (var column = 0; column < snapshot.Headers.Count; column++)
                    {
                        table.SetTextString(1, column, snapshot.Headers[column]);
                        table.SetTextHeight(1, column, textHeight);
                    }
                    for (var row = 0; row < snapshot.Rows.Count; row++)
                    {
                        for (var column = 0; column < snapshot.Headers.Count; column++)
                        {
                            table.SetTextString(row + 2, column, snapshot.Rows[row][column]);
                            table.SetTextHeight(row + 2, column, textHeight);
                        }
                    }

                    table.GenerateLayout();
                    modelSpace.AppendEntity(table);
                    transaction.AddNewlyCreatedDBObject(table, true);
                    MarkOwned(document.Database, transaction, table, project.ProjectId, definition, fingerprint);

                    project.Metadata[definition.HandleKey] = table.Handle.ToString();
                    project.Metadata[definition.OwnerProjectKey] = project.ProjectId;
                    project.Metadata[definition.OwnershipVersionKey] = OwnershipVersion;
                    project.Metadata[definition.FingerprintKey] = fingerprint;
                    project.Metadata[definition.PositionXKey] = Format(position.X);
                    project.Metadata[definition.PositionYKey] = Format(position.Y);
                    project.Metadata[definition.PositionZKey] = Format(position.Z);
                    project.Metadata[definition.RowCountKey] = snapshot.Rows.Count.ToString(CultureInfo.InvariantCulture);
                    project.Metadata[definition.ColumnCountKey] = snapshot.Headers.Count.ToString(CultureInfo.InvariantCulture);
                    AuditTrail.ForProject(project).Record(
                        "documentation.table.replace",
                        string.Empty,
                        definition.DocumentId + " • " + table.Handle + " • rows=" + snapshot.Rows.Count.ToString(CultureInfo.InvariantCulture));

                    transaction.Commit();
                    cadCommitted = true;
                    return table.Handle.ToString();
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
                            "Native documentation Table failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static void Remove(Document document, ProjectState project, ProjectOwnedNativeTableDefinition definition)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Native documentation Table remove yêu cầu DWG đích vẫn là MdiActiveDocument.");
            ValidatePersistedState(project, definition);
            if (!project.Metadata.ContainsKey(definition.HandleKey)) return;

            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    ErasePrevious(document, transaction, project, definition);
                    foreach (var key in definition.StateKeys) project.Metadata.Remove(key);
                    AuditTrail.ForProject(project).Record("documentation.table.remove", string.Empty, definition.DocumentId);
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
                            "Native documentation Table removal failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static Point3d StoredPosition(ProjectState project, ProjectOwnedNativeTableDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ValidatePersistedState(project, definition);
            if (!project.Metadata.ContainsKey(definition.HandleKey))
                throw new InvalidOperationException("Project chưa có generated native Table cho " + definition.DocumentId + ".");
            return new Point3d(
                ParseFinite(project.Metadata[definition.PositionXKey], definition.PositionXKey),
                ParseFinite(project.Metadata[definition.PositionYKey], definition.PositionYKey),
                ParseFinite(project.Metadata[definition.PositionZKey], definition.PositionZKey));
        }

        public static IReadOnlyList<ModelHealthIssue> Inspect(
            Document document,
            ProjectState project,
            ProjectOwnedNativeTableDefinition definition,
            Func<NativeDocumentationTableSnapshot> snapshotProvider)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (snapshotProvider == null) throw new ArgumentNullException(nameof(snapshotProvider));
            var issues = new List<ModelHealthIssue>();
            if (!definition.StateKeys.Any(project.Metadata.ContainsKey)) return issues.AsReadOnly();

            try { ValidatePersistedState(project, definition); }
            catch (Exception ex)
            {
                issues.Add(Issue("METADATA_INVALID", HealthSeverity.Error, ex.Message));
                return issues.AsReadOnly();
            }

            NativeDocumentationTableSnapshot expected;
            try
            {
                expected = snapshotProvider();
                ValidateSnapshot(expected);
            }
            catch (Exception ex)
            {
                issues.Add(Issue("RENDER_INVALID", HealthSeverity.Error, ex.Message));
                return issues.AsReadOnly();
            }

            var storedFingerprint = project.Metadata[definition.FingerprintKey].Trim();
            var expectedFingerprint = ComputeFingerprint(expected);
            if (!string.Equals(storedFingerprint, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue("STALE", HealthSeverity.Warning, "Authoritative schedule snapshot no longer matches the generated Table fingerprint."));

            var handle = project.Metadata[definition.HandleKey].Trim();
            if (!TryResolve(document.Database, handle, out var id))
            {
                issues.Add(Issue("MISSING", HealthSeverity.Error, "Generated native Table handle is not live: " + handle + "."));
                return issues.AsReadOnly();
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                {
                    issues.Add(Issue("MISSING", HealthSeverity.Error, "Generated native Table is erased or unavailable: " + handle + "."));
                    transaction.Commit();
                    return issues.AsReadOnly();
                }
                if (!(entity is Table table))
                {
                    issues.Add(Issue("WRONG_TYPE", HealthSeverity.Error, "Generated documentation handle is live but is not a Table: " + handle + "."));
                    transaction.Commit();
                    return issues.AsReadOnly();
                }
                if (!HasMatchingOwnership(table, project.ProjectId, definition, storedFingerprint))
                {
                    issues.Add(Issue("OWNERSHIP_MISMATCH", HealthSeverity.Error, "Native Table QS3DDOC ownership does not match project metadata."));
                    transaction.Commit();
                    return issues.AsReadOnly();
                }

                InspectShape(table, expected, definition, issues);
                InspectPosition(project, table, definition, issues);
                if (table.Rows.Count == expected.Rows.Count + 2 && table.Columns.Count == expected.Headers.Count)
                    InspectText(table, expected, definition, issues);
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static void InspectShape(Table table, NativeDocumentationTableSnapshot expected, ProjectOwnedNativeTableDefinition definition, ICollection<ModelHealthIssue> issues)
        {
            var rows = expected.Rows.Count + 2;
            var columns = expected.Headers.Count;
            if (table.Rows.Count == rows && table.Columns.Count == columns) return;
            issues.Add(Issue("CAD_SHAPE_DRIFT", HealthSeverity.Warning,
                "Live Table shape is " + table.Rows.Count.ToString(CultureInfo.InvariantCulture) + "x" + table.Columns.Count.ToString(CultureInfo.InvariantCulture) +
                ", expected " + rows.ToString(CultureInfo.InvariantCulture) + "x" + columns.ToString(CultureInfo.InvariantCulture) + "."));
        }

        private static void InspectText(Table table, NativeDocumentationTableSnapshot expected, ProjectOwnedNativeTableDefinition definition, ICollection<ModelHealthIssue> issues)
        {
            var detailCount = 0;
            CompareCell(table, 0, 0, expected.Title, "title", definition, issues, ref detailCount);
            for (var column = 0; column < expected.Headers.Count; column++)
                CompareCell(table, 1, column, expected.Headers[column], "header[" + column + "]", definition, issues, ref detailCount);
            for (var row = 0; row < expected.Rows.Count; row++)
            {
                for (var column = 0; column < expected.Headers.Count; column++)
                {
                    CompareCell(table, row + 2, column, expected.Rows[row][column], "row[" + row + "]/column[" + column + "]", definition, issues, ref detailCount);
                    if (detailCount >= MaxDetailedCellIssues) return;
                }
            }
        }

        private static void CompareCell(Table table, int row, int column, string expected, string label, ProjectOwnedNativeTableDefinition definition, ICollection<ModelHealthIssue> issues, ref int detailCount)
        {
            string actual;
            try { actual = table.TextString(row, column) ?? string.Empty; }
            catch (Exception ex)
            {
                issues.Add(Issue("CAD_CELL_UNREADABLE", HealthSeverity.Warning, "Cannot read live Table cell " + label + ": " + ex.Message));
                detailCount++;
                return;
            }
            if (string.Equals(actual, expected ?? string.Empty, StringComparison.Ordinal)) return;
            issues.Add(Issue("CAD_TEXT_DRIFT", HealthSeverity.Warning, "Live Table cell no longer matches authoritative snapshot at " + label + "."));
            detailCount++;
        }

        private static void InspectPosition(ProjectState project, Table table, ProjectOwnedNativeTableDefinition definition, ICollection<ModelHealthIssue> issues)
        {
            var expected = new Point3d(
                ParseFinite(project.Metadata[definition.PositionXKey], definition.PositionXKey),
                ParseFinite(project.Metadata[definition.PositionYKey], definition.PositionYKey),
                ParseFinite(project.Metadata[definition.PositionZKey], definition.PositionZKey));
            var actual = table.Position;
            var dx = actual.X - expected.X;
            var dy = actual.Y - expected.Y;
            var dz = actual.Z - expected.Z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var scale = Math.Max(1d, Math.Max(Math.Abs(expected.X), Math.Max(Math.Abs(expected.Y), Math.Abs(expected.Z))));
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance > Math.Max(1e-7d, scale * 1e-10d))
                issues.Add(Issue("CAD_POSITION_DRIFT", HealthSeverity.Warning, "Live Table Position no longer matches persisted drawing-local WCS position."));
        }

        private static ModelHealthIssue Issue(string suffix, HealthSeverity severity, string message)
        {
            return new ModelHealthIssue("DOCUMENTATION_TABLE_" + suffix, severity, message, string.Empty);
        }

        private static void ValidateSnapshot(NativeDocumentationTableSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            Required(snapshot.Title, "table title", 160);
            if (snapshot.Headers.Count == 0 || snapshot.Headers.Count > MaxColumns)
                throw new InvalidOperationException("Native documentation Table requires 1.." + MaxColumns + " columns.");
            if (snapshot.Rows.Count == 0 || snapshot.Rows.Count > MaxRows)
                throw new InvalidOperationException("Native documentation Table requires 1.." + MaxRows + " rows.");
            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var column = 0; column < snapshot.Headers.Count; column++)
            {
                var header = Required(snapshot.Headers[column], "header[" + column + "]", 96);
                if (!headers.Add(header)) throw new InvalidOperationException("Duplicate native documentation Table header: " + header + ".");
            }
            for (var row = 0; row < snapshot.Rows.Count; row++)
            {
                var cells = snapshot.Rows[row] ?? throw new InvalidOperationException("Native documentation Table row " + row + " is null.");
                if (cells.Count != snapshot.Headers.Count)
                    throw new InvalidOperationException("Native documentation Table row " + row + " has " + cells.Count + " cells but expected " + snapshot.Headers.Count + ".");
                for (var column = 0; column < cells.Count; column++)
                    if ((cells[column] ?? string.Empty).Length > MaxCellLength)
                        throw new InvalidOperationException("Native documentation Table cell exceeds " + MaxCellLength + " characters at row " + row + ", column " + column + ".");
            }
        }

        private static string Required(string value, string label, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(label + " is required.");
            var normalized = value.Trim();
            if (normalized.Length > maxLength) throw new InvalidOperationException(label + " exceeds " + maxLength + " characters.");
            return normalized;
        }

        private static void ValidatePersistedState(ProjectState project, ProjectOwnedNativeTableDefinition definition)
        {
            var keys = definition.StateKeys;
            var present = keys.Count(project.Metadata.ContainsKey);
            if (present == 0) return;
            if (present != keys.Count)
                throw new InvalidOperationException(definition.DocumentId + " metadata is partial. Refusing destructive Table replacement/removal.");
            foreach (var key in keys)
                if (string.IsNullOrWhiteSpace(project.Metadata[key])) throw new InvalidOperationException(key + " is empty.");
            if (!string.Equals(project.Metadata[definition.OwnerProjectKey].Trim(), (project.ProjectId ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException(definition.DocumentId + " owner project does not match active project.");
            if (!string.Equals(project.Metadata[definition.OwnershipVersionKey].Trim(), OwnershipVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported " + definition.DocumentId + " ownership version.");
            ParseFinite(project.Metadata[definition.PositionXKey], definition.PositionXKey);
            ParseFinite(project.Metadata[definition.PositionYKey], definition.PositionYKey);
            ParseFinite(project.Metadata[definition.PositionZKey], definition.PositionZKey);
            if (!int.TryParse(project.Metadata[definition.RowCountKey], NumberStyles.None, CultureInfo.InvariantCulture, out var rows) || rows < 0 || rows > MaxRows)
                throw new InvalidOperationException(definition.RowCountKey + " is invalid.");
            if (!int.TryParse(project.Metadata[definition.ColumnCountKey], NumberStyles.None, CultureInfo.InvariantCulture, out var columns) || columns <= 0 || columns > MaxColumns)
                throw new InvalidOperationException(definition.ColumnCountKey + " is invalid.");
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, ProjectOwnedNativeTableDefinition definition)
        {
            if (!project.Metadata.TryGetValue(definition.HandleKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var handle = raw.Trim();
            if (!TryResolve(document.Database, handle, out var id)) return;
            var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
            if (entity == null || entity.IsErased) return;
            if (!(entity is Table table))
                throw new InvalidOperationException(definition.DocumentId + " generated handle resolves to live non-Table object. Refusing destructive replacement.");
            var storedFingerprint = project.Metadata[definition.FingerprintKey].Trim();
            if (!HasMatchingOwnership(table, project.ProjectId, definition, storedFingerprint))
                throw new InvalidOperationException("Refusing to erase native Table because QS3DDOC ownership/fingerprint does not match " + definition.DocumentId + " metadata.");
            table.Erase();
        }

        private static void MarkOwned(Database database, Transaction transaction, Table table, string projectId, ProjectOwnedNativeTableDefinition definition, string fingerprint)
        {
            EnsureRegApp(database, transaction);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ProjectIdentityToken(projectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, definition.DocumentId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, definition.DocumentKind),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, fingerprint)))
                table.XData = marker;
        }

        private static bool HasMatchingOwnership(Table table, string projectId, ProjectOwnedNativeTableDefinition definition, string fingerprint)
        {
            using (var marker = table.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                return values.Length >= 6 &&
                    string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal) &&
                    MatchesProjectIdentity(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), projectId) &&
                    string.Equals(Convert.ToString(values[3].Value, CultureInfo.InvariantCulture), definition.DocumentId, StringComparison.Ordinal) &&
                    string.Equals(Convert.ToString(values[4].Value, CultureInfo.InvariantCulture), definition.DocumentKind, StringComparison.Ordinal) &&
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

        private static BlockTableRecord OpenModelSpace(Database database, Transaction transaction, OpenMode mode)
        {
            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], mode);
        }

        private static bool TryResolve(Database database, string raw, out ObjectId id)
        {
            id = ObjectId.Null;
            if (!long.TryParse((raw ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return false;
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

        private static string ComputeFingerprint(NativeDocumentationTableSnapshot snapshot)
        {
            var builder = new StringBuilder();
            Append(builder, snapshot.Title);
            foreach (var header in snapshot.Headers) Append(builder, header);
            foreach (var row in snapshot.Rows) foreach (var cell in row) Append(builder, cell);
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

        private static double PositiveDrawing(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new InvalidOperationException(label + " must be finite and > 0.");
            return value;
        }

        private static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Native documentation Table position must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static double ParseFinite(string raw, string key)
        {
            if (!double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(key + " is not a finite invariant number.");
            return value;
        }

        private static void ValidatePoint(Point3d point)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) ||
                double.IsNaN(point.Y) || double.IsInfinity(point.Y) ||
                double.IsNaN(point.Z) || double.IsInfinity(point.Z))
                throw new InvalidOperationException("Native documentation Table position must be finite.");
        }
    }
}
