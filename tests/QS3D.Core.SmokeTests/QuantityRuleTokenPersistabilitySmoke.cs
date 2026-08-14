using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleTokenPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedTokensRemainSupported();
            ControlCharacterTokensFailAtConstruction();
        }

        private static void CanonicalAndPaddedTokensRemainSupported()
        {
            var rule = new QuantityRule(
                "  RULE-01  ",
                ElementCategory.ArchitecturalWall,
                "  AreaM2  ",
                "  Width * Height  ",
                "  v1  ");

            Equal("RULE-01", rule.Id);
            Equal("AreaM2", rule.OutputName);
            Equal("Width * Height", rule.Expression);
            Equal("v1", rule.Version);
        }

        private static void ControlCharacterTokensFailAtConstruction()
        {
            Throws<ArgumentException>(() => new QuantityRule(
                "RULE\u0001-02", ElementCategory.ArchitecturalWall, "AreaM2", "Width * Height", "v1"));

            Throws<ArgumentException>(() => new QuantityRule(
                "RULE-03", ElementCategory.ArchitecturalWall, "Area\u0001M2", "Width * Height", "v1"));

            Throws<ArgumentException>(() => new QuantityRule(
                "RULE-04", ElementCategory.ArchitecturalWall, "AreaM2", "Width * Height", "v\u00011"));
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

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
