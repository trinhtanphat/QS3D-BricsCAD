using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var canonical = new EntitySnapshot("AB12", "LINE", "Layer 1");
            Require(canonical.Handle == "AB12", "Canonical entity snapshot handle changed unexpectedly.");
            Require(canonical.EntityType == "LINE", "Canonical entity snapshot EntityType changed unexpectedly.");

            ExpectArgument(() => new EntitySnapshot(" AB12", "LINE", "Layer 1"), "leading-space handle");
            ExpectArgument(() => new EntitySnapshot("AB12 ", "LINE", "Layer 1"), "trailing-space handle");
            ExpectArgument(() => new EntitySnapshot("\tAB12", "LINE", "Layer 1"), "leading-tab handle");
            ExpectArgument(() => new EntitySnapshot("AB12\t", "LINE", "Layer 1"), "trailing-tab handle");
            ExpectArgument(() => new EntitySnapshot("\rAB12", "LINE", "Layer 1"), "leading-CR handle");
            ExpectArgument(() => new EntitySnapshot("AB12\n", "LINE", "Layer 1"), "trailing-LF handle");

            ExpectArgument(() => new EntitySnapshot("AB12", " LINE", "Layer 1"), "leading-space EntityType");
            ExpectArgument(() => new EntitySnapshot("AB12", "LINE ", "Layer 1"), "trailing-space EntityType");
            ExpectArgument(() => new EntitySnapshot("AB12", "\tLINE", "Layer 1"), "leading-tab EntityType");
            ExpectArgument(() => new EntitySnapshot("AB12", "LINE\r", "Layer 1"), "trailing-CR EntityType");
        }

        private static void ExpectArgument(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception("EntitySnapshot accepted malformed " + scenario + ".");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}