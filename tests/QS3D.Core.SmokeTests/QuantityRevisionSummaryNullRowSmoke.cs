using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionSummaryNullRowSmoke
    {
        internal static void Run()
        {
            NullRowsFailClosed();
            BlankQuantityRowsRemainIgnored();
            ValidRowsStillAggregateCaseInsensitively();
        }

        private static void NullRowsFailClosed()
        {
            var report = new QuantityRevisionReport();
            var error = Throws<ArgumentException>(() => report.Summarize(new QuantityRevisionRow[]
            {
                new QuantityRevisionRow { ElementId = "E1", QuantityName = "VolumeM3", Before = 1d, After = 2d },
                null!
            }));
            Contains(error.Message, "null row at index 1");
        }

        private static void BlankQuantityRowsRemainIgnored()
        {
            var report = new QuantityRevisionReport();
            var summary = report.Summarize(new[]
            {
                new QuantityRevisionRow { ElementId = "E-ADDED", Change = "Added" },
                new QuantityRevisionRow { ElementId = "E1", QuantityName = "AreaM2", Before = 2d, After = 3d }
            });
            Equal(1, summary.Count);
            Equal("AreaM2", summary[0].QuantityName);
        }

        private static void ValidRowsStillAggregateCaseInsensitively()
        {
            var report = new QuantityRevisionReport();
            var summary = report.Summarize(new[]
            {
                new QuantityRevisionRow { ElementId = "E1", QuantityName = "VolumeM3", Before = 1d, After = 2d },
                new QuantityRevisionRow { ElementId = "E2", QuantityName = "volumem3", Before = 3d, After = 5d }
            });
            Equal(1, summary.Count);
            Equal(4d, summary[0].Before);
            Equal(7d, summary[0].After);
            Equal(3d, summary[0].Delta);
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class QuantityRevisionSummaryNullRowSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRevisionSummaryNullRowSmoke.Run();
    }
}
