using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Diagnostics;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticScheduleNativeTableBuilder
    {
        internal const string MetadataPrefix = "QS3D.Documentation.NativeSemanticScheduleTable.";
        private const string RegAppName = "QS3DDOC";
        private const string OwnershipVersion = "1";
        private const string ProjectIdentityTokenPrefix = "p1:";
        private const string DocumentId = "SemanticCustomSchedule";
        private const string DocumentKind = "SemanticScheduleTable";
        private const double TextHeightM = 0.0035d;
        private const double RowHeightM = 0.008d;
        private const double ColumnWidthM = 0.035d;
        private const int MaxDetailedCellIssues = 32;

        private sealed class StateKeys
        {
            public StateKeys(string token)
            {
                Token = token;
                Prefix = MetadataPrefix + token + ".";
                Handle = Prefix + "Handle";
                ScheduleId = Prefix + "ScheduleId";
                OwnerProjectId = Prefix + "OwnerProjectId";
                Version = Prefix + "OwnershipVersion";
                Fingerprint = Prefix + "Fingerprint";
                PositionX = Prefix + "PositionX";
                PositionY = Prefix + "PositionY";
                PositionZ = Prefix + "PositionZ";
                RowCount = Prefix + "RowCount";
                ColumnCount = Prefix + "ColumnCount";
                All = new[] { Handle, ScheduleId, OwnerProjectId, Version, Fingerprint, PositionX, PositionY, PositionZ, RowCount, ColumnCount };
            }

            public string Token { get; }
            public string Prefix { get; }
            public string Handle { get; }
            public string ScheduleId { get; }
            public string OwnerProjectId { get; }
            public string Version { get; }
            public string Fingerprint { get; }
            public string PositionX { get; }
            public string PositionY { get; }
            public string PositionZ { get; }
            public string RowCount { get; }
            public string ColumnCount { get; }
            public IReadOnlyList<string> All { get; }
        }

        public static string Build(Document document, ProjectState project, SemanticScheduleDefinition definition, Point3d position)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            RequireActiveModelSpace(document);
            RequireFinite(position);

            var currentDefinition = ResolveDefinition(project, definition.Id);
            var semanticTable = SemanticScheduleCatalog.Build(project, currentDefinition);
            var fingerprint = ComputeFingerprint(semanticTable);
            var keys = Keys(currentDefinition.Id);
            ValidatePersistedState(project, keys, currentDefinition.Id);

            var snapshot = ProjectStateSnapshot.Capture(project);
            var committed = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var modelSpace = OpenModelSpace(document.Database, transaction, OpenMode.ForWrite);
                    ErasePrevious(document, transaction, project, keys, currentDefinition.Id);

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
                    MarkOwned(document.Database, transaction, table, project.ProjectId, currentDefinition.Id, fingerprint);

                    project.Metadata[keys.Handle] = table.Handle.ToString();
                    project.Metadata[keys.ScheduleId] = currentDefinition.Id;
                    project.Metadata[keys.OwnerProjectId] = project.ProjectId;
                    project.Metadata[keys.Version] = OwnershipVersion;
                    project.Metadata[keys.Fingerprint] = fingerprint;
                    project.Metadata[keys.PositionX] = Format(position.X);
                    project.Metadata[keys.PositionY] = Format(position.Y);
                    project.Metadata[keys.PositionZ] = Format(position.Z);
                    project.Metadata[keys.RowCount] = semanticTable.Rows.Count.ToString(CultureInfo.InvariantCulture);
                    project.Metadata[keys.ColumnCount] = semanticTable.Headers.Count.ToString(CultureInfo.InvariantCulture);
                    AuditTrail.ForProject(project).Record(
                        "BuildSemanticCustomScheduleTable",
                        currentDefinition.Id,
                        "Generated native custom schedule Table " + table.Handle + " for " + currentDefinition.Name + ".");

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
                            "Custom semantic schedule Table build failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static void Remove(Document document, ProjectState project, string scheduleId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            RequireActiveModelSpace(document);
            var normalizedId = NormalizeScheduleId(scheduleId);
            var keys = Keys(normalizedId);
            ValidatePersistedState(project, keys, normalizedId);
            if (!project.Metadata.ContainsKey(keys.Handle)) return;

            var snapshot = ProjectStateSnapshot.Capture(project);
            var committed = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    ErasePrevious(document, transaction, project, keys, normalizedId);
                    foreach (var key in keys.All) project.Metadata.Remove(key);
                    AuditTrail.ForProject(project).Record(
                        "RemoveSemanticCustomScheduleTable",
                        normalizedId,
                        "Removed project-owned native custom schedule Table metadata/entity.");
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
                            "Custom semantic schedule Table removal failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }
        }

        public static Point3d StoredPosition(ProjectState project, string scheduleId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = NormalizeScheduleId(scheduleId);
            var keys = Keys(normalizedId);
            ValidatePersistedState(project, keys, normalizedId);
            if (!project.Metadata.ContainsKey(keys.Handle))
                throw new InvalidOperationException("Custom schedule " + normalizedId + " chưa có generated native Table.");
            return new Point3d(
                ParseFinite(project.Metadata[keys.PositionX], keys.PositionX),
                ParseFinite(project.Metadata[keys.PositionY], keys.PositionY),
                ParseFinite(project.Metadata[keys.PositionZ], keys.PositionZ));
        }

        public static SemanticScheduleDefinition ResolveDefinition(ProjectState project, string scheduleId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = NormalizeScheduleId(scheduleId);
            var matches = SemanticScheduleCatalog.Load(project)
                .Where(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Không tìm thấy đúng một custom semantic schedule có ID " + normalizedId + ".");
            return matches[0];
        }

        public static IReadOnlyList<string> PersistedScheduleIds(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var result = new List<string>();
            foreach (var token in PersistedTokens(project))
            {
                var keys = new StateKeys(token);
                if (project.Metadata.TryGetValue(keys.ScheduleId, out var id) && !string.IsNullOrWhiteSpace(id))
                    result.Add(id.Trim());
            }
            return result
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        public static IReadOnlyList<string> PersistedHandles(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var result = new List<string>();
            foreach (var token in PersistedTokens(project))
            {
                var keys = new StateKeys(token);
                if (project.Metadata.TryGetValue(keys.Handle, out var handle) && !string.IsNullOrWhiteSpace(handle))
                    result.Add(handle.Trim());
            }
            return result
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            IReadOnlyList<SemanticScheduleDefinition> definitions;
            try { definitions = SemanticScheduleCatalog.Load(project); }
            catch (Exception ex)
            {
                issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_CATALOG_INVALID", HealthSeverity.Error, ex.Message, string.Empty));
                return issues.AsReadOnly();
            }

            foreach (var token in PersistedTokenCandidates(project))
            {
                if (!IsToken(token))
                {
                    issues.Add(Issue(
                        "CUSTOM_SCHEDULE_TABLE_METADATA_INVALID",
                        HealthSeverity.Error,
                        "Persisted custom schedule Table metadata contains a malformed owner token.",
                        string.Empty));
                    continue;
                }
                InspectToken(document, project, definitions, token, issues);
            }
            return issues.AsReadOnly();
        }

        private static void InspectToken(
            Document document,
            ProjectState project,
            IReadOnlyList<SemanticScheduleDefinition> definitions,
            string token,
            ICollection<ModelHealthIssue> issues)
        {
            var keys = new StateKeys(token);
            var present = keys.All.Count(project.Metadata.ContainsKey);
            if (present != keys.All.Count)
            {
                issues.Add(Issue(
                    "CUSTOM_SCHEDULE_TABLE_METADATA_INVALID",
                    HealthSeverity.Error,
                    "Persisted custom schedule Table metadata is partial for owner token " + token + ".",
                    string.Empty));
                return;
            }

            var scheduleId = project.Metadata[keys.ScheduleId].Trim();
            try { ValidatePersistedState(project, keys, scheduleId); }
            catch (Exception ex)
            {
                issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_METADATA_INVALID", HealthSeverity.Error, ex.Message, scheduleId));
                return;
            }

            var definition = definitions.FirstOrDefault(x => string.Equals(x.Id, scheduleId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                issues.Add(Issue(
                    "CUSTOM_SCHEDULE_TABLE_DEFINITION_MISSING",
                    HealthSeverity.Warning,
                    "Native Table references removed custom schedule; remove the orphan Table or restore the definition.",
                    scheduleId));
                InspectNativeOwnership(document, project, keys, scheduleId, null, issues);
                return;
            }

            SemanticDocumentationTable expected;
            try { expected = SemanticScheduleCatalog.Build(project, definition); }
            catch (Exception ex)
            {
                issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_RENDER_INVALID", HealthSeverity.Error, ex.Message, scheduleId));
                InspectNativeOwnership(document, project, keys, scheduleId, null, issues);
                return;
            }

            var expectedFingerprint = ComputeFingerprint(expected);
            if (!string.Equals(project.Metadata[keys.Fingerprint].Trim(), expectedFingerprint, StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue(
                    "CUSTOM_SCHEDULE_TABLE_STALE",
                    HealthSeverity.Warning,
                    "Custom schedule native Table content no longer matches the current semantic schedule snapshot.",
                    scheduleId));
            InspectNativeOwnership(document, project, keys, scheduleId, expected, issues);
        }

        private static void InspectNativeOwnership(
            Document document,
            ProjectState project,
            StateKeys keys,
            string scheduleId,
            SemanticDocumentationTable? expected,
            ICollection<ModelHealthIssue> issues)
        {
            var rawHandle = project.Metadata[keys.Handle].Trim();
            if (!TryResolve(document.Database, rawHandle, out var id))
            {
                issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_MISSING", HealthSeverity.Error, "Generated Table handle " + rawHandle + " is not live.", scheduleId));
                return;
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                {
                    issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_MISSING", HealthSeverity.Error, "Generated Table is erased or unavailable.", scheduleId));
                    transaction.Commit();
                    return;
                }
                if (!(entity is Table table))
                {
                    issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_WRONG_TYPE", HealthSeverity.Error, "Generated handle is not a Table.", scheduleId));
                    transaction.Commit();
                    return;
                }
                if (!HasMatchingOwnership(table, project.ProjectId, scheduleId, project.Metadata[keys.Fingerprint].Trim()))
                {
                    issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_OWNERSHIP_MISMATCH", HealthSeverity.Error, "QS3DDOC ownership does not match project/schedule/fingerprint metadata.", scheduleId));
                    transaction.Commit();
                    return;
                }

                InspectPosition(project, keys, table, scheduleId, issues);
                if (expected != null)
                {
                    InspectShape(table, expected, scheduleId, issues);
                    if (table.Rows.Count == expected.Rows.Count + 2 && table.Columns.Count == expected.Headers.Count)
                        InspectText(table, expected, scheduleId, issues);
                }
                transaction.Commit();
            }
        }

        private static void InspectShape(Table table, SemanticDocumentationTable expected, string scheduleId, ICollection<ModelHealthIssue> issues)
        {
            var expectedRows = expected.Rows.Count + 2;
            var expectedColumns = expected.Headers.Count;
            if (table.Rows.Count == expectedRows && table.Columns.Count == expectedColumns) return;
            issues.Add(Issue(
                "CUSTOM_SCHEDULE_TABLE_CAD_SHAPE_DRIFT",
                HealthSeverity.Warning,
                "Live Table shape " + table.Rows.Count.ToString(CultureInfo.InvariantCulture) + "x" + table.Columns.Count.ToString(CultureInfo.InvariantCulture) +
                " differs from expected " + expectedRows.ToString(CultureInfo.InvariantCulture) + "x" + expectedColumns.ToString(CultureInfo.InvariantCulture) + ".",
                scheduleId));
        }

        private static void InspectText(Table table, SemanticDocumentationTable expected, string scheduleId, ICollection<ModelHealthIssue> issues)
        {
            var details = 0;
            CompareCell(table, 0, 0, expected.Title, "title", scheduleId, issues, ref details);
            for (var column = 0; column < expected.Headers.Count && details < MaxDetailedCellIssues; column++)
                CompareCell(table, 1, column, expected.Headers[column], "header[" + column + "]", scheduleId, issues, ref details);
            for (var row = 0; row < expected.Rows.Count && details < MaxDetailedCellIssues; row++)
                for (var column = 0; column < expected.Headers.Count && details < MaxDetailedCellIssues; column++)
                    CompareCell(table, row + 2, column, expected.Rows[row].Cells[column], expected.Rows[row].ElementId + "/" + expected.Headers[column], scheduleId, issues, ref details);
        }

        private static void CompareCell(Table table, int row, int column, string expected, string label, string scheduleId, ICollection<ModelHealthIssue> issues, ref int details)
        {
            string actual;
            try { actual = table.TextString(row, column) ?? string.Empty; }
            catch (Exception ex)
            {
                issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_CAD_CELL_UNREADABLE", HealthSeverity.Warning, "Cannot read live Table cell " + label + ": " + ex.Message, scheduleId));
                details++;
                return;
            }
            if (string.Equals(actual, expected ?? string.Empty, StringComparison.Ordinal)) return;
            issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_CAD_TEXT_DRIFT", HealthSeverity.Warning, "Live Table cell differs from semantic snapshot at " + label + ".", scheduleId));
            details++;
        }

        private static void InspectPosition(ProjectState project, StateKeys keys, Table table, string scheduleId, ICollection<ModelHealthIssue> issues)
        {
            if (!TryFinite(project.Metadata, keys.PositionX, out var x) ||
                !TryFinite(project.Metadata, keys.PositionY, out var y) ||
                !TryFinite(project.Metadata, keys.PositionZ, out var z)) return;
            var actual = table.Position;
            var dx = actual.X - x;
            var dy = actual.Y - y;
            var dz = actual.Z - z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var scale = Math.Max(1d, Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z))));
            var tolerance = Math.Max(1e-7d, scale * 1e-10d);
            if (!double.IsNaN(distance) && !double.IsInfinity(distance) && distance <= tolerance) return;
            issues.Add(Issue("CUSTOM_SCHEDULE_TABLE_CAD_POSITION_DRIFT", HealthSeverity.Warning, "Live Table Position differs from persisted WCS placement.", scheduleId));
        }

        private static void ValidatePersistedState(ProjectState project, StateKeys keys, string scheduleId)
        {
            var present = keys.All.Count(project.Metadata.ContainsKey);
            if (present == 0) return;
            if (present != keys.All.Count)
                throw new InvalidOperationException("Generated custom schedule Table metadata is partial for " + scheduleId + ". Refusing destructive replacement.");
            foreach (var key in keys.All)
                if (string.IsNullOrWhiteSpace(project.Metadata[key])) throw new InvalidOperationException(key + " is empty.");
            var storedScheduleId = project.Metadata[keys.ScheduleId].Trim();
            if (!string.Equals(storedScheduleId, scheduleId, StringComparison.OrdinalIgnoreCase) || !string.Equals(keys.Token, Token(storedScheduleId), StringComparison.Ordinal))
                throw new InvalidOperationException("Generated custom schedule Table schedule identity does not match its owner slot.");
            if (!string.Equals(project.Metadata[keys.OwnerProjectId].Trim(), (project.ProjectId ?? string.Empty).Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Generated custom schedule Table owner project does not match the active project.");
            if (!string.Equals(project.Metadata[keys.Version].Trim(), OwnershipVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported custom schedule Table ownership version: " + project.Metadata[keys.Version]);
            _ = ParseFinite(project.Metadata[keys.PositionX], keys.PositionX);
            _ = ParseFinite(project.Metadata[keys.PositionY], keys.PositionY);
            _ = ParseFinite(project.Metadata[keys.PositionZ], keys.PositionZ);
            if (!int.TryParse(project.Metadata[keys.RowCount], NumberStyles.None, CultureInfo.InvariantCulture, out var rowCount) || rowCount < 0)
                throw new InvalidOperationException(keys.RowCount + " is invalid.");
            if (!int.TryParse(project.Metadata[keys.ColumnCount], NumberStyles.None, CultureInfo.InvariantCulture, out var columnCount) || columnCount <= 0 || columnCount > 32)
                throw new InvalidOperationException(keys.ColumnCount + " is invalid.");
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, StateKeys keys, string scheduleId)
        {
            if (!project.Metadata.TryGetValue(keys.Handle, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var handle = raw.Trim();
            if (!TryResolve(document.Database, handle, out var id)) return;
            var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
            if (entity == null || entity.IsErased) return;
            if (!(entity is Table table))
                throw new InvalidOperationException("Generated custom schedule Table handle " + handle + " resolves to a live non-Table object. Refusing replacement.");
            var fingerprint = project.Metadata[keys.Fingerprint].Trim();
            if (!HasMatchingOwnership(table, project.ProjectId, scheduleId, fingerprint))
                throw new InvalidOperationException("Refusing to erase custom schedule Table " + handle + " because QS3DDOC ownership does not match persisted project/schedule state.");
            table.Erase();
        }

        private static void RequireActiveModelSpace(Document document)
        {
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Custom schedule Table yêu cầu DWG đích vẫn là MdiActiveDocument.");
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Custom schedule Table P0 chỉ hỗ trợ ModelSpace.");
        }

        private static BlockTableRecord OpenModelSpace(Database database, Transaction transaction, OpenMode mode)
        {
            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], mode);
        }

        private static void MarkOwned(Database database, Transaction transaction, Table table, string projectId, string scheduleId, string fingerprint)
        {
            EnsureRegApp(database, transaction);
            var scheduleToken = Token(scheduleId);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, ProjectIdentityToken(projectId)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, DocumentId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, DocumentKind),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, scheduleToken),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, fingerprint)))
                table.XData = marker;
        }

        private static bool HasMatchingOwnership(Table table, string projectId, string scheduleId, string fingerprint)
        {
            using (var marker = table.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return false;
                var values = marker.AsArray();
                if (values.Length < 7) return false;
                var normalizedScheduleId = NormalizeScheduleId(scheduleId);
                var persistedScheduleIdentity = Convert.ToString(values[5].Value, CultureInfo.InvariantCulture) ?? string.Empty;
                var scheduleMatches =
                    string.Equals(persistedScheduleIdentity, Token(normalizedScheduleId), StringComparison.Ordinal) ||
                    string.Equals(persistedScheduleIdentity, normalizedScheduleId, StringComparison.OrdinalIgnoreCase);
                return
                    string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal) &&
                    MatchesProjectIdentity(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), projectId) &&
                    string.Equals(Convert.ToString(values[3].Value, CultureInfo.InvariantCulture), DocumentId, StringComparison.Ordinal) &&
                    string.Equals(Convert.ToString(values[4].Value, CultureInfo.InvariantCulture), DocumentKind, StringComparison.Ordinal) &&
                    scheduleMatches &&
                    string.Equals(Convert.ToString(values[6].Value, CultureInfo.InvariantCulture), fingerprint, StringComparison.OrdinalIgnoreCase);
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
                var builder = new StringBuilder(ProjectIdentityTokenPrefix.Length + hash.Length * 2);
                builder.Append(ProjectIdentityTokenPrefix);
                foreach (var value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static bool MatchesProjectIdentity(string storedIdentity, string projectId)
        {
            var normalized = (projectId ?? string.Empty).Trim();
            return string.Equals(storedIdentity, ProjectIdentityToken(normalized), StringComparison.Ordinal) ||
                string.Equals(storedIdentity, normalized, StringComparison.Ordinal);
        }

        private static IReadOnlyList<string> PersistedTokens(ProjectState project)
        {
            return PersistedTokenCandidates(project)
                .Where(IsToken)
                .ToList()
                .AsReadOnly();
        }

        private static IReadOnlyList<string> PersistedTokenCandidates(ProjectState project)
        {
            return project.Metadata.Keys
                .Where(x => x.StartsWith(MetadataPrefix, StringComparison.Ordinal))
                .Select(x => x.Substring(MetadataPrefix.Length))
                .Select(x =>
                {
                    var separator = x.IndexOf('.');
                    return separator > 0 ? x.Substring(0, separator) : string.Empty;
                })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        private static bool IsToken(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }

        private static StateKeys Keys(string scheduleId) => new StateKeys(Token(NormalizeScheduleId(scheduleId)));

        private static string Token(string scheduleId)
        {
            var canonical = NormalizeScheduleId(scheduleId).ToUpperInvariant();
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string NormalizeScheduleId(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0 || normalized.Length > 80)
                throw new InvalidOperationException("Custom semantic schedule ID must contain 1..80 characters.");
            return normalized;
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
            builder.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('|');
        }

        private static ModelHealthIssue Issue(string code, HealthSeverity severity, string message, string scheduleId)
        {
            var prefix = string.IsNullOrWhiteSpace(scheduleId) ? string.Empty : "[schedule " + scheduleId.Trim() + "] ";
            return new ModelHealthIssue(code, severity, prefix + message, string.Empty);
        }

        private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static double ParseFinite(string raw, string label)
        {
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " is not a finite number.");
            return value;
        }

        private static bool TryFinite(IDictionary<string, string> metadata, string key, out double value)
        {
            value = 0d;
            return metadata.TryGetValue(key, out var raw) &&
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void RequireFinite(Point3d point)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) ||
                double.IsNaN(point.Y) || double.IsInfinity(point.Y) ||
                double.IsNaN(point.Z) || double.IsInfinity(point.Z))
                throw new InvalidOperationException("Custom schedule Table insertion point is not finite.");
        }

        private static void RequirePositiveFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new InvalidOperationException(label + " must be positive and finite.");
        }
    }
}
