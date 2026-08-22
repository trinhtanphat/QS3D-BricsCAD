using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var segment = new WallAxisSegment("BOUND", new Point2(0d, 0d), new Point2(1d, 0d));

            IEnumerable<WallAxisSegment> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 10001) throw new Exception("WallJunctionPlanner enumerated beyond the declared segment cap probe.");
                    yield return segment;
                }
            }

            try
            {
                new WallJunctionPlanner().Plan(Source());
            }
            catch (InvalidOperationException)
            {
                if (yielded != 10001) throw new Exception("Expected exactly MaxSegments + 1 yielded segments before rejection.");
                return;
            }

            throw new Exception("Expected oversize Wall junction source rejection.");
        }
    }
}
