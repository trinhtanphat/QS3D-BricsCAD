using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Coordination;
using QS3D.Core.Reporting;

namespace QS3D.Core.Export
{
    public static partial class Qs3dReviewWorkbookExporter
    {
        public const string SummarySheet = "01_TONG_HOP";
        public const string QuantitySheet = "02_CHI_TIET_QTO";
        public const string ClashSheet = "03_CLASHES";
        public const string DuplicateSheet = "04_DUPLICATES";
        public const string RulesSheet = "05_RULES";
        public const string ModelInfoSheet = "06_MODEL_INFO";
        public const string TraceHeader = "TRACE_KEY";
        public const string SchemaVersion = "QS3D_REVIEW_XLSX_V1";
        private const int MaxRows = 1048575;

        public static void Export(
            string path,
            IReadOnlyList<QuantityReportRow> quantityDetails,
            IReadOnlyList<QuantityReportRow> quantitySummary,
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates,
            CoordinationRuleProfile? ruleProfile,
            Qs3dReviewModelInfo modelInfo,
            IReadOnlyList<Qs3dReviewIssueMetadata>? issueMetadata = null)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (quantityDetails == null) throw new ArgumentNullException(nameof(quantityDetails));
            if (quantitySummary == null) throw new ArgumentNullException(nameof(quantitySummary));
            if (clashes == null) throw new ArgumentNullException(nameof(clashes));
            if (duplicates == null) throw new ArgumentNullException(nameof(duplicates));
            if (modelInfo == null) throw new ArgumentNullException(nameof(modelInfo));
            if (quantityDetails.Count == 0 || quantitySummary.Count == 0) throw new InvalidDataException("QS3D Review workbook requires QTO detail and summary rows.");
            Limit(quantityDetails.Count, QuantitySheet); Limit(quantitySummary.Count + 16, SummarySheet);
            Limit(clashes.Count, ClashSheet); Limit(duplicates.Count, DuplicateSheet);
            if (ruleProfile != null) Limit(ruleProfile.Rules.Count, RulesSheet);

            var details = Quantity(quantityDetails, true, modelInfo.DrawingFingerprint);
            var summaries = Quantity(quantitySummary, false, modelInfo.DrawingFingerprint);
            Scope(details, summaries);
            var clashRows = Clash(clashes, modelInfo.DrawingFingerprint);
            var duplicateRows = Duplicate(duplicates, modelInfo.DrawingFingerprint);
            var metadata = Metadata(issueMetadata, clashRows, duplicateRows);

            Qs3dReviewXlsx.WritePackage(path,
                Summary(summaries, clashRows.Count, duplicateRows.Count, modelInfo),
                QuantitySheetXml(details, modelInfo),
                ClashSheetXml(clashRows, metadata, modelInfo),
                DuplicateSheetXml(duplicateRows, metadata, modelInfo),
                Rules(ruleProfile),
                ModelInfo(details.Count, summaries.Count, clashRows.Count, duplicateRows.Count, ruleProfile, modelInfo));
        }

