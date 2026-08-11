using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var line = GridReferenceCurve.Line("BOUND", new Point2(0d, 0d), new Point2(1d, 0d));

            IEnumerable<GridReferenceCurve> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 2001) throw new Exception("GridIntersectionPlanner enumerated beyond the declared curve cap probe.");
                    yield return line;
                }
            }

            try
            {
                GridIntersectionPlanner.FindIntersections(Source());
            }
            catch (InvalidOperationException)
            {
                if (yielded != 2001) throw new Exception("Expected exactly MaxCurves + 1 yielded curves before rejection.");
                return;
            }

            throw new Exception("Expected oversize Grid intersection source rejection.");
        }
    }
}
