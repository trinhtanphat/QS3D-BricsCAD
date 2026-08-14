using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementIdPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedIdsRemainSupported();
            ControlCharacterIdsFailAtConstruction();
        }

        private static void CanonicalAndPaddedIdsRemainSupported()
        {
            var canonical = new ProjectElement("ELEMENT-001", ElementCategory.ArchitecturalWall);
            Equal("ELEMENT-001", canonical.Id);
            Equal(ElementCategory.ArchitecturalWall, canonical.Category);

            var padded = new ProjectElement(
                "  ELEMENT-002  ",
                ElementCategory.ArchitecturalWall,
                "  FAMILY-01  ",
                "  FLOOR-01  ",
                "  ZONE-01  ");

            Equal("ELEMENT-002", padded.Id);
            Equal(ElementCategory.ArchitecturalWall, padded.Category);
            Equal("FAMILY-01", padded.FamilyId);
            Equal("FLOOR-01", padded.FloorId);
            Equal("ZONE-01", padded.ZoneId);
        }

        private static void ControlCharacterIdsFailAtConstruction()
        {
            Throws<ArgumentException>(() =>
                new ProjectElement("ELEMENT\u0001-003", ElementCategory.ArchitecturalWall));
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
