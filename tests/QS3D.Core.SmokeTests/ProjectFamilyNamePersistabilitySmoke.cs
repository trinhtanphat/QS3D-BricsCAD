using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyNamePersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedNamesRemainSupported();
            ControlCharacterNamesFailBeforeStateChange();
        }

        private static void CanonicalAndPaddedNamesRemainSupported()
        {
            var canonical = new ProjectFamily("FAM-NAME-1", "Wall Type", ElementCategory.ArchitecturalWall);
            Equal("Wall Type", canonical.Name);

            var padded = new ProjectFamily("FAM-NAME-2", "  Wall Type  ", ElementCategory.ArchitecturalWall);
            Equal("Wall Type", padded.Name);

            var eventCount = 0;
            padded.PropertyChanged += (_, e) =>
            {
                if (string.Equals(e.PropertyName, nameof(ProjectFamily.Name), StringComparison.Ordinal)) eventCount++;
            };
            padded.Name = "  Renamed Wall Type  ";
            Equal("Renamed Wall Type", padded.Name);
            Equal(1, eventCount);
        }

        private static void ControlCharacterNamesFailBeforeStateChange()
        {
            Throws<ArgumentException>(() =>
                new ProjectFamily("FAM-NAME-3", "Broken\u0001Wall Type", ElementCategory.ArchitecturalWall));

            var family = new ProjectFamily("FAM-NAME-4", "Original Wall Type", ElementCategory.ArchitecturalWall);
            var eventCount = 0;
            family.PropertyChanged += (_, e) =>
            {
                if (string.Equals(e.PropertyName, nameof(ProjectFamily.Name), StringComparison.Ordinal)) eventCount++;
            };

            Throws<ArgumentException>(() => family.Name = "Broken\u0001Wall Type");

            Equal("Original Wall Type", family.Name);
            Equal(0, eventCount);
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