        private static List<QuantityReportRow> Quantity(IReadOnlyList<QuantityReportRow> source, bool single, string fingerprint)
        {
            var result = new List<QuantityReportRow>(source.Count);
            foreach (var row in source)
            {
                if (row == null) throw new InvalidDataException("QS3D Review QTO contains a null row.");
                if (row.Count <= 0 || row.ElementIds.Count != row.Count) throw new InvalidDataException("QS3D Review QTO Count must equal ElementId provenance cardinality.");
                if (single && (row.Count != 1 || row.ElementIds.Count != 1)) throw new InvalidDataException("02_CHI_TIET_QTO requires exactly one semantic element per row.");
                if (row.SourceHandles.Count == 0) throw new InvalidDataException("QS3D Review QTO row requires CAD Handle provenance.");
                if (!string.Equals((row.DrawingFingerprint ?? string.Empty).Trim(), fingerprint, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QS3D Review QTO row belongs to a different DrawingFingerprint.");
                Distinct(row.ElementIds, "QTO ElementId"); Distinct(row.SourceHandles, "QTO CAD Handle"); Evidence(row);
                result.Add(row);
            }
            return result;
        }

        private static void Evidence(QuantityReportRow row)
        {
            Evidence(row.GrossConcreteM3, row.HasGrossConcreteM3Evidence, "GrossConcreteM3");
            Evidence(row.DeductionM3, row.HasDeductionM3Evidence, "DeductionM3");
            Evidence(row.NetConcreteM3, row.HasNetConcreteM3Evidence, "NetConcreteM3");
            Evidence(row.FormworkM2, row.HasFormworkM2Evidence, "FormworkM2");
            Evidence(row.LengthM, row.HasLengthMEvidence, "LengthM");
            Evidence(row.OuterPerimeterM, row.HasOuterPerimeterMEvidence, "OuterPerimeterM");
            Evidence(row.InnerPerimeterM, row.HasInnerPerimeterMEvidence, "InnerPerimeterM");
            Evidence(row.DoorAreaM2, row.HasDoorAreaM2Evidence, "DoorAreaM2");
            Evidence(row.SideAreaM2, row.HasSideAreaM2Evidence, "SideAreaM2");
            Evidence(row.BottomAreaM2, row.HasBottomAreaM2Evidence, "BottomAreaM2");
            Evidence(row.TopAreaM2, row.HasTopAreaM2Evidence, "TopAreaM2");
            Evidence(row.OtherAreaM2, row.HasOtherAreaM2Evidence, "OtherAreaM2");
            if (row.DensityKgM3.HasValue && (!Qs3dReviewXlsx.Finite(row.DensityKgM3.Value) || row.DensityKgM3.Value < 0d)) throw new InvalidDataException("QTO DensityKgM3 must be finite and non-negative.");
            if (row.MassKg.HasValue && (!Qs3dReviewXlsx.Finite(row.MassKg.Value) || row.MassKg.Value < 0d)) throw new InvalidDataException("QTO MassKg must be finite and non-negative.");
        }
        private static void Evidence(double value, bool hasEvidence, string name)
        {
            if (!Qs3dReviewXlsx.Finite(value)) throw new InvalidDataException("QTO " + name + " must be finite.");
            if (!hasEvidence && value != 0d) throw new InvalidDataException("QTO " + name + " has a value without evidence; use blank/no-evidence semantics instead.");
        }
        private static void Scope(IReadOnlyList<QuantityReportRow> details, IReadOnlyList<QuantityReportRow> summaries)
        {
            var detail = new HashSet<string>(details.SelectMany(row => row.ElementIds), StringComparer.OrdinalIgnoreCase);
            var summary = new HashSet<string>(summaries.SelectMany(row => row.ElementIds), StringComparer.OrdinalIgnoreCase);
            if (detail.Count != details.Count || !summary.SetEquals(detail) || summaries.Sum(row => row.Count) != details.Count) throw new InvalidDataException("QS3D Review QTO detail and summary rows do not describe the same semantic scope.");
        }

        private static List<CoordinationClashExportRow> Clash(IReadOnlyList<CoordinationClashExportRow> source, string fingerprint)
        {
            var result = new List<CoordinationClashExportRow>(source.Count); var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in source)
            {
                if (row == null) throw new InvalidDataException("QS3D Review clash collection contains null.");
                if (!string.Equals(row.DrawingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QS3D Review clash row belongs to a different DrawingFingerprint.");
                if (!ids.Add(Required(row.ClashId, "ClashId"))) throw new InvalidDataException("Duplicate ClashId in QS3D Review workbook: " + row.ClashId + ".");
                Required(row.LeftElementId, "Clash ElementA ID"); Required(row.RightElementId, "Clash ElementB ID");
                Required(row.LeftHandle, "Clash ElementA Handle"); Required(row.RightHandle, "Clash ElementB Handle");
                if (string.Equals(row.LeftHandle, row.RightHandle, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Clash pair must contain two different Handles.");
                result.Add(row);
            }
            return result;
        }
        private static List<CoordinationDuplicateExportRow> Duplicate(IReadOnlyList<CoordinationDuplicateExportRow> source, string fingerprint)
        {
            var result = new List<CoordinationDuplicateExportRow>(source.Count); var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in source)
            {
                if (row == null) throw new InvalidDataException("QS3D Review duplicate collection contains null.");
                if (!string.Equals(row.DrawingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QS3D Review duplicate row belongs to a different DrawingFingerprint.");
                if (!ids.Add(Required(row.DuplicateId, "DuplicateId"))) throw new InvalidDataException("Duplicate DuplicateId in QS3D Review workbook: " + row.DuplicateId + ".");
                Required(row.LeftElementId, "Duplicate ElementA ID"); Required(row.RightElementId, "Duplicate ElementB ID");
                Required(row.LeftHandle, "Duplicate ElementA Handle"); Required(row.RightHandle, "Duplicate ElementB Handle");
                result.Add(row);
            }
            return result;
        }
        private static Dictionary<string, Qs3dReviewIssueMetadata> Metadata(IReadOnlyList<Qs3dReviewIssueMetadata>? source, IReadOnlyList<CoordinationClashExportRow> clashes, IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            var result = new Dictionary<string, Qs3dReviewIssueMetadata>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return result;
            var issueIds = new HashSet<string>(clashes.Select(row => row.ClashId), StringComparer.OrdinalIgnoreCase);
            foreach (var row in duplicates) issueIds.Add(row.DuplicateId);
            foreach (var item in source)
            {
                if (item == null) throw new InvalidDataException("QS3D Review issue metadata contains null.");
                if (!issueIds.Contains(item.IssueId)) throw new InvalidDataException("QS3D Review issue metadata references an IssueId that is not exported: " + item.IssueId + ".");
                if (result.ContainsKey(item.IssueId)) throw new InvalidDataException("QS3D Review issue metadata contains duplicate IssueId: " + item.IssueId + ".");
                result.Add(item.IssueId, item);
            }
            return result;
        }

    }
}
