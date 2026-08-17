using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticSheetIndexRow
    {
        internal SemanticSheetIndexRow(
            string sheetId,
            string number,
            string name,
            string? titleBlockName,
            int placedViewCount)
        {
            SheetId = sheetId;
            Number = number;
            Name = name;
            TitleBlockName = titleBlockName;
            PlacedViewCount = placedViewCount;
        }

        public string SheetId { get; }
        public string Number { get; }
        public string Name { get; }
        public string? TitleBlockName { get; }
        public int PlacedViewCount { get; }
    }

    public sealed class SemanticSheetIndex
    {
        internal SemanticSheetIndex(IEnumerable<SemanticSheetIndexRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            Rows = new List<SemanticSheetIndexRow>(rows).AsReadOnly();
        }

        public IReadOnlyList<SemanticSheetIndexRow> Rows { get; }
    }

    public static class SemanticSheetIndexBuilder
    {
        private const int MaxSheets = 10000;

        public static SemanticSheetIndex Build(IEnumerable<SemanticSheetPlan> sheets)
        {
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));

            var materialized = MaterializeBounded(sheets);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rows = new List<SemanticSheetIndexRow>(materialized.Count);

            for (var i = 0; i < materialized.Count; i++)
            {
                var sheet = materialized[i];
                if (!ids.Add(sheet.Id))
                    throw new InvalidOperationException("Semantic sheet index contains duplicate sheet id: " + sheet.Id + ".");
                if (!numbers.Add(sheet.Number))
                    throw new InvalidOperationException("Semantic sheet index contains duplicate sheet number: " + sheet.Number + ".");

                rows.Add(new SemanticSheetIndexRow(
                    sheet.Id,
                    sheet.Number,
                    sheet.Name,
                    sheet.TitleBlockName,
                    sheet.Placements.Count));
            }

            return new SemanticSheetIndex(rows
                .OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SheetId, StringComparer.OrdinalIgnoreCase));
        }

        private static List<SemanticSheetPlan> MaterializeBounded(IEnumerable<SemanticSheetPlan> sheets)
        {
            if (sheets is ICollection<SemanticSheetPlan> collection && collection.Count > MaxSheets)
                throw TooManySheets();
            if (sheets is IReadOnlyCollection<SemanticSheetPlan> readOnlyCollection && readOnlyCollection.Count > MaxSheets)
                throw TooManySheets();

            var result = new List<SemanticSheetPlan>(Math.Min(MaxSheets, 256));
            using (var enumerator = sheets.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= MaxSheets)
                        throw TooManySheets();
                    var sheet = enumerator.Current;
                    if (sheet == null)
                        throw new ArgumentException("Semantic sheet index source cannot contain a null sheet at index " + result.Count + ".", nameof(sheets));
                    result.Add(sheet);
                }
            }
            return result;
        }

        private static InvalidOperationException TooManySheets()
        {
            return new InvalidOperationException("Semantic sheet index supports at most " + MaxSheets + " sheets.");
        }
    }
}
