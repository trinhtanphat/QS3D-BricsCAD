using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonRegionEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var seed = new PolygonRegionSeed2("BOUND", new[]
            {
                new Point2(0d, 0d),
                new Point2(1d, 0d),
                new Point2(0d, 1d)
            });

            IEnumerable<PolygonRegionSeed2> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 257) throw new Exception("PolygonRegionSetTopology enumerated beyond the declared island cap probe.");
                    yield return seed;
                }
            }

            try
            {
                PolygonRegionSetTopology.NormalizeAndValidate(Source());
            }
            catch (ArgumentException)
            {
                if (yielded != 257) throw new Exception("Expected exactly MaxRegions + 1 yielded islands before rejection.");
                return;
            }

            throw new Exception("Expected oversize polygon region source rejection.");
        }
    }
}
