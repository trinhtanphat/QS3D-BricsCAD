using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectIdPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndPaddedIdsRemainSupported();
            ControlCharacterIdsFailAtConstruction();
        }

        private static void CanonicalAndPaddedIdsRemainSupported()
        {
            var canonical = new ProjectState("PROJECT-001", "Project");
            Equal("PROJECT-001", canonical.ProjectId);

            var padded = new ProjectState("  PROJECT-002  ", "Project");
            Equal("PROJECT-002", padded.ProjectId);
        }

        private static void ControlCharacterIdsFailAtConstruction()
        {
            Throws<ArgumentException>(() => new ProjectState("PROJECT\u0001-003", "Project"));
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
