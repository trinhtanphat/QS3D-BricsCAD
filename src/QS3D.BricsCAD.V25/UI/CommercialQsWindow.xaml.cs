using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class CommercialQsWindow : Window
    {
        private readonly Document _document;
        private CommercialVariationRegister? _variationRegister;
        private InterimPaymentCertificate? _ipc;
        private FinalAccountResult? _finalAccount;

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

        private void OnExportCommercialCsv(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_variationRegister == null && _ipc == null && _finalAccount == null)
                    throw new InvalidOperationException("Build at least one validated commercial result before export.");

                var dialog = new SaveFileDialog
                {
                    Title = "Export Commercial QS CSV",
                    Filter = "CSV (*.csv)|*.csv",
                    AddExtension = true,
                    DefaultExt = ".csv",
                    FileName = "QS3D-Commercial-" + SafeDrawingStem(_document) + ".csv"
                };
                if (dialog.ShowDialog(this) != true) return;

                WriteCommercialCsv(dialog.FileName);
                SetStatus("Commercial CSV exported from validated Core results: " + dialog.FileName);
            }
            catch (Exception ex)
            {
                SetStatus("Commercial export failed: " + ex.Message);
            }
        }

        private void WriteCommercialCsv(string path)
        {
            var rows = new List<string>
            {
                Csv("section", "id", "metric", "value", "currency", "detail")
            };

            if (_variationRegister != null)
            {
                foreach (var variation in _variationRegister.Variations)
                {
                    rows.Add(Csv("variation", variation.VariationId, "proposed_amount", Invariant(variation.ProposedAmount), variation.Currency, variation.Description));
                    rows.Add(Csv("variation", variation.VariationId, "approved_amount", Invariant(variation.ApprovedAmount), variation.Currency, variation.Status.ToString()));
                    rows.Add(Csv("variation", variation.VariationId, "revision", variation.Revision.RevisionId, variation.Currency, variation.Revision.SourceKind));
                }
                rows.Add(Csv("variation_register", "register", "approved_net_change", Invariant(_variationRegister.ApprovedNetChange), _variationRegister.Currency, "Core validated"));
            }

            if (_ipc != null)
            {
                rows.Add(Csv("ipc", _ipc.CertificateId, "gross_certified_this_period", Invariant(_ipc.GrossCertifiedThisPeriod), _ipc.Currency, "Core result"));
                rows.Add(Csv("ipc", _ipc.CertificateId, "retention_this_period", Invariant(_ipc.RetentionThisPeriod), _ipc.Currency, "Core result"));
                rows.Add(Csv("ipc", _ipc.CertificateId, "net_certified_this_period", Invariant(_ipc.NetCertifiedThisPeriod), _ipc.Currency, "Core result"));
                rows.Add(Csv("ipc", _ipc.CertificateId, "cumulative_net_certified", Invariant(_ipc.CumulativeNetCertified), _ipc.Currency, "Core result"));
            }

            if (_finalAccount != null)
            {
                rows.Add(Csv("final_account", _finalAccount.FinalAccountId, "final_contract_value", Invariant(_finalAccount.FinalContractValue), _finalAccount.Currency, "Core result"));
                rows.Add(Csv("final_account", _finalAccount.FinalAccountId, "amount_due", Invariant(_finalAccount.AmountDue), _finalAccount.Currency, "Core result"));
                rows.Add(Csv("final_account", _finalAccount.FinalAccountId, "recovery_due", Invariant(_finalAccount.RecoveryDue), _finalAccount.Currency, "Core result"));
                rows.Add(Csv("final_account", _finalAccount.FinalAccountId, "unreleased_retention", Invariant(_finalAccount.UnreleasedRetention), _finalAccount.Currency, "Core result"));
            }

            File.WriteAllText(path, string.Join("\r\n", rows) + "\r\n", new UTF8Encoding(false));
        }

        private CommercialVariationRegister RequireVariationRegister()
        {
            if (_variationRegister == null)
                throw new InvalidOperationException("Build and validate the Variation register first.");
            return _variationRegister;
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

        private static string Csv(params string[] values)
        {
            return string.Join(",", values.Select(EscapeCsv));
        }

        private static string EscapeCsv(string value)
        {
            var text = value ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);

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
    }
}
