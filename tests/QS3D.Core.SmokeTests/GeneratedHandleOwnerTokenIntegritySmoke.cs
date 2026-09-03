using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnerTokenIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LeadingDelimiterFailsClosed();
            TrailingDelimiterFailsClosed();
            DoubleDelimiterFailsClosed();
            WhitespaceOnlyTokenFailsClosed();
            DuplicateTokenFailsClosed();
            PaddedTokenFailsClosed();
            NonCanonicalTokenFailsClosed();
            MalformedOwnershipFailsAcrossPublicSurfacesBeforeCallback();
            ValidMultiHandleOwnershipPreservesLogicalEquality();
        }

        private static void LeadingDelimiterFailsClosed() => AssertMalformed(";A", "leading delimiter");
        private static void TrailingDelimiterFailsClosed() => AssertMalformed("A;", "trailing delimiter");
        private static void DoubleDelimiterFailsClosed() => AssertMalformed("A;;B", "double delimiter");
        private static void WhitespaceOnlyTokenFailsClosed() => AssertMalformed("A; ;B", "whitespace-only token");
        private static void DuplicateTokenFailsClosed() => AssertMalformed("A;A", "duplicate token");
        private static void PaddedTokenFailsClosed() => AssertMalformed(" A;B", "padded token");
        private static void NonCanonicalTokenFailsClosed() => AssertMalformed("a;B", "non-canonical token");

        private static void AssertMalformed(string raw, string label)
        {
            var owner = OwnerWith(raw);
            ThrowsIntegrity(() => GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(owner).ToList(), label);
        }

        private static void MalformedOwnershipFailsAcrossPublicSurfacesBeforeCallback()
        {
            var project = ProjectWith("A;;B", out var owner);
            ThrowsIntegrity(() => GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(owner).ToList(), "EnumerateOwnerHandles");
            ThrowsIntegrity(() => GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(owner).ToList(), "EnumerateLogicalOwnerHandles");
            ThrowsIntegrity(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project), "CollectOwnerHandles");
            ThrowsIntegrity(() =>
            {
                ProjectElement? ignoredOwner;
                string ignoredKey;
                GeneratedHandleOwnershipPolicy.TryFindOwner(project, "A", out ignoredOwner, out ignoredKey);
            }, "TryFindOwner");

            var callbacks = 0;
            ThrowsIntegrity(() => GeneratedHandleOwnershipPolicy.ValidateAllBeforeErase(
                project,
                owner,
                "GeneratedRebarHandles",
                new[] { "A" },
                _ => callbacks++), "ValidateAllBeforeErase");
            Equal(0, callbacks, "malformed persisted ownership native callbacks");
        }

        private static void ValidMultiHandleOwnershipPreservesLogicalEquality()
        {
            var project = ProjectWith("A;B", out var owner);
            var enumerated = GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(owner).ToList();
            Equal(2, enumerated.Count, "valid owner token Count");
            Equal("A", enumerated[0].Key, "valid first owner token");
            Equal("B", enumerated[1].Key, "valid second owner token");

            ProjectElement? foundOwner;
            string propertyKey;
            var found = GeneratedHandleOwnershipPolicy.TryFindOwner(project, "a", out foundOwner, out propertyKey);
            Equal(true, found, "case-insensitive logical lookup");
            Equal(true, ReferenceEquals(owner, foundOwner), "logical lookup owner identity");
            Equal("GeneratedRebarHandles", propertyKey, "logical lookup property key");

            var callbacks = new List<string>();
            var validated = GeneratedHandleOwnershipPolicy.ValidateAllBeforeErase(
                project,
                owner,
                "GeneratedRebarHandles",
                new[] { "b", "a" },
                callbacks.Add);
            Equal(2, validated.Count, "valid destructive result Count");
            Equal("A", validated[0], "valid destructive sorted first handle");
            Equal("B", validated[1], "valid destructive sorted second handle");
            Equal(2, callbacks.Count, "valid destructive callback Count");
        }

        private static ProjectState ProjectWith(string raw, out ProjectElement owner)
        {
            var project = new ProjectState("GH-TOKEN-PROJECT", "Generated handle owner token integrity");
            owner = OwnerWith(raw);
            project.Elements.Add(owner);
            return project;
        }

        private static ProjectElement OwnerWith(string raw)
        {
            var owner = new ProjectElement("GH-TOKEN-OWNER", ElementCategory.Beam);
            owner.Properties["GeneratedRebarHandles"] = raw;
            return owner;
        }

        private static void ThrowsIntegrity(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException(label + " must fail closed on malformed generated owner provenance.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
