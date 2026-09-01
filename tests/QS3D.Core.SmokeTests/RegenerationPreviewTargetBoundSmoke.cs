using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationPreviewTargetBoundSmoke
    {
        internal static void Run()
        {
            NullProjectFailsBeforeTargetEnumeration();
            StopsAtProjectCardinality();
            ExactProjectCardinalityRemainsAccepted();
            DuplicateValidationKeepsPrecedence();
        }

        private static void NullProjectFailsBeforeTargetEnumeration()
        {
            var service = new RegenerationPreviewService();
            Throws<ArgumentNullException>(() => service.PreviewSubset(null!, SentinelImmediately()));
        }

        private static void StopsAtProjectCardinality()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var error = Throws<ArgumentException>(() => service.PreviewSubset(project, ThreeThenSentinel()));
            Contains(error.Message, "cannot exceed project element count of 2");
        }

        private static void ExactProjectCardinalityRemainsAccepted()
        {
            var project = Fixture();
            var preview = new RegenerationPreviewService().PreviewSubset(project, new[] { "B2", "B1" });
            Equal(2, preview.TargetElementIds.Count);
            Equal("B1", preview.TargetElementIds[0]);
            Equal("B2", preview.TargetElementIds[1]);
        }

        private static void DuplicateValidationKeepsPrecedence()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var error = Throws<ArgumentException>(() => service.PreviewSubset(project, new[] { "B1", "B1" }));
            Contains(error.Message, "Duplicate regeneration preview target: B1");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-REGEN-PREVIEW-BOUND", "Regen Preview Bound");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            var family = new ProjectFamily("FAM", "Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var first = new ProjectElement("B1", ElementCategory.Beam, "FAM", "F", "Z");
            first.Properties["LengthM"] = "6";
            first.Properties["WidthM"] = "0.3";
            first.Properties["HeightM"] = "0.5";
            project.Elements.Add(first);

            var second = new ProjectElement("B2", ElementCategory.Beam, "FAM", "F", "Z");
            second.Properties["LengthM"] = "4";
            second.Properties["WidthM"] = "0.3";
            second.Properties["HeightM"] = "0.4";
            project.Elements.Add(second);
            return project;
        }

        private static IEnumerable<string> ThreeThenSentinel()
        {
            yield return "B1";
            yield return "B2";
            yield return "B3";
            throw new InvalidOperationException("Preview target enumeration continued beyond the project cardinality bound.");
        }

        private static IEnumerable<string> SentinelImmediately()
        {
            throw new InvalidOperationException("Preview target enumeration should not start for a null project.");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RegenerationPreviewTargetBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationPreviewTargetBoundSmoke.Run();
    }
}