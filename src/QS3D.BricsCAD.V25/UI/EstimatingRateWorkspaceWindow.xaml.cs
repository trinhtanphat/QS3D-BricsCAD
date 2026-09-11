using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using Bricscad.ApplicationServices;
using QS3D.Core.Cost;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class EstimatingRateWorkspaceWindow : Window
    {
        private readonly Document _document;
        private readonly IntPtr _nativeDatabaseIdentity;

        public EstimatingRateWorkspaceWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _nativeDatabaseIdentity = NativeDatabaseIdentity(document);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, document);
            Title = "QS3D — Estimating & Rate Build-up — " + document.Name;
            Loaded += (_, __) => RefreshWorkspace();
        }

        public void RefreshWorkspace()
        {
            try
            {
                var context = RequireContext("Estimating workspace refresh");
                var state = context.State;
                var adjusted = state.PreviewAdjustment();
                ProjectSummaryText.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    "Project {0} | {1} | CFA {2:N2} m² | Base {3:N2} | Adjusted {4:N2} | {5} bill item(s) | {6} build-up(s)",
                    context.Project.ProjectId,
                    state.Currency,
                    state.CfaM2,
                    state.BaseTotal,
                    adjusted.AdjustedTotal,
                    state.BillItems.Count,
                    state.BuildUpRates.Count);

                AdjustmentBox.Text = state.AdjustmentRatioPercent.ToString(CultureInfo.InvariantCulture);
                MarkupBox.Text = state.MarkupRatioPercent.ToString(CultureInfo.InvariantCulture);
                BillItemsGrid.ItemsSource = state.BillItems;
                RateReferencesGrid.ItemsSource = state.RateReferences.Edges;
                LibraryGrid.ItemsSource = state.Library.Entries;
                TradesGrid.ItemsSource = state.AnalyzeTrades();
                BuildUpsGrid.ItemsSource = state.AnalyzeBuildUps(false)
                    .Select(x => new BuildUpRow(
                        x.Rate.RateCode,
                        x.Rate.UnitRate,
                        x.Mark.IsUnused ? "UNUSED" : "ADOPTED",
                        Join(x.BillItems),
                        Join(x.UnitRates)))
                    .ToArray();
                PreviewSummaryText.Text = "Persisted adjustment " + Format(state.AdjustmentRatioPercent) +
                    "% + markup " + Format(state.MarkupRatioPercent) + "% → " +
                    Format(adjusted.AdjustedTotal) + " " + state.Currency;
                SetStatus("Estimating workspace refreshed from canonical ProjectTbqWorkspace state.");
            }
            catch (Exception ex)
            {
                ClearGrids();
                SetStatus("Estimating workspace unavailable: " + ex.Message);
            }
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            RefreshWorkspace();
        }

        private void OnPreviewAdjustment(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = RequireContext("Estimating adjustment preview");
                var adjustment = ParseDecimal(AdjustmentBox.Text, "adjustment ratio");
                var markup = ParseDecimal(MarkupBox.Text, "markup ratio");
                var preview = context.Workspace.PreviewAdjustment(adjustment, markup);
                context.EnsureFresh("Estimating adjustment preview");
                PreviewSummaryText.Text = "Preview: base " + Format(preview.BaseTotal) +
                    " → adjusted " + Format(preview.AdjustedTotal) + " " + context.State.Currency +
                    " (adjustment " + Format(adjustment) + "%, markup " + Format(markup) + "%).";
                SetStatus("Preview calculated by Core CostAdjustmentService; project data was not changed.");
            }
            catch (Exception ex)
            {
                SetStatus("Adjustment preview rejected: " + ex.Message);
            }
        }
        private void OnApplyAdjustment(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = RequireContext("Estimating adjustment apply");
                var adjustment = ParseDecimal(AdjustmentBox.Text, "adjustment ratio");
                var markup = ParseDecimal(MarkupBox.Text, "markup ratio");
                context.EnsureFresh("Estimating adjustment apply");
                var preview = context.Workspace.PreviewAdjustment(adjustment, markup);

                if (context.State.AdjustmentRatioPercent == adjustment &&
                    context.State.MarkupRatioPercent == markup)
                {
                    SetStatus("Requested adjustment already matches persisted workspace; no save required.");
                    RefreshWorkspace();
                    return;
                }

                var rollback = ProjectStateSnapshot.Capture(context.Project);
                string savedPath;
                try
                {
                    context.Workspace.ApplyAdjustment(adjustment, markup);
                    savedPath = ProjectContextCoordinator.Save(_document);
                }
                catch (Exception saveFailure)
                {
                    RestoreAfterFailedSave(context.Project, rollback, saveFailure);
                    throw;
                }
                PreviewSummaryText.Text = "Saved adjusted total " + Format(preview.AdjustedTotal) +
                    " " + context.State.Currency + ".";
                SetStatus("Adjustment persisted through ProjectTbqWorkspace: " + savedPath);
                RefreshWorkspace();
            }
            catch (Exception ex)
            {
                SetStatus("Adjustment apply failed: " + ex.Message);
            }
        }

        private WorkspaceContext RequireContext(string operation)
        {
            EnsureDocumentAffinity(operation);
            var project = ExistingProjectMutationContext.Require(_document, operation);
            ProjectContextCoordinator.RequireBackingStoreUnchanged(_document, project, operation);
            var workspace = ProjectTbqWorkspace.Open(project);
            var state = workspace.Current ?? throw new InvalidOperationException(
                operation + " requires a TBQ workspace already bound to the existing QS3D project.");
            return new WorkspaceContext(_document, project, workspace, state);
        }

        private void EnsureDocumentAffinity(string operation)
        {
            var active = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (!ReferenceEquals(active, _document))
                throw new InvalidOperationException(operation + " requires the drawing that opened this workspace to be active.");
            if (_document.Database == null ||
                _document.Database.UnmanagedObject == IntPtr.Zero ||
                _document.Database.UnmanagedObject != _nativeDatabaseIdentity)
                throw new InvalidOperationException(operation + " lost its exact native database identity.");
        }
        private void RestoreAfterFailedSave(
            ProjectState project,
            ProjectStateSnapshot rollback,
            Exception saveFailure)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception rollbackFailure)
            {
                ProjectContextCoordinator.Forget(_document);
                throw new InvalidOperationException(
                    "Estimating adjustment failed to save and rollback could not restore the in-memory project.",
                    new AggregateException(saveFailure, rollbackFailure));
            }

            ProjectContextCoordinator.Forget(_document);
        }

        private static decimal ParseDecimal(string text, string label)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
                return invariant;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var current))
                return current;
            throw new FormatException(label + " must be a valid decimal value.");
        }

        private static string Format(decimal value)
        {
            return value.ToString("0.############################", CultureInfo.InvariantCulture);
        }
        private static string Join(System.Collections.Generic.IReadOnlyList<string> values)
        {
            return values == null || values.Count == 0 ? "—" : string.Join(", ", values);
        }

        private void ClearGrids()
        {
            BillItemsGrid.ItemsSource = null;
            BuildUpsGrid.ItemsSource = null;
            RateReferencesGrid.ItemsSource = null;
            LibraryGrid.ItemsSource = null;
            TradesGrid.ItemsSource = null;
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }

        private static IntPtr NativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null || database.UnmanagedObject == IntPtr.Zero)
                throw new InvalidOperationException("Estimating workspace requires a live BricsCAD database.");
            return database.UnmanagedObject;
        }

        private sealed class BuildUpRow
        {
            internal BuildUpRow(string rateCode, decimal unitRate, string usage, string billItems, string unitRates)
            {
                RateCode = rateCode;
                UnitRate = unitRate;
                Usage = usage;
                BillItems = billItems;
                UnitRates = unitRates;
            }

            public string RateCode { get; }
            public decimal UnitRate { get; }
            public string Usage { get; }
            public string BillItems { get; }
            public string UnitRates { get; }
        }

        private sealed class WorkspaceContext
        {
            internal WorkspaceContext(
                Document document,
                ProjectState project,
                ProjectTbqWorkspace workspace,
                TbqProjectWorkspaceState state)
            {
                Document = document;
                Project = project;
                Workspace = workspace;
                State = state;
            }

            internal Document Document { get; }
            internal ProjectState Project { get; }
            internal ProjectTbqWorkspace Workspace { get; }
            internal TbqProjectWorkspaceState State { get; }

            internal void EnsureFresh(string operation)
            {
                ProjectContextCoordinator.RequireBackingStoreUnchanged(Document, Project, operation);
            }
        }
    }
}
