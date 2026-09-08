using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Audit;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;
using QS3D.Core.Export;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class CommercialQsWindow : Window
    {
        private readonly Document _document;
        private CommercialVariationRegister? _variationRegister;
        private InterimPaymentCertificate? _ipc;
        private FinalAccountResult? _finalAccount;
        private TenderProcurementPackage? _tenderPackage;
        private TenderProcurementEvaluation? _tenderEvaluation;
        private TenderAwardDecision? _tenderAward;
        private CommercialControlPeriod? _cvrPeriod;
        private CommercialCostControlResult? _cvrResult;

        public CommercialQsWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, document);
            Title = "QS3D • Commercial QS • " + DrawingLabel(document);
        }

        private void OnBuildVariationRegister(object sender, RoutedEventArgs e)
        {
            try
            {
                var currency = CanonicalCurrency(CurrencyBox.Text);
                var variations = ParseVariationRows(VariationLinesBox.Text, currency);
                _variationRegister = new CommercialVariationRegister(currency, variations);
                _ipc = null;
                _finalAccount = null;

                VariationGrid.ItemsSource = _variationRegister.Variations;
                VariationSummary.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} variation(s) validated by Core • approved net change {1:N2} {2}",
                    _variationRegister.Variations.Count,
                    _variationRegister.ApprovedNetChange,
                    _variationRegister.Currency);
                if (_variationRegister.Variations.Count > 0)
                    VariationGrid.SelectedIndex = 0;
                else
                    SelectedVariationContext.Text = "No variation selected";
                SetStatus("Variation register validated by CommercialVariationRegister.");
            }
            catch (Exception ex)
            {
                SetStatus("Variation register rejected: " + ex.Message);
            }
        }

        private void OnVariationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VariationGrid.SelectedItem is CommercialVariation variation)
            {
                SelectedVariationContext.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} • {1} • proposed {2:N2} • approved {3:N2} {4} • {5}",
                    variation.VariationId,
                    variation.Status,
                    variation.ProposedAmount,
                    variation.ApprovedAmount,
                    variation.Currency,
                    variation.Description);
            }
            else
            {
                SelectedVariationContext.Text = "No variation selected";
            }
        }

        private void OnCreateIpc(object sender, RoutedEventArgs e)
        {
            try
            {
                var register = RequireVariationRegister();
                var progressRows = ParseProgressRows(ProgressLinesBox.Text);
                var contractItems = progressRows.Select(x => x.ContractItem).ToArray();
                var claimLines = progressRows.Select(x => x.ClaimLine).ToArray();
                var progress = new ProgressClaimService().Evaluate(
                    contractItems,
                    claimLines,
                    retentionPercent: ParseDecimal(ProgressRetentionPercentBox.Text, "progress retention percent"));

                var variationLines = ParseVariationCertificationRows(IpcVariationLinesBox.Text);
                _ipc = new InterimPaymentCertificateService().Create(
                    RequireToken(IpcIdBox.Text, "IPC certificate id"),
                    register.Currency,
                    progress,
                    register,
                    variationLines,
                    variationRetentionThisPeriod: ParseDecimal(VariationRetentionBox.Text, "variation retention"),
                    retentionRelease: ParseDecimal(IpcRetentionReleaseBox.Text, "IPC retention release"),
                    advanceRecovery: ParseDecimal(AdvanceRecoveryBox.Text, "advance recovery"),
                    otherDeductions: ParseDecimal(IpcOtherDeductionsBox.Text, "other deductions"),
                    previousNetCertified: ParseDecimal(PreviousNetCertifiedBox.Text, "previous net certified"));

                IpcSummary.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "IPC {0} • gross {1:N2} • retention {2:N2} • net this period {3:N2} • cumulative net {4:N2} {5}",
                    _ipc.CertificateId,
                    _ipc.GrossCertifiedThisPeriod,
                    _ipc.RetentionThisPeriod,
                    _ipc.NetCertifiedThisPeriod,
                    _ipc.CumulativeNetCertified,
                    _ipc.Currency);
                SetStatus("IPC calculated by ProgressClaimService + InterimPaymentCertificateService.");
            }
            catch (Exception ex)
            {
                SetStatus("IPC rejected: " + ex.Message);
            }
        }

        private void OnReconcileFinalAccount(object sender, RoutedEventArgs e)
        {
            try
            {
                var register = RequireVariationRegister();
                _finalAccount = new FinalAccountService().Reconcile(
                    RequireToken(FinalAccountIdBox.Text, "final account id"),
                    register.Currency,
                    ParseDecimal(OriginalContractValueBox.Text, "original contract value"),
                    register,
                    ParseDecimal(FinalAdjustmentBox.Text, "final adjustment"),
                    ParseDecimal(PreviousGrossCertifiedBox.Text, "previous gross certified"),
                    ParseDecimal(RetentionHeldBox.Text, "retention held"),
                    ParseDecimal(FinalRetentionReleaseBox.Text, "retention release"),
                    ParseDecimal(FinalDeductionsBox.Text, "final deductions"));

                FinalAccountSummary.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Final Account {0} • final contract {1:N2} • amount due {2:N2} • recovery due {3:N2} • unreleased retention {4:N2} {5}",
                    _finalAccount.FinalAccountId,
                    _finalAccount.FinalContractValue,
                    _finalAccount.AmountDue,
                    _finalAccount.RecoveryDue,
                    _finalAccount.UnreleasedRetention,
                    _finalAccount.Currency);
                SetStatus("Final Account reconciled by FinalAccountService.");
            }
            catch (Exception ex)
            {
                SetStatus("Final Account rejected: " + ex.Message);
            }
        }

        private void OnEvaluateTender(object sender, RoutedEventArgs e)
        {
            try
            {
                var packageId = RequireToken(TenderPackageIdBox.Text, "tender package id");
                var currency = CanonicalCurrency(TenderCurrencyBox.Text);
                _tenderPackage = new TenderProcurementPackage(
                    packageId,
                    RequireText(TenderDescriptionBox.Text, "tender package description"),
                    currency,
                    ProcurementPackageStatus.Closed,
                    ParseTenderRequirements(TenderRequirementLinesBox.Text),
                    ParseTenderComplianceRequirements(TenderComplianceRequirementLinesBox.Text),
                    ParseTenderBids(TenderBidLinesBox.Text, currency),
                    new CommercialRevisionRef("procurement-package", packageId, RequireToken(TenderPackageRevisionBox.Text, "tender package revision")));

                _tenderEvaluation = new TenderProcurementService().Evaluate(
                    _tenderPackage,
                    ParseTenderComplianceResponses(TenderComplianceResponseLinesBox.Text));
                _tenderAward = null;
                CommercialWorkflowAudit.RecordTenderEvaluation(CommercialAudit(), _tenderEvaluation);

                if (!string.IsNullOrWhiteSpace(_tenderEvaluation.RecommendedBidId))
                    TenderAwardBidBox.Text = _tenderEvaluation.RecommendedBidId;
                TenderSummary.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Package {0} • {1} bid(s) • recommended {2} • revision {3}",
                    _tenderPackage.PackageId,
                    _tenderPackage.Bids.Count,
                    string.IsNullOrWhiteSpace(_tenderEvaluation.RecommendedBidId) ? "none" : _tenderEvaluation.RecommendedBidId,
                    _tenderPackage.Revision.RevisionId);
                TenderAwardSummary.Text = "No award recorded for the current evaluation.";
                SetStatus("Tender evaluated by TenderProcurementService; audit event recorded in the canonical QS3D project state.");
            }
            catch (Exception ex)
            {
                SetStatus("Tender evaluation rejected: " + ex.Message);
            }
        }

        private void OnAwardTender(object sender, RoutedEventArgs e)
        {
            try
            {
                var package = RequireTenderPackage();
                var evaluation = RequireTenderEvaluation();
                var awardId = RequireToken(TenderAwardIdBox.Text, "tender award id");
                _tenderAward = new TenderProcurementService().Award(
                    package,
                    evaluation,
                    awardId,
                    RequireToken(TenderAwardBidBox.Text, "tender award bid id"),
                    new CommercialRevisionRef("procurement-award", awardId, RequireToken(TenderAwardRevisionBox.Text, "tender award revision")));
                CommercialWorkflowAudit.RecordTenderAward(CommercialAudit(), _tenderAward);
                TenderAwardSummary.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Award {0} • {1} / {2} • evaluated total {3:N2} {4} • revision {5}",
                    _tenderAward.AwardId,
                    _tenderAward.BidId,
                    _tenderAward.Bidder,
                    _tenderAward.EvaluatedTotal,
                    _tenderAward.Currency,
                    _tenderAward.AwardRevision.RevisionId);
                SetStatus("Tender award validated by TenderProcurementService and recorded in the canonical audit trail.");
            }
            catch (Exception ex)
            {
                SetStatus("Tender award rejected: " + ex.Message);
            }
        }

        private void OnEvaluateCvr(object sender, RoutedEventArgs e)
        {
            try
            {
                var periodId = RequireToken(CvrPeriodIdBox.Text, "CVR period id");
                _cvrPeriod = new CommercialControlPeriod(
                    periodId,
                    CanonicalCurrency(CvrCurrencyBox.Text),
                    CommercialControlPeriodStatus.Open,
                    ParseDecimal(CvrOriginalBudgetBox.Text, "CVR original budget"),
                    ParseDecimal(CvrVariationNetBox.Text, "CVR approved variation net change"),
                    ParseDecimal(CvrCommittedCostBox.Text, "CVR committed cost"),
                    ParseDecimal(CvrActualCostBox.Text, "CVR actual cost"),
                    ParseDecimal(CvrAccruedCostBox.Text, "CVR accrued cost"),
                    ParseDecimal(CvrEarnedValueBox.Text, "CVR earned value"),
                    ParseDecimal(CvrForecastToCompleteBox.Text, "CVR forecast cost to complete"),
                    string.Empty,
                    new CommercialRevisionRef("commercial-control-period", periodId, RequireToken(CvrRevisionBox.Text, "CVR revision")));
                _cvrResult = new CommercialCostControlService().Evaluate(_cvrPeriod);
                CommercialWorkflowAudit.RecordCvrEvaluation(CommercialAudit(), _cvrResult);
                UpdateCvrSummary();
                SetStatus("CVR evaluated by CommercialCostControlService; Core owns revised budget, EAC, variance and margin.");
            }
            catch (Exception ex)
            {
                SetStatus("CVR evaluation rejected: " + ex.Message);
            }
        }

        private void OnFreezeCvr(object sender, RoutedEventArgs e)
        {
            try
            {
                var period = RequireCvrPeriod();
                _cvrPeriod = new CommercialCostControlService().Freeze(
                    period,
                    CvrRevision(period.PeriodId, CvrTransitionRevisionBox.Text, "CVR freeze revision"));
                _cvrResult = new CommercialCostControlService().Evaluate(_cvrPeriod);
                CommercialWorkflowAudit.RecordCvrFreeze(CommercialAudit(), _cvrPeriod);
                UpdateCvrSummary();
                SetStatus("CVR period frozen with a fresh Core revision.");
            }
            catch (Exception ex)
            {
                SetStatus("CVR freeze rejected: " + ex.Message);
            }
        }

        private void OnReopenCvr(object sender, RoutedEventArgs e)
        {
            try
            {
                var period = RequireCvrPeriod();
                _cvrPeriod = new CommercialCostControlService().Reopen(
                    period,
                    RequireText(CvrReopenReasonBox.Text, "CVR reopen reason"),
                    CvrRevision(period.PeriodId, CvrTransitionRevisionBox.Text, "CVR reopen revision"));
                _cvrResult = new CommercialCostControlService().Evaluate(_cvrPeriod);
                CommercialWorkflowAudit.RecordCvrReopen(CommercialAudit(), _cvrPeriod);
                UpdateCvrSummary();
                SetStatus("CVR period reopened with explicit reason and fresh Core revision.");
            }
            catch (Exception ex)
            {
                SetStatus("CVR reopen rejected: " + ex.Message);
            }
        }

        private void OnReviseCvrForecast(object sender, RoutedEventArgs e)
        {
            try
            {
                var period = RequireCvrPeriod();
                _cvrPeriod = new CommercialCostControlService().ReviseForecast(
                    period,
                    ParseDecimal(CvrRevisedForecastBox.Text, "CVR revised forecast cost to complete"),
                    CvrRevision(period.PeriodId, CvrForecastRevisionBox.Text, "CVR forecast revision"));
                _cvrResult = new CommercialCostControlService().Evaluate(_cvrPeriod);
                CommercialWorkflowAudit.RecordCvrForecastRevision(CommercialAudit(), _cvrPeriod, _cvrResult);
                UpdateCvrSummary();
                SetStatus("CVR forecast revised through CommercialCostControlService with fresh revision provenance.");
            }
            catch (Exception ex)
            {
                SetStatus("CVR forecast revision rejected: " + ex.Message);
            }
        }

        private void OnExportCommercialWorkbook(object sender, RoutedEventArgs e)
        {
            try
            {
                var snapshot = new CommercialQsWorkbookSnapshot(
                    _variationRegister,
                    _ipc,
                    _finalAccount,
                    _tenderPackage,
                    _tenderEvaluation,
                    _tenderAward,
                    _cvrResult);
                var dialog = new SaveFileDialog
                {
                    Title = "Export Commercial QS XLSX",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    AddExtension = true,
                    DefaultExt = ".xlsx",
                    FileName = "QS3D-Commercial-" + SafeDrawingStem(_document) + ".xlsx"
                };
                if (dialog.ShowDialog(this) != true) return;

                CommercialQsWorkbook.Export(dialog.FileName, snapshot);
                CommercialWorkflowAudit.RecordReportExport(CommercialAudit(), CommercialQsWorkbook.SchemaVersion, dialog.FileName);
                SetStatus("Commercial XLSX exported from validated Core results and recorded in the canonical audit trail: " + dialog.FileName);
            }
            catch (Exception ex)
            {
                SetStatus("Commercial export failed: " + ex.Message);
            }
        }

        private void UpdateCvrSummary()
        {
            if (_cvrResult == null)
            {
                CvrSummary.Text = "No CVR result is available.";
                return;
            }
            CvrSummary.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} • {1} • revised budget {2:N2} • cost to date {3:N2} • committed exposure {4:N2} • EAC {5:N2} • variance {6:N2} • CVR margin {7:N2} {8} • revision {9}",
                _cvrResult.PeriodId,
                _cvrResult.Status,
                _cvrResult.RevisedBudget,
                _cvrResult.CostToDate,
                _cvrResult.CommittedExposure,
                _cvrResult.ForecastFinalCost,
                _cvrResult.ForecastVariance,
                _cvrResult.CvrMargin,
                _cvrResult.Currency,
                _cvrResult.Revision.RevisionId);
        }

        private AuditTrail CommercialAudit()
        {
            return AuditTrail.ForProject(ProjectContextCoordinator.GetOrCreate(_document));
        }

        private CommercialVariationRegister RequireVariationRegister()
        {
            if (_variationRegister == null)
                throw new InvalidOperationException("Build and validate the Variation register first.");
            return _variationRegister;
        }

        private TenderProcurementPackage RequireTenderPackage()
        {
            if (_tenderPackage == null)
                throw new InvalidOperationException("Evaluate a closed Tender package first.");
            return _tenderPackage;
        }

        private TenderProcurementEvaluation RequireTenderEvaluation()
        {
            if (_tenderEvaluation == null)
                throw new InvalidOperationException("Evaluate a closed Tender package first.");
            return _tenderEvaluation;
        }

        private CommercialControlPeriod RequireCvrPeriod()
        {
            if (_cvrPeriod == null)
                throw new InvalidOperationException("Evaluate a CVR period first.");
            return _cvrPeriod;
        }

        private static IReadOnlyList<CommercialVariation> ParseVariationRows(string text, string currency)
        {
            var rows = new List<CommercialVariation>();
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 6, "variation");
                if (!Enum.TryParse(parts[4], true, out CommercialVariationStatus status))
                    throw new FormatException("Unknown variation status: " + parts[4] + ".");
                var id = RequireToken(parts[0], "variation id");
                rows.Add(new CommercialVariation(
                    id,
                    RequireText(parts[1], "variation description"),
                    currency,
                    ParseDecimal(parts[2], "proposed amount"),
                    ParseDecimal(parts[3], "approved amount"),
                    status,
                    new CommercialRevisionRef("variation", id, RequireToken(parts[5], "variation revision"))));
            }
            return rows;
        }

        private static IReadOnlyList<ProgressRow> ParseProgressRows(string text)
        {
            var rows = new List<ProgressRow>();
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 6, "progress");
                var itemId = RequireToken(parts[0], "progress item id");
                rows.Add(new ProgressRow(
                    new ProgressContractItem(
                        itemId,
                        RequireToken(parts[1], "progress unit").ToLowerInvariant(),
                        ParseDecimal(parts[2], "contract quantity"),
                        ParseDecimal(parts[3], "unit rate")),
                    new ProgressClaimLine(
                        itemId,
                        ParseDecimal(parts[4], "previous quantity"),
                        ParseDecimal(parts[5], "this-period quantity"))));
            }
            if (rows.Count == 0)
                throw new FormatException("At least one progress row is required for IPC.");
            return rows;
        }

        private static IReadOnlyList<VariationCertificationLine> ParseVariationCertificationRows(string text)
        {
            var rows = new List<VariationCertificationLine>();
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 3, "variation certification");
                rows.Add(new VariationCertificationLine(
                    RequireToken(parts[0], "variation certification id"),
                    ParseDecimal(parts[1], "previous variation certification"),
                    ParseDecimal(parts[2], "variation certified this period")));
            }
            return rows;
        }

        private static IReadOnlyList<TenderRequirement> ParseTenderRequirements(string text)
        {
            var rows = new List<TenderRequirement>();
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 4, "tender requirement");
                rows.Add(new TenderRequirement(
                    RequireToken(parts[0], "tender requirement item code"),
                    RequireText(parts[1], "tender requirement description"),
                    RequireToken(parts[2], "tender requirement unit").ToLowerInvariant(),
                    ParseDecimal(parts[3], "tender requirement quantity")));
            }
            if (rows.Count == 0) throw new FormatException("At least one tender requirement is required.");
            return rows;
        }

        private static IReadOnlyList<TenderComplianceRequirement> ParseTenderComplianceRequirements(string text)
        {
            var rows = new List<TenderComplianceRequirement>();
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 3, "tender compliance requirement");
                rows.Add(new TenderComplianceRequirement(
                    RequireToken(parts[0], "tender compliance code"),
                    RequireText(parts[1], "tender compliance description"),
                    ParseBoolean(parts[2], "tender compliance mandatory flag")));
            }
            return rows;
        }

        private static IReadOnlyList<TenderBid> ParseTenderBids(string text, string currency)
        {
            var grouped = new Dictionary<string, TenderBidBuilder>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 4, "tender bid price");
                var bidId = RequireToken(parts[0], "tender bid id");
                var bidder = RequireText(parts[1], "tender bidder");
                if (!grouped.TryGetValue(bidId, out var builder))
                {
                    builder = new TenderBidBuilder(bidder);
                    grouped.Add(bidId, builder);
                }
                else if (!string.Equals(builder.Bidder, bidder, StringComparison.Ordinal))
                {
                    throw new FormatException("Tender bid " + bidId + " uses inconsistent bidder names.");
                }
                builder.Lines.Add(new TenderQuoteLine(
                    RequireToken(parts[2], "tender bid item code"),
                    ParseDecimal(parts[3], "tender bid unit rate")));
            }
            if (grouped.Count == 0) throw new FormatException("At least one tender bid is required.");
            return grouped
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new TenderBid(x.Key, x.Value.Bidder, currency, x.Value.Lines))
                .ToArray();
        }

        private static IReadOnlyList<TenderComplianceResponse> ParseTenderComplianceResponses(string text)
        {
            var rows = new List<TenderComplianceResponse>();
            foreach (var line in NonEmptyLines(text))
            {
                var parts = SplitPipe(line, 4, "tender compliance response");
                rows.Add(new TenderComplianceResponse(
                    RequireToken(parts[0], "tender compliance bid id"),
                    RequireToken(parts[1], "tender compliance requirement code"),
                    ParseBoolean(parts[2], "tender compliance result"),
                    parts[3].Trim()));
            }
            return rows;
        }

        private static CommercialRevisionRef CvrRevision(string periodId, string revision, string label)
        {
            return new CommercialRevisionRef("commercial-control-period", periodId, RequireToken(revision, label));
        }

        private static IEnumerable<string> NonEmptyLines(string text)
        {
            return (text ?? string.Empty)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);
        }

        private static string[] SplitPipe(string line, int expectedCount, string label)
        {
            var parts = line.Split('|').Select(x => x.Trim()).ToArray();
            if (parts.Length != expectedCount)
                throw new FormatException(label + " row must contain exactly " + expectedCount + " pipe-delimited fields: " + line);
            return parts;
        }

        private static string CanonicalCurrency(string value)
        {
            return RequireToken(value, "currency").ToUpperInvariant();
        }

        private static string RequireToken(string value, string label)
        {
            var canonical = (value ?? string.Empty).Trim();
            if (canonical.Length == 0)
                throw new FormatException(label + " is required.");
            return canonical;
        }

        private static string RequireText(string value, string label)
        {
            var canonical = (value ?? string.Empty).Trim();
            if (canonical.Length == 0)
                throw new FormatException(label + " is required.");
            return canonical;
        }

        private static decimal ParseDecimal(string value, string label)
        {
            if (!decimal.TryParse((value ?? string.Empty).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                throw new FormatException(label + " must be an invariant decimal number.");
            return parsed;
        }

        private static bool ParseBoolean(string value, string label)
        {
            if (!bool.TryParse((value ?? string.Empty).Trim(), out var parsed))
                throw new FormatException(label + " must be true or false.");
            return parsed;
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text;
            try { PaletteCoordinator.SetStatus(text); } catch { }
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Unsaved drawing";
            try { return Path.GetFileName(name); }
            catch { return name; }
        }

        private static string SafeDrawingStem(Document document)
        {
            var label = DrawingLabel(document);
            try { label = Path.GetFileNameWithoutExtension(label); } catch { }
            foreach (var invalid in Path.GetInvalidFileNameChars()) label = label.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(label) ? "Drawing" : label;
        }

        private sealed class ProgressRow
        {
            public ProgressRow(ProgressContractItem contractItem, ProgressClaimLine claimLine)
            {
                ContractItem = contractItem;
                ClaimLine = claimLine;
            }

            public ProgressContractItem ContractItem { get; }
            public ProgressClaimLine ClaimLine { get; }
        }

        private sealed class TenderBidBuilder
        {
            public TenderBidBuilder(string bidder)
            {
                Bidder = bidder;
                Lines = new List<TenderQuoteLine>();
            }

            public string Bidder { get; }
            public List<TenderQuoteLine> Lines { get; }
        }
    }
}
