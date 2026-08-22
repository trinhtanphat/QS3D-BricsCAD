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
        private static string Summary(IReadOnlyList<QuantityReportRow> rows, int clashes, int duplicates, Qs3dReviewModelInfo model)
        {
            var x = Qs3dReviewXlsx.Begin("A1:J" + (rows.Count + 14).ToString(CultureInfo.InvariantCulture));
            Qs3dReviewXlsx.TextRow(x, 1, true, "QS3D REVIEW — TỔNG HỢP");
            Qs3dReviewXlsx.TextRow(x, 2, false, "Project", model.ProjectId);
            Qs3dReviewXlsx.TextRow(x, 3, false, "Model", model.DrawingName);
            Qs3dReviewXlsx.TextRow(x, 4, false, "Model Revision", model.ModelRevision);
            Qs3dReviewXlsx.Header(x, 6, "Chỉ tiêu", "Giá trị", "Đơn vị");
            Kpi(x, 7, "Total Concrete", Sum(rows, r => r.NetConcreteM3, r => r.HasNetConcreteM3Evidence), "m³");
            Kpi(x, 8, "Reinforcement", model.ReinforcementTon, "ton");
            Kpi(x, 9, "Formwork", Sum(rows, r => r.FormworkM2, r => r.HasFormworkM2Evidence), "m²");
            Kpi(x, 10, "Clashes", clashes, "issue");
            Kpi(x, 11, "Duplicates", duplicates, "issue");
            Qs3dReviewXlsx.Header(x, 13, "STT", "Tên cấu kiện", "Loại", "Tầng/Zone", "SL", "BT còn (m³)", "Cốp pha (m²)", "Khối lượng (kg)", "Element IDs", "CAD Handles");
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var er = i + 14;
                Qs3dReviewXlsx.StartRow(x, er);
                Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(0, er), i + 1);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(1, er), Name(r));
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(2, er), r.Category);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(3, er), r.FloorZoneText);
                Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(4, er), r.Count);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(5, er), r.NetConcreteM3, r.HasNetConcreteM3Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(6, er), r.FormworkM2, r.HasFormworkM2Evidence);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(7, er), r.MassKg);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(8, er), Join(r.ElementIds), Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(9, er), Join(r.SourceHandles), Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.EndRow(x);
            }
            return Qs3dReviewXlsx.End(x, "A13:J" + Math.Max(13, rows.Count + 13).ToString(CultureInfo.InvariantCulture));
        }

        private static string QuantitySheetXml(IReadOnlyList<QuantityReportRow> rows, Qs3dReviewModelInfo model)
        {
            var x = Qs3dReviewXlsx.Begin(
                "A1:AB" + (rows.Count + 1).ToString(CultureInfo.InvariantCulture),
                "<cols><col min=\"25\" max=\"28\" hidden=\"1\" width=\"18\" customWidth=\"1\"/></cols>");
            Qs3dReviewXlsx.Header(x, 1,
                "STT", "Element ID", "Tên cấu kiện", "Floor", "Zone", "Category", "Family ID", "Family", "Material", "Count",
                "BT gộp (m³)", "Trừ giao (m³)", "BT còn (m³)", "Cốp pha (m²)", "Dài (m)", "Chu vi ngoài (m)", "Chu vi trong (m)",
                "DT cửa (m²)", "Thành bên (m²)", "DT đáy (m²)", "DT đỉnh (m²)", "DT khác (m²)", "Khối lượng riêng (kg/m³)", "Khối lượng (kg)",
                "CAD Handles", "DrawingFingerprint", "ModelRevision", TraceHeader);
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var er = i + 2;
                var id = r.ElementIds[0];
                var handles = Join(r.SourceHandles);
                Qs3dReviewXlsx.StartRow(x, er);
                Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(0, er), i + 1);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(1, er), id, Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(2, er), Name(r));
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(3, er), r.Floor);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(4, er), r.Zone);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(5, er), r.Category);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(6, er), r.FamilyId);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(7, er), r.FamilyName);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(8, er), r.Material);
                Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(9, er), r.Count);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(10, er), r.GrossConcreteM3, r.HasGrossConcreteM3Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(11, er), r.DeductionM3, r.HasDeductionM3Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(12, er), r.NetConcreteM3, r.HasNetConcreteM3Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(13, er), r.FormworkM2, r.HasFormworkM2Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(14, er), r.LengthM, r.HasLengthMEvidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(15, er), r.OuterPerimeterM, r.HasOuterPerimeterMEvidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(16, er), r.InnerPerimeterM, r.HasInnerPerimeterMEvidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(17, er), r.DoorAreaM2, r.HasDoorAreaM2Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(18, er), r.SideAreaM2, r.HasSideAreaM2Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(19, er), r.BottomAreaM2, r.HasBottomAreaM2Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(20, er), r.TopAreaM2, r.HasTopAreaM2Evidence);
                Qs3dReviewXlsx.Evidence(x, Qs3dReviewXlsx.Cell(21, er), r.OtherAreaM2, r.HasOtherAreaM2Evidence);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(22, er), r.DensityKgM3);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(23, er), r.MassKg);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(24, er), handles, Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(25, er), model.DrawingFingerprint, Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(26, er), model.ModelRevision, Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(27, er), Qs3dReviewXlsx.TraceKey("QTO", model.DrawingFingerprint, id, handles), Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.EndRow(x);
            }
            return Qs3dReviewXlsx.End(x, "A1:AB" + (rows.Count + 1).ToString(CultureInfo.InvariantCulture));
        }

        private static string ClashSheetXml(
            IReadOnlyList<CoordinationClashExportRow> rows,
            IReadOnlyDictionary<string, CoordinationIssueExcelRow> lifecycle,
            IReadOnlyDictionary<string, Qs3dReviewIssueGeometry> geometry,
            Qs3dReviewModelInfo model)
        {
            var x = IssueSheet(rows.Count, "Clash ID", "Type", "Severity", "OverlapX", "OverlapY", "OverlapZ");
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                CoordinationIssueExcelRow? life;
                Qs3dReviewIssueGeometry? geo;
                lifecycle.TryGetValue(r.ClashId, out life);
                geometry.TryGetValue(r.ClashId, out geo);
                var er = i + 2;
                Qs3dReviewXlsx.StartRow(x, er);
                IssueBase(
                    x, er, i + 1, r.ClashId, r.Type, r.Floor,
                    Display(r.LeftElementId, r.LeftHandle), Display(r.RightElementId, r.RightHandle),
                    r.LeftCategory, r.RightCategory,
                    life == null ? r.Status : life.Status.ToString(),
                    r.LeftElementId, r.LeftHandle, r.RightElementId, r.RightHandle,
                    model, r.RuleId);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(16, er), life == null ? r.Severity : life.Severity.ToString());
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(17, er), geo?.OverlapX);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(18, er), geo?.OverlapY);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(19, er), geo?.OverlapZ);
                IssueTail(
                    x, er, life, geo, r.Comment,
                    Qs3dReviewXlsx.TraceKey("CLASH", model.DrawingFingerprint, r.ClashId, r.LeftHandle, r.RightHandle));
                Qs3dReviewXlsx.EndRow(x);
            }
            return Qs3dReviewXlsx.End(x, "A1:AE" + Math.Max(1, rows.Count + 1).ToString(CultureInfo.InvariantCulture));
        }

        private static string DuplicateSheetXml(
            IReadOnlyList<CoordinationDuplicateExportRow> rows,
            IReadOnlyDictionary<string, CoordinationIssueExcelRow> lifecycle,
            IReadOnlyDictionary<string, Qs3dReviewIssueGeometry> geometry,
            Qs3dReviewModelInfo model)
        {
            var x = IssueSheet(rows.Count, "Duplicate ID", "Type", "MatchKinds", "Distance (mm)", "Rotation Δ (°)", "Confidence (%)");
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                CoordinationIssueExcelRow? life;
                Qs3dReviewIssueGeometry? geo;
                lifecycle.TryGetValue(r.DuplicateId, out life);
                geometry.TryGetValue(r.DuplicateId, out geo);
                var er = i + 2;
                Qs3dReviewXlsx.StartRow(x, er);
                IssueBase(
                    x, er, i + 1, r.DuplicateId, "Duplicate", r.Floor,
                    Display(r.LeftElementId, r.LeftHandle), Display(r.RightElementId, r.RightHandle),
                    r.LeftCategory, r.RightCategory,
                    life == null ? string.Empty : life.Status.ToString(),
                    r.LeftElementId, r.LeftHandle, r.RightElementId, r.RightHandle,
                    model, r.RuleId);
                Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(16, er), r.MatchKindsText, Qs3dReviewXlsx.WrappedStyle);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(17, er), geo?.DistanceMm);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(18, er), geo?.RotationDeltaDegrees);
                Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(19, er), geo?.ConfidencePercent);
                IssueTail(
                    x, er, life, geo, r.Comment,
                    Qs3dReviewXlsx.TraceKey("DUPLICATE", model.DrawingFingerprint, r.DuplicateId, r.LeftElementId, r.RightElementId));
                Qs3dReviewXlsx.EndRow(x);
            }
            return Qs3dReviewXlsx.End(x, "A1:AE" + Math.Max(1, rows.Count + 1).ToString(CultureInfo.InvariantCulture));
        }

        private static StringBuilder IssueSheet(int count, string id, string type, string evidence, string m1, string m2, string m3)
        {
            var columns =
                "<cols>" +
                "<col min=\"10\" max=\"16\" hidden=\"1\" width=\"18\" customWidth=\"1\"/>" +
                "<col min=\"21\" max=\"22\" hidden=\"1\" width=\"18\" customWidth=\"1\"/>" +
                "<col min=\"24\" max=\"31\" hidden=\"1\" width=\"18\" customWidth=\"1\"/>" +
                "</cols>";
            var x = Qs3dReviewXlsx.Begin("A1:AE" + Math.Max(1, count + 1).ToString(CultureInfo.InvariantCulture), columns);
            Qs3dReviewXlsx.Header(x, 1,
                "STT", id, type, "Floor", "Element A", "Element B", "Category A", "Category B", "Status",
                "ElementA_ID", "ElementA_Handle", "ElementB_ID", "ElementB_Handle", "DrawingFingerprint", "ModelRevision", "RuleId",
                evidence, m1, m2, m3, "CreatedAt", "LastCheckedAt", "Comment", TraceHeader,
                "CoordinationIssueId", "IssueRevision", "Assignee", "IssueUpdatedAtUtc", "IssueTitle", "IssueKind", "IssueSeverity");
            return x;
        }

        private static void IssueBase(
            StringBuilder x, int er, int index, string id, string type, string floor,
            string a, string b, string ca, string cb, string status,
            string aid, string ah, string bid, string bh,
            Qs3dReviewModelInfo model, string rule)
        {
            Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(0, er), index);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(1, er), id, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(2, er), type);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(3, er), floor);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(4, er), a, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(5, er), b, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(6, er), ca);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(7, er), cb);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(8, er), status);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(9, er), aid, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(10, er), ah, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(11, er), bid, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(12, er), bh, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(13, er), model.DrawingFingerprint, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(14, er), model.ModelRevision, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(15, er), rule);
        }

        private static void IssueTail(
            StringBuilder x,
            int er,
            CoordinationIssueExcelRow? lifecycle,
            Qs3dReviewIssueGeometry? geometry,
            string fallbackComment,
            string trace)
        {
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(20, er), Time(geometry?.CreatedAtUtc));
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(21, er), Time(geometry?.LastCheckedAtUtc));
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(22, er), fallbackComment ?? string.Empty, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(23, er), trace, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(24, er), lifecycle == null ? string.Empty : lifecycle.IssueId, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(25, er), lifecycle == null ? string.Empty : lifecycle.IssueRevision, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(26, er), lifecycle == null ? string.Empty : lifecycle.Assignee);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(27, er), LifecycleTime(lifecycle));
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(28, er), lifecycle == null ? string.Empty : lifecycle.Title, Qs3dReviewXlsx.WrappedStyle);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(29, er), lifecycle == null ? string.Empty : lifecycle.Kind.ToString());
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(30, er), lifecycle == null ? string.Empty : lifecycle.Severity.ToString());
        }

        private static string Rules(CoordinationRuleProfile? profile)
        {
            var count = profile?.Rules.Count ?? 0;
            var x = Qs3dReviewXlsx.Begin("A1:J" + Math.Max(1, count + 1).ToString(CultureInfo.InvariantCulture));
            Qs3dReviewXlsx.Header(x, 1, "ProfileId", "ProfileVersion", "RuleId", "RuleVersion", "Category A", "Category B", "Kind", "Severity", "Clearance", "Enabled");
            if (profile != null)
            {
                var rules = profile.Rules.OrderBy(r => r.RuleId, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.RuleId, StringComparer.Ordinal).ToArray();
                for (var i = 0; i < rules.Length; i++)
                {
                    var r = rules[i];
                    var er = i + 2;
                    Qs3dReviewXlsx.StartRow(x, er);
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(0, er), profile.ProfileId);
                    Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(1, er), profile.ProfileVersion);
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(2, er), r.RuleId);
                    Qs3dReviewXlsx.Integer(x, Qs3dReviewXlsx.Cell(3, er), r.RuleVersion);
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(4, er), r.LeftCategory);
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(5, er), r.RightCategory);
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(6, er), r.Kind.ToString());
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(7, er), r.Severity);
                    Qs3dReviewXlsx.Number(x, Qs3dReviewXlsx.Cell(8, er), r.Clearance);
                    Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(9, er), r.Enabled ? "TRUE" : "FALSE");
                    Qs3dReviewXlsx.EndRow(x);
                }
            }
            return Qs3dReviewXlsx.End(x, "A1:J" + Math.Max(1, count + 1).ToString(CultureInfo.InvariantCulture));
        }

        private static string ModelInfo(int detail, int summary, int clash, int duplicate, CoordinationRuleProfile? profile, Qs3dReviewModelInfo model)
        {
            var rows = new[]
            {
                new[] { "Key", "Value" },
                new[] { "SchemaVersion", SchemaVersion },
                new[] { "ProjectId", model.ProjectId },
                new[] { "DrawingName", model.DrawingName },
                new[] { "DrawingFingerprint", model.DrawingFingerprint },
                new[] { "ModelRevision", model.ModelRevision },
                new[] { "ExportedAtUtc", model.ExportedAtUtc.ToString("O", CultureInfo.InvariantCulture) },
                new[] { "QuantityDetailRows", detail.ToString(CultureInfo.InvariantCulture) },
                new[] { "QuantitySummaryRows", summary.ToString(CultureInfo.InvariantCulture) },
                new[] { "ClashRows", clash.ToString(CultureInfo.InvariantCulture) },
                new[] { "DuplicateRows", duplicate.ToString(CultureInfo.InvariantCulture) },
                new[] { "RuleProfileId", profile?.ProfileId ?? string.Empty },
                new[] { "RuleProfileVersion", profile == null ? string.Empty : profile.ProfileVersion.ToString(CultureInfo.InvariantCulture) },
                new[] { "RuleCount", (profile?.Rules.Count ?? 0).ToString(CultureInfo.InvariantCulture) },
                new[] { "LifecycleProjection", "CoordinationIssueExcelRow (optional, canonical #3496 contract)" },
                new[] { "TracePolicy", "DrawingFingerprint + ModelRevision + semantic ElementId/CAD Handle; fail closed on mismatch" }
            };
            var x = Qs3dReviewXlsx.Begin("A1:B" + rows.Length.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < rows.Length; i++) Qs3dReviewXlsx.TextRow(x, i + 1, i == 0, rows[i]);
            return Qs3dReviewXlsx.End(x, "A1:B" + rows.Length.ToString(CultureInfo.InvariantCulture));
        }

        private static void Kpi(StringBuilder x, int row, string label, double? value, string unit)
        {
            Qs3dReviewXlsx.StartRow(x, row);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(0, row), label);
            Qs3dReviewXlsx.OptionalNumber(x, Qs3dReviewXlsx.Cell(1, row), value);
            Qs3dReviewXlsx.Text(x, Qs3dReviewXlsx.Cell(2, row), unit);
            Qs3dReviewXlsx.EndRow(x);
        }

        private static double? Sum(IReadOnlyList<QuantityReportRow> rows, Func<QuantityReportRow, double> value, Func<QuantityReportRow, bool> evidence)
        {
            var found = false;
            var total = 0d;
            foreach (var row in rows)
            {
                if (!evidence(row)) continue;
                found = true;
                total += value(row);
                if (!Qs3dReviewXlsx.Finite(total)) throw new InvalidDataException("QS3D Review summary total overflowed a finite double.");
            }
            return found ? total : (double?)null;
        }

        private static string Join(IEnumerable<string> values) =>
            string.Join(";", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ThenBy(v => v, StringComparer.Ordinal));

        private static string Name(QuantityReportRow row) => string.IsNullOrWhiteSpace(row.ElementName) ? row.FamilyName : row.ElementName;
        private static string Display(string elementId, string handle) => string.IsNullOrWhiteSpace(elementId) ? handle : elementId;
        private static string Time(DateTimeOffset? value) => value.HasValue ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) : string.Empty;
        private static string LifecycleTime(CoordinationIssueExcelRow? value) => value == null ? string.Empty : value.UpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        private static void Limit(int count, string sheet) { if (count > MaxRows) throw new InvalidDataException(sheet + " exceeds the Excel row limit."); }
        private static string Required(string value, string label) { var v = (value ?? string.Empty).Trim(); if (v.Length == 0) throw new InvalidDataException(label + " is required."); Qs3dReviewModelInfo.VerifyXml(v, label); return v; }
        private static void Distinct(IEnumerable<string> values, string label) { var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (var value in values) { var v = Required(value, label); if (!seen.Add(v)) throw new InvalidDataException(label + " collection contains duplicate value: " + v + "."); } }
    }
}
