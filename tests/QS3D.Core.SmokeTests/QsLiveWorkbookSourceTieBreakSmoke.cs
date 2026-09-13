using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsLiveWorkbookSourceTieBreakSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var binding = new LiveWorkbookBinding(
                "B1",
                "WB",
                "BOQ",
                "D10",
                "L1",
                LiveWorkbookSourceKind.BimElement,
                "E1",
                "R2",
                Array.Empty<string>(),
                1d,
                0d,
                12d);

            var sources = new[]
            {
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E1", "r2", 12d, "ifc://model/e1"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "e1", "R2", 12d, "IFC://MODEL/E1")
            };

            var engine = new LiveWorkbookRefreshEngine2();
            var forward = engine.Refresh(new[] { binding }, sources, "R2").Results.Single();
            var reverse = engine.Refresh(new[] { binding }, sources.Reverse(), "R2").Results.Single();

            Equal(forward.Freshness, reverse.Freshness, "freshness independent of equivalent source input order");
            Equal(forward.ResolvedRevision, reverse.ResolvedRevision, "resolved revision independent of equivalent source input order");
            Equal(forward.SourceEvidenceReference, reverse.SourceEvidenceReference, "evidence independent of equivalent source input order");
            Equal(forward.Trace.Single(), reverse.Trace.Single(), "trace independent of equivalent source input order");
            Equal(forward.Value, reverse.Value, "value independent of equivalent source input order");
            Equal("R2", forward.ResolvedRevision, "ordinal revision tie-break");
            Equal("IFC://MODEL/E1", forward.SourceEvidenceReference, "evidence follows canonical selected snapshot");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
