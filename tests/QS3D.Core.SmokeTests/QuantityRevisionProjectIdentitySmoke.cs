using System;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionProjectIdentitySmoke
    {
        internal static void Run()
        {
            RejectsDifferentProjectsBeforeElementValidation();
            RejectsOneMissingProjectIdentity();
            RejectsNonCanonicalProjectIdentity();
            AllowsLegacyIdentitylessPair();
            PreservesSameProjectQuantityDiff();
        }

        private static void RejectsDifferentProjectsBeforeElementValidation()
        {
            var before = Snapshot("project-a", 1d);
            var after = Snapshot("project-b", 2d);
            after.Elements[0].Category = "not-a-category";

            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(before, after));
        }

        private static void RejectsOneMissingProjectIdentity()
        {
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(Snapshot(string.Empty, 1d), Snapshot("project-a", 2d)));
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(Snapshot("project-a", 1d), Snapshot(string.Empty, 2d)));
        }

        private static void RejectsNonCanonicalProjectIdentity()
        {
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(Snapshot(" project-a", 1d), Snapshot(" project-a", 2d)));
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(Snapshot("project-a", 1d), Snapshot("project-a ", 2d)));
        }

        private static void AllowsLegacyIdentitylessPair()
        {
            var rows = new QuantityRevisionReport().Build(Snapshot(string.Empty, 1d), Snapshot(string.Empty, 2d));
            Equal(1, rows.Count);
            Equal("Changed", rows[0].Change);
            Near(1d, rows[0].Before);
            Near(2d, rows[0].After);
        }

        private static void PreservesSameProjectQuantityDiff()
        {
            var rows = new QuantityRevisionReport().Build(Snapshot("project-a", 1d), Snapshot("project-a", 2.5d));
            Equal(1, rows.Count);
            Equal("E1", rows[0].ElementId);
            Equal("LengthM", rows[0].QuantityName);
            Equal("Changed", rows[0].Change);
            Near(1d, rows[0].Before);
            Near(2.5d, rows[0].After);
            Near(1.5d, rows[0].Delta);
        }

        private static RevisionSnapshot Snapshot(string projectId, double value)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = "revision",
                CreatedUtc = DateTime.UtcNow,
                ProjectId = projectId
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E1",
                Category = ElementCategory.Beam.ToString()
            };
            element.Quantities["LengthM"] = value;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
