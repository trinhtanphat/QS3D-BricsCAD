using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnershipPolicyIdentitySmoke
    {
        internal static void Run()
        {
            PreservesDistinctPairsAcrossNewlineCollision();
            DeduplicatesSameLogicalHostAlias();
            CaptureTargetRequiresCanonicalElementId();
        }

        private static void PreservesDistinctPairsAcrossNewlineCollision()
        {
            var element = new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedX\nGeneratedYHandle"] = "A";
            element.Properties["GeneratedYHandle"] = "A\nGeneratedX";

            var pairs = GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element).ToList();
            Equal(2, pairs.Count, "Distinct generated owner pairs collapsed through newline-delimited identity.");
            True(pairs.Any(x => x.Key == "A" && x.Value == "GeneratedX\nGeneratedYHandle"), "First logical owner pair was lost.");
            True(pairs.Any(x => x.Key == "A\nGeneratedX" && x.Value == "GeneratedYHandle"), "Second logical owner pair was lost.");
        }

        private static void DeduplicatesSameLogicalHostAlias()
        {
            var element = new ProjectElement("E2", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "H1";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "h1";

            var pairs = GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element).ToList();
            Equal(1, pairs.Count, "Host-solid aliases no longer deduplicate as one logical owner pair.");
            True(string.Equals("H1", pairs[0].Key, StringComparison.OrdinalIgnoreCase), "Logical owner handle changed unexpectedly.");
            Equal("GeneratedSolidHandle", pairs[0].Value, "Logical host alias did not canonicalize to GeneratedSolidHandle.");
        }

        private static void CaptureTargetRequiresCanonicalElementId()
        {
            var project = new ProjectState("P1", "Capture target identity");
            var element = new ProjectElement("ELEMENT-1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);

            Same(
                element,
                SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "AB12", ElementCategory.Room, "ELEMENT-1"),
                "Canonical capture target ID no longer resolves the stored semantic element.");
            Throws<ArgumentException>(() =>
                SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "AB12", ElementCategory.Room, " ELEMENT-1 "));
            Throws<ArgumentException>(() =>
                SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, "AB12", ElementCategory.Room, "   "));
        }

        private static void Same(object expected, object? actual, string message)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
