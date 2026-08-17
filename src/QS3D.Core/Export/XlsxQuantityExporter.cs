using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public static class XlsxQuantityExporter
    {
        private const int Decimal2Style = 2;
        private const int WrappedTextStyle = 3;
        private const int IntegerStyle = 4;
        private const int Decimal3Style = 5;
        private const int MaxDataRows = 1048575;
        private const int MaxCellTextCharacters = 32767;

        public static void Export(string path, IReadOnlyList<QuantityReportRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var snapshot = SnapshotStandardRows(rows);
            ExportCore(path, snapshot, null);
        }

        private static IReadOnlyList<QuantityReportRow> SnapshotStandardRows(IReadOnlyList<QuantityReportRow> rows)
        {
            var count = rows.Count;
            if (count > MaxDataRows) throw new ArgumentOutOfRangeException(nameof(rows), "Quantity XLSX export supports at most " + MaxDataRows + " data rows.");
            var snapshot = new List<QuantityReportRow>(count);
            for (var rowIndex = 0; rowIndex < count; rowIndex++)
            {
                var source = rows[rowIndex];
                if (source == null)
                    throw new ArgumentException("Export rows cannot contain null entries. Invalid row index: " + rowIndex + ".", nameof(rows));
                var row = new QuantityReportRow
                {
                    Floor = source.Floor ?? string.Empty,
                    Zone = source.Zone ?? string.Empty,
                    Category = source.Category ?? string.Empty,
                    FamilyName = source.FamilyName ?? string.Empty,
                    DrawingFingerprint = source.DrawingFingerprint ?? string.Empty,
                    Count = source.Count,
                    GrossConcreteM3 = source.GrossConcreteM3,
                    DeductionM3 = source.DeductionM3,
                    NetConcreteM3 = source.NetConcreteM3,
                    FormworkM2 = source.FormworkM2,
                    LengthM = source.LengthM,
                    OuterPerimeterM = source.OuterPerimeterM,
                    InnerPerimeterM = source.InnerPerimeterM,
                    DoorAreaM2 = source.DoorAreaM2,
                    SideAreaM2 = source.SideAreaM2,
                    BottomAreaM2 = source.BottomAreaM2,
                    TopAreaM2 = source.TopAreaM2,
                    OtherAreaM2 = source.OtherAreaM2
                };
                SnapshotStrings(source.ElementIds, row.ElementIds);
                SnapshotStrings(source.SourceHandles, row.SourceHandles);
                ValidateStandardRowText(row, rowIndex);
                ValidateStandardRowNumbers(row, rowIndex);
                snapshot.Add(row);
            }
            return snapshot;
        }

        private static void SnapshotStrings(IList<string> source, IList<string> target)
        {
            var count = source.Count;
            for (var index = 0; index < count; index++)
                target.Add(source[index] ?? string.Empty);
        }

        public static void ExportEd2(string path, IReadOnlyList<QuantityReportRow> detailRows, IReadOnlyList<QuantityReportRow> summaryRows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (detailRows == null) throw new ArgumentNullException(nameof(detailRows));
            if (summaryRows == null) throw new ArgumentNullException(nameof(summaryRows));
            var detailSnapshot = SnapshotEd2Rows(detailRows, nameof(detailRows), "ED2 CHI_TIET");
            var summarySnapshot = SnapshotEd2Rows(summaryRows, nameof(summaryRows), "ED2 TONG_HOP");

            var detailIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var detailHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? drawingFingerprint = null;
            var detailRowIndex = 0;
            foreach (var row in detailSnapshot)
            {
                ValidateEd2RowText(row, detailRowIndex, "ED2 CHI_TIET");
                ValidateEd2RowNumbers(row, detailRowIndex, "ED2 CHI_TIET");
                if (row.Count != 1 || row.ElementIds.Count != 1)
                    throw new InvalidDataException("ED2 CHI_TIET must contain exactly one semantic element per row.");
                var elementId = Required(row.ElementIds[0], "ED2 CHI_TIET Element ID");
                if (!detailIds.Add(elementId)) throw new InvalidDataException("ED2 CHI_TIET contains duplicate Element ID: " + elementId + ".");
                if (row.SourceHandles.Count == 0) throw new InvalidDataException("ED2 CHI_TIET row " + elementId + " has no CAD Handle provenance.");
                foreach (var handle in row.SourceHandles) detailHandles.Add(ValidHandle(handle, elementId));
                var fingerprint = Required(row.DrawingFingerprint, "ED2 drawing fingerprint");
                if (drawingFingerprint == null) drawingFingerprint = fingerprint;
                else if (!string.Equals(drawingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("ED2 CHI_TIET contains conflicting drawing fingerprints.");
                detailRowIndex++;
            }
            if (detailSnapshot.Count == 0) throw new InvalidDataException("ED2 CHI_TIET must contain at least one row.");

            var summaryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var summaryHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var summaryCount = 0;
            var summaryRowIndex = 0;
            foreach (var row in summarySnapshot)
            {
                ValidateEd2RowText(row, summaryRowIndex, "ED2 TONG_HOP");
                ValidateEd2RowNumbers(row, summaryRowIndex, "ED2 TONG_HOP");
                summaryCount = checked(summaryCount + row.Count);
                if (!string.Equals(Required(row.DrawingFingerprint, "ED2 TONG_HOP drawing fingerprint"), drawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("ED2 TONG_HOP drawing fingerprint does not match CHI_TIET.");
                foreach (var id in row.ElementIds)
                {
                    var elementId = Required(id, "ED2 TONG_HOP Element ID");
                    if (!summaryIds.Add(elementId)) throw new InvalidDataException("ED2 TONG_HOP repeats Element ID: " + elementId + ".");
                }
                foreach (var handle in row.SourceHandles) summaryHandles.Add(ValidHandle(handle, "TONG_HOP"));
                summaryRowIndex++;
            }
            if (summarySnapshot.Count == 0) throw new InvalidDataException("ED2 TONG_HOP must contain at least one row.");
            if (summaryCount != detailSnapshot.Count || !summaryIds.SetEquals(detailIds) || !summaryHandles.SetEquals(detailHandles))
                throw new InvalidDataException("ED2 CHI_TIET and TONG_HOP do not describe the same semantic scope.");
            ValidateEd2NumericParity(detailSnapshot, summarySnapshot);
            ExportCore(path, detailSnapshot, summarySnapshot);
        }

        private static IReadOnlyList<QuantityReportRow> SnapshotEd2Rows(
            IReadOnlyList<QuantityReportRow> rows,
            string parameterName,
            string sheetLabel)
        {
            var count = rows.Count;
            if (count > MaxDataRows)
                throw new ArgumentOutOfRangeException(parameterName, sheetLabel + " supports at most " + MaxDataRows + " data rows.");

            var snapshot = new List<QuantityReportRow>(count);
            for (var rowIndex = 0; rowIndex < count; rowIndex++)
            {
                var source = rows[rowIndex];
                if (source == null) throw new InvalidDataException(sheetLabel + " contains a null row.");
                var row = new QuantityReportRow
                {
                    Floor = source.Floor ?? string.Empty,
                    Zone = source.Zone ?? string.Empty,
                    Category = source.Category ?? string.Empty,
                    FamilyId = source.FamilyId ?? string.Empty,
                    FamilyName = source.FamilyName ?? string.Empty,
                    ElementName = source.ElementName ?? string.Empty,
                    Material = source.Material ?? string.Empty,
                    Note = source.Note ?? string.Empty,
                    DrawingFingerprint = source.DrawingFingerprint ?? string.Empty,
                    Count = source.Count,
                    GrossConcreteM3 = source.GrossConcreteM3,
                    DeductionM3 = source.DeductionM3,
                    NetConcreteM3 = source.NetConcreteM3,
                    FormworkM2 = source.FormworkM2,
                    LengthM = source.LengthM,
                    OuterPerimeterM = source.OuterPerimeterM,
                    InnerPerimeterM = source.InnerPerimeterM,
                    DoorAreaM2 = source.DoorAreaM2,
                    SideAreaM2 = source.SideAreaM2,
                    BottomAreaM2 = source.BottomAreaM2,
                    TopAreaM2 = source.TopAreaM2,
                    OtherAreaM2 = source.OtherAreaM2,
                    DensityKgM3 = source.DensityKgM3,
                    MassKg = source.MassKg
                };
                SnapshotStrings(source.ElementIds, row.ElementIds);
                SnapshotStrings(source.SourceHandles, row.SourceHandles);
                snapshot.Add(row);
            }
            return snapshot;
        }

        private static void ValidateEd2NumericParity(
            IReadOnlyList<QuantityReportRow> detailRows,
            IReadOnlyList<QuantityReportRow> summaryRows)
        {
            var detailById = detailRows.ToDictionary(
                row => Required(row.ElementIds[0], "ED2 CHI_TIET Element ID"),
                StringComparer.OrdinalIgnoreCase);

            foreach (var summary in summaryRows)
            {
                var group = summary.ElementIds
                    .Select(id => detailById[Required(id, "ED2 TONG_HOP Element ID")])
                    .ToList();
                if (group.Count == 0)
                    throw new InvalidDataException("ED2 TONG_HOP contains a summary row without CHI_TIET elements.");

                foreach (var detail in group) ValidateEd2SummaryIdentity(summary, detail);
                ValidateEd2SummaryHandleParity(summary, group);
                if (summary.Count != group.Count)
                    throw NumericParityError("Count");

                RequireAggregateParity(summary.GrossConcreteM3, group, x => x.GrossConcreteM3, "GrossConcreteM3");
                RequireAggregateParity(summary.DeductionM3, group, x => x.DeductionM3, "DeductionM3");
                RequireAggregateParity(summary.NetConcreteM3, group, x => x.NetConcreteM3, "NetConcreteM3");
                RequireAggregateParity(summary.FormworkM2, group, x => x.FormworkM2, "FormworkM2");
                RequireAggregateParity(summary.LengthM, group, x => x.LengthM, "LengthM");
                RequireAggregateParity(summary.OuterPerimeterM, group, x => x.OuterPerimeterM, "OuterPerimeterM");
                RequireAggregateParity(summary.InnerPerimeterM, group, x => x.InnerPerimeterM, "InnerPerimeterM");
                RequireAggregateParity(summary.DoorAreaM2, group, x => x.DoorAreaM2, "DoorAreaM2");
                RequireAggregateParity(summary.SideAreaM2, group, x => x.SideAreaM2, "SideAreaM2");
                RequireAggregateParity(summary.BottomAreaM2, group, x => x.BottomAreaM2, "BottomAreaM2");
                RequireAggregateParity(summary.TopAreaM2, group, x => x.TopAreaM2, "TopAreaM2");
                RequireAggregateParity(summary.OtherAreaM2, group, x => x.OtherAreaM2, "OtherAreaM2");
                RequireDensityParity(summary, group);
                RequireMassParity(summary, group);
            }
        }

        private static void ValidateEd2SummaryHandleParity(
            QuantityReportRow summary,
            IReadOnlyList<QuantityReportRow> group)
        {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var detail in group)
                foreach (var handle in detail.SourceHandles)
                    expected.Add(ValidHandle(handle, Required(detail.ElementIds[0], "ED2 CHI_TIET Element ID")));
            var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in summary.SourceHandles) actual.Add(ValidHandle(handle, "TONG_HOP"));
            if (!actual.SetEquals(expected))
                throw new InvalidDataException("ED2 TONG_HOP CAD Handle provenance does not match its CHI_TIET elements.");
        }

        private static void ValidateEd2SummaryIdentity(QuantityReportRow summary, QuantityReportRow detail)
        {
            RequireIdentityParity(summary.Floor, detail.Floor, "Floor");
            RequireIdentityParity(summary.Zone, detail.Zone, "Zone");
            RequireIdentityParity(summary.Category, detail.Category, "Category");
            RequireIdentityParity(summary.FamilyId, detail.FamilyId, "FamilyId");
            RequireIdentityParity(summary.FamilyName, detail.FamilyName, "FamilyName");
            RequireIdentityParity(summary.Material, detail.Material, "Material");
        }

        private static void RequireIdentityParity(string summaryValue, string detailValue, string field)
        {
            if (!string.Equals((summaryValue ?? string.Empty).Trim(), (detailValue ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("ED2 TONG_HOP " + field + " does not match its CHI_TIET elements.");
        }

        private static void RequireAggregateParity(
            double actual,
            IReadOnlyList<QuantityReportRow> group,
            Func<QuantityReportRow, double> selector,
            string field)
        {
            var accumulator = new QuantityReportMath.FiniteAccumulator();
            foreach (var detail in group)
                AddCompensatedEd2(ref accumulator, selector(detail), field);
            var expected = ValueCompensatedEd2(ref accumulator, field);
            RequireFinite(actual, field);
            if (actual != expected) throw NumericParityError(field);
        }

        private static void RequireDensityParity(QuantityReportRow summary, IReadOnlyList<QuantityReportRow> group)
        {
            var expected = group[0].DensityKgM3;
            ValidateDensity(expected);
            foreach (var detail in group)
            {
                ValidateDensity(detail.DensityKgM3);
                if (!NullableEqual(detail.DensityKgM3, expected))
                    throw new InvalidDataException("ED2 TONG_HOP groups CHI_TIET elements with different density values.");
            }
            ValidateDensity(summary.DensityKgM3);
            if (!NullableEqual(summary.DensityKgM3, expected)) throw NumericParityError("DensityKgM3");
        }

        private static void RequireMassParity(QuantityReportRow summary, IReadOnlyList<QuantityReportRow> group)
        {
            var accumulator = new QuantityReportMath.FiniteAccumulator();
            var hasNullMass = false;
            foreach (var detail in group)
            {
                ValidateMass(detail.MassKg);
                if (!detail.MassKg.HasValue)
                {
                    hasNullMass = true;
                    continue;
                }
                if (!hasNullMass)
                    AddCompensatedEd2(ref accumulator, detail.MassKg.Value, "MassKg");
            }
            double? expected = hasNullMass ? (double?)null : ValueCompensatedEd2(ref accumulator, "MassKg");
            ValidateMass(summary.MassKg);
            if (!NullableEqual(summary.MassKg, expected)) throw NumericParityError("MassKg");
        }

        private static void AddCompensatedEd2(
            ref QuantityReportMath.FiniteAccumulator accumulator,
            double value,
            string field)
        {
            RequireFinite(value, field);
            try
            {
                accumulator.Add(value, "ED2/" + field);
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("ED2 " + field + " aggregate exceeds the supported numeric range.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException("ED2 " + field + " aggregate must be finite.", ex);
            }
        }

        private static double ValueCompensatedEd2(
            ref QuantityReportMath.FiniteAccumulator accumulator,
            string field)
        {
            try
            {
                return accumulator.Value("ED2/" + field);
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("ED2 " + field + " aggregate exceeds the supported numeric range.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException("ED2 " + field + " aggregate must be finite.", ex);
            }
        }

        private static void ValidateDensity(double? value)
        {
            if (!value.HasValue) return;
            RequireFinite(value.Value, "DensityKgM3");
            if (value.Value <= 0d) throw new InvalidDataException("ED2 density must be greater than zero when present.");
        }

        private static void ValidateMass(double? value)
        {
            if (!value.HasValue) return;
            RequireFinite(value.Value, "MassKg");
            if (value.Value < 0d) throw new InvalidDataException("ED2 mass must be non-negative when present.");
        }

        private static void RequireFinite(double value, string field)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException("ED2 " + field + " must be finite.");
        }

        private static bool NullableEqual(double? left, double? right) =>
            left.HasValue == right.HasValue && (!left.HasValue || left.Value == right!.Value);

        private static InvalidDataException NumericParityError(string field) =>
            new InvalidDataException("ED2 TONG_HOP " + field + " does not equal the CHI_TIET aggregate.");

        private static string Required(string? value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidDataException(label + " is required.");
            return normalized;
        }

        private static string ValidHandle(string? value, string owner)
        {
            var handle = Required(value, "ED2 CAD Handle for " + owner);
            var token = handle.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? handle.Substring(2) : handle;
            if (!long.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number) || number <= 0)
                throw new InvalidDataException("ED2 contains an invalid CAD Handle for " + owner + ": " + handle + ".");
            return number.ToString("X", CultureInfo.InvariantCulture);
        }

        private static void ValidateStandardRowText(QuantityReportRow row, int rowIndex)
        {
            ValidateCellText(row.Floor, rowIndex, "Floor", "Quantity XLSX");
            ValidateCellText(row.Zone, rowIndex, "Zone", "Quantity XLSX");
            ValidateCellText(row.Category, rowIndex, "Category", "Quantity XLSX");
            ValidateCellText(row.FamilyName, rowIndex, "FamilyName", "Quantity XLSX");
            ValidateJoinedNonBlankCellText(row.ElementIds, rowIndex, "ElementIds", "Quantity XLSX");
            ValidateJoinedNonBlankCellText(row.SourceHandles, rowIndex, "SourceHandles", "Quantity XLSX");
            ValidateCellText(row.DrawingFingerprint, rowIndex, "DrawingFingerprint", "Quantity XLSX");
        }

        private static void ValidateStandardRowNumbers(QuantityReportRow row, int rowIndex)
        {
            if (row.Count < 0)
                throw StandardNumericError(rowIndex, "Count", "must be non-negative");
            ValidateStandardNumber(row.GrossConcreteM3, rowIndex, "GrossConcreteM3");
            ValidateStandardNumber(row.DeductionM3, rowIndex, "DeductionM3");
            ValidateStandardNumber(row.NetConcreteM3, rowIndex, "NetConcreteM3");
            ValidateStandardNumber(row.FormworkM2, rowIndex, "FormworkM2");
            ValidateStandardNumber(row.LengthM, rowIndex, "LengthM");
            ValidateStandardNumber(row.OuterPerimeterM, rowIndex, "OuterPerimeterM");
            ValidateStandardNumber(row.InnerPerimeterM, rowIndex, "InnerPerimeterM");
            ValidateStandardNumber(row.DoorAreaM2, rowIndex, "DoorAreaM2");
            ValidateStandardNumber(row.SideAreaM2, rowIndex, "SideAreaM2");
            ValidateStandardNumber(row.BottomAreaM2, rowIndex, "BottomAreaM2");
            ValidateStandardNumber(row.TopAreaM2, rowIndex, "TopAreaM2");
            ValidateStandardNumber(row.OtherAreaM2, rowIndex, "OtherAreaM2");
        }

        private static void ValidateStandardNumber(double value, int rowIndex, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw StandardNumericError(rowIndex, fieldName, "must be finite");
            if (value < 0d)
                throw StandardNumericError(rowIndex, fieldName, "must be non-negative");
        }

        private static ArgumentOutOfRangeException StandardNumericError(int rowIndex, string fieldName, string requirement)
        {
            return new ArgumentOutOfRangeException(
                "rows",
                "Quantity XLSX worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) + " field " + fieldName + " " + requirement + ".");
        }

        private static void ValidateEd2RowText(QuantityReportRow row, int rowIndex, string sheetLabel)
        {
            ValidateCellText(string.IsNullOrWhiteSpace(row.ElementName) ? row.FamilyName : row.ElementName, rowIndex, "DisplayName", sheetLabel);
            ValidateCellText(row.Category, rowIndex, "Category", sheetLabel);
            ValidateCellText(row.Material, rowIndex, "Material", sheetLabel);
            ValidateCellText(row.FamilyId, rowIndex, "FamilyId", sheetLabel);
            ValidateFloorZoneCellText(row, rowIndex, sheetLabel);
            ValidateCellText(row.Note, rowIndex, "Note", sheetLabel);
            ValidateJoinedNonBlankCellText(row.ElementIds, rowIndex, "ElementIds", sheetLabel);
            ValidateJoinedNonBlankCellText(row.SourceHandles, rowIndex, "SourceHandles", sheetLabel);
            ValidateCellText(row.DrawingFingerprint, rowIndex, "DrawingFingerprint", sheetLabel);
        }

        private static void ValidateEd2RowNumbers(QuantityReportRow row, int rowIndex, string sheetLabel)
        {
            if (row.Count < 0) ThrowEd2Negative(rowIndex, "Count", sheetLabel);
            ValidateEd2NonNegative(row.GrossConcreteM3, rowIndex, "GrossConcreteM3", sheetLabel);
            ValidateEd2NonNegative(row.DeductionM3, rowIndex, "DeductionM3", sheetLabel);
            ValidateEd2NonNegative(row.NetConcreteM3, rowIndex, "NetConcreteM3", sheetLabel);
            ValidateEd2NonNegative(row.FormworkM2, rowIndex, "FormworkM2", sheetLabel);
            ValidateEd2NonNegative(row.LengthM, rowIndex, "LengthM", sheetLabel);
            ValidateEd2NonNegative(row.OuterPerimeterM, rowIndex, "OuterPerimeterM", sheetLabel);
            ValidateEd2NonNegative(row.InnerPerimeterM, rowIndex, "InnerPerimeterM", sheetLabel);
            ValidateEd2NonNegative(row.DoorAreaM2, rowIndex, "DoorAreaM2", sheetLabel);
            ValidateEd2NonNegative(row.SideAreaM2, rowIndex, "SideAreaM2", sheetLabel);
            ValidateEd2NonNegative(row.BottomAreaM2, rowIndex, "BottomAreaM2", sheetLabel);
            ValidateEd2NonNegative(row.TopAreaM2, rowIndex, "TopAreaM2", sheetLabel);
            ValidateEd2NonNegative(row.OtherAreaM2, rowIndex, "OtherAreaM2", sheetLabel);
        }

        private static void ValidateEd2NonNegative(double value, int rowIndex, string fieldName, string sheetLabel)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value) && value < 0d)
                ThrowEd2Negative(rowIndex, fieldName, sheetLabel);
        }

        private static void ThrowEd2Negative(int rowIndex, string fieldName, string sheetLabel)
        {
            throw new InvalidDataException(
                sheetLabel + " worksheet row " + (rowIndex + 2).ToString(CultureInfo.InvariantCulture) +
                " field " + fieldName + " must be non-negative.");
        }

        private static void ValidateFloorZoneCellText(QuantityReportRow row, int rowIndex, string sheetLabel)
        {
            var floor = row.Floor ?? string.Empty;
            var zone = row.Zone ?? string.Empty;
            if (string.IsNullOrWhiteSpace(floor))
            {
                ValidateCellText(zone, rowIndex, "FloorZone", sheetLabel);
                return;
            }
            if (string.IsNullOrWhiteSpace(zone))
            {
                ValidateCellText(floor, rowIndex, "FloorZone", sheetLabel);
                return;
            }
            if ((long)floor.Length + 3L + zone.Length > MaxCellTextCharacters)
                ThrowCellTextLimit(rowIndex, "FloorZone", sheetLabel);
        }

        private static void ValidateCellText(string? value, int rowIndex, string fieldName, string sheetLabel)
        {
            if ((value ?? string.Empty).Length > MaxCellTextCharacters)
                ThrowCellTextLimit(rowIndex, fieldName, sheetLabel);
        }

        private static void ValidateJoinedNonBlankCellText(IList<string> values, int rowIndex, string fieldName, string sheetLabel)
        {
            long length = 0;
            var hasValue = false;
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (hasValue) length++;
                length += value.Length;
                if (length > MaxCellTextCharacters)
                    ThrowCellTextLimit(rowIndex, fieldName, sheetLabel);
                hasValue = true;
            }
        }

        private static void ThrowCellTextLimit(int rowIndex, string fieldName, string sheetLabel)
        {
            throw new ArgumentOutOfRangeException(
                "rows",
                sheetLabel + " row " + rowIndex + " field " + fieldName + " exceeds Excel's " + MaxCellTextCharacters + "-character cell text limit.");
        }

        private static void ExportCore(string path, IReadOnlyList<QuantityReportRow> rows, IReadOnlyList<QuantityReportRow>? summaryRows)
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
                {
                    var isEd2 = summaryRows != null;
                    WriteEntry(archive, "[Content_Types].xml", isEd2 ? Ed2ContentTypesXml : ContentTypesXml);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml);
                    WriteEntry(archive, "xl/workbook.xml", isEd2 ? Ed2WorkbookXml : WorkbookXml);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", isEd2 ? Ed2WorkbookRelationshipsXml : WorkbookRelationshipsXml);
                    WriteEntry(archive, "xl/styles.xml", StylesXml);
                    WriteEntry(archive, "xl/worksheets/sheet1.xml", isEd2 ? BuildEd2Sheet(rows) : BuildSheet(rows));
                    if (summaryRows != null) WriteEntry(archive, "xl/worksheets/sheet2.xml", BuildEd2Sheet(summaryRows));
                }
                ValidatePackage(tempPath, summaryRows != null);
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static string BuildSheet(IReadOnlyList<QuantityReportRow> rows)
        {
            var headers = new[]
            {
                "Tầng", "Zone", "Loại", "Tên cấu kiện", "SL", "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)",
                "Cốp pha (m²)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)", "DT cửa (m²)",
                "Thành bên (m²)", "DT đáy (m²)", "DT đỉnh (m²)", "DT khác (m²)",
                "QS3D Element ID", "CAD Handle (hex)", "QS3D Drawing Fingerprint"
            };

            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:T" + lastRow.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sb.Append("<sheetData>");
            sb.Append("<row r=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendInlineStringCell(sb, CellRef(c, 1), headers[c], 1);
            sb.Append("</row>");

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var r = i + 2;
                sb.Append("<row r=\"").Append(r).Append("\">");
                AppendInlineStringCell(sb, CellRef(0, r), row.Floor, 0);
                AppendInlineStringCell(sb, CellRef(1, r), row.Zone, 0);
                AppendInlineStringCell(sb, CellRef(2, r), row.Category, 0);
                AppendInlineStringCell(sb, CellRef(3, r), row.FamilyName, 0);
                AppendNumberCell(sb, CellRef(4, r), row.Count, IntegerStyle);
                AppendNumberCell(sb, CellRef(5, r), row.GrossConcreteM3, Decimal3Style);
                AppendNumberCell(sb, CellRef(6, r), row.DeductionM3, Decimal3Style);
                AppendNumberCell(sb, CellRef(7, r), row.NetConcreteM3, Decimal3Style);
                AppendNumberCell(sb, CellRef(8, r), row.FormworkM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(9, r), row.LengthM, Decimal3Style);
                AppendNumberCell(sb, CellRef(10, r), row.OuterPerimeterM, Decimal3Style);
                AppendNumberCell(sb, CellRef(11, r), row.InnerPerimeterM, Decimal3Style);
                AppendNumberCell(sb, CellRef(12, r), row.DoorAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(13, r), row.SideAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(14, r), row.BottomAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(15, r), row.TopAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(16, r), row.OtherAreaM2, Decimal3Style);
                AppendInlineStringCell(sb, CellRef(17, r), row.ElementIdText, 0);
                AppendInlineStringCell(sb, CellRef(18, r), row.SourceHandleText, 0);
                AppendInlineStringCell(sb, CellRef(19, r), row.DrawingFingerprint, 0);
                sb.Append("</row>");
            }

            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static string BuildEd2Sheet(IReadOnlyList<QuantityReportRow> rows)
        {
            var headers = new[]
            {
                "STT", "Tên cấu kiện", "Loại", "Vật liệu", "Family ID", "Tầng/Zone", "SL",
                "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)", "Cốp pha (m²)", "Dài (m)",
                "Chu vi ngoài (m)", "Chu vi trong (m)", "DT cửa (m²)", "Thành bên (m²)",
                "DT đáy (m²)", "DT đỉnh (m²)", "DT khác (m²)", "Khối lượng riêng (kg/m³)",
                "Khối lượng (kg)", "Ghi chú", "QS3D Element ID", "CAD Handle (hex)", "QS3D Drawing Fingerprint"
            };

            var lastRow = Math.Max(1, rows.Count + 1);
            var range = "A1:Y" + lastRow.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<dimension ref=\"").Append(range).Append("\"/>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sb.Append(Ed2ColumnWidthsXml);
            sb.Append("<sheetData><row r=\"1\" ht=\"30\" customHeight=\"1\">");
            for (var c = 0; c < headers.Length; c++) AppendInlineStringCell(sb, CellRef(c, 1), headers[c], 1);
            sb.Append("</row>");

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) throw new InvalidDataException("ED2 worksheet contains a null quantity row.");
                var r = i + 2;
                var displayName = string.IsNullOrWhiteSpace(row.ElementName) ? row.FamilyName : row.ElementName;
                sb.Append("<row r=\"").Append(r).Append("\"");
                if (row.Count > 1) sb.Append(" ht=\"96\" customHeight=\"1\"");
                sb.Append(">");
                AppendNumberCell(sb, CellRef(0, r), i + 1, IntegerStyle);
                AppendInlineStringCell(sb, CellRef(1, r), displayName, 0);
                AppendInlineStringCell(sb, CellRef(2, r), row.Category, 0);
                AppendInlineStringCell(sb, CellRef(3, r), row.Material, 0);
                AppendInlineStringCell(sb, CellRef(4, r), row.FamilyId, 0);
                AppendInlineStringCell(sb, CellRef(5, r), row.FloorZoneText, 0);
                AppendNumberCell(sb, CellRef(6, r), row.Count, IntegerStyle);
                AppendNumberCell(sb, CellRef(7, r), row.GrossConcreteM3, Decimal3Style);
                AppendNumberCell(sb, CellRef(8, r), row.DeductionM3, Decimal3Style);
                AppendNumberCell(sb, CellRef(9, r), row.NetConcreteM3, Decimal3Style);
                AppendNumberCell(sb, CellRef(10, r), row.FormworkM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(11, r), row.LengthM, Decimal3Style);
                AppendNumberCell(sb, CellRef(12, r), row.OuterPerimeterM, Decimal3Style);
                AppendNumberCell(sb, CellRef(13, r), row.InnerPerimeterM, Decimal3Style);
                AppendNumberCell(sb, CellRef(14, r), row.DoorAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(15, r), row.SideAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(16, r), row.BottomAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(17, r), row.TopAreaM2, Decimal3Style);
                AppendNumberCell(sb, CellRef(18, r), row.OtherAreaM2, Decimal3Style);
                AppendNullableNumberCell(sb, CellRef(19, r), row.DensityKgM3, Decimal2Style);
                AppendNullableNumberCell(sb, CellRef(20, r), row.MassKg, Decimal2Style);
                AppendInlineStringCell(sb, CellRef(21, r), row.Note, WrappedTextStyle);
                AppendInlineStringCell(sb, CellRef(22, r), row.ElementIdText, WrappedTextStyle);
                AppendInlineStringCell(sb, CellRef(23, r), row.SourceHandleText, WrappedTextStyle);
                AppendInlineStringCell(sb, CellRef(24, r), row.DrawingFingerprint, WrappedTextStyle);
                sb.Append("</row>");
            }

            sb.Append("</sheetData><autoFilter ref=\"").Append(range).Append("\"/></worksheet>");
            return sb.ToString();
        }

        private static void ValidatePackage(string path, bool isEd2)
        {
            if (isEd2)
                XlsxPackageValidator.Validate(path, "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml");
            else
                XlsxPackageValidator.Validate(path, "[Content_Types].xml", "xl/workbook.xml", "xl/styles.xml", "xl/worksheets/sheet1.xml");
        }

        private static void AppendInlineStringCell(StringBuilder sb, string cellRef, string value, int style)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\" s=\"").Append(style).Append("\"><is><t>")
                .Append(XlsxXmlText.Escape(value)).Append("</t></is></c>");
        }

        private static void AppendNumberCell(StringBuilder sb, string cellRef, double value, int style = Decimal2Style)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "XLSX numeric values must be finite.");
            sb.Append("<c r=\"").Append(cellRef).Append("\" s=\"").Append(style).Append("\"><v>")
                .Append(value.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
        }

        private static void AppendNullableNumberCell(StringBuilder sb, string cellRef, double? value, int style = Decimal2Style)
        {
            if (!value.HasValue) return;
            AppendNumberCell(sb, cellRef, value.Value, style);
        }

        private static string CellRef(int columnZeroBased, int row)
        {
            var n = columnZeroBased + 1;
            var name = string.Empty;
            while (n > 0)
            {
                n--;
                name = (char)('A' + (n % 26)) + name;
                n /= 26;
            }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);
        }

        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string Ed2ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>";
        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Khối lượng\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        private const string Ed2WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"CHI_TIET\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"TONG_HOP\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string Ed2WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
        private const string Ed2ColumnWidthsXml =
            "<cols>" +
            "<col min=\"1\" max=\"1\" width=\"7\" customWidth=\"1\"/>" +
            "<col min=\"2\" max=\"2\" width=\"22\" customWidth=\"1\"/>" +
            "<col min=\"3\" max=\"4\" width=\"18\" customWidth=\"1\"/>" +
            "<col min=\"5\" max=\"5\" width=\"20\" customWidth=\"1\"/>" +
            "<col min=\"6\" max=\"6\" width=\"22\" customWidth=\"1\"/>" +
            "<col min=\"7\" max=\"7\" width=\"7\" customWidth=\"1\"/>" +
            "<col min=\"8\" max=\"19\" width=\"14\" customWidth=\"1\"/>" +
            "<col min=\"20\" max=\"21\" width=\"18\" customWidth=\"1\"/>" +
            "<col min=\"22\" max=\"22\" width=\"28\" customWidth=\"1\"/>" +
            "<col min=\"23\" max=\"23\" width=\"26\" customWidth=\"1\"/>" +
            "<col min=\"24\" max=\"24\" width=\"28\" customWidth=\"1\"/>" +
            "<col min=\"25\" max=\"25\" width=\"42\" customWidth=\"1\"/>" +
            "</cols>";

        private const string StylesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.000\"/></numFmts><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font><font><b/><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFC000\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"2\"><border/><border><left style=\"thin\"><color rgb=\"FFD9D9D9\"/></left><right style=\"thin\"><color rgb=\"FFD9D9D9\"/></right><top style=\"thin\"><color rgb=\"FFD9D9D9\"/></top><bottom style=\"thin\"><color rgb=\"FFD9D9D9\"/></bottom></border></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"6\"><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"4\" applyNumberFormat=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"3\" applyNumberFormat=\"1\"/><xf fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" numFmtId=\"164\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>";
    }
}