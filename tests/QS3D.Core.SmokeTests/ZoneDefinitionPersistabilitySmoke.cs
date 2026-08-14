using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneDefinitionPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedTextRemainSupported();
            ControlCharactersFailBeforeStateChange();
        }

        private static void CanonicalAndPaddedTextRemainSupported()
        {
            var zone = new ZoneDefinition("  ZONE-01  ", "  Main Zone  ");
            Equal("ZONE-01", zone.Id);
            Equal("Main Zone", zone.Name);

            zone.Name = "  Renamed Zone  ";
            Equal("Renamed Zone", zone.Name);
        }

        private static void ControlCharactersFailBeforeStateChange()
        {
            Throws<ArgumentException>(() => new ZoneDefinition("ZONE\u0001-02", "Zone"));
            Throws<ArgumentException>(() => new ZoneDefinition("ZONE-03", "Broken\u0001Zone"));

            var zone = new ZoneDefinition("ZONE-04", "Original Zone");
            Throws<ArgumentException>(() => zone.Name = "Broken\u0001Zone");
            Equal("Original Zone", zone.Name);
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
