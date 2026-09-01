using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleNumericIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NormalizerMatchesCadNumericIdentity();
            PolicyAndIndexResolveNumericAliases();
            DifferentOwnersAliasSameCadHandleFailClosed();
            SameOwnerLogicalHostAliasRemainsAllowed();
            SafeOwnershipDetectsSourceGeneratedAliasConflict();
            RebarOwnershipDetectsCrossSlotAliasConflict();
            MalformedPersistedTokensFailClosed();
            DistinctHandlesRemainDistinct();
        }

        private static void NormalizerMatchesCadNumericIdentity()
        {
            Equal("A", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("A"), "canonical handle");
            Equal("A", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("0A"), "leading-zero alias");
            Equal("A", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(" 0x000a "), "0x/case/leading-zero alias");
            Equal("BAD-G", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(" BAD-G "), "malformed identity preservation");
            Equal("0", GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity("0"), "zero identity preservation");
        }

        private static void PolicyAndIndexResolveNumericAliases()
        {
            var project = Project("LOOKUP");
            var owner = Element("E-1");
            owner.Properties["GeneratedSolidHandle"] = "A";
            project.Elements.Add(owner);

            if (!GeneratedHandleOwnershipPolicy.TryFindOwner(project, "0x000A", out var policyOwner, out var policySlot) || !ReferenceEquals(owner, policyOwner))
                throw new InvalidOperationException("GeneratedHandleOwnershipPolicy did not resolve a numeric handle alias to the existing owner.");
            Equal("GeneratedSolidHandle", policySlot, "policy owner slot");

            var index = GeneratedHandleOwnershipIndex.Build(project);
            if (!index.TryFindOwner("000a", out var indexOwner, out var indexSlot) || !ReferenceEquals(owner, indexOwner))
                throw new InvalidOperationException("GeneratedHandleOwnershipIndex did not resolve a numeric handle alias to the existing owner.");
            Equal("GeneratedSolidHandle", indexSlot, "index owner slot");
        }

        private static void DifferentOwnersAliasSameCadHandleFailClosed()
        {
            var project = Project("AMBIGUOUS");
            var first = Element("E-1");
            var second = Element("E-2");
            first.Properties["GeneratedSolidHandle"] = "A";
            second.Properties["GeneratedRebarHandles"] = "A";
            project.Elements.Add(first);
            project.Elements.Add(second);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            ExpectInvalid(() => index.TryFindOwner("0A", out _, out _), "numeric aliases claimed by different semantic owners");
            ExpectInvalid(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "0x000A", out _, out _), "policy lookup across numeric aliases claimed by different semantic owners");
        }

        private static void SameOwnerLogicalHostAliasRemainsAllowed()
        {
            var project = Project("HOST-ALIAS");
            var owner = Element("E-1");
            owner.Properties["GeneratedSolidHandle"] = "A";
            owner.Properties["PhysicalOpeningCutSolidHandle"] = "A";
            project.Elements.Add(owner);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            if (!index.TryFindOwner("0xA", out var actual, out _) || !ReferenceEquals(owner, actual))
                throw new InvalidOperationException("Same-owner logical host-solid numeric aliases must remain allowed.");
        }

        private static void SafeOwnershipDetectsSourceGeneratedAliasConflict()
        {
            var project = Project("SAFE");
            var sourceOwner = Element("SOURCE");
            var generatedOwner = Element("GENERATED");
            sourceOwner.SourceHandles.Add("0x000A");
            generatedOwner.Properties["GeneratedSolidHandle"] = "A";
            project.Elements.Add(sourceOwner);
            project.Elements.Add(generatedOwner);

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);
            RequireCode(issues, "GENERATED_HANDLE_OWNERSHIP_CONFLICT", "SourceHandles and generated owner aliases of the same CAD handle must conflict.");
        }

        private static void RebarOwnershipDetectsCrossSlotAliasConflict()
        {
            var project = Project("REBAR");
            var first = Element("E-1");
            var second = Element("E-2");
            first.Properties["GeneratedRebarHandles"] = "A";
            second.Properties["GeneratedTieRebarHandles"] = "A";
            project.Elements.Add(first);
            project.Elements.Add(second);

            var issues = new GeneratedRebarOwnershipHealthService().Inspect(project);
            RequireCode(issues, "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT", "Rebar owner slots using numeric aliases of the same CAD handle must conflict.");
        }

        private static void MalformedPersistedTokensFailClosed()
        {
            var project = Project("MALFORMED");
            var owner = Element("E-1");
            owner.Properties["GeneratedSolidHandle"] = " BAD-G ";
            project.Elements.Add(owner);

            ExpectInvalid(() => GeneratedHandleOwnershipIndex.Build(project), "non-canonical persisted generated-handle token");
            ExpectInvalid(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, "BAD-G", out _, out _), "policy lookup with malformed persisted generated-handle token");
        }

        private static void DistinctHandlesRemainDistinct()
        {
            var project = Project("DISTINCT");
            var first = Element("E-1");
            var second = Element("E-2");
            first.Properties["GeneratedRebarHandles"] = "A";
            second.Properties["GeneratedTieRebarHandles"] = "B";
            project.Elements.Add(first);
            project.Elements.Add(second);

            var issues = new GeneratedRebarOwnershipHealthService().Inspect(project);
            if (issues.Any(x => string.Equals(x.Code, "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT", StringComparison.Ordinal)))
                throw new InvalidOperationException("Distinct numeric CAD handles must remain distinct ownership identities.");
        }

        private static ProjectState Project(string suffix) =>
            new ProjectState("P-HANDLE-ID-" + suffix, "Generated handle numeric identity smoke");

        private static ProjectElement Element(string id) =>
            new ProjectElement(id, ElementCategory.Column, string.Empty, string.Empty, string.Empty);

        private static void RequireCode(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
        }

        private static void ExpectInvalid(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("Expected fail-closed ownership behavior for " + scenario + ".");
        }

        private static void Equal(string expected, string actual, string scenario)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected normalized handle for " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
