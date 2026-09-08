using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.LocalQualification;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

#if BRICSCAD_V26
namespace QS3D.LocalQualification.V26
#else
namespace QS3D.LocalQualification.V25
#endif
{
    public sealed partial class Local022NativeFootingProbeCommands
    {
        private static QuantityObserver? _quantityObserver;

        [CommandMethod("QL22QTY", CommandFlags.Modal)]
        public void QuantityUi()
        {
            Context? context = null;
            var phase = "quantity";
            try
            {
                if (Environment.GetEnvironmentVariable("QS3D_LOCAL022_QUANTITY_UI") != "1" ||
                    Environment.GetEnvironmentVariable("QS3D_LOCAL022_RENDER_EXPERIMENT") != "0")
                    throw new ProbeException("quantity_mode_required");
                var nativePhase = Environment.GetEnvironmentVariable(PhaseVariable);
                if (nativePhase != "run" && nativePhase != "reopen") throw new ProbeException("quantity_phase_invalid");
                phase = nativePhase == "run" ? "quantity" : "quantityreopen";
                context = BindContext(nativePhase);
                RequireMeterDrawing(context.Document);
                var project = GetOrCreateProject(context.Document);
                var continuity = RequireContinuity(context, project);
                RequireTotalDeltas(context.Document, project, continuity.SemanticBaseline, continuity.FamilyBaseline, continuity.NativeBaseline);
                VerifyExpectedPersistedState(context.Document, project, context.RunId, "quantity_before");
                // Previous native failures cannot be hidden by the reporting observer.
                foreach (var required in nativePhase == "run" ? new[] { "run", "saved" } : new[] { "reopen" })
                {
                    var raw = File.ReadAllText(RequireUiChildPath(context, "phase-" + required + ".json"));
                    if (!raw.Contains("\"status\":\"PASS\"") || !raw.Contains("\"run_id\":\"" + context.RunId + "\""))
                        throw new ProbeException("quantity_native_precondition_failed");
                }
                if (project.Elements.Count != 2 || _quantityObserver != null)
                    throw new ProbeException("quantity_fixture_not_isolated");
                RequireUiOutputAbsent(context, phase);
                _quantityObserver = new QuantityObserver(context, project, phase);
                _quantityObserver.Start();
                context.Document.SendStringToExecute("QS3DBQ ", true, false, false);
            }
            catch (System.Exception error)
            {
                WriteUiFailure(context, phase, "quantity_bind", error);
                QueueOwnedQuit(context, true);
            }
        }

        // Observes the real product window and its actual routed events. This
        // class never clicks/invokes a button, changes a report mode, selects a
        // row, or supplies report rows. Input remains the external operator's.
        private sealed class QuantityObserver
        {
            private readonly Context _context;
            private readonly ProjectState _project;
            private readonly string _phase;
            private readonly string _projectDigest;
            private readonly string _nativeDigest;
            private readonly string _diskDigest;
            private readonly Dictionary<string, string> _sources;
            private readonly DispatcherTimer _timer;
            private readonly DateTime _deadline = DateTime.UtcNow.AddMinutes(55);
            private Window? _window;
            private DataGrid? _grid;
            private object? _lastRows;
            private string _stage = "open";
            private string? _clicked;
            private bool _done;

            public QuantityObserver(Context context, ProjectState project, string phase)
            {
                _context = context; _project = project; _phase = phase;
                _projectDigest = QuantityProjectDigest();
                _nativeDigest = NativeModelDigest(context.Document);
                _diskDigest = DiskDigest();
                _sources = project.Elements.ToDictionary(x => x.Id, x => x.SourceHandles.Single(), StringComparer.OrdinalIgnoreCase);
                _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher) { Interval = TimeSpan.FromMilliseconds(350) };
                _timer.Tick += Tick;
            }

            public void Start() { Trace("waiting_for_actual_product_bq"); _timer.Start(); }

            private void Tick(object? sender, EventArgs args)
            {
                if (_done) return;
                try
                {
                    RequireUiContextStable(_context);
                    if (DateTime.UtcNow >= _deadline) throw new ProbeException("quantity_timeout");
                    if (_window == null)
                    {
                        // CAD can host WPF without an Application singleton.
                        // Inspect only current in-process presentation roots.
                        var windows = PresentationSource.CurrentSources.Cast<PresentationSource>()
                            .Select(x => x.RootVisual).OfType<Window>()
                            .Concat(System.Windows.Application.Current?.Windows.Cast<Window>() ?? Enumerable.Empty<Window>())
                            .Distinct().Where(x => x.GetType().Assembly == _context.Product && x.GetType().Name == "QuantitySummaryWindow" && x.IsVisible).ToArray();
                        if (windows.Length == 0) return;
                        if (windows.Length != 1) throw new ProbeException("quantity_window_ambiguous");
                        _window = windows[0];
                        if (!ReferenceEquals(Field("_document"), _context.Document) || (string)Field("_projectId") != _project.ProjectId)
                            throw new ProbeException("quantity_window_binding");
                        _grid = _window.FindName("QuantityGrid") as DataGrid ?? throw new ProbeException("quantity_grid_missing");
                        _window.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnClick), true);
                        _window.Closed += OnClosed;
                        VerifyRows(false, false);
                        _stage = _phase == "quantityreopen" ? "close" : "recalculate";
                        Trace("await_" + _stage);
                        return;
                    }
                    if (_clicked == null) return;
                    var action = _clicked; _clicked = null;
                    if (action == "recalculate" && _stage == "recalculate") { VerifyRows(false, true); _stage = "detail"; }
                    else if (action == "detail" && _stage == "detail") { VerifyRows(true, true); _stage = "summary"; }
                    else if (action == "summary" && _stage == "summary") { VerifyRows(false, true); _stage = "locate"; }
                    else if (action == "locate" && _stage == "locate")
                    {
                        VerifyRows(false, false);
                        if (!(_grid!.SelectedItem is QuantityReportRow row) || row.Count != 2) throw new ProbeException("quantity_selected_row");
                        var selected = _context.Document.Editor.SelectImplied();
                        if (selected.Status != PromptStatus.OK || selected.Value == null ||
                            !selected.Value.GetObjectIds().Select(id => id.Handle.ToString()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                .SequenceEqual(_sources.Values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase))
                            throw new ProbeException("quantity_locate_native_selection");
                        _stage = "close";
                    }
                    else throw new ProbeException("quantity_action_out_of_order");
                    Trace("await_" + _stage);
                }
                catch (System.Exception error) { Fail(error); }
            }

            private void OnClick(object sender, RoutedEventArgs args)
            {
                if (_done) return;
                var button = args.OriginalSource as ButtonBase;
                if (button == null) return;
                var label = Convert.ToString(button.Content, CultureInfo.InvariantCulture);
                var action = button.Name == "DetailModeRadio" ? "detail" : button.Name == "SummaryModeRadio" ? "summary" :
                    label == "Tính lại" ? "recalculate" : label == "Định vị" ? "locate" : null;
                if (action == null) return;
                if (_clicked != null) { Fail(new ProbeException("quantity_overlapping_clicks")); return; }
                _clicked = action;
                Trace("observed_product_click_" + action);
            }

            private void VerifyRows(bool detail, bool requireFresh)
            {
                var rowsObject = Field("_rows");
                if (requireFresh && ReferenceEquals(rowsObject, _lastRows)) throw new ProbeException("quantity_recalculation_not_fresh");
                if ((bool)Field("_detailMode") != detail) throw new ProbeException("quantity_mode_not_changed");
                var rows = ((IEnumerable<QuantityReportRow>)rowsObject).ToArray();
                if (_grid == null || !_grid.IsVisible || _grid.Items.Count != rows.Length ||
                    !_grid.Items.Cast<object>().SequenceEqual(rows.Cast<object>())) throw new ProbeException("quantity_displayed_rows_differ");
                Trace("observed_rows=" + rows.Length + " count=" + rows.Sum(x => x.Count) +
                    " gross_m3=" + rows.Sum(x => x.GrossConcreteM3).ToString("R", CultureInfo.InvariantCulture) +
                    " net_m3=" + rows.Sum(x => x.NetConcreteM3).ToString("R", CultureInfo.InvariantCulture));
                try
                {
                    Local022QuantityOracle.Verify(rows.Select(x => new Local022QuantityOracle.Row {
                        FamilyId=x.FamilyId, Category=x.Category, Fingerprint=x.DrawingFingerprint,
                        ElementIds=x.ElementIds.ToArray(), SourceHandles=x.SourceHandles.ToArray(), Count=x.Count,
                        Gross=x.GrossConcreteM3, Net=x.NetConcreteM3, Deduction=x.DeductionM3,
                        GrossEvidence=x.HasGrossConcreteM3Evidence, NetEvidence=x.HasNetConcreteM3Evidence, DeductionEvidence=x.HasDeductionM3Evidence
                    }).ToArray(), detail, ExpectedFamilyId(_context.RunId), _project.DrawingFingerprint, _sources);
                }
                catch (InvalidOperationException error) { throw new ProbeException(error.Message); }
                var totals = _window!.FindName("TotalsText") as TextBlock ?? throw new ProbeException("quantity_totals_missing");
                var expectedPrefix = "TỔNG: " + 2.ToString("N0", CultureInfo.CurrentCulture) + " cấu kiện  •  Bê tông " +
                    Local022QuantityOracle.TotalVolume.ToString("N3", CultureInfo.CurrentCulture) + " m³";
                if (!totals.IsVisible || !totals.Text.StartsWith(expectedPrefix, StringComparison.Ordinal)) throw new ProbeException("quantity_displayed_total");
                VerifyReadonly();
                _lastRows = rowsObject;
            }

            private void VerifyReadonly()
            {
                if (_projectDigest != QuantityProjectDigest() || _nativeDigest != NativeModelDigest(_context.Document) || _diskDigest != DiskDigest())
                    throw new ProbeException("quantity_readonly_mutation");
                VerifyExpectedPersistedState(_context.Document, _project, _context.RunId, "quantity_after");
            }

            private string DiskDigest() => HashLines(new[] { HashFile(_context.Drawing), HashFile(ProjectPath(_context.Document)) });
            private string QuantityProjectDigest() => HashLines(new[] {
                ProjectMutationDigest(_project), _project.DrawingFingerprint, _project.UpdatedUtc.ToString("O")
            }.Concat(_project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x =>
                x.Id + "|" + x.Dirty + "|" + x.UpdatedUtc.ToString("O") + "|" +
                string.Join(";", x.Quantities.OrderBy(q => q.Key, StringComparer.Ordinal).Select(q => q.Key + "=" + q.Value.ToString("R", CultureInfo.InvariantCulture))))));
            private static string HashFile(string path)
            {
                using (var hash = System.Security.Cryptography.SHA256.Create())
                using (var stream = File.OpenRead(path))
                    return string.Concat(hash.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
            }
            private object Field(string name) => _window!.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_window)
                ?? throw new ProbeException("quantity_window_contract_missing");
            private void Trace(string value) => File.AppendAllText(RequireUiChildPath(_context, _phase + ".private.txt"), DateTime.UtcNow.ToString("O") + " " + value + "\n");

            private void OnClosed(object? sender, EventArgs args)
            {
                if (_done) return;
                try
                {
                    RequireUiContextStable(_context);
                    if (_stage != "close" || _clicked != null) throw new ProbeException("quantity_closed_before_completion");
                    VerifyReadonly();
                    var checks = Checks("actual_product_bq_window", true, "exact_displayed_rows_and_totals", true,
                        "analytic_footing_volume", true, "exact_element_source_identity", true, "reporting_readonly", true, "observed_window_closed", true);
                    if (_phase == "quantity")
                    {
                        checks.Add("recalculate_click_fresh_rows", true); checks.Add("detail_summary_clicks", true); checks.Add("locate_click_native_selection", true);
                    }
                    else checks.Add("cold_bq_same_quantities", true);
                    WriteUiMarker(_context, _phase, "PASS", _phase, "NONE", checks);
                    _done = true; _timer.Stop(); Trace("complete");
                    QueueOwnedQuit(_context, false);
                }
                catch (System.Exception error) { Fail(error); }
            }

            private void Fail(System.Exception error)
            {
                if (_done) return;
                _done = true; _timer.Stop();
                WriteUiFailure(_context, _phase, "quantity_" + _stage, error);
                QueueOwnedQuit(_context, true);
            }
        }
    }
}
