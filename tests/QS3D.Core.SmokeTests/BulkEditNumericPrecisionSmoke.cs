using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditNumericPrecisionSmoke
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
            var project = new ProjectState("bulk-precision", "Bulk Precision");
            var first = Element("A", "2");
            var second = Element("B", double.Epsilon.ToString("R", CultureInfo.InvariantCulture));
            project.Elements.Add(first);
            project.Elements.Add(second);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(
                () => new BulkEditService().MultiplyNumericProperty(
                    project,
                    new[] { first, second },
                    PropertyKey,
                    NextAfterOne),
                "non-unit direct bulk multiplication rounded back to the original value");

            Equal("2", first.Properties[PropertyKey], "earlier pending direct bulk update must not be applied after a later precision failure");
            Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), second.Properties[PropertyKey], "precision-collapsed direct bulk property must remain unchanged");
            Equal(version, project.ChangeVersion, "rejected direct bulk multiplication must not touch the project");
        }

        private static void ExactUnitFactorRemainsNoOp()
        {
            var project = ProjectWithSingle("unit", double.Epsilon.ToString("R", CultureInfo.InvariantCulture), out var element);
            var version = project.ChangeVersion;
            var changed = new BulkEditService().MultiplyNumericProperty(project, new[] { element }, PropertyKey, 1d);

            Equal(0, changed.Count, "exact unit factor direct bulk changed count");
            Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), element.Properties[PropertyKey], "exact unit factor direct bulk property");
            Equal(version, project.ChangeVersion, "exact unit factor direct bulk project version");
        }

        private static void OrdinaryMultiplicationStillUpdates()
        {
            var project = ProjectWithSingle("ordinary", "2", out var element);
            var version = project.ChangeVersion;
            var changed = new BulkEditService().MultiplyNumericProperty(project, new[] { element }, PropertyKey, 3d);

            Equal(1, changed.Count, "ordinary direct bulk changed count");
            Equal("6", element.Properties[PropertyKey], "ordinary direct bulk multiplied value");
            Equal(version + 1L, project.ChangeVersion, "ordinary direct bulk project version");
        }

        private static void ExistingZeroUnderflowStillFailsClosed()
        {
            var project = ProjectWithSingle("underflow", double.Epsilon.ToString("R", CultureInfo.InvariantCulture), out var element);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(
                () => new BulkEditService().MultiplyNumericProperty(project, new[] { element }, PropertyKey, 0.5d),
                "direct bulk nonzero multiplication underflow to zero");

            Equal(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), element.Properties[PropertyKey], "direct bulk underflow property");
            Equal(version, project.ChangeVersion, "direct bulk underflow project version");
        }

        private static ProjectState ProjectWithSingle(string suffix, string value, out ProjectElement element)
        {
            var project = new ProjectState("bulk-" + suffix, "Bulk " + suffix);
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
