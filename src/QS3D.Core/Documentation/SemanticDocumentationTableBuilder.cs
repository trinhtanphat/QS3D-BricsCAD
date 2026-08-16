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
            SemanticTagRenderer.ValidateTemplate(template);
            Header = header;
            Template = template;
        }

        public string Header { get; }
        public string Template { get; }
    }

    public sealed class SemanticDocumentationRow
    {
        public SemanticDocumentationRow(string elementId, IReadOnlyList<string> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            ElementId = elementId;
            Cells = new List<string>(cells).AsReadOnly();
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
            Title = title;
            Headers = new List<string>(headers).AsReadOnly();
            Rows = new List<SemanticDocumentationRow>(rows).AsReadOnly();
        }

        public string Title { get; }
        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<SemanticDocumentationRow> Rows { get; }
    }

    public static class SemanticDocumentationTableBuilder
    {
        private const int MaxRows = 5000;
        private const int MaxColumns = 32;
        private const int MaxTitleLength = 160;
        private const int MaxHeaderLength = 96;
        private const int MaxElementIdLength = 128;

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
                .Select((value, index) => Required(value, "orderedElementIds[" + index + "]", MaxElementIdLength))
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
                var template = Required(column.Template, "columns[" + i + "].Template", 512);
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
            var result = new List<T>(Math.Min(maxCount, 256));
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= maxCount)
                        throw new InvalidOperationException("Documentation table supports at most " + maxCount + " " + label + ".");
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        private static string Required(string? value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value!.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }
    }
}
