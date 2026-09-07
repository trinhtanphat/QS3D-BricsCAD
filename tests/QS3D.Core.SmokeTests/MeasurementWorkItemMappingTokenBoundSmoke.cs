using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingTokenBoundSmoke
    {
        private const int MaximumTokenLength = 1024;

        [ModuleInitializer]
        internal static void Run()
        {
            ExactBoundaryRemainsAcceptedAcrossConstructorSurfaces();
            EveryConstructorIdentityRejectsBoundaryPlusOne();
            ResolveRejectsBoundaryPlusOneBeforeLookup();
            LengthBoundPrecedesXmlValidation();
            ExistingCanonicalityRulesRemainIntact();
        }

        private static void ExactBoundaryRemainsAcceptedAcrossConstructorSurfaces()
        {
            var token = Repeat('A', MaximumTokenLength);
            var mapping = new MeasurementWorkItemMapping(
                token,
                ElementCategory.StructuralWall,
                token,
                token,
                token);

            Equal(token, mapping.MappingId, "Exact-bound mapping id changed.");
            Equal(token, mapping.MeasurementItemId, "Exact-bound measurement item id changed.");
            Equal(token, mapping.ClassificationId, "Exact-bound classification id changed.");
            Equal(token, mapping.WorkItemId, "Exact-bound work item id changed.");

            var catalog = new MeasurementWorkItemMappingCatalog(new[] { mapping });
            var resolution = catalog.Resolve(ElementCategory.StructuralWall, token);
            Equal(true, resolution.IsMapped, "Exact-bound Resolve lookup changed.");
        }

        private static void EveryConstructorIdentityRejectsBoundaryPlusOne()
        {
            var over = Repeat('A', MaximumTokenLength + 1);

            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                over, ElementCategory.StructuralWall, "MEASURE", "CLASS", "WORK"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP", ElementCategory.StructuralWall, over, "CLASS", "WORK"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP", ElementCategory.StructuralWall, "MEASURE", over, "WORK"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP", ElementCategory.StructuralWall, "MEASURE", "CLASS", over));
        }

        private static void ResolveRejectsBoundaryPlusOneBeforeLookup()
        {
            var mapping = new MeasurementWorkItemMapping(
                "MAP", ElementCategory.StructuralWall, "MEASURE", "CLASS", "WORK");
            var catalog = new MeasurementWorkItemMappingCatalog(new[] { mapping });
            var error = Capture<ArgumentException>(() =>
                catalog.Resolve(ElementCategory.StructuralWall, Repeat('A', MaximumTokenLength + 1)));

            Contains("at most 1024 UTF-16 code units", error.Message,
                "Over-bound Resolve identity did not report the canonical mapping token budget.");
        }

        private static void LengthBoundPrecedesXmlValidation()
        {
            var overWithInvalidXmlTail = Repeat('A', MaximumTokenLength) + "\uD800";
            var error = Capture<ArgumentException>(() => new MeasurementWorkItemMapping(
                overWithInvalidXmlTail,
                ElementCategory.StructuralWall,
                "MEASURE",
                "CLASS",
                "WORK"));

            Contains("at most 1024 UTF-16 code units", error.Message,
                "Resource bound must reject an oversized token before XML character validation.");
        }

        private static void ExistingCanonicalityRulesRemainIntact()
        {
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                " MAP", ElementCategory.StructuralWall, "MEASURE", "CLASS", "WORK"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP", ElementCategory.StructuralWall, "MEASURE\t", "CLASS", "WORK"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP", ElementCategory.StructuralWall, "MEASURE", "\uD800", "WORK"));
        }

        private static string Repeat(char value, int count) => new string(value, count);

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
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            Capture<TException>(action);
        }
    }
}
