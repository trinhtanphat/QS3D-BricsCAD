using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotIdentifierControlCharacterSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            HandleRejectsInternalControlCharacters();
            EntityTypeRejectsInternalControlCharacters();
            CanonicalTrimAndEntityTypeCasingRemainStable();
        }

        private static void HandleRejectsInternalControlCharacters()
        {
            ExpectArgument(() => new EntitySnapshot("A1\nB2", "Line", "layer"));
            ExpectArgument(() => new EntitySnapshot("A1\u001fB2", "Line", "layer"));
        }

        private static void EntityTypeRejectsInternalControlCharacters()
        {
            ExpectArgument(() => new EntitySnapshot("A1", "Proxy\tEntity", "layer"));
            ExpectArgument(() => new EntitySnapshot("A1", "Proxy\u001fEntity", "layer"));
        }

        private static void CanonicalTrimAndEntityTypeCasingRemainStable()
        {
            var snapshot = new EntitySnapshot("  A1  ", "  pRoXyEnTiTy  ", "layer");
            Equal("A1", snapshot.Handle, "EntitySnapshot must preserve existing CAD-handle trim behavior.");
            Equal("pRoXyEnTiTy", snapshot.EntityType, "EntitySnapshot must preserve existing EntityType trim and casing behavior.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static void ExpectArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected malformed EntitySnapshot identifier to fail closed.");
        }
    }
}
