using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionBulkNumericPrecisionSmoke
    {
        private const string PropertyKey = "ScaleFactor";
        private const double NextAfterOne = 1.0000000000000002d;

        internal static void Run()
        {
            PrecisionCollapsedNonUnitFactorFailsClosedAtomically();
            ExactUnitFactorRemainsNoOp();
            OrdinaryMultiplicationStillUpdates();
            ExistingZeroUnderflowStillFailsClosed();
        }

        private static void PrecisionCollapsedNonUnitFactorFailsClosedAtomically()
        {
            var project = new ProjectState("selection-precision", "Selection Precision");
            var first = Element("A", "2");
            var second = Element("B", double.Epsilon.ToString("R", CultureInfo.InvariantCulture));
            project.Elements.Add(first);
            project.Elements.Add(second);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(
                () => new SemanticSelectionBulkEditService().MultiplyNumericProperty(
                    project,
                    new[] { first.Id, second.Id },
                    PropertyKey,
                    NextAfterOne),
                "non-unit multiplication factor rounded back to the original value");

            Equal("2", first.Properties[PropertyKey], "earlier pending update must not be applied after a later precision failure");
            Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), second.Properties[PropertyKey], "precision-collapsed property must remain unchanged");
            Equal(version, project.ChangeVersion, "rejected bulk multiplication must not touch the project");
        }

        private static void ExactUnitFactorRemainsNoOp()
        {
            var project = ProjectWithSingle("unit", double.Epsilon.ToString("R", CultureInfo.InvariantCulture), out var element);
            var version = project.ChangeVersion;
            var result = new SemanticSelectionBulkEditService().MultiplyNumericProperty(project, new[] { element.Id }, PropertyKey, 1d);

            Equal(0, result.ChangedCount, "exact unit factor changed count");
            Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), element.Properties[PropertyKey], "exact unit factor property");
            Equal(version, project.ChangeVersion, "exact unit factor project version");
        }

        private static void OrdinaryMultiplicationStillUpdates()
        {
            var project = ProjectWithSingle("ordinary", "2", out var element);
            var version = project.ChangeVersion;
            var result = new SemanticSelectionBulkEditService().MultiplyNumericProperty(project, new[] { element.Id }, PropertyKey, 3d);

            Equal(1, result.ChangedCount, "ordinary changed count");
            Equal("6", element.Properties[PropertyKey], "ordinary multiplied value");
            Equal(version + 1L, project.ChangeVersion, "ordinary project version");
        }

        private static void ExistingZeroUnderflowStillFailsClosed()
        {
            var project = ProjectWithSingle("underflow", double.Epsilon.ToString("R", CultureInfo.InvariantCulture), out var element);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(
                () => new SemanticSelectionBulkEditService().MultiplyNumericProperty(project, new[] { element.Id }, PropertyKey, 0.5d),
                "nonzero multiplication underflow to zero");

            Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), element.Properties[PropertyKey], "underflow property");
            Equal(version, project.ChangeVersion, "underflow project version");
        }

        private static ProjectState ProjectWithSingle(string suffix, string value, out ProjectElement element)
        {
            var project = new ProjectState("selection-" + suffix, "Selection " + suffix);
            element = Element("E", value);
            project.Elements.Add(element);
            return project;
        }

        private static ProjectElement Element(string id, string value)
        {
            var element = new ProjectElement(id, ElementCategory.StructuralWall);
            element.SetProperty(PropertyKey, value);
            return element;
        }

        private static void Throws<TException>(Action action, string scenario) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " for " + scenario + ".");
        }

        private static void Equal<T>(T expected, T actual, string scenario)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Unexpected " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
