using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedSemanticElementTableRuntimeHealthService
    {
        private const int MaxDetailedCellIssues = 32;

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            var baseIssues = SemanticElementTableBuilder.ValidateRuntime(document, project);
            foreach (var issue in baseIssues)
                issues.Add(ToHealthIssue(issue));

            if (!project.Metadata.TryGetValue(SemanticElementTableBuilder.HandleKey, out var rawHandle) ||
                string.IsNullOrWhiteSpace(rawHandle))
                return issues.AsReadOnly();

            if (HasBlockingNativeIssue(baseIssues)) return issues.AsReadOnly();
            if (!TryResolve(document.Database, rawHandle.Trim(), out var id)) return issues.AsReadOnly();

            QS3D.Core.Documentation.SemanticDocumentationTable expected;
            try { expected = SemanticElementTableBuilder.BuildSnapshot(project); }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_ELEMENT_TABLE_RENDER_INVALID",
                    HealthSeverity.Error,
                    "Không thể dựng semantic snapshot để đối chiếu live native Table: " + ex.Message,
                    string.Empty));
                return issues.AsReadOnly();
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var table = transaction.GetObject(id, OpenMode.ForRead, false) as Table;
                if (table == null || table.IsErased) return issues.AsReadOnly();

                InspectShape(table, expected, issues);
                InspectPosition(project, table, issues);
                if (table.Rows.Count == expected.Rows.Count + 2 && table.Columns.Count == expected.Headers.Count)
                    InspectText(table, expected, issues);
                transaction.Commit();
            }

            return issues.AsReadOnly();
        }

        private static void InspectShape(
            Table table,
            QS3D.Core.Documentation.SemanticDocumentationTable expected,
            ICollection<ModelHealthIssue> issues)
        {
            var expectedRows = expected.Rows.Count + 2;
            var expectedColumns = expected.Headers.Count;
            if (table.Rows.Count != expectedRows || table.Columns.Count != expectedColumns)
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_ELEMENT_TABLE_CAD_SHAPE_DRIFT",
                    HealthSeverity.Warning,
                    "Live native Table row/column count không còn khớp semantic snapshot. CAD=" +
                    table.Rows.Count.ToString(CultureInfo.InvariantCulture) + "x" +
                    table.Columns.Count.ToString(CultureInfo.InvariantCulture) + ", expected=" +
                    expectedRows.ToString(CultureInfo.InvariantCulture) + "x" +
                    expectedColumns.ToString(CultureInfo.InvariantCulture) + ".",
                    string.Empty));
            }
        }

        private static void InspectText(
            Table table,
            QS3D.Core.Documentation.SemanticDocumentationTable expected,
            ICollection<ModelHealthIssue> issues)
        {
            var detailCount = 0;
            CompareCell(table, 0, 0, expected.Title, "title", issues, ref detailCount);
            for (var column = 0; column < expected.Headers.Count; column++)
                CompareCell(table, 1, column, expected.Headers[column], "header[" + column + "]", issues, ref detailCount);

            for (var row = 0; row < expected.Rows.Count; row++)
            {
                for (var column = 0; column < expected.Headers.Count; column++)
                {
                    CompareCell(
                        table,
                        row + 2,
                        column,
                        expected.Rows[row].Cells[column],
                        expected.Rows[row].ElementId + "/" + expected.Headers[column],
                        issues,
                        ref detailCount);
                    if (detailCount >= MaxDetailedCellIssues) return;
                }
            }
        }

        private static void CompareCell(
            Table table,
            int row,
            int column,
            string expected,
            string label,
            ICollection<ModelHealthIssue> issues,
            ref int detailCount)
        {
            string actual;
            try { actual = table.TextString(row, column) ?? string.Empty; }
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_ELEMENT_TABLE_CAD_CELL_UNREADABLE",
                    HealthSeverity.Warning,
                    "Không đọc được live Table cell " + label + ": " + ex.Message,
                    string.Empty));
                detailCount++;
                return;
            }

            if (string.Equals(actual, expected ?? string.Empty, StringComparison.Ordinal)) return;
            issues.Add(new ModelHealthIssue(
                "SEMANTIC_ELEMENT_TABLE_CAD_TEXT_DRIFT",
                HealthSeverity.Warning,
                "Live native Table cell không còn khớp semantic snapshot tại " + label + ".",
                string.Empty));
            detailCount++;
        }

        private static void InspectPosition(ProjectState project, Table table, ICollection<ModelHealthIssue> issues)
        {
            if (!TryFinite(project.Metadata, SemanticElementTableBuilder.PositionXKey, out var x) ||
                !TryFinite(project.Metadata, SemanticElementTableBuilder.PositionYKey, out var y) ||
                !TryFinite(project.Metadata, SemanticElementTableBuilder.PositionZKey, out var z))
                return;

            var expected = new Point3d(x, y, z);
            var actual = table.Position;
            var dx = actual.X - expected.X;
            var dy = actual.Y - expected.Y;
            var dz = actual.Z - expected.Z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var scale = Math.Max(1d, Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z))));
            var tolerance = Math.Max(1e-7d, scale * 1e-10d);
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance > tolerance)
            {
                issues.Add(new ModelHealthIssue(
                    "SEMANTIC_ELEMENT_TABLE_CAD_POSITION_DRIFT",
                    HealthSeverity.Warning,
                    "Live native Table Position không còn khớp drawing-local WCS position đã lưu.",
                    string.Empty));
            }
        }

        private static ModelHealthIssue ToHealthIssue(string raw)
        {
            var text = raw ?? string.Empty;
            var separator = text.IndexOf(':');
            var code = separator > 0 ? text.Substring(0, separator).Trim() : "SEMANTIC_ELEMENT_TABLE_RUNTIME";
            var message = separator > 0 ? text.Substring(separator + 1).Trim() : text;
            var severity = code.EndsWith("_STALE", StringComparison.OrdinalIgnoreCase)
                ? HealthSeverity.Warning
                : HealthSeverity.Error;
            return new ModelHealthIssue(code, severity, message, string.Empty);
        }

        private static bool HasBlockingNativeIssue(IEnumerable<string> issues)
        {
            foreach (var issue in issues)
            {
                var value = issue ?? string.Empty;
                if (value.StartsWith("SEMANTIC_ELEMENT_TABLE_METADATA_INVALID", StringComparison.Ordinal) ||
                    value.StartsWith("SEMANTIC_ELEMENT_TABLE_MISSING", StringComparison.Ordinal) ||
                    value.StartsWith("SEMANTIC_ELEMENT_TABLE_WRONG_TYPE", StringComparison.Ordinal) ||
                    value.StartsWith("SEMANTIC_ELEMENT_TABLE_OWNERSHIP_MISMATCH", StringComparison.Ordinal))
                    return true;
            }
            return false;
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
            catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))
            {
                return false;
            }
        }

        private static bool TryFinite(IDictionary<string, string> metadata, string key, out double value)
        {
            value = 0d;
            return metadata.TryGetValue(key, out var raw) &&
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsRecoverableDiagnosticFailure(Exception exception)
        {
            return !(exception is OutOfMemoryException) &&
                   !(exception is StackOverflowException) &&
                   !(exception is AccessViolationException);
        }
    }
}
