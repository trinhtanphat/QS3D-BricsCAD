using System;
using System.Collections;
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
            IReadOnlyDictionary<string, CoordinationIssueExcelRow>? lifecycleByFindingId = null,
            IReadOnlyList<Qs3dReviewIssueGeometry>? issueGeometry = null)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Export path is required.", nameof(path));
            if (quantityDetails == null) throw new ArgumentNullException(nameof(quantityDetails));
            if (quantitySummary == null) throw new ArgumentNullException(nameof(quantitySummary));
            if (clashes == null) throw new ArgumentNullException(nameof(clashes));
            if (duplicates == null) throw new ArgumentNullException(nameof(duplicates));
            if (modelInfo == null) throw new ArgumentNullException(nameof(modelInfo));

            var detailCount = quantityDetails.Count;
            var summaryCount = quantitySummary.Count;
            var clashCount = clashes.Count;
            var duplicateCount = duplicates.Count;
            var geometryCount = issueGeometry == null ? (int?)null : issueGeometry.Count;
            if (detailCount == 0 || summaryCount == 0) throw new InvalidDataException("QS3D Review workbook requires QTO detail and summary rows.");
            Limit(detailCount, QuantitySheet); Limit(summaryCount + 16, SummarySheet);
            Limit(clashCount, ClashSheet); Limit(duplicateCount, DuplicateSheet);
            if (ruleProfile != null) Limit(ruleProfile.Rules.Count, RulesSheet);

            var detailInput = SnapshotCounted(quantityDetails, detailCount, "QTO detail");
            var summaryInput = SnapshotCounted(quantitySummary, summaryCount, "QTO summary");
            var clashInput = SnapshotCounted(clashes, clashCount, "clash");
            var duplicateInput = SnapshotCounted(duplicates, duplicateCount, "duplicate");
            var geometryInput = issueGeometry == null ? null : SnapshotCounted(issueGeometry, geometryCount!.Value, "issue geometry");

            var details = Quantity(detailInput, true, modelInfo.DrawingFingerprint);
            var summaries = Quantity(summaryInput, false, modelInfo.DrawingFingerprint);
            Scope(details, summaries);
            var clashRows = Clash(clashInput, modelInfo.DrawingFingerprint);
            var duplicateRows = Duplicate(duplicateInput, modelInfo.DrawingFingerprint);
            var lifecycle = Lifecycle(lifecycleByFindingId, clashRows, duplicateRows);
            var geometry = Geometry(geometryInput, clashRows, duplicateRows);

            Qs3dReviewXlsx.WritePackage(path,
                Summary(summaries, clashRows.Count, duplicateRows.Count, modelInfo),
                QuantitySheetXml(details, modelInfo),
                ClashSheetXml(clashRows, lifecycle, geometry, modelInfo),
                DuplicateSheetXml(duplicateRows, lifecycle, geometry, modelInfo),
                Rules(ruleProfile),
                ModelInfo(details.Count, summaries.Count, clashRows.Count, duplicateRows.Count, ruleProfile, modelInfo));
        }

        private static List<T> SnapshotCounted<T>(IReadOnlyList<T> source, int expectedCount, string label)
        {
            if (expectedCount < 0)
                throw new InvalidDataException("QS3D Review " + label + " collection advertised a negative Count.");

            var genericCollection = source as ICollection<T>;
            var nonGenericCollection = source as ICollection;
            var genericExpectedCount = genericCollection == null ? (int?)null : genericCollection.Count;
            var nonGenericExpectedCount = nonGenericCollection == null ? (int?)null : nonGenericCollection.Count;

            static void RequireAdmittedCount(int observed, int expected, string channel, string labelValue)
            {
                if (observed < 0)
                    throw new InvalidDataException("QS3D Review " + labelValue + " collection advertised a negative " + channel + " Count.");
                if (observed != expected)
                    throw new InvalidDataException("QS3D Review " + labelValue + " collection Count channels disagree at admission.");
            }

            if (genericExpectedCount.HasValue)
                RequireAdmittedCount(genericExpectedCount.Value, expectedCount, "ICollection<T>", label);
            if (nonGenericExpectedCount.HasValue)
                RequireAdmittedCount(nonGenericExpectedCount.Value, expectedCount, "ICollection", label);

            void RequireStableCount()
            {
                if (source.Count != expectedCount)
                    throw new InvalidDataException("QS3D Review " + label + " collection Count changed during traversal.");
                if (genericCollection != null && genericCollection.Count != expectedCount)
                    throw new InvalidDataException("QS3D Review " + label + " ICollection<T> Count changed during traversal.");
                if (nonGenericCollection != null && nonGenericCollection.Count != expectedCount)
                    throw new InvalidDataException("QS3D Review " + label + " ICollection Count changed during traversal.");
            }

            var result = new List<T>();
            using (var enumerator = source.GetEnumerator())
            {
                while (true)
                {
                    RequireStableCount();
                    var moved = enumerator.MoveNext();
                    RequireStableCount();
                    if (!moved)
                        break;

                    if (result.Count >= expectedCount)
                        throw new InvalidDataException("QS3D Review " + label + " collection Count does not match completed traversal.");
                    var value = enumerator.Current;
                    RequireStableCount();
                    result.Add(value);
                }
            }

            if (result.Count != expectedCount)
                throw new InvalidDataException("QS3D Review " + label + " collection Count does not match completed traversal.");
            RequireStableCount();
            return result;
        }

        private static List<QuantityReportRow> Quantity(IReadOnlyList<QuantityReportRow> source, bool single, string fingerprint)
        {
            var result = new List<QuantityReportRow>(source.Count);
            foreach (var sourceRow in source)
            {
                if (sourceRow == null) throw new InvalidDataException("QS3D Review QTO contains a null row.");
                var row = SnapshotQuantityRow(sourceRow);
                EnsureQuantityRowStable(sourceRow, row);
                if (row.Count <= 0 || row.ElementIds.Count != row.Count) throw new InvalidDataException("QS3D Review QTO Count must equal ElementId provenance cardinality.");
                if (single && (row.Count != 1 || row.ElementIds.Count != 1)) throw new InvalidDataException("02_CHI_TIET_QTO requires exactly one semantic element per row.");
                if (row.SourceHandles.Count == 0) throw new InvalidDataException("QS3D Review QTO row requires CAD Handle provenance.");
                if (!string.Equals((row.DrawingFingerprint ?? string.Empty).Trim(), fingerprint, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("QS3D Review QTO row belongs to a different DrawingFingerprint.");
                CanonicalElementIds(row.ElementIds); Distinct(row.ElementIds, "QTO ElementId"); Distinct(row.SourceHandles, "QTO CAD Handle"); Evidence(row);
                result.Add(row);
            }
            return result;
        }

        private static QuantityReportRow SnapshotQuantityRow(QuantityReportRow source)
        {
            var snapshot = new QuantityReportRow
            {
                Floor = source.Floor ?? string.Empty, Zone = source.Zone ?? string.Empty, Category = source.Category ?? string.Empty,
                FamilyId = source.FamilyId ?? string.Empty, FamilyName = source.FamilyName ?? string.Empty, ElementName = source.ElementName ?? string.Empty,
                Material = source.Material ?? string.Empty, Note = source.Note ?? string.Empty, DrawingFingerprint = source.DrawingFingerprint ?? string.Empty,
                Count = source.Count, GrossConcreteM3 = source.GrossConcreteM3, DeductionM3 = source.DeductionM3, NetConcreteM3 = source.NetConcreteM3,
                FormworkM2 = source.FormworkM2, GrossFormworkM2 = source.GrossFormworkM2, ConcreteContactDeductionM2 = source.ConcreteContactDeductionM2,
                NetFormworkM2 = source.NetFormworkM2, LengthM = source.LengthM, WidthM = source.WidthM, HeightM = source.HeightM,
                OuterPerimeterM = source.OuterPerimeterM, InnerPerimeterM = source.InnerPerimeterM, DoorAreaM2 = source.DoorAreaM2,
                SideAreaM2 = source.SideAreaM2, BottomAreaM2 = source.BottomAreaM2, TopAreaM2 = source.TopAreaM2, OtherAreaM2 = source.OtherAreaM2,
                HasGrossConcreteM3Evidence = source.HasGrossConcreteM3Evidence, HasDeductionM3Evidence = source.HasDeductionM3Evidence,
                HasNetConcreteM3Evidence = source.HasNetConcreteM3Evidence, HasFormworkM2Evidence = source.HasFormworkM2Evidence,
                HasGrossFormworkM2Evidence = source.HasGrossFormworkM2Evidence, HasConcreteContactDeductionM2Evidence = source.HasConcreteContactDeductionM2Evidence,
                HasNetFormworkM2Evidence = source.HasNetFormworkM2Evidence, HasLengthMEvidence = source.HasLengthMEvidence, HasWidthMEvidence = source.HasWidthMEvidence,
                HasHeightMEvidence = source.HasHeightMEvidence, HasOuterPerimeterMEvidence = source.HasOuterPerimeterMEvidence, HasInnerPerimeterMEvidence = source.HasInnerPerimeterMEvidence,
                HasDoorAreaM2Evidence = source.HasDoorAreaM2Evidence, HasSideAreaM2Evidence = source.HasSideAreaM2Evidence, HasBottomAreaM2Evidence = source.HasBottomAreaM2Evidence,
                HasTopAreaM2Evidence = source.HasTopAreaM2Evidence, HasOtherAreaM2Evidence = source.HasOtherAreaM2Evidence,
                DensityKgM3 = source.DensityKgM3, MassKg = source.MassKg
            };
            CopyStableValues(source.ElementIds, snapshot.ElementIds, "QTO ElementIds");
            CopyStableValues(source.SourceHandles, snapshot.SourceHandles, "QTO SourceHandles");
            return snapshot;
        }

        private static void CopyStableValues(IList<string> source, IList<string> destination, string label)
        {
            var admittedCount = source.Count;
            for (var index = 0; index < admittedCount; index++)
            {
                if (source.Count != admittedCount) throw new InvalidDataException("QS3D Review " + label + " changed Count during snapshot capture.");
                destination.Add(source[index] ?? string.Empty);
                if (source.Count != admittedCount) throw new InvalidDataException("QS3D Review " + label + " changed Count during snapshot capture.");
            }
            if (source.Count != admittedCount) throw new InvalidDataException("QS3D Review " + label + " changed Count during snapshot capture.");
        }

        private static void EnsureQuantityRowStable(QuantityReportRow source, QuantityReportRow snapshot)
        {
            var scalarStable = string.Equals(source.Floor ?? string.Empty, snapshot.Floor, StringComparison.Ordinal) &&
                string.Equals(source.Zone ?? string.Empty, snapshot.Zone, StringComparison.Ordinal) && string.Equals(source.Category ?? string.Empty, snapshot.Category, StringComparison.Ordinal) &&
                string.Equals(source.FamilyId ?? string.Empty, snapshot.FamilyId, StringComparison.Ordinal) && string.Equals(source.FamilyName ?? string.Empty, snapshot.FamilyName, StringComparison.Ordinal) &&
                string.Equals(source.ElementName ?? string.Empty, snapshot.ElementName, StringComparison.Ordinal) && string.Equals(source.Material ?? string.Empty, snapshot.Material, StringComparison.Ordinal) &&
                string.Equals(source.Note ?? string.Empty, snapshot.Note, StringComparison.Ordinal) && string.Equals(source.DrawingFingerprint ?? string.Empty, snapshot.DrawingFingerprint, StringComparison.Ordinal) &&
                source.Count == snapshot.Count && source.GrossConcreteM3 == snapshot.GrossConcreteM3 && source.DeductionM3 == snapshot.DeductionM3 && source.NetConcreteM3 == snapshot.NetConcreteM3 &&
                source.FormworkM2 == snapshot.FormworkM2 && source.GrossFormworkM2 == snapshot.GrossFormworkM2 && source.ConcreteContactDeductionM2 == snapshot.ConcreteContactDeductionM2 &&
                source.NetFormworkM2 == snapshot.NetFormworkM2 && source.LengthM == snapshot.LengthM && source.WidthM == snapshot.WidthM && source.HeightM == snapshot.HeightM &&
                source.OuterPerimeterM == snapshot.OuterPerimeterM && source.InnerPerimeterM == snapshot.InnerPerimeterM && source.DoorAreaM2 == snapshot.DoorAreaM2 &&
                source.SideAreaM2 == snapshot.SideAreaM2 && source.BottomAreaM2 == snapshot.BottomAreaM2 && source.TopAreaM2 == snapshot.TopAreaM2 && source.OtherAreaM2 == snapshot.OtherAreaM2 &&
                source.HasGrossConcreteM3Evidence == snapshot.HasGrossConcreteM3Evidence && source.HasDeductionM3Evidence == snapshot.HasDeductionM3Evidence &&
                source.HasNetConcreteM3Evidence == snapshot.HasNetConcreteM3Evidence && source.HasFormworkM2Evidence == snapshot.HasFormworkM2Evidence &&
                source.HasGrossFormworkM2Evidence == snapshot.HasGrossFormworkM2Evidence && source.HasConcreteContactDeductionM2Evidence == snapshot.HasConcreteContactDeductionM2Evidence &&
                source.HasNetFormworkM2Evidence == snapshot.HasNetFormworkM2Evidence && source.HasLengthMEvidence == snapshot.HasLengthMEvidence &&
                source.HasWidthMEvidence == snapshot.HasWidthMEvidence && source.HasHeightMEvidence == snapshot.HasHeightMEvidence &&
                source.HasOuterPerimeterMEvidence == snapshot.HasOuterPerimeterMEvidence && source.HasInnerPerimeterMEvidence == snapshot.HasInnerPerimeterMEvidence &&
                source.HasDoorAreaM2Evidence == snapshot.HasDoorAreaM2Evidence && source.HasSideAreaM2Evidence == snapshot.HasSideAreaM2Evidence &&
                source.HasBottomAreaM2Evidence == snapshot.HasBottomAreaM2Evidence && source.HasTopAreaM2Evidence == snapshot.HasTopAreaM2Evidence &&
                source.HasOtherAreaM2Evidence == snapshot.HasOtherAreaM2Evidence && source.DensityKgM3 == snapshot.DensityKgM3 && source.MassKg == snapshot.MassKg;
            if (!scalarStable || !source.ElementIds.SequenceEqual(snapshot.ElementIds, StringComparer.Ordinal) || !source.SourceHandles.SequenceEqual(snapshot.SourceHandles, StringComparer.Ordinal))
                throw new InvalidDataException("QS3D Review QTO row changed during detached snapshot capture.");
        }

        private static void CanonicalElementIds(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                var original = value ?? string.Empty;
                if (!string.Equals(original, original.Trim(), StringComparison.Ordinal))
                    throw new InvalidDataException("QTO ElementId must not contain surrounding whitespace.");
                try
                {
                    Qs3dReviewModelInfo.VerifyXml(original, "QTO ElementId");
                }
                catch (ArgumentException error)
                {
                    throw new InvalidDataException("QTO ElementId contains characters that cannot be stored as provenance identity.", error);
                }
            }
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
        private static Dictionary<string, CoordinationIssueExcelRow> Lifecycle(
            IReadOnlyDictionary<string, CoordinationIssueExcelRow>? source,
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            var result = new Dictionary<string, CoordinationIssueExcelRow>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return result;
            var pairs = FindingPairs(clashes, duplicates);
            foreach (var pair in source)
            {
                var findingId = Required(pair.Key, "Lifecycle finding id");
                var item = pair.Value ?? throw new InvalidDataException("QS3D Review lifecycle mapping contains null for " + findingId + ".");
                string[] semanticPair;
                if (!pairs.TryGetValue(findingId, out semanticPair))
                    throw new InvalidDataException("QS3D Review lifecycle mapping references a finding that is not exported: " + findingId + ".");
                if (!SamePair(semanticPair[0], semanticPair[1], item.LeftSemanticId, item.RightSemanticId))
                    throw new InvalidDataException("QS3D Review lifecycle semantic pair does not match exported finding " + findingId + ".");
                result.Add(findingId, item);
            }
            return result;
        }

        private static Dictionary<string, Qs3dReviewIssueGeometry> Geometry(
            IReadOnlyList<Qs3dReviewIssueGeometry>? source,
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            var result = new Dictionary<string, Qs3dReviewIssueGeometry>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return result;
            var findingIds = new HashSet<string>(clashes.Select(row => row.ClashId), StringComparer.OrdinalIgnoreCase);
            foreach (var row in duplicates) findingIds.Add(row.DuplicateId);
            foreach (var item in source)
            {
                if (item == null) throw new InvalidDataException("QS3D Review issue geometry contains null.");
                if (!findingIds.Contains(item.FindingId))
                    throw new InvalidDataException("QS3D Review issue geometry references a finding that is not exported: " + item.FindingId + ".");
                if (result.ContainsKey(item.FindingId))
                    throw new InvalidDataException("QS3D Review issue geometry contains duplicate finding id: " + item.FindingId + ".");
                result.Add(item.FindingId, item);
            }
            return result;
        }

        private static Dictionary<string, string[]> FindingPairs(
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in clashes) result.Add(row.ClashId, new[] { row.LeftElementId, row.RightElementId });
            foreach (var row in duplicates) result.Add(row.DuplicateId, new[] { row.LeftElementId, row.RightElementId });
            return result;
        }

        private static bool SamePair(string leftA, string rightA, string leftB, string rightB) =>
            (string.Equals(leftA, leftB, StringComparison.OrdinalIgnoreCase) && string.Equals(rightA, rightB, StringComparison.OrdinalIgnoreCase)) ||
            (string.Equals(leftA, rightB, StringComparison.OrdinalIgnoreCase) && string.Equals(rightA, leftB, StringComparison.OrdinalIgnoreCase));
    }
}