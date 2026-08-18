using System;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotHandleCanonicalitySmoke
    {
        public static void Run()
        {
            var canonical = new EntitySnapshot("AB12", "LINE", "Layer 1");
            Require(canonical.Handle == "AB12", "Canonical entity snapshot handle changed unexpectedly.");

            ExpectArgument(() => new EntitySnapshot(" AB12", "LINE", "Layer 1"), "leading-space handle");
            ExpectArgument(() => new EntitySnapshot("AB12 ", "LINE", "Layer 1"), "trailing-space handle");
            ExpectArgument(() => new EntitySnapshot("\tAB12", "LINE", "Layer 1"), "leading-tab handle");
            ExpectArgument(() => new EntitySnapshot("AB12\t", "LINE", "Layer 1"), "trailing-tab handle");
            ExpectArgument(() => new EntitySnapshot("\rAB12", "LINE", "Layer 1"), "leading-CR handle");
            ExpectArgument(() => new EntitySnapshot("AB12\n", "LINE", "Layer 1"), "trailing-LF handle");

            var normalizedEntityType = new EntitySnapshot("AB12", " LINE ", "Layer 1");
            Require(normalizedEntityType.EntityType == "LINE", "Handle hardening unexpectedly changed EntityType normalization.");
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
