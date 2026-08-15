using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Cost;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqProjectWorkspaceSmoke
    {
        internal static void Run()
        {
            ProjectBoundMutationAndAnalysis();
            SnapshotRollback();
            QsdbRoundTrip();
            ReservedMetadataFailsClosed();
        }

        private static void ProjectBoundMutationAndAnalysis()
        {
            var project = new ProjectState("TBQ-SMOKE", "TBQ smoke");
            var workspace = ProjectTbqWorkspace.Open(project);
            Equal(false, workspace.HasValue, "new TBQ workspace should be empty");
            Equal(0L, project.ChangeVersion, "new project change version");

            var state = CreateState(10m, 5m);
            workspace.Replace(state);
            Equal(1L, project.ChangeVersion, "TBQ replace change version");
            workspace.Replace(state);
            Equal(1L, project.ChangeVersion, "TBQ deterministic no-op replace");

            var current = workspace.Current ?? throw new InvalidOperationException("TBQ workspace did not persist its current state.");
            Equal("VND", current.Currency, "TBQ currency");
            Equal(2, current.BillItems.Count, "TBQ bill item count");
            Equal(400m, current.BaseTotal, "TBQ base total");
            Equal(462m, current.PreviewAdjustment().AdjustedTotal, "TBQ adjusted total");

            var trades = current.AnalyzeTrades();
            Equal(1, trades.Count, "TBQ trade row count");
            Equal("Structure", trades[0].TradeCode, "TBQ trade code");
            Equal(462m, trades[0].TotalCost, "TBQ adjusted trade total");
            Equal(0.462m, trades[0].CostPerCfaM2 ?? -1m, "TBQ cost per CFA");

            var buildUps = current.AnalyzeBuildUps();
            Equal(2, buildUps.Count, "TBQ adopted build-up count");
            Equal("A", current.RateReferences.GetReverseReferences("R-CONC", RateReferenceTargetKind.BillItem)[0], "TBQ reverse reference");
        }

        private static void SnapshotRollback()
        {
            var project = new ProjectState("TBQ-SNAPSHOT", "TBQ snapshot");
            var workspace = ProjectTbqWorkspace.Open(project);
            workspace.Replace(CreateState(10m, 5m));
            var snapshot = ProjectStateSnapshot.Capture(project);
            var capturedVersion = project.ChangeVersion;

            workspace.ApplyAdjustment(20m, 0m);
            Equal(capturedVersion + 1L, project.ChangeVersion, "TBQ adjustment mutation version");
            Equal(480m, (workspace.Current ?? throw new InvalidOperationException()).PreviewAdjustment().AdjustedTotal, "TBQ changed total");

            snapshot.Restore(project);
            Equal(capturedVersion, project.ChangeVersion, "TBQ snapshot restore version");
            var restored = ProjectTbqWorkspace.Open(project).Current ?? throw new InvalidOperationException("TBQ workspace disappeared during snapshot restore.");
            Equal(10m, restored.AdjustmentRatioPercent, "TBQ restored adjustment ratio");
            Equal(5m, restored.MarkupRatioPercent, "TBQ restored markup ratio");
            Equal(462m, restored.PreviewAdjustment().AdjustedTotal, "TBQ restored adjusted total");
        }

        private static void QsdbRoundTrip()
        {
            var project = new ProjectState("TBQ-QSDB", "TBQ QSDB");
            ProjectTbqWorkspace.Open(project).Replace(CreateState(7.5m, 2m));
            var path = Path.Combine(Path.GetTempPath(), "qs3d-tbq-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var current = ProjectTbqWorkspace.Open(loaded).Current ?? throw new InvalidOperationException("TBQ workspace did not survive QSDB roundtrip.");
                Equal("VND", current.Currency, "TBQ QSDB currency");
                Equal(7.5m, current.AdjustmentRatioPercent, "TBQ QSDB adjustment");
                Equal(2m, current.MarkupRatioPercent, "TBQ QSDB markup");
                Equal(2, current.BillItems.Count, "TBQ QSDB bill item count");
                Equal(2, current.BuildUpRates.Count, "TBQ QSDB build-up count");
                Equal(3, current.RateReferences.Edges.Count, "TBQ QSDB reference count");
                Equal(1, current.Library.Entries.Count, "TBQ QSDB library count");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void ReservedMetadataFailsClosed()
        {
            var project = new ProjectState("TBQ-INVALID", "TBQ invalid");
            Throws<FormatException>(() => project.Metadata["QS3D.TBQ.v2.Workspace"] = "1:1", "unsupported TBQ reserved version");
            Equal(0L, project.ChangeVersion, "unsupported TBQ metadata must not touch project");
            Equal(false, project.Metadata.ContainsKey("QS3D.TBQ.v2.Workspace"), "unsupported TBQ metadata must not be stored");

            Throws<FormatException>(() => project.Metadata["QS3D.TBQ.v1.Workspace"] = "1:2", "malformed TBQ workspace payload");
            Equal(0L, project.ChangeVersion, "malformed TBQ metadata must not touch project");
            Equal(false, project.Metadata.ContainsKey("QS3D.TBQ.v1.Workspace"), "malformed TBQ metadata must not be stored");
        }

        private static TbqProjectWorkspaceState CreateState(decimal adjustment, decimal markup)
        {
            return new TbqProjectWorkspaceState(
                "VND",
                1000m,
                new[]
                {
                    new TbqBillItem("A", "Concrete", "m3", "Structure", 2m, 100m, "R-CONC"),
                    new TbqBillItem("B", "Rebar", "kg", "Structure", 10m, 20m, "R-REB")
                },
                new[]
                {
                    new BuildUpRateSnapshot("R-CONC", 100m),
                    new BuildUpRateSnapshot("R-REB", 20m)
                },
                new[]
                {
                    new RateReferenceEdge("R-CONC", RateReferenceTargetKind.BillItem, "A"),
                    new RateReferenceEdge("R-CONC", RateReferenceTargetKind.UnitRate, "R-REB"),
                    new RateReferenceEdge("R-REB", RateReferenceTargetKind.BillItem, "B")
                },
                "PROJECT",
                new[]
                {
                    new BqLibraryEntry("A", "Concrete", "m3", "Structure/Concrete", 100m)
                },
                adjustment,
                markup);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
