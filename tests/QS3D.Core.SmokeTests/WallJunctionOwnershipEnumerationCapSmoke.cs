using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionOwnershipEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            JunctionSourceIsBoundedBeforeMappings();
            OwnerMappingSourceIsBounded();
        }

        private static void JunctionSourceIsBoundedBeforeMappings()
        {
            var yieldedJunctions = 0;
            var touchedMappings = false;
            var junction = new WallJunction(new Point2(0d, 0d), WallJunctionKind.L, new[] { "S1", "S2" }, 2);

            IEnumerable<WallJunction> Junctions()
            {
                while (true)
                {
                    yieldedJunctions++;
                    if (yieldedJunctions > 10001) throw new Exception("WallJunctionOwnershipPlanner enumerated beyond the junction cap probe.");
                    yield return junction;
                }
            }

            IEnumerable<WallJunctionOwnerContext> Mappings()
            {
                touchedMappings = true;
                yield break;
            }

            Throws<InvalidOperationException>(() => WallJunctionOwnershipPlanner.Plan(Junctions(), Mappings()));
            Equal(10001, yieldedJunctions);
            if (touchedMappings) throw new Exception("Owner mappings must not be enumerated after the junction cap is exceeded.");
        }

        private static void OwnerMappingSourceIsBounded()
        {
            var yieldedMappings = 0;
            var mapping = new WallJunctionOwnerContext("S1", "W1", "P1", "D1", 0d, 3d, 0.2d);

            IEnumerable<WallJunctionOwnerContext> Mappings()
            {
                while (true)
                {
                    yieldedMappings++;
                    if (yieldedMappings > 20001) throw new Exception("WallJunctionOwnershipPlanner enumerated beyond the owner-mapping cap probe.");
                    yield return mapping;
                }
            }

            Throws<InvalidOperationException>(() => WallJunctionOwnershipPlanner.Plan(Array.Empty<WallJunction>(), Mappings()));
            Equal(20001, yieldedMappings);
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
