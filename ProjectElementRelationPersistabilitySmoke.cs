using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementRelationPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ConstructorAndSetterRelationsNormalize();
            ControlCharacterRelationsFailAtomically();
            NullStillClearsRelations();
            DrawingFingerprintRemainsExact();
        }

        private static void ConstructorAndSetterRelationsNormalize()
        {
            var element = new ProjectElement(
                "ELEMENT-REL-01",
                ElementCategory.ArchitecturalWall,
                "  FAMILY-01  ",
                "  FLOOR-01  ",
                "  ZONE-01  ");

            Equal("FAMILY-01", element.FamilyId);
            Equal("FLOOR-01", element.FloorId);
            Equal("ZONE-01", element.ZoneId);

            element.FamilyId = "  FAMILY-02  ";
            element.FloorId = "  FLOOR-02  ";
            element.ZoneId = "  ZONE-02  ";

            Equal("FAMILY-02", element.FamilyId);
            Equal("FLOOR-02", element.FloorId);
            Equal("ZONE-02", element.ZoneId);
        }

        private static void ControlCharacterRelationsFailAtomically()
        {
            var element = new ProjectElement(
                "ELEMENT-REL-CONTROL",
                ElementCategory.ArchitecturalWall,
                "FAMILY-01",
                "FLOOR-01",
                "ZONE-01");

            Throws<ArgumentException>(() => element.FamilyId = "FAMILY\u0001-02");
            Equal("FAMILY-01", element.FamilyId);
            Equal("FLOOR-01", element.FloorId);
            Equal("ZONE-01", element.ZoneId);

            Throws<ArgumentException>(() => element.FloorId = "FLOOR\u0001-02");
            Equal("FAMILY-01", element.FamilyId);
            Equal("FLOOR-01", element.FloorId);
            Equal("ZONE-01", element.ZoneId);

            Throws<ArgumentException>(() => element.ZoneId = "ZONE\u0001-02");
            Equal("FAMILY-01", element.FamilyId);
            Equal("FLOOR-01", element.FloorId);
            Equal("ZONE-01", element.ZoneId);
        }

        private static void NullStillClearsRelations()
        {
            var element = new ProjectElement(
                "ELEMENT-REL-NULL",
                ElementCategory.ArchitecturalWall,
                "FAMILY-01",
                "FLOOR-01",
                "ZONE-01");

            element.FamilyId = null!;
            element.FloorId = null!;
            element.ZoneId = null!;

            Equal(string.Empty, element.FamilyId);
            Equal(string.Empty, element.FloorId);
            Equal(string.Empty, element.ZoneId);
        }

        private static void DrawingFingerprintRemainsExact()
        {
            var element = new ProjectElement("ELEMENT-REL-DRAWING", ElementCategory.ArchitecturalWall)
            {
                DrawingFingerprint = "  drawing:fingerprint:AbC123  "
            };

            Equal("  drawing:fingerprint:AbC123  ", element.DrawingFingerprint);
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
