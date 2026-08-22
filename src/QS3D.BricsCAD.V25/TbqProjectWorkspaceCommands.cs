using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Cost;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class TbqProjectWorkspaceCommands
    {
        private const int MaxRows = 200;

        [CommandMethod("QS3DTBQSTATUS", CommandFlags.Modal)]
        public void ShowStatus()
        {
            Execute("QS3DTBQSTATUS", (document, context) =>
            {
                var state = context.State;
                var adjusted = state.PreviewAdjustment();
                context.EnsureFresh("TBQ Status");
                WriteLine(document, "TBQ project=" + context.Project.ProjectId +
                    ", currency=" + state.Currency +
                    ", CFA(m2)=" + Format(state.CfaM2) +
                    ", items=" + state.BillItems.Count +
                    ", build-ups=" + state.BuildUpRates.Count +
                    ", references=" + state.RateReferences.Edges.Count +
                    ", library=" + state.LibraryId + " (" + state.Library.Entries.Count + ")" +
                    ", base=" + Format(adjusted.BaseTotal) +
                    ", adjusted=" + Format(adjusted.AdjustedTotal) + ".");
            });
        }

        [CommandMethod("QS3DTBQRATEREFERENCE", CommandFlags.Modal)]
        public void ShowRateReferences()
        {
            Execute("QS3DTBQRATEREFERENCE", (document, context) =>
            {
                var edges = context.State.RateReferences.Edges;
                var lines = new List<string>(Math.Min(edges.Count, MaxRows));
                for (var i = 0; i < edges.Count && i < MaxRows; i++)
                {
                    var edge = edges[i];
                    lines.Add(edge.SourceRateCode + " -> " + edge.TargetKind + ":" + edge.TargetId);
                }
                context.EnsureFresh("TBQ Rate Reference");
                WriteLine(document, "TBQ Rate Reference: " + edges.Count + " edge(s)." + TruncationSuffix(edges.Count));
                for (var i = 0; i < lines.Count; i++) WriteLine(document, "  " + lines[i]);
            });
        }

        [CommandMethod("QS3DTBQBUILDUPANALYSIS", CommandFlags.Modal)]
        public void ShowBuildUpAnalysis()
        {
            Execute("QS3DTBQBUILDUPANALYSIS", (document, context) =>
            {
                var rows = context.State.AnalyzeBuildUps(false);
                var lines = new List<string>(Math.Min(rows.Count, MaxRows));
                for (var i = 0; i < rows.Count && i < MaxRows; i++)
                {
                    var row = rows[i];
                    lines.Add(row.Rate.RateCode +
                        " | rate=" + Format(row.Rate.UnitRate) +
                        " | " + (row.Mark.IsUnused ? "UNUSED" : "ADOPTED") +
                        " | bill-items=" + Join(row.BillItems) +
                        " | unit-rates=" + Join(row.UnitRates));
                }
                context.EnsureFresh("TBQ Build-up Analysis");
                WriteLine(document, "TBQ Build-up Analysis: " + rows.Count + " rate(s)." + TruncationSuffix(rows.Count));
                for (var i = 0; i < lines.Count; i++) WriteLine(document, "  " + lines[i]);
            });
        }

        [CommandMethod("QS3DTBQTRADECFA", CommandFlags.Modal)]
        public void ShowTradeCfaAnalysis()
        {
            Execute("QS3DTBQTRADECFA", (document, context) =>
            {
                var rows = context.State.AnalyzeTrades();
                var lines = new List<string>(Math.Min(rows.Count, MaxRows));
                for (var i = 0; i < rows.Count && i < MaxRows; i++)
                {
                    var row = rows[i];
                    lines.Add(row.TradeCode +
                        " | items=" + row.ItemCount +
                        " | total=" + Format(row.TotalCost) +
                        " | cost/CFA=" + (row.CostPerCfaM2.HasValue ? Format(row.CostPerCfaM2.Value) : "N/A"));
                }
                context.EnsureFresh("TBQ Trade/CFA");
                WriteLine(document, "TBQ Trade/CFA: CFA(m2)=" + Format(context.State.CfaM2) +
                    ", " + rows.Count + " trade(s)." + TruncationSuffix(rows.Count));
                for (var i = 0; i < lines.Count; i++) WriteLine(document, "  " + lines[i]);
            });
        }

        [CommandMethod("QS3DTBQBQLIBRARY", CommandFlags.Modal)]
        public void ShowBqLibrary()
        {
            Execute("QS3DTBQBQLIBRARY", (document, context) =>
            {
                var entries = context.State.Library.Entries;
                var lines = new List<string>(Math.Min(entries.Count, MaxRows));
                for (var i = 0; i < entries.Count && i < MaxRows; i++)
                {
                    var entry = entries[i];
                    lines.Add(entry.CategoryPath + " | " + entry.ItemCode +
                        " | " + entry.Description +
                        " | unit=" + entry.Unit +
                        " | reference-rate=" + (entry.ReferenceUnitRate.HasValue ? Format(entry.ReferenceUnitRate.Value) : "N/A"));
                }
                context.EnsureFresh("TBQ BQ Library");
                WriteLine(document, "TBQ BQ Library: " + context.State.LibraryId +
                    ", " + entries.Count + " item(s)." + TruncationSuffix(entries.Count));
                for (var i = 0; i < lines.Count; i++) WriteLine(document, "  " + lines[i]);
            });
        }

        [CommandMethod("QS3DTBQADJUSTPREVIEW", CommandFlags.Modal)]
        public void PreviewCostAdjustment()
        {
            Execute("QS3DTBQADJUSTPREVIEW", (document, context) =>
            {
                if (!TryPromptRatios(document, out var adjustment, out var markup)) return;
                context.EnsureFresh("TBQ Adjust Cost Preview");
                var preview = context.Workspace.PreviewAdjustment(adjustment, markup);
                context.EnsureFresh("TBQ Adjust Cost Preview");
                WriteLine(document, "TBQ Adjust Preview: base=" + Format(preview.BaseTotal) +
                    ", adjustment=" + Format(preview.AdjustmentRatioPercent) + "%" +
                    ", markup=" + Format(preview.MarkupRatioPercent) + "%" +
                    ", combined=" + Format(preview.CombinedRatioPercent) + "%" +
                    ", adjusted=" + Format(preview.AdjustedTotal) + " " + context.State.Currency +
                    ". No project data was changed.");
            });
        }

        [CommandMethod("QS3DTBQADJUSTAPPLY", CommandFlags.Modal)]
        public void ApplyCostAdjustment()
        {
            Execute("QS3DTBQADJUSTAPPLY", (document, context) =>
            {
                if (!TryPromptRatios(document, out var adjustment, out var markup)) return;
                context.EnsureFresh("TBQ Adjust Cost Apply");

                if (context.State.AdjustmentRatioPercent == adjustment && context.State.MarkupRatioPercent == markup)
                {
                    WriteLine(document, "TBQ Adjust Apply: requested ratios already match the persisted workspace; no save was required.");
                    return;
                }

                var preview = context.Workspace.PreviewAdjustment(adjustment, markup);
                var snapshot = ProjectStateSnapshot.Capture(context.Project);
                string path;
                try
                {
                    context.Workspace.ApplyAdjustment(adjustment, markup);
                    path = ProjectContextCoordinator.Save(document);
                }
                catch (Exception saveFailure)
                {
                    try
                    {
                        snapshot.Restore(context.Project);
                    }
                    catch (Exception rollbackFailure)
                    {
                        ProjectContextCoordinator.Forget(document);
                        throw new InvalidOperationException(
                            "TBQ Adjust Cost Apply failed to save and the in-memory snapshot could not be restored. " +
                            "The project cache was discarded; reload the project before any further mutation.",
                            new AggregateException(saveFailure, rollbackFailure));
                    }

                    ProjectContextCoordinator.Forget(document);
                    throw new InvalidOperationException(
                        "TBQ Adjust Cost Apply was not retained in memory because project save failed. " +
                        "The project cache was discarded so the next operation must rebind from the actual sidecar. " +
                        "Reload/review before retrying.",
                        saveFailure);
                }

                WriteLine(document, "TBQ Adjust Apply saved: base=" + Format(preview.BaseTotal) +
                    ", adjustment=" + Format(adjustment) + "%" +
                    ", markup=" + Format(markup) + "%" +
                    ", adjusted=" + Format(preview.AdjustedTotal) + " " + context.State.Currency +
                    ", path=" + path + ".");
            });
        }

        private static void Execute(string commandName, Action<Document, TbqContext> action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                action(document, RequireContext(document, commandName));
            }
            catch (Exception ex)
            {
                WriteFailure(document, commandName, ex);
            }
        }

        private static TbqContext RequireContext(Document document, string operation)
        {
            var project = ExistingProjectMutationContext.Require(document, operation);
            var workspace = ProjectTbqWorkspace.Open(project);
            var state = workspace.Current;
            if (state == null)
                throw new InvalidOperationException(
                    operation + " requires a TBQ workspace already bound to the existing QS3D project. " +
                    "This command does not create a project or invent TBQ data.");
            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, operation);
            return new TbqContext(document, project, workspace, state);
        }

        private static bool TryPromptRatios(Document document, out decimal adjustment, out decimal markup)
        {
            adjustment = 0m;
            markup = 0m;
            var editor = document.Editor;

            var adjustmentResult = editor.GetDouble(new PromptDoubleOptions("\nTBQ adjustment ratio % (>= -100): "));
            if (adjustmentResult.Status != PromptStatus.OK) return false;
            var markupResult = editor.GetDouble(new PromptDoubleOptions("\nTBQ markup ratio % (>= -100): "));
            if (markupResult.Status != PromptStatus.OK) return false;

            try
            {
                adjustment = checked((decimal)adjustmentResult.Value);
                markup = checked((decimal)markupResult.Value);
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException("TBQ adjustment ratios must fit decimal arithmetic.", ex);
            }

            if (adjustment < -100m) throw new ArgumentOutOfRangeException(nameof(adjustment), "Adjustment ratio must be -100% or greater.");
            if (markup < -100m) throw new ArgumentOutOfRangeException(nameof(markup), "Markup ratio must be -100% or greater.");
            return true;
        }

        private static string Format(decimal value)
        {
            return value.ToString("0.############################", CultureInfo.InvariantCulture);
        }

        private static string Join(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0) return "-";
            return string.Join(",", values);
        }

        private static string TruncationSuffix(int count)
        {
            return count > MaxRows ? " Showing first " + MaxRows + " deterministic row(s)." : string.Empty;
        }

        private static void WriteLine(Document document, string message)
        {
            document.Editor.WriteMessage("\n" + message);
        }

        private static void WriteFailure(Document document, string commandName, Exception error)
        {
            try { WriteLine(document, commandName + " error: " + Describe(error)); }
            catch { }
        }

        private static string Describe(Exception error)
        {
            var parts = new List<string>();
            for (var current = error; current != null; current = current.InnerException)
            {
                var message = (current.Message ?? string.Empty).Trim();
                var part = current.GetType().Name + (message.Length == 0 ? string.Empty : ": " + message);
                if (!parts.Contains(part)) parts.Add(part);
            }
            return parts.Count == 0 ? "Unknown error." : string.Join(" -> ", parts);
        }

        private sealed class TbqContext
        {
            internal TbqContext(Document document, ProjectState project, ProjectTbqWorkspace workspace, TbqProjectWorkspaceState state)
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
