using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageEvaluatorBoundSmoke
    {
        private const int MaximumFindings = 10000;

        internal static void Run()
        {
            ExactQuantityBoundaryIsAccepted();
            QuantityOverflowFailsAtAdmission();
            ElementOverflowFailsBeforeSnapshotMaterialization();
        }

        private static void ExactQuantityBoundaryIsAccepted()
        {
            var project = ProjectWithQuantities(MaximumFindings);
            var findings = MeasurementWorkItemCoverageEvaluator.Evaluate(project, EmptyCatalog());
            Equal(MaximumFindings, findings.Count, "Coverage evaluator must accept exactly 10,000 findings.");
        }

        private static void QuantityOverflowFailsAtAdmission()
        {
            var project = ProjectWithQuantities(MaximumFindings + 1);
            var error = Capture<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(project, EmptyCatalog()));
            Contains("maximum supported finding count of 10000", error.Message, "Coverage evaluator overflow must report the shared finding budget.");
        }

        private static void ElementOverflowFailsBeforeSnapshotMaterialization()
        {
            var project = new ProjectState("coverage-elements-bound", "Coverage elements bound");
            for (var i = 0; i < MaximumFindings + 1; i++)
            {
                var element = new ProjectElement("E" + i.ToString("D5"), ElementCategory.Slab);
                element.MarkClean(ElementDirtyFlags.All);
                project.Elements.Add(element);
            }

            var error = Capture<InvalidOperationException>(() => MeasurementWorkItemCoverageEvaluator.Evaluate(project, EmptyCatalog()));
            Contains("maximum supported finding count of 10000", error.Message, "Element overflow must fail at the evaluator admission boundary.");
        }

        private static ProjectState ProjectWithQuantities(int quantityCount)
        {
            var project = new ProjectState("coverage-quantity-bound", "Coverage quantity bound");
            var element = new ProjectElement("E", ElementCategory.Slab);
            for (var i = 0; i < quantityCount; i++)
                element.SetQuantity("Q" + i.ToString("D5"), 1d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return project;
        }

        private static MeasurementWorkItemMappingCatalog EmptyCatalog() =>
            new MeasurementWorkItemMappingCatalog(Array.Empty<MeasurementWorkItemMapping>());

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    internal static class MeasurementWorkItemCoverageEvaluatorBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementWorkItemCoverageEvaluatorBoundSmoke.Run();
        }
    }
}
