using System;
using System.Collections;
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
            RequireKnownCountsWithinLimit(sheets);

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

        private static void RequireKnownCountsWithinLimit(IEnumerable<SemanticSheetPlan> sheets)
        {
            var counts = new List<int>(3);
            if (sheets is ICollection<SemanticSheetPlan> collection)
                counts.Add(collection.Count);
            if (sheets is IReadOnlyCollection<SemanticSheetPlan> readOnlyCollection)
                counts.Add(readOnlyCollection.Count);
            if (sheets is ICollection nonGenericCollection)
                counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0) return;

            var expected = counts[0];
            var maximum = expected;
            var hasNegative = expected < 0;
            var hasConflict = false;
            for (var i = 1; i < counts.Count; i++)
            {
                if (counts[i] < 0) hasNegative = true;
                if (counts[i] != expected) hasConflict = true;
                if (counts[i] > maximum) maximum = counts[i];
            }

            if (maximum > MaxSheets) throw TooManySheets();
            if (hasNegative)
                throw new InvalidOperationException("Semantic sheet index source reports an invalid negative known count.");
            if (hasConflict)
                throw new InvalidOperationException("Semantic sheet index source reports conflicting known counts.");
        }

        private static InvalidOperationException TooManySheets()
        {
            return new InvalidOperationException("Semantic sheet index supports at most " + MaxSheets + " sheets.");
        }
    }
}
