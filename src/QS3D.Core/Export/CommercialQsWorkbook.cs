using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using QS3D.Core.Commercial;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public sealed class CommercialQsWorkbookSnapshot
    {
        public CommercialQsWorkbookSnapshot(
            CommercialVariationRegister? variations,
            InterimPaymentCertificate? ipc,
            FinalAccountResult? finalAccount,
            TenderProcurementPackage? tenderPackage,
            TenderProcurementEvaluation? tenderEvaluation,
            TenderAwardDecision? tenderAward,
            CommercialCostControlResult? cvr)
        {
            if (variations == null && ipc == null && finalAccount == null && tenderPackage == null && tenderEvaluation == null && tenderAward == null && cvr == null)
                throw new ArgumentException("Commercial workbook requires at least one validated Core result.");

            if (tenderEvaluation != null)
            {
                if (tenderPackage == null)
                    throw new InvalidOperationException("Commercial workbook tender evaluation requires the current procurement package.");
                if (!string.Equals(tenderEvaluation.PackageId, tenderPackage.PackageId, StringComparison.OrdinalIgnoreCase) ||
                    !SameRevision(tenderEvaluation.PackageRevision, tenderPackage.Revision))
                    throw new InvalidOperationException("Commercial workbook tender evaluation is stale for the procurement package revision.");
            }

            if (tenderAward != null && tenderPackage != null)
            {
                if (!string.Equals(tenderAward.PackageId, tenderPackage.PackageId, StringComparison.OrdinalIgnoreCase) ||
                    !SameRevision(tenderAward.PackageRevision, tenderPackage.Revision) ||
                    !string.Equals(tenderAward.Currency, tenderPackage.Currency, StringComparison.Ordinal))
                    throw new InvalidOperationException("Commercial workbook tender award is stale for the procurement package revision.");
            }

            if (tenderAward != null && tenderEvaluation != null)
            {
                if (!string.Equals(tenderAward.PackageId, tenderEvaluation.PackageId, StringComparison.OrdinalIgnoreCase) ||
                    !SameRevision(tenderAward.PackageRevision, tenderEvaluation.PackageRevision) ||
                    !string.Equals(tenderAward.BidId, tenderEvaluation.RecommendedBidId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Commercial workbook tender award is stale for the tender evaluation.");
            }

            Variations = variations;
            Ipc = ipc;
            FinalAccount = finalAccount;
            TenderPackage = tenderPackage;
            TenderEvaluation = tenderEvaluation;
            TenderAward = tenderAward;
            Cvr = cvr;
        }

        public CommercialVariationRegister? Variations { get; }
        public InterimPaymentCertificate? Ipc { get; }
        public FinalAccountResult? FinalAccount { get; }
        public TenderProcurementPackage? TenderPackage { get; }
        public TenderProcurementEvaluation? TenderEvaluation { get; }
        public TenderAwardDecision? TenderAward { get; }
        public CommercialCostControlResult? Cvr { get; }

        private static bool SameRevision(CommercialRevisionRef left, CommercialRevisionRef right)
        {
            return left != null && right != null &&
                string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal) &&
                string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal) &&
                string.Equals(left.RevisionId, right.RevisionId, StringComparison.Ordinal);
        }
    }

    public static class CommercialQsWorkbook
    {
        public const string SchemaVersion = "QS3D_COMMERCIAL_QS_V1";
        private const string MetaSheetName = "META";
        private const string VariationsSheetName = "VARIATIONS";
        private const string IpcSheetName = "IPC";
        private const string FinalAccountSheetName = "FINAL_ACCOUNT";
        private const string TenderSheetName = "TENDER";
        private const string CvrSheetName = "CVR";
        private static readonly DateTimeOffset FixedZipTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private const int MaxCellCharacters = 32767;
        private const int MaxWorksheetRows = 10002;
        private const long MaxEntryBytes = 32L * 1024L * 1024L;
        private const long MaxTotalUncompressedBytes = 64L * 1024L * 1024L;
        private const long MaxArchiveBytes = 64L * 1024L * 1024L;

        public static void Export(string path, CommercialQsWorkbookSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var metaRows = MetaRows(snapshot);
            var variationRows = VariationRows(snapshot.Variations);
            var ipcRows = IpcRows(snapshot.Ipc);
            var finalAccountRows = FinalAccountRows(snapshot.FinalAccount);
            var tenderRows = TenderRows(snapshot);
            var cvrRows = CvrRows(snapshot.Cvr);
            ValidateWorkbookPayload(metaRows, variationRows, ipcRows, finalAccountRows, tenderRows, cvrRows);

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                long totalUncompressedBytes = 0L;
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var boundedStream = new BoundedArchiveWriteStream(stream, MaxArchiveBytes))
                using (var archive = new ZipArchive(boundedStream, ZipArchiveMode.Create, true, Encoding.UTF8))
                {
                    WriteEntry(archive, "[Content_Types].xml", ContentTypesXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "_rels/.rels", RootRelationshipsXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/workbook.xml", WorkbookXml, ref totalUncompressedBytes);
                    WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml, ref totalUncompressedBytes);
                    WriteSheetEntry(archive, "xl/worksheets/sheet1.xml", new[] { "KEY", "VALUE" }, metaRows, ref totalUncompressedBytes);
                    WriteSheetEntry(
                        archive,
                        "xl/worksheets/sheet2.xml",
                        new[] { "VARIATION_ID", "DESCRIPTION", "STATUS", "PROPOSED", "APPROVED", "CURRENCY", "REVISION" },
                        variationRows,
                        ref totalUncompressedBytes);
                    WriteSheetEntry(
                        archive,
                        "xl/worksheets/sheet3.xml",
                        new[] { "CERTIFICATE_ID", "GROSS_THIS_PERIOD", "RETENTION_THIS_PERIOD", "NET_THIS_PERIOD", "CUMULATIVE_NET", "CURRENCY" },
                        ipcRows,
                        ref totalUncompressedBytes);
                    WriteSheetEntry(
                        archive,
                        "xl/worksheets/sheet4.xml",
                        new[] { "FINAL_ACCOUNT_ID", "FINAL_CONTRACT_VALUE", "AMOUNT_DUE", "RECOVERY_DUE", "UNRELEASED_RETENTION", "CURRENCY" },
                        finalAccountRows,
                        ref totalUncompressedBytes);
                    WriteSheetEntry(
                        archive,
                        "xl/worksheets/sheet5.xml",
                        new[] { "RECORD", "PACKAGE_ID", "DESCRIPTION", "STATUS", "PACKAGE_REVISION", "RECOMMENDED_BID", "AWARD_ID", "AWARDED_BID", "BIDDER", "EVALUATED_TOTAL", "CURRENCY", "AWARD_REVISION", "BID_ID", "COMMERCIAL_RANK", "MANDATORY_COMPLIANCE" },
                        tenderRows,
                        ref totalUncompressedBytes);
                    WriteSheetEntry(
                        archive,
                        "xl/worksheets/sheet6.xml",
                        new[] { "PERIOD_ID", "STATUS", "REVISION", "REVISED_BUDGET", "COST_TO_DATE", "COMMITTED_EXPOSURE", "FORECAST_TO_COMPLETE", "FORECAST_FINAL_COST", "FORECAST_VARIANCE", "EARNED_VALUE", "CVR_MARGIN", "CURRENCY" },
                        cvrRows,
                        ref totalUncompressedBytes);
                }

                var archiveLength = new FileInfo(tempPath).Length;
                if (archiveLength <= 0L || archiveLength > MaxArchiveBytes)
                    throw new InvalidDataException("Commercial workbook archive exceeds the bounded output contract.");

                XlsxPackageValidator.Validate(
                    tempPath,
                    "[Content_Types].xml",
                    "xl/workbook.xml",
                    "xl/_rels/workbook.xml.rels",
                    "xl/worksheets/sheet1.xml",
                    "xl/worksheets/sheet2.xml",
                    "xl/worksheets/sheet3.xml",
                    "xl/worksheets/sheet4.xml",
                    "xl/worksheets/sheet5.xml",
                    "xl/worksheets/sheet6.xml");
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static IReadOnlyList<IReadOnlyList<string>> MetaRows(CommercialQsWorkbookSnapshot snapshot)
        {
            var rows = new List<IReadOnlyList<string>>
            {
                new[] { "SCHEMA", SchemaVersion },
                new[] { "SHEET", MetaSheetName },
                new[] { VariationsSheetName, snapshot.Variations == null ? "absent" : "present" },
                new[] { IpcSheetName, snapshot.Ipc == null ? "absent" : "present" },
                new[] { FinalAccountSheetName, snapshot.FinalAccount == null ? "absent" : "present" },
                new[] { TenderSheetName, snapshot.TenderPackage == null && snapshot.TenderEvaluation == null && snapshot.TenderAward == null ? "absent" : "present" },
                new[] { CvrSheetName, snapshot.Cvr == null ? "absent" : "present" }
            };
            return rows;
        }

        private static IReadOnlyList<IReadOnlyList<string>> VariationRows(CommercialVariationRegister? register)
        {
            var rows = new List<IReadOnlyList<string>>();
            if (register == null) return rows;
            for (var i = 0; i < register.Variations.Count; i++)
            {
                var item = register.Variations[i];
                rows.Add(new[]
                {
                    item.VariationId,
                    item.Description,
                    item.Status.ToString(),
                    Invariant(item.ProposedAmount),
                    Invariant(item.ApprovedAmount),
                    item.Currency,
                    item.Revision.RevisionId
                });
            }
            return rows;
        }

        private static IReadOnlyList<IReadOnlyList<string>> IpcRows(InterimPaymentCertificate? ipc)
        {
            var rows = new List<IReadOnlyList<string>>();
            if (ipc == null) return rows;
            rows.Add(new[]
            {
                ipc.CertificateId,
                Invariant(ipc.GrossCertifiedThisPeriod),
                Invariant(ipc.RetentionThisPeriod),
                Invariant(ipc.NetCertifiedThisPeriod),
                Invariant(ipc.CumulativeNetCertified),
                ipc.Currency
            });
            return rows;
        }

        private static IReadOnlyList<IReadOnlyList<string>> FinalAccountRows(FinalAccountResult? finalAccount)
        {
            var rows = new List<IReadOnlyList<string>>();
            if (finalAccount == null) return rows;
            rows.Add(new[]
            {
                finalAccount.FinalAccountId,
                Invariant(finalAccount.FinalContractValue),
                Invariant(finalAccount.AmountDue),
                Invariant(finalAccount.RecoveryDue),
                Invariant(finalAccount.UnreleasedRetention),
                finalAccount.Currency
            });
            return rows;
        }

        private static IReadOnlyList<IReadOnlyList<string>> TenderRows(CommercialQsWorkbookSnapshot snapshot)
        {
            var rows = new List<IReadOnlyList<string>>();
            if (snapshot.TenderPackage != null)
            {
                var package = snapshot.TenderPackage;
                rows.Add(new[]
                {
                    "PACKAGE",
                    package.PackageId,
                    package.Description,
                    package.Status.ToString(),
                    package.Revision.RevisionId,
                    snapshot.TenderEvaluation?.RecommendedBidId ?? string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    package.Currency,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty
                });

                if (snapshot.TenderEvaluation != null)
                {
                    var evaluation = snapshot.TenderEvaluation;
                    for (var i = 0; i < package.Bids.Count; i++)
                    {
                        var bid = package.Bids[i];
                        var commercial = evaluation.FindCommercialResult(bid.BidId);
                        var compliance = evaluation.FindComplianceResult(bid.BidId);
                        rows.Add(new[]
                        {
                            "BID_RESULT",
                            package.PackageId,
                            string.Empty,
                            commercial.IsComplete ? "Complete" : "Incomplete",
                            evaluation.PackageRevision.RevisionId,
                            evaluation.RecommendedBidId,
                            string.Empty,
                            string.Empty,
                            bid.Bidder,
                            Invariant(commercial.EvaluatedTotal),
                            package.Currency,
                            string.Empty,
                            bid.BidId,
                            commercial.Rank.ToString(CultureInfo.InvariantCulture),
                            compliance.PassesMandatoryCompliance ? "PASS" : "FAIL"
                        });
                    }
                }
            }
            if (snapshot.TenderAward != null)
            {
                var award = snapshot.TenderAward;
                rows.Add(new[]
                {
                    "AWARD",
                    award.PackageId,
                    string.Empty,
                    "Awarded",
                    award.PackageRevision.RevisionId,
                    snapshot.TenderEvaluation?.RecommendedBidId ?? string.Empty,
                    award.AwardId,
                    award.BidId,
                    award.Bidder,
                    Invariant(award.EvaluatedTotal),
                    award.Currency,
                    award.AwardRevision.RevisionId,
                    string.Empty,
                    string.Empty,
                    string.Empty
                });
            }
            return rows;
        }

        private static IReadOnlyList<IReadOnlyList<string>> CvrRows(CommercialCostControlResult? cvr)
        {
            var rows = new List<IReadOnlyList<string>>();
            if (cvr == null) return rows;
            rows.Add(new[]
            {
                cvr.PeriodId,
                cvr.Status.ToString(),
                cvr.Revision.RevisionId,
                Invariant(cvr.RevisedBudget),
                Invariant(cvr.CostToDate),
                Invariant(cvr.CommittedExposure),
                Invariant(cvr.ForecastCostToComplete),
                Invariant(cvr.ForecastFinalCost),
                Invariant(cvr.ForecastVariance),
                Invariant(cvr.EarnedValue),
                Invariant(cvr.CvrMargin),
                cvr.Currency
            });
            return rows;
        }

        private static void ValidateWorkbookPayload(
            IReadOnlyList<IReadOnlyList<string>> metaRows,
            IReadOnlyList<IReadOnlyList<string>> variationRows,
            IReadOnlyList<IReadOnlyList<string>> ipcRows,
            IReadOnlyList<IReadOnlyList<string>> finalAccountRows,
            IReadOnlyList<IReadOnlyList<string>> tenderRows,
            IReadOnlyList<IReadOnlyList<string>> cvrRows)
        {
            ValidateWorkbookRows(metaRows, MetaSheetName);
            ValidateWorkbookRows(variationRows, VariationsSheetName);
            ValidateWorkbookRows(ipcRows, IpcSheetName);
            ValidateWorkbookRows(finalAccountRows, FinalAccountSheetName);
            ValidateWorkbookRows(tenderRows, TenderSheetName);
            ValidateWorkbookRows(cvrRows, CvrSheetName);
        }

        private static void ValidateWorkbookRows(IReadOnlyList<IReadOnlyList<string>> rows, string sheetName)
        {
            if (rows == null)
                throw new InvalidDataException("Commercial workbook worksheet rows are missing: " + sheetName + ".");
            if (rows.Count > MaxWorksheetRows)
                throw new InvalidDataException("Commercial workbook worksheet exceeds the bounded row contract: " + sheetName + ".");

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row == null)
                    throw new InvalidDataException("Commercial workbook worksheet contains a null row: " + sheetName + ".");
                for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
                    ValidateStrictUtf8Cell(row[columnIndex]);
            }
        }

        private static void ValidateStrictUtf8Cell(string value)
        {
            value = value ?? string.Empty;
            if (value.Length > MaxCellCharacters)
                throw new InvalidDataException("Commercial workbook cell exceeds the Excel text limit.");
            _ = StrictUtf8.GetByteCount(value);
        }

        private static void WriteSheetEntry(
            ZipArchive archive,
            string name,
            IReadOnlyList<string> headers,
            IReadOnlyList<IReadOnlyList<string>> rows,
            ref long totalUncompressedBytes)
        {
            if (rows.Count > MaxWorksheetRows)
                throw new InvalidDataException("Commercial workbook worksheet exceeds the bounded row contract: " + name + ".");

            using (var buffer = new MemoryStream())
            {
                using (var boundedEntry = new BoundedEntryWriteStream(buffer, MaxEntryBytes))
                using (var writer = new StreamWriter(boundedEntry, StrictUtf8, 4096, true))
                    WriteSheet(writer, headers, rows);

                ReserveUncompressed(buffer.Length, ref totalUncompressedBytes, name);
                buffer.Position = 0L;
                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                entry.LastWriteTime = FixedZipTimestamp;
                using (var target = entry.Open())
                    buffer.CopyTo(target);
            }
        }

        private static void WriteSheet(TextWriter writer, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            writer.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AppendRow(writer, 1, headers);
            for (var i = 0; i < rows.Count; i++) AppendRow(writer, i + 2, rows[i]);
            writer.Write("</sheetData></worksheet>");
        }

        private static void AppendRow(TextWriter writer, int rowNumber, IReadOnlyList<string> values)
        {
            writer.Write("<row r=\"");
            writer.Write(rowNumber.ToString(CultureInfo.InvariantCulture));
            writer.Write("\">");
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i] ?? string.Empty;
                if (value.Length > MaxCellCharacters)
                    throw new InvalidDataException("Commercial workbook cell exceeds the Excel text limit.");
                writer.Write("<c r=\"");
                writer.Write(CellReference(i, rowNumber));
                writer.Write("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">");
                writer.Write(SecurityElement.Escape(value) ?? string.Empty);
                writer.Write("</t></is></c>");
            }
            writer.Write("</row>");
        }

        private static string CellReference(int column, int row)
        {
            var n = column + 1;
            var name = string.Empty;
            while (n > 0)
            {
                n--;
                name = (char)('A' + n % 26) + name;
                n /= 26;
            }
            return name + row.ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content, ref long totalUncompressedBytes)
        {
            var bytes = StrictUtf8.GetBytes(content);
            ReserveUncompressed(bytes.LongLength, ref totalUncompressedBytes, name);
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            entry.LastWriteTime = FixedZipTimestamp;
            using (var target = entry.Open())
                target.Write(bytes, 0, bytes.Length);
        }

        private static void ReserveUncompressed(long entryBytes, ref long totalUncompressedBytes, string name)
        {
            if (entryBytes < 0L || entryBytes > MaxEntryBytes)
                throw new InvalidDataException("Commercial workbook XML entry exceeds the bounded size: " + name + ".");
            var projectedTotal = checked(totalUncompressedBytes + entryBytes);
            if (projectedTotal > MaxTotalUncompressedBytes)
                throw new InvalidDataException("Commercial workbook total uncompressed XML exceeds the bounded output contract.");
            totalUncompressedBytes = projectedTotal;
        }

        private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        private const string RootRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
        private const string ContentTypesXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet4.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet5.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet6.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
        private const string WorkbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"META\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"VARIATIONS\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"IPC\" sheetId=\"3\" r:id=\"rId3\"/><sheet name=\"FINAL_ACCOUNT\" sheetId=\"4\" r:id=\"rId4\"/><sheet name=\"TENDER\" sheetId=\"5\" r:id=\"rId5\"/><sheet name=\"CVR\" sheetId=\"6\" r:id=\"rId6\"/></sheets></workbook>";
        private const string WorkbookRelationshipsXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet4.xml\"/><Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet5.xml\"/><Relationship Id=\"rId6\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet6.xml\"/></Relationships>";

        private sealed class BoundedEntryWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;

            internal BoundedEntryWriteStream(Stream inner, long maxLength)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxLength < 0L) throw new ArgumentOutOfRangeException(nameof(maxLength));
                _maxLength = maxLength;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => _inner.Length;
            public override long Position
            {
                get => _inner.Position;
                set => throw new NotSupportedException();
            }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                var projectedLength = checked(_inner.Position + count);
                if (projectedLength > _maxLength) throw EntrySizeExceeded();
                _inner.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                var projectedLength = checked(_inner.Position + 1L);
                if (projectedLength > _maxLength) throw EntrySizeExceeded();
                _inner.WriteByte(value);
            }

            private static InvalidDataException EntrySizeExceeded()
                => new InvalidDataException("Commercial workbook XML entry exceeds the bounded output contract.");
        }

        private sealed class BoundedArchiveWriteStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxLength;

            internal BoundedArchiveWriteStream(Stream inner, long maxLength)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                if (maxLength < 0L) throw new ArgumentOutOfRangeException(nameof(maxLength));
                _maxLength = maxLength;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position
            {
                get => _inner.Position;
                set
                {
                    if (value < 0L || value > _maxLength) throw ArchiveSizeExceeded();
                    _inner.Position = value;
                }
            }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin)
            {
                long target;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        target = offset;
                        break;
                    case SeekOrigin.Current:
                        target = checked(_inner.Position + offset);
                        break;
                    case SeekOrigin.End:
                        target = checked(_inner.Length + offset);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin));
                }
                if (target < 0L || target > _maxLength) throw ArchiveSizeExceeded();
                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                if (value < 0L || value > _maxLength) throw ArchiveSizeExceeded();
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var projectedLength = Math.Max(_inner.Length, checked(_inner.Position + count));
                if (projectedLength > _maxLength) throw ArchiveSizeExceeded();
                _inner.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                var projectedLength = Math.Max(_inner.Length, checked(_inner.Position + 1L));
                if (projectedLength > _maxLength) throw ArchiveSizeExceeded();
                _inner.WriteByte(value);
            }

            private static InvalidDataException ArchiveSizeExceeded()
                => new InvalidDataException("Commercial workbook archive exceeds the bounded output contract.");
        }
    }
}
