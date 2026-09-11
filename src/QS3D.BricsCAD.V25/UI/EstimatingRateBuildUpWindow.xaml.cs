using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Bricscad.ApplicationServices;
using QS3D.Core.Cost;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class EstimatingRateBuildUpWindow : Window
    {
        private readonly Document _document;
        private readonly IntPtr _nativeDatabaseIdentity;
        private EstimatingRateBuildUpRevision? _revision;

        public EstimatingRateBuildUpWindow(Document document, IntPtr nativeDatabaseIdentity)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (nativeDatabaseIdentity == IntPtr.Zero)
                throw new ArgumentException("A live native database identity is required.", nameof(nativeDatabaseIdentity));
            _nativeDatabaseIdentity = nativeDatabaseIdentity;
            ResourceRows = new ObservableCollection<EstimatingResourceRow>();
            InitializeComponent();
            DataContext = this;
            SeedStarterRows();
        }

        public ObservableCollection<EstimatingResourceRow> ResourceRows { get; }

        private void SeedStarterRows()
        {
            AddStarterRow("Material", "MAT-001", "Material", "kg", "QUOTE-MAT", "Supplier", "Quotation");
            AddStarterRow("Labour", "LAB-001", "Labour", "hr", "QUOTE-LAB", "Labour source", "Rate sheet");
            AddStarterRow("Plant", "PLANT-001", "Plant", "hr", "QUOTE-PLANT", "Plant source", "Rate sheet");
            AddStarterRow("Subcontract", "SUB-001", "Subcontract", "ea", "QUOTE-SUB", "Subcontractor", "Quotation");
        }

        private void AddStarterRow(
            string category,
            string resourceCode,
            string description,
            string unit,
            string sourceId,
            string supplier,
            string reference)
        {
            ResourceRows.Add(new EstimatingResourceRow
            {
                Category = category,
                ResourceCode = resourceCode,
                Description = description,
                Unit = unit,
                QuantityPerBillUnit = category == "Material" ? 1m : 0m,
                UnitRate = 0m,
                WastagePercent = 0m,
                SourceId = sourceId,
                Supplier = supplier,
                Reference = reference,
                SourceEffectiveDate = EffectiveDateBox.Text
            });
        }

        private void AddResource_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            var suffix = (ResourceRows.Count + 1).ToString("D3", CultureInfo.InvariantCulture);
            ResourceRows.Add(new EstimatingResourceRow
            {
                Category = "Material",
                ResourceCode = "RES-" + suffix,
                Description = "Resource " + suffix,
                Unit = "ea",
                QuantityPerBillUnit = 0m,
                UnitRate = 0m,
                WastagePercent = 0m,
                SourceId = "SOURCE-" + suffix,
                Supplier = "Supplier",
                Reference = "Quotation",
                SourceEffectiveDate = EffectiveDateBox.Text
            });
        }

        private void RemoveResource_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            if (ResourceGrid.SelectedItem is EstimatingResourceRow row)
                ResourceRows.Remove(row);
        }

        private void Evaluate_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                CommitGridEdits();
                var next = BuildRevisionFromInput();
                var result = next.Evaluate();
                _revision = next;
                PresentResult(result);
                PresentLifecycle();
                Report("Rate build-up evaluated through QS3D.Core.Cost.CostRateBuildUp.");
            }
            catch (Exception ex)
            {
                ReportSafeFailure("Unable to evaluate rate build-up", ex);
            }
        }

        private void NewRevision_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            if (_revision == null)
            {
                Report("Evaluate the initial draft before creating a successor revision.");
                return;
            }

            RevisionIdBox.Text = "R-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            EffectiveDateBox.Text = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ReasonBox.Text = "Rate revision";
            StatusText.Text = "New revision pending evaluation";
            MessageText.Text = "Edit resource/provenance inputs, then Evaluate to create a revision linked to " + _revision.RevisionId + ".";
        }

        private void Review_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_revision == null)
                    throw new InvalidOperationException("Evaluate the draft before review.");
                _revision = _revision.MarkReviewed(
                    RequireDecisionBy(),
                    RequireDecisionNote(),
                    DateTime.UtcNow);
                PresentLifecycle();
                Report("Estimating revision marked Reviewed.");
            }
            catch (Exception ex)
            {
                ReportSafeFailure("Unable to review estimating revision", ex);
            }
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_revision == null)
                    throw new InvalidOperationException("Evaluate and review the draft before approval.");
                _revision = _revision.MarkApproved(
                    RequireDecisionBy(),
                    RequireDecisionNote(),
                    DateTime.UtcNow);
                PresentLifecycle();
                Report("Estimating revision approved. Editing now requires a new revision identity.");
            }
            catch (Exception ex)
            {
                ReportSafeFailure("Unable to approve estimating revision", ex);
            }
        }

        private EstimatingRateBuildUpRevision BuildRevisionFromInput()
        {
            var buildUpId = RequireBox(BuildUpIdBox.Text, "Build-up ID");
            var revisionId = RequireBox(RevisionIdBox.Text, "Revision ID");
            var costCode = new CostCode(RequireBox(CostCodeBox.Text, "Cost code"));
            var billUnit = RequireBox(BillUnitBox.Text, "Bill unit");
            var currency = RequireBox(CurrencyBox.Text, "Currency");
            var effectiveUtc = ParseUtcDate(EffectiveDateBox.Text, "Revision effective date");
            var author = RequireBox(AuthorBox.Text, "Author");
            var reason = RequireBox(ReasonBox.Text, "Revision reason");
            var overhead = ParseDecimal(OverheadBox.Text, "Overhead %");
            var profit = ParseDecimal(ProfitBox.Text, "Profit %");
            var lines = BuildCoreLines(currency);

            if (_revision == null)
            {
                return EstimatingRateBuildUpRevision.CreateDraft(
                    buildUpId,
                    revisionId,
                    costCode,
                    billUnit,
                    currency,
                    effectiveUtc,
                    author,
                    reason,
                    lines,
                    overhead,
                    profit);
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(revisionId, _revision.RevisionId))
            {
                if (_revision.Status != EstimatingRevisionStatus.Draft)
                    throw new InvalidOperationException("Reviewed/approved content is immutable. Use New revision before editing or recalculating it.");
                return EstimatingRateBuildUpRevision.CreateDraft(
                    buildUpId,
                    revisionId,
                    costCode,
                    billUnit,
                    currency,
                    effectiveUtc,
                    author,
                    reason,
                    lines,
                    overhead,
                    profit);
            }

            RequireRevisionIdentityUnchanged(buildUpId, costCode, billUnit, currency);
            return _revision.CreateNextRevision(
                revisionId,
                effectiveUtc,
                author,
                reason,
                lines,
                overhead,
                profit);
        }

        private List<EstimatingRateLine> BuildCoreLines(string currency)
        {
            var lines = new List<EstimatingRateLine>(ResourceRows.Count);
            for (var i = 0; i < ResourceRows.Count; i++)
            {
                var row = ResourceRows[i];
                if (!Enum.TryParse(row.Category, true, out EstimatingResourceCategory category))
                    throw new ArgumentException("Resource row " + (i + 1) + " has an unknown category. Use Material, Labour, Plant or Subcontract.");

                var source = new EstimatingRateSource(
                    RequireBox(row.SourceId, "Source ID"),
                    RequireBox(row.Supplier, "Supplier"),
                    RequireBox(row.Reference, "Source reference"),
                    ParseUtcDate(row.SourceEffectiveDate, "Source effective date"),
                    currency);
                lines.Add(new EstimatingRateLine(
                    RequireBox(row.ResourceCode, "Resource code"),
                    category,
                    RequireBox(row.Description, "Resource description"),
                    RequireBox(row.Unit, "Resource unit"),
                    row.QuantityPerBillUnit,
                    row.UnitRate,
                    row.WastagePercent,
                    source));
            }
            return lines;
        }

        private void RequireRevisionIdentityUnchanged(string buildUpId, CostCode costCode, string billUnit, string currency)
        {
            if (_revision == null) return;
            if (!StringComparer.OrdinalIgnoreCase.Equals(buildUpId, _revision.BuildUpId) ||
                !_revision.CostCode.Equals(costCode) ||
                !StringComparer.Ordinal.Equals(billUnit, _revision.BillUnit) ||
                !StringComparer.Ordinal.Equals(currency, _revision.Currency))
            {
                throw new InvalidOperationException("A successor revision must retain build-up ID, cost code, bill unit and currency. Start a separate build-up for a different identity/scope.");
            }
        }

        private void PresentResult(CostRateBuildUp result)
        {
            DirectCostText.Text = FormatMoney(result.DirectUnitCost);
            OverheadCostText.Text = FormatMoney(result.OverheadUnitCost);
            ProfitCostText.Text = FormatMoney(result.ProfitUnitCost);
            UnitRateText.Text = FormatMoney(result.UnitRate) + " " + result.Currency + "/" + result.BillUnit;
        }

        private void PresentLifecycle()
        {
            if (_revision == null) return;
            StatusText.Text = _revision.Status + " • " + _revision.RevisionId;
            ProvenanceText.Text = "Build-up " + _revision.BuildUpId +
                                  " • " + _revision.Lines.Count + " resources" +
                                  (_revision.PreviousRevisionId == null ? string.Empty : " • previous " + _revision.PreviousRevisionId);
        }

        private bool EnsureActive()
        {
            try
            {
                var active = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (!ReferenceEquals(active, _document))
                {
                    Report("Estimating workspace is bound to another drawing. Reactivate its drawing or reopen QS3DESTIMATE.");
                    return false;
                }
                var database = _document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero || database.UnmanagedObject != _nativeDatabaseIdentity)
                {
                    Report("Estimating workspace drawing generation is stale. Close and reopen QS3DESTIMATE.");
                    return false;
                }
                return true;
            }
            catch
            {
                Report("Estimating workspace cannot validate the active drawing generation.");
                return false;
            }
        }

        private void CommitGridEdits()
        {
            ResourceGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
            ResourceGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        }

        private static string RequireBox(string? value, string label)
        {
            var candidate = value ?? string.Empty;
            if (candidate.Length == 0 || !StringComparer.Ordinal.Equals(candidate, candidate.Trim()))
                throw new ArgumentException(label + " is required and must be canonically trimmed.");
            return candidate;
        }

        private static decimal ParseDecimal(string? value, string label)
        {
            var candidate = value ?? string.Empty;
            if (decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.CurrentCulture, out var current))
                return current;
            if (decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
                return invariant;
            throw new ArgumentException(label + " is not a valid decimal value.");
        }

        private static DateTime ParseUtcDate(string? value, string label)
        {
            var candidate = value ?? string.Empty;
            if (!DateTime.TryParseExact(
                    candidate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                throw new ArgumentException(label + " must use yyyy-MM-dd.");
            }
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        private string RequireDecisionBy() => RequireBox(DecisionByBox.Text, "Reviewer/approver");
        private string RequireDecisionNote() => RequireBox(DecisionNoteBox.Text, "Decision note");

        private static string FormatMoney(decimal value) =>
            value.ToString("N2", CultureInfo.CurrentCulture);

        private void ReportSafeFailure(string prefix, Exception exception)
        {
            var message = prefix + ". Check the highlighted/input values and revision state.";
            Report(message);
            try { PaletteCoordinator.SetStatus(prefix + ": " + exception.GetType().Name); } catch { }
        }

        private void Report(string message)
        {
            MessageText.Text = message;
            try { _document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }

    public sealed class EstimatingResourceRow
    {
        public string Category { get; set; } = "Material";
        public string ResourceCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = "ea";
        public decimal QuantityPerBillUnit { get; set; }
        public decimal UnitRate { get; set; }
        public decimal WastagePercent { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string SourceEffectiveDate { get; set; } = "2026-09-11";
    }
}
