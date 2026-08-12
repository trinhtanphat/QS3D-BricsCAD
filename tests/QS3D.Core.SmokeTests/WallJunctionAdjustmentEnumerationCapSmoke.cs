using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionAdjustmentEnumerationCapSmoke
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
                    if (yielded > 10001) throw new Exception("WallJunctionAdjustmentPlanner enumerated beyond the Wall junction segment cap probe.");
                    yield return segment;
                }
            }

            try
            {
                new WallJunctionAdjustmentPlanner().Plan(Source());
            }
            catch (InvalidOperationException)
            {
                if (yielded != 10001) throw new Exception("Expected exactly Wall junction MaxSegments + 1 yielded segments before rejection.");
                return;
            }

            throw new Exception("Expected oversize Wall junction adjustment source rejection.");
        }
    }
}
