using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticDocumentationColumn
    {
        public SemanticDocumentationColumn(string header, string template)
        {
            Header = SemanticDocumentationTableBuilder.Required(
                header,
                nameof(header),
                SemanticDocumentationTableBuilder.MaxHeaderLength);
            Template = SemanticDocumentationTableBuilder.Required(
                template,
                nameof(template),
                SemanticDocumentationTableBuilder.MaxTemplateLength);
            SemanticTagRenderer.ValidateTemplate(Template);
        }

        public string Header { get; }
        public string Template { get; }
    }

    public sealed class SemanticDocumentationRow
    {
        public SemanticDocumentationRow(string elementId, IReadOnlyList<string> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (cells.Count > SemanticDocumentationTableBuilder.MaxColumns)
                throw SemanticDocumentationTableBuilder.LimitExceeded(
                    SemanticDocumentationTableBuilder.MaxColumns,
                    "cells per row");

            ElementId = SemanticDocumentationTableBuilder.RequiredCanonicalId(
                elementId,
                nameof(elementId),
                SemanticDocumentationTableBuilder.MaxElementIdLength);

            var snapshot = new List<string>(cells.Count);
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell == null)
                    throw new ArgumentException("Documentation table row cell cannot be null at index " + i + ".", nameof(cells));
                snapshot.Add(cell);
            }
            Cells = snapshot.AsReadOnly();
        }

        public string ElementId { get; }
        public IReadOnlyList<string> Cells { get; }
    }

    public sealed class SemanticDocumentationTable
    {
        public SemanticDocumentationTable(
            string title,
            IReadOnlyList<string> headers,
            IReadOnlyList<SemanticDocumentationRow> rows)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (headers.Count == 0)
                throw new InvalidOperationException("Documentation table requires at least one header.");
            if (headers.Count > SemanticDocumentationTableBuilder.MaxColumns)
                throw SemanticDocumentationTableBuilder.LimitExceeded(
                    SemanticDocumentationTableBuilder.MaxColumns,
                    "columns");
            if (rows.Count > SemanticDocumentationTableBuilder.MaxRows)
                throw SemanticDocumentationTableBuilder.LimitExceeded(
                    SemanticDocumentationTableBuilder.MaxRows,
                    "rows");

            Title = SemanticDocumentationTableBuilder.Required(
                title,
                nameof(title),
                SemanticDocumentationTableBuilder.MaxTitleLength);

            var headerSnapshot = new List<string>(headers.Count);
            var uniqueHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var header = SemanticDocumentationTableBuilder.Required(
                    headers[i],
                    "headers[" + i + "]",
                    SemanticDocumentationTableBuilder.MaxHeaderLength);
                if (!uniqueHeaders.Add(header))
                    throw new InvalidOperationException("Documentation table contains duplicate column header: " + header + ".");
                headerSnapshot.Add(header);
            }

            var rowSnapshot = new List<SemanticDocumentationRow>(rows.Count);
            var uniqueElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                    throw new ArgumentException("Documentation table row cannot be null at index " + i + ".", nameof(rows));
                if (row.Cells.Count != headerSnapshot.Count)
                    throw new InvalidOperationException(
                        "Documentation table row " + row.ElementId + " has " + row.Cells.Count +
                        " cells but the table has " + headerSnapshot.Count + " headers.");
                if (!uniqueElementIds.Add(row.ElementId))
                    throw new InvalidOperationException("Documentation table contains duplicate semantic element id: " + row.ElementId + ".");
                rowSnapshot.Add(row);
            }

            Headers = headerSnapshot.AsReadOnly();
            Rows = rowSnapshot.AsReadOnly();
        }

        public string Title { get; }
        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<SemanticDocumentationRow> Rows { get; }
    }

    public static class SemanticDocumentationTableBuilder
    {
        internal const int MaxRows = 5000;
        internal const int MaxColumns = 32;
        internal const int MaxTitleLength = 160;
        internal const int MaxHeaderLength = 96;
        internal const int MaxElementIdLength = 128;
        internal const int MaxTemplateLength = 512;

        public static SemanticDocumentationTable Build(
            ProjectState project,
            string title,
            IEnumerable<string> orderedElementIds,
            IEnumerable<SemanticDocumentationColumn> columns)
        {
            return Build(project, title, orderedElementIds, columns, allowEmpty: false);
        }

        public static SemanticDocumentationTable Build(
            ProjectState project,
            string title,
            IEnumerable<string> orderedElementIds,
            IEnumerable<SemanticDocumentationColumn> columns,
            bool allowEmpty)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (orderedElementIds == null) throw new ArgumentNullException(nameof(orderedElementIds));
            if (columns == null) throw new ArgumentNullException(nameof(columns));

            var normalizedTitle = Required(title, nameof(title), MaxTitleLength);
            var rawIds = MaterializeBounded(orderedElementIds, MaxRows, "rows");
            var ids = rawIds
                .Select((value, index) => RequiredCanonicalId(value, "orderedElementIds[" + index + "]", MaxElementIdLength))
                .ToList();
            if (ids.Count == 0 && !allowEmpty)
                throw new InvalidOperationException("Documentation table requires at least one semantic element.");
            if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
                throw new InvalidOperationException("Documentation table input contains duplicate semantic element ids.");

            var columnList = MaterializeBounded(columns, MaxColumns, "columns");
            if (columnList.Count == 0) throw new InvalidOperationException("Documentation table requires at least one column.");

            var normalizedColumns = new List<SemanticDocumentationColumn>(columnList.Count);
            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columnList.Count; i++)
            {
                var column = columnList[i] ?? throw new ArgumentException("Documentation table column cannot be null at index " + i + ".", nameof(columns));
                var header = Required(column.Header, "columns[" + i + "].Header", MaxHeaderLength);
                var template = Required(column.Template, "columns[" + i + "].Template", MaxTemplateLength);
                if (!headers.Add(header)) throw new InvalidOperationException("Documentation table contains duplicate column header: " + header + ".");
                SemanticTagRenderer.ValidateTemplate(template);
                normalizedColumns.Add(new SemanticDocumentationColumn(header, template));
            }

            var context = new SemanticTagRenderContext(project);
            var elements = new List<ProjectElement>(ids.Count);
            foreach (var id in ids) elements.Add(context.ResolveElement(id));

            var rows = new List<SemanticDocumentationRow>(elements.Count);
            foreach (var element in elements)
            {
                var cells = new List<string>(normalizedColumns.Count);
                foreach (var column in normalizedColumns)
                    cells.Add(SemanticTagRenderer.Render(context, element, column.Template, allowEmpty: true));
                rows.Add(new SemanticDocumentationRow(element.Id, cells));
            }

            return new SemanticDocumentationTable(
                normalizedTitle,
                normalizedColumns.Select(x => x.Header).ToArray(),
                rows);
        }

        private static List<T> MaterializeBounded<T>(IEnumerable<T> source, int maxCount, string label)
        {
            var knownCount = ValidateKnownCounts(source, maxCount, label);

            var result = new List<T>(Math.Min(maxCount, 256));
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= maxCount)
                        throw LimitExceeded(maxCount, label);
                    result.Add(enumerator.Current);
                }
            }

            if (knownCount.HasValue && knownCount.Value != result.Count)
                throw new InvalidOperationException("Documentation table " + label + " source known count does not match completed traversal.");

            return result;
        }

        private static int? ValidateKnownCounts<T>(IEnumerable<T> source, int maxCount, string label)
        {
            int? knownCount = null;
            if (source is ICollection<T> collection)
                ValidateKnownCount(collection.Count, maxCount, label, ref knownCount);
            if (source is IReadOnlyCollection<T> readOnlyCollection)
                ValidateKnownCount(readOnlyCollection.Count, maxCount, label, ref knownCount);
            if (source is System.Collections.ICollection nonGenericCollection)
                ValidateKnownCount(nonGenericCollection.Count, maxCount, label, ref knownCount);
            return knownCount;
        }

        private static void ValidateKnownCount(int count, int maxCount, string label, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Documentation table " + label + " source reported an invalid negative known count.");
            if (count > maxCount)
                throw LimitExceeded(maxCount, label);
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException("Documentation table " + label + " source exposes conflicting known counts.");
            knownCount = count;
        }

        internal static InvalidOperationException LimitExceeded(int maxCount, string label)
        {
            return new InvalidOperationException("Documentation table supports at most " + maxCount + " " + label + ".");
        }

        internal static string Required(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }

        internal static string RequiredCanonicalId(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var raw = value!;
            var normalized = raw.Trim();
            if (!string.Equals(raw, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Semantic element id must not contain leading or trailing whitespace.", name);
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }
    }
}
