using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyIdPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedIdsRemainSupported();
            ControlCharacterIdsFailAtConstruction();
        }

        private static void CanonicalAndPaddedIdsRemainSupported()
        {
            var canonical = new ProjectFamily("FAM-001", "Wall Type", ElementCategory.ArchitecturalWall);
            Equal("FAM-001", canonical.Id);

            var padded = new ProjectFamily("  FAM-002  ", "Wall Type", ElementCategory.ArchitecturalWall);
            Equal("FAM-002", padded.Id);
        }

        private static void ControlCharacterIdsFailAtConstruction()
        {
            Throws<ArgumentException>(() =>
                new ProjectFamily("FAM\u0001-003", "Wall Type", ElementCategory.ArchitecturalWall));
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
