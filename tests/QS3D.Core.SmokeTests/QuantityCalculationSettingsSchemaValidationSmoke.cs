using System;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsSchemaValidationSmoke
    {
        internal static void Run()
        {
            ZeroSchemaKeepsLegacyCompatibility();
            NegativeSchemaFailsClosedWithoutMutation();
            CurrentSchemaRemainsValid();
        }

        private static void ZeroSchemaKeepsLegacyCompatibility()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.SchemaVersion = 0;

            settings.NormalizeAndValidate();

            Equal(QuantityCalculationSettings.CurrentSchemaVersion, settings.SchemaVersion, "zero schema compatibility");
        }

        private static void NegativeSchemaFailsClosedWithoutMutation()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.SchemaVersion = -1;

            var error = Throws<InvalidOperationException>(() => settings.NormalizeAndValidate());
            Equal("Quantity settings schema cannot be negative.", error.Message, "negative schema validation message");
            Equal(-1, settings.SchemaVersion, "negative schema remains unchanged after rejection");

            var runtimeError = Throws<InvalidOperationException>(() => new QuantityCalculationRuleSet(settings));
            Equal("Quantity settings schema cannot be negative.", runtimeError.Message, "runtime snapshot rejects negative schema");
            Equal(-1, settings.SchemaVersion, "runtime rejection does not mutate caller schema");
        }

        private static void CurrentSchemaRemainsValid()
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.NormalizeAndValidate();
            Equal(QuantityCalculationSettings.CurrentSchemaVersion, settings.SchemaVersion, "current schema remains valid");
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
