using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingSourceHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNumericAliasesAcrossGroupedElements();
            PreservesDistinctNumericHandles();
        }

        private static void RejectsNumericAliasesAcrossGroupedElements()
        {
            var family = new FamilyDefinition("Wall-1", ElementCategory.ArchitecturalWall, "Concrete");
            var first = new ElementInstance("E1", family, "F1");
            var second = new ElementInstance("E2", family, "F1");
            first.SourceHandles.Add("A");
            second.SourceHandles.Add("0A");

            Throws<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, second }));
        }

        private static void PreservesDistinctNumericHandles()
        {
            var family = new FamilyDefinition("Wall-1", ElementCategory.ArchitecturalWall, "Concrete");
            var first = new ElementInstance("E1", family, "F1");
            var second = new ElementInstance("E2", family, "F1");
            first.SourceHandles.Add("A");
            second.SourceHandles.Add("B");

            var rows = QuantityReportBuilder.Group(new[] { first, second });
            Require(rows.Count == 1, "Distinct source handles unexpectedly split the report row.");
            Require(rows[0].SourceHandles.Count == 2, "Distinct source handles were incorrectly collapsed.");
            Require(rows[0].SourceHandles[0] == "A", "First distinct source handle changed.");
            Require(rows[0].SourceHandles[1] == "B", "Second distinct source handle changed.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
