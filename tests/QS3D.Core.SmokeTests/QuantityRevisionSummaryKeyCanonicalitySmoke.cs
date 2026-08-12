using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionSummaryKeyCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedQuantityNames();
            PreservesBlankSkipAndCaseInsensitiveGrouping();
        }

        private static void RejectsPaddedQuantityNames()
        {
            var report = new QuantityRevisionReport();
            Throws<InvalidOperationException>(() => report.Summarize(new[]
            {
                new QuantityRevisionRow { QuantityName = " NetVolumeM3 ", Before = 1d, After = 2d }
            }));
        }

        private static void PreservesBlankSkipAndCaseInsensitiveGrouping()
        {
            var report = new QuantityRevisionReport();
            var summary = report.Summarize(new[]
            {
                new QuantityRevisionRow { QuantityName = string.Empty, Before = 100d, After = 200d },
                new QuantityRevisionRow { QuantityName = "NetVolumeM3", Before = 1d, After = 3d },
                new QuantityRevisionRow { QuantityName = "netvolumem3", Before = 2d, After = 4d }
            });

            Equal(1, summary.Count);
            Equal("NetVolumeM3", summary[0].QuantityName);
            Equal(3d, summary[0].Before);
            Equal(7d, summary[0].After);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
