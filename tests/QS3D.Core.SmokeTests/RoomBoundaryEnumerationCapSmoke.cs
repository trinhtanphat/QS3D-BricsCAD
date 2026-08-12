using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var segment = new BoundarySegment(new Point2(0d, 0d), new Point2(1d, 0d), "BOUND");

            IEnumerable<BoundarySegment> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 5001) throw new Exception("RoomBoundaryEngine enumerated beyond the declared input-segment cap probe.");
                    yield return segment;
                }
            }

            try
            {
                new RoomBoundaryEngine().Discover(Source());
            }
            catch (InvalidOperationException)
            {
                if (yielded != 5001) throw new Exception("Expected exactly MaxInputSegments + 1 yielded segments before rejection.");
                return;
            }

            throw new Exception("Expected oversize Room boundary source rejection.");
        }
    }
}
