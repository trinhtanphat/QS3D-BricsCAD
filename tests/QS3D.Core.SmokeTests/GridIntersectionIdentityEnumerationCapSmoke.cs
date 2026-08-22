using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionIdentityEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var intersection = new GridIntersection("A", "B", new Point2(0d, 0d));

            IEnumerable<GridIntersection> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 100001) throw new Exception("GridIntersectionIdentityPlanner enumerated beyond the declared intersection cap probe.");
                    yield return intersection;
                }
            }

            try
            {
                GridIntersectionIdentityPlanner.Assign(Source());
            }
            catch (InvalidOperationException)
            {
                if (yielded != 100001) throw new Exception("Expected exactly MaxIntersections + 1 yielded items before rejection.");
                return;
            }

            throw new Exception("Expected oversize Grid intersection identity source rejection.");
        }
    }
}
