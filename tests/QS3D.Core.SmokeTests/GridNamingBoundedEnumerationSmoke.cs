using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingBoundedEnumerationSmoke
    {
        public static void Run()
        {
            OversizeLazyInputStopsAtFirstItemBeyondCapacity();
        }

        private static void OversizeLazyInputStopsAtFirstItemBeyondCapacity()
        {
            var project = new ProjectState("P-GRID-NAMING-BOUND", "Grid naming bounded enumeration");
            var source = new GuardedInfiniteGridIds();
            var beforeVersion = project.ChangeVersion;

            try
            {
                GridNamingService.Renumber(project, source.Values());
            }
            catch (InvalidOperationException ex)
            {
                Equal("A Grid renumber batch supports at most 2000 elements.", ex.Message);
                Equal(2001, source.YieldCount);
                Equal(beforeVersion, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected Grid renumber capacity rejection.");
        }

        private sealed class GuardedInfiniteGridIds
        {
            public int YieldCount { get; private set; }

            public IEnumerable<string> Values()
            {
                while (true)
                {
                    YieldCount++;
                    if (YieldCount > 2001)
                        throw new Exception("Grid renumber enumerated beyond the first item over capacity.");
                    yield return "G-" + YieldCount.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
