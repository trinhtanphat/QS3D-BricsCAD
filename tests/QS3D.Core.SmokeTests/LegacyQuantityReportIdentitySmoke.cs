using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyQuantityReportIdentitySmoke
    {
        public static void Run()
        {
            var family = new FamilyDefinition("Legacy wall", ElementCategory.ArchitecturalWall, "Concrete");
            var first = new ElementInstance("Legacy-A", family, "Floor") { LengthM = 2d, GrossConcreteM3 = 1d };
            first.SourceHandles.Add("AA");
            var sameIdentityDifferentCase = new ElementInstance("legacy-a", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            sameIdentityDifferentCase.SourceHandles.Add("BB");

            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, first }));
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, sameIdentityDifferentCase }));

            var second = new ElementInstance("Legacy-B", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            var valid = QuantityReportBuilder.Group(new[] { first, second }).Single();
            if (valid.Count != 2 || Math.Abs(valid.LengthM - 5d) > 1e-12 || Math.Abs(valid.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity grouping must remain unchanged for distinct element identities.");
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
