using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationElementCanonicalIdSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalIdentityIsPreserved();
            PaddedIdentityFailsClosed();
            ControlCharacterIdentityFailsClosed();
            ClassificationTextFailsClosedInsteadOfNormalizing();
            DuplicateIdentityRemainsCaseInsensitive();
        }

        private static void CanonicalIdentityIsPreserved()
        {
            var element = Create("E-01", "Structure");
            Assert(element.ElementId == "E-01", "Canonical coordination element id changed.");
            Assert(element.Discipline == "Structure", "Canonical coordination discipline changed.");
        }

        private static void PaddedIdentityFailsClosed()
        {
            ExpectArgument(() => Create(" E-01 ", "Structure"));
            ExpectArgument(() => Create("\tE-01", "Structure"));
            ExpectArgument(() => Create("E-01\n", "Structure"));
        }

        private static void ControlCharacterIdentityFailsClosed()
        {
            ExpectArgument(() => Create("E-\t01", "Structure"));
            ExpectArgument(() => Create("E-\n01", "Structure"));
            ExpectArgument(() => Create("E-\r01", "Structure"));
            ExpectArgument(() => Create("E-\0-01", "Structure"));
        }

        private static void ClassificationTextFailsClosedInsteadOfNormalizing()
        {
            var bounds = new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);
            ExpectArgument(() => new CoordinationElement("E-02", " Structure ", "Beam", "Primary", "Zone A", bounds));
            ExpectArgument(() => new CoordinationElement("E-02", "Structure", " Beam ", "Primary", "Zone A", bounds));
            ExpectArgument(() => new CoordinationElement("E-02", "Structure", "Beam", " Primary ", "Zone A", bounds));
            ExpectArgument(() => new CoordinationElement("E-02", "Structure", "Beam", "Primary", " Zone A ", bounds));
        }

        private static void DuplicateIdentityRemainsCaseInsensitive()
        {
            var service = new ClashDetectionService();
            var elements = new List<CoordinationElement>
            {
                Create("E-DUP", "Structure"),
                Create("e-dup", "MEP")
            };

            ExpectArgument(() => service.Detect(elements, includeSameDiscipline: true));
        }

        private static CoordinationElement Create(string elementId, string discipline)
        {
            return new CoordinationElement(
                elementId,
                discipline,
                "Beam",
                "Primary",
                "Zone A",
                new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d));
        }

        private static void ExpectArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected coordination canonical validation to reject the input.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
