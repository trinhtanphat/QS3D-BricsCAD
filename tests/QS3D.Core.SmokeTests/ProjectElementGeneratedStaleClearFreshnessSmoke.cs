using System;
using System.Runtime.CompilerServices;
using System.Threading;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementGeneratedStaleClearFreshnessSmoke
    {
        internal static void Run()
        {
            PerKindClearTracksRealMutationAndPreservesOtherStaleKinds();
            FinalPerKindClearRemovesAggregateAndTracksMutation();
            RepeatedPerKindClearIsTimestampNoOp();
            ClearAllTracksRealMutationAndPreservesGeneratedHandles();
            EmptyClearAllIsTimestampNoOp();
        }

        private static void PerKindClearTracksRealMutationAndPreservesOtherStaleKinds()
        {
            var element = StaleSolidAndRebar();
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.ClearGeneratedSolidStale();

            Require(element.UpdatedUtc > before, "real per-kind stale clear did not advance UpdatedUtc");
            Equal(expectedDirty, element.Dirty, "per-kind stale clear changed Dirty");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStateKey), "solid state marker remained after clear");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStaleSnapshotKey), "solid stale snapshot remained after clear");
            True(element.IsGeneratedRebarStale(), "unrelated rebar stale state was cleared");
            True(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey), "aggregate stale state was cleared while another stale kind remained");
            Equal("AA", element.Properties["GeneratedSolidHandle"], "generated solid handle was removed or changed");
            Equal("BB", element.Properties["GeneratedRebarHandles"], "generated rebar handles were removed or changed");
        }

        private static void FinalPerKindClearRemovesAggregateAndTracksMutation()
        {
            var element = StaleSolidOnly();
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.ClearGeneratedSolidStale();

            Require(element.UpdatedUtc > before, "final per-kind stale clear did not advance UpdatedUtc");
            Equal(expectedDirty, element.Dirty, "final per-kind stale clear changed Dirty");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStateKey), "solid state marker remained");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStaleSnapshotKey), "solid stale snapshot remained");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey), "aggregate stale state remained after final clear");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStaleReasonKey), "aggregate stale reason remained after final clear");
            Equal("AA", element.Properties["GeneratedSolidHandle"], "generated solid handle was removed by final clear");
        }

        private static void RepeatedPerKindClearIsTimestampNoOp()
        {
            var element = StaleSolidOnly();
            element.ClearGeneratedSolidStale();
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.ClearGeneratedSolidStale();

            Equal(before, element.UpdatedUtc, "empty repeated per-kind clear changed UpdatedUtc");
            Equal(expectedDirty, element.Dirty, "empty repeated per-kind clear changed Dirty");
            Equal("AA", element.Properties["GeneratedSolidHandle"], "empty repeated clear changed generated handle");
        }

        private static void ClearAllTracksRealMutationAndPreservesGeneratedHandles()
        {
            var element = StaleSolidAndRebar();
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.ClearGeneratedGeometryStale();

            Require(element.UpdatedUtc > before, "real clear-all did not advance UpdatedUtc");
            Equal(expectedDirty, element.Dirty, "clear-all changed Dirty");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey), "clear-all left aggregate stale state");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStaleReasonKey), "clear-all left aggregate stale reason");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStateKey), "clear-all left solid stale state");
            False(element.Properties.ContainsKey(ProjectElement.GeneratedRebarStateKey), "clear-all left rebar stale state");
            Equal("AA", element.Properties["GeneratedSolidHandle"], "clear-all removed generated solid handle");
            Equal("BB", element.Properties["GeneratedRebarHandles"], "clear-all removed generated rebar handles");
        }

        private static void EmptyClearAllIsTimestampNoOp()
        {
            var element = new ProjectElement("E-STALE-CLEAR-EMPTY", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA";
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.ClearGeneratedGeometryStale();

            Equal(before, element.UpdatedUtc, "empty clear-all changed UpdatedUtc");
            Equal(expectedDirty, element.Dirty, "empty clear-all changed Dirty");
            Equal("AA", element.Properties["GeneratedSolidHandle"], "empty clear-all changed generated handle");
        }

        private static ProjectElement StaleSolidOnly()
        {
            var element = new ProjectElement("E-STALE-CLEAR-SOLID", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.MarkGeneratedGeometryStale("source changed");
            True(element.IsGeneratedSolidStale(), "solid stale seed failed");
            return element;
        }

        private static ProjectElement StaleSolidAndRebar()
        {
            var element = new ProjectElement("E-STALE-CLEAR-MULTI", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB";
            element.MarkGeneratedGeometryStale("source changed");
            True(element.IsGeneratedSolidStale(), "solid stale seed failed");
            True(element.IsGeneratedRebarStale(), "rebar stale seed failed");
            return element;
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("ProjectElementGeneratedStaleClearFreshnessSmoke: " + message + ".");
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new Exception("ProjectElementGeneratedStaleClearFreshnessSmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectElementGeneratedStaleClearFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class ProjectElementGeneratedStaleClearFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementGeneratedStaleClearFreshnessSmoke.Run();
    }
}
