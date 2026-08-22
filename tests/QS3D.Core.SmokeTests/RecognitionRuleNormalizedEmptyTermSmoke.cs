using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionRuleNormalizedEmptyTermSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNormalizedEmptyTerms();
            PreservesBlankSkipAndValidNormalization();
        }

        private static void RejectsNormalizedEmptyTerms()
        {
            Throws<ArgumentException>(() => new RecognitionRule(
                "bad-layer",
                ElementCategory.Beam,
                layerTerms: new[] { "---" }));

            Throws<ArgumentException>(() => new RecognitionRule(
                "bad-text",
                ElementCategory.Beam,
                textTerms: new[] { "___" }));

            Throws<ArgumentException>(() => new RecognitionRule(
                "bad-type",
                ElementCategory.Beam,
                entityTypes: new[] { "..." }));
        }

        private static void PreservesBlankSkipAndValidNormalization()
        {
            var rule = new RecognitionRule(
                "valid",
                ElementCategory.Beam,
                layerTerms: new[] { "  ", "Đầm", "dam" },
                textTerms: new[] { " Beam ", "beam" },
                entityTypes: new[] { " PolyLine ", "polyline" });

            Equal(1, rule.LayerTerms.Count);
            Equal("dam", rule.LayerTerms[0]);
            Equal(1, rule.TextTerms.Count);
            Equal("beam", rule.TextTerms[0]);
            Equal(1, rule.EntityTypes.Count);
            Equal("polyline", rule.EntityTypes[0]);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
