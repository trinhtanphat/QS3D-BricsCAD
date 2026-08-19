using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepElementIdentityCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExpectArgument(() => Create(" MEP-1 "), "canonical without surrounding whitespace", "space-padded MEP element id");
            ExpectArgument(() => Create("\tMEP-1"), "canonical without surrounding whitespace", "tab-prefixed MEP element id");
            ExpectArgument(() => Create("MEP-1\n"), "canonical without surrounding whitespace", "newline-suffixed MEP element id");
            ExpectArgument(() => Create("MEP\u0001-1"), "must not contain control characters", "embedded-control MEP element id");
            ExpectArgument(() => Create("   "), "required", "blank MEP element id");

            var canonical = new MepElement(
                "MEP-1",
                MepElementKind.Pipe,
                " CHW ",
                " DN25 ",
                " L1 ",
                count: 1,
                lengthM: 2d);

            Equal("MEP-1", canonical.ElementId, "canonical MEP element id must remain unchanged");
            Equal("CHW", canonical.System, "MEP system normalization must remain unchanged");
            Equal("DN25", canonical.Specification, "MEP specification normalization must remain unchanged");
            Equal("L1", canonical.Region, "MEP region normalization must remain unchanged");

            ExpectArgument(
                () => new MepQuantityService().Aggregate(new[] { canonical, Create("mep-1") }),
                "Duplicate MEP element id",
                "case-insensitive MEP duplicate identity");
        }

        private static MepElement Create(string id)
        {
            return new MepElement(
                id,
                MepElementKind.Pipe,
                "CHW",
                "DN25",
                "L1",
                count: 1,
                lengthM: 1d);
        }

        private static void ExpectArgument(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(label + ": unexpected error message: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ": expected ArgumentException.");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": expected '" + expected + "', actual '" + actual + "'.");
        }
    }
}
