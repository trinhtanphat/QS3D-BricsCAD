using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleIdentityUnsignedSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OrdinaryHandlesRemainCanonical();
            HighBitHandlesCanonicalizeAcrossSpellings();
            MaximumUnsignedHandleCanonicalizesAcrossSpellings();
            ZeroAndMalformedTokensPreserveFailSafeBehavior();
            OwnershipIndexResolvesEquivalentHighBitSpellings();
        }

        private static void OrdinaryHandlesRemainCanonical()
        {
            AssertEqual("A", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(" 0x000a "), "Ordinary prefixed handle canonicalization changed.");
            AssertEqual("7FFFFFFF", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("00007fffffff"), "Ordinary unprefixed handle canonicalization changed.");
        }

        private static void HighBitHandlesCanonicalizeAcrossSpellings()
        {
            const string expected = "8000000000000000";
            AssertEqual(expected, GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(expected), "High-bit unprefixed handle must remain canonical.");
            AssertEqual(expected, GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("0x" + expected), "High-bit prefixed handle must canonicalize to the same identity.");
            AssertEqual(expected, GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("00008000000000000000"), "High-bit handle leading zeros must be removed.");
        }

        private static void MaximumUnsignedHandleCanonicalizesAcrossSpellings()
        {
            const string expected = "FFFFFFFFFFFFFFFF";
            AssertEqual(expected, GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("ffffffffffffffff"), "Maximum unsigned handle must uppercase canonically.");
            AssertEqual(expected, GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("0xFFFFFFFFFFFFFFFF"), "Maximum unsigned prefixed handle must drop the prefix.");
        }

        private static void ZeroAndMalformedTokensPreserveFailSafeBehavior()
        {
            AssertEqual("0x0", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(" 0x0 "), "Zero handle fail-safe behavior changed unexpectedly.");
            AssertEqual("not-a-handle", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(" not-a-handle "), "Malformed handle fail-safe behavior changed unexpectedly.");
            AssertEqual(string.Empty, GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("   "), "Blank handle normalization changed unexpectedly.");
        }

        private static void OwnershipIndexResolvesEquivalentHighBitSpellings()
        {
            var project = new ProjectState("generated-handle-smoke", "Generated handle smoke");
            var owner = new ProjectElement("BEAM-1", ElementCategory.Beam);
            owner.Properties["GeneratedSolidHandle"] = "0x8000000000000000";
            project.Elements.Add(owner);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            var found = index.TryFindOwner("8000000000000000", out var resolved, out var propertyKey);
            Assert(found, "Ownership index must resolve an equivalent high-bit handle spelling.");
            Assert(ReferenceEquals(owner, resolved), "Ownership index resolved the wrong owner for an equivalent high-bit handle spelling.");
            AssertEqual("GeneratedSolidHandle", propertyKey, "Ownership index returned the wrong owner slot.");
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message + " Expected='" + expected + "', Actual='" + actual + "'.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
