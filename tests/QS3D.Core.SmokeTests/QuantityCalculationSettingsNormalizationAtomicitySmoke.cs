using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsNormalizationAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LateValidationFailurePreservesNormalizationTargets();
            SuccessfulValidationCommitsEstablishedNormalization();
        }

        private static void LateValidationFailurePreservesNormalizationTargets()
        {
            var settings = new QuantityCalculationSettings
            {
                SchemaVersion = 0,
                FormworkTolerance = 0d,
                BlindingConcreteOffset = 0d,
                MinSubtractAreaMm2 = 0d,
                MinFormworkAreaMm2 = 0d,
                MinConcreteVolumeM3 = 0d,
                EngulfRelPercent = 0d,
                EngulfMinAreaMm2 = 0d,
                RoomGapFillMm = 0d,
                RoomSearchRadiusMm = 0d,
                DimColor = "  #abcdef  ",
                DimTextHeight = 0d,
                CategoryRules = null!,
                IntersectionRules = null!
            };

            ThrowsContaining<InvalidOperationException>(
                settings.NormalizeAndValidate,
                "DimTextHeight must be greater than zero.");

            if (settings.SchemaVersion != 0)
                throw new InvalidOperationException("Failed settings validation changed SchemaVersion.");
            if (!string.Equals(settings.DimColor, "  #abcdef  ", StringComparison.Ordinal))
                throw new InvalidOperationException("Failed settings validation normalized DimColor before the call succeeded.");
            if (settings.CategoryRules != null)
                throw new InvalidOperationException("Failed settings validation replaced null CategoryRules.");
            if (settings.IntersectionRules != null)
                throw new InvalidOperationException("Failed settings validation replaced null IntersectionRules.");
        }

        private static void SuccessfulValidationCommitsEstablishedNormalization()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.SchemaVersion = 0;
            settings.DimColor = "  #abcdef  ";
            settings.CategoryRules = null!;
            settings.IntersectionRules = null!;

            settings.NormalizeAndValidate();

            if (settings.SchemaVersion != QuantityCalculationSettings.CurrentSchemaVersion)
                throw new InvalidOperationException("Successful settings normalization did not upgrade schema 0 to the current schema.");
            if (!string.Equals(settings.DimColor, "#ABCDEF", StringComparison.Ordinal))
                throw new InvalidOperationException("Successful settings normalization did not preserve the established DimColor normalization.");
            if (settings.CategoryRules == null || settings.CategoryRules.Count != 0)
                throw new InvalidOperationException("Successful settings normalization did not replace null CategoryRules with an empty list.");
            if (settings.IntersectionRules == null || settings.IntersectionRules.Count != 0)
                throw new InvalidOperationException("Successful settings normalization did not replace null IntersectionRules with an empty list.");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException(
                    "Expected " + typeof(TException).Name + " containing '" + expectedText + "', actual='" + ex.Message + "'.",
                    ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
