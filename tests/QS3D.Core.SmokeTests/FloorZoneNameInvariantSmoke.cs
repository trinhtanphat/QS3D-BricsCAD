using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorZoneNameInvariantSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var floor = new FloorDefinition("F1", " Floor 1 ", 0d);
            var zone = new ZoneDefinition("Z1", " Zone 1 ");
            Equal("Floor 1", floor.Name, "floor constructor name");
            Equal("Zone 1", zone.Name, "zone constructor name");

            floor.Name = " Floor renamed ";
            zone.Name = " Zone renamed ";
            Equal("Floor renamed", floor.Name, "floor setter name");
            Equal("Zone renamed", zone.Name, "zone setter name");

            Throws<ArgumentException>(() => floor.Name = "   ");
            Throws<ArgumentException>(() => zone.Name = "\t");
            Equal("Floor renamed", floor.Name, "floor state after rejected blank setter");
            Equal("Zone renamed", zone.Name, "zone state after rejected blank setter");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("FloorZoneNameInvariantSmoke expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("FloorZoneNameInvariantSmoke: " + label + " expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
