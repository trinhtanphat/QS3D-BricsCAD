using System;
using System.Reflection;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotConsistencySmoke
    {
        internal static void Run()
        {
            DetachedCaptureIsolatesNestedMutation();
            CompareAndReportPreserveStableResults();
            RootElementBoundFailsClosed();
            NestedCollectionBoundFailsClosed();
        }

        private static void DetachedCaptureIsolatesNestedMutation()
        {
            var source = Snapshot("source", 10d);
            source.Elements[0].Properties["Mark"] = "B1";
            source.Elements[0].SourceHandles.Add("AA");
            source.Elements[0].Dependencies.Add("HOST-1");

            var detacher = typeof(RevisionService).Assembly.GetType("QS3D.Core.Revisions.RevisionSnapshotDetacher", true)
                ?? throw new Exception("RevisionSnapshotDetacher type was not found.");
            var capture = detacher.GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new Exception("RevisionSnapshotDetacher.Capture was not found.");
            var detached = (RevisionSnapshot)(capture.Invoke(null, new object[] { source, "smoke" })
                ?? throw new Exception("Detached revision snapshot was null."));

            source.ProjectId = "OTHER";
            source.Elements[0].ElementId = "MUTATED";
            source.Elements[0].Properties["Mark"] = "B2";
            source.Elements[0].Quantities["Q"] = 99d;
            source.Elements[0].SourceHandles[0] = "BB";
            source.Elements[0].Dependencies[0] = "HOST-2";

            var item = detached.Elements[0];
            if (!string.Equals(detached.ProjectId, "P1", StringComparison.Ordinal) ||
                !string.Equals(item.ElementId, "E1", StringComparison.Ordinal) ||
                !string.Equals(item.Properties["Mark"], "B1", StringComparison.Ordinal) ||
                item.Quantities["Q"] != 10d ||
                !string.Equals(item.SourceHandles[0], "AA", StringComparison.Ordinal) ||
                !string.Equals(item.Dependencies[0], "HOST-1", StringComparison.Ordinal))
                throw new Exception("Detached revision capture changed after caller-side mutation.");
        }

        private static void CompareAndReportPreserveStableResults()
        {
            var before = Snapshot("before", 10d);
            var after = Snapshot("after", 12d);
            after.Elements[0].Properties["Mark"] = "B2";

            var deltas = new RevisionService().Compare(before, after);
            if (deltas.Count != 1 || deltas[0].Fields.Count != 2)
                throw new Exception("Stable revision comparison changed unexpectedly after detachment.");

            var rows = new QuantityRevisionReport().Build(before, after);
            if (rows.Count != 1 ||
                !string.Equals(rows[0].ElementId, "E1", StringComparison.Ordinal) ||
                !string.Equals(rows[0].QuantityName, "Q", StringComparison.Ordinal) ||
                rows[0].Before != 10d || rows[0].After != 12d)
                throw new Exception("Stable quantity revision report changed unexpectedly after detachment.");
        }

        private static void RootElementBoundFailsClosed()
        {
            var before = new RevisionSnapshot { Id = "before", CreatedUtc = DateTime.UtcNow, ProjectId = "P1" };
            for (var index = 0; index <= 100000; index++)
                before.Elements.Add(new RevisionElementSnapshot { ElementId = "E" + index, Category = "Beam" });
            var after = new RevisionSnapshot { Id = "after", CreatedUtc = DateTime.UtcNow, ProjectId = "P1" };

            Throws<InvalidOperationException>(() => new RevisionService().Compare(before, after));
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(before, after));
        }

        private static void NestedCollectionBoundFailsClosed()
        {
            var before = Snapshot("before", 1d);
            for (var index = 0; index <= 100000; index++)
                before.Elements[0].SourceHandles.Add("H" + index);
            var after = Snapshot("after", 1d);

            Throws<InvalidOperationException>(() => new RevisionService().Compare(before, after));
        }

        private static RevisionSnapshot Snapshot(string id, double quantity)
        {
            var snapshot = new RevisionSnapshot { Id = id, CreatedUtc = DateTime.UtcNow, ProjectId = "P1" };
            var element = new RevisionElementSnapshot { ElementId = "E1", Category = "Beam" };
            element.Properties["Mark"] = "B1";
            element.Quantities["Q"] = quantity;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is T)
            {
                return;
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
