using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorDefinitionPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedTextRemainSupported();
            ControlCharactersFailBeforeStateChange();
        }

        private static void CanonicalAndPaddedTextRemainSupported()
        {
            var floor = new FloorDefinition("  FLOOR-01  ", "  Ground Floor  ", -3.5d);
            Equal("FLOOR-01", floor.Id);
            Equal("Ground Floor", floor.Name);
            Equal(-3.5d, floor.ElevationM);

            floor.Name = "  Renamed Floor  ";
            Equal("Renamed Floor", floor.Name);
            Equal(-3.5d, floor.ElevationM);
        }

        private static void ControlCharactersFailBeforeStateChange()
        {
            Throws<ArgumentException>(() => new FloorDefinition("FLOOR\u0001-02", "Floor", 0d));
            Throws<ArgumentException>(() => new FloorDefinition("FLOOR-03", "Broken\u0001Floor", 0d));

            var floor = new FloorDefinition("FLOOR-04", "Original Floor", 4.25d);
            Throws<ArgumentException>(() => floor.Name = "Broken\u0001Floor");
            Equal("Original Floor", floor.Name);
            Equal(4.25d, floor.ElevationM);
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
