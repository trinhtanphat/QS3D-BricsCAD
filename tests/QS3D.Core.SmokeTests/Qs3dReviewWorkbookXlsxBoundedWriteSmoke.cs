using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            HostileWorksheetBudgetFailsClosed();
            HostileArchiveBudgetFailsClosed();
            ExportDoesNotRetainCompleteWorksheetXml();
            CumulativeWorksheetBudgetFailsBeforeDestinationReplacement();
            HostileEncodingFailurePreservesDestination();
        }

        private static void HostileWorksheetBudgetFailsClosed()
        {
            using (var inner = new MemoryStream())
            using (var bounded = CreateBoundedStream("BoundedEntryWriteStream", inner, 8L))
            {
                try
                {
                    bounded.Write(new byte[9], 0, 9);
                    throw new InvalidOperationException("QS3D Review bounded smoke: expected worksheet budget failure.");
                }
                catch (InvalidDataException error)
                {
                    if (!error.Message.Contains("QS3D Review XLSX worksheet exceeds", StringComparison.Ordinal))
                        throw new InvalidOperationException("QS3D Review bounded smoke: worksheet budget diagnostic drifted.", error);
                }
            }
        }
        private static void HostileArchiveBudgetFailsClosed()
        {
            using (var inner = new MemoryStream())
            using (var bounded = CreateBoundedStream("BoundedArchiveWriteStream", inner, 8L))
            {
                ThrowsInvalidData(() => bounded.Write(new byte[9], 0, 9));
            }
        }

        private static void ExportDoesNotRetainCompleteWorksheetXml()
        {
            var method = XlsxType().GetMethod("WritePackage", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("QS3D Review bounded smoke: WritePackage not found.");
            var parameters = method.GetParameters();
            if (parameters.Length != 2 || parameters[1].ParameterType != typeof(Action<TextWriter>[]))
                throw new InvalidOperationException("QS3D Review bounded smoke: worksheets are not lazy TextWriter callbacks.");
            if (parameters[1].ParameterType == typeof(string[]))
                throw new InvalidOperationException("QS3D Review bounded smoke: complete worksheet XML is retained as string[].");
        }

        private static void CumulativeWorksheetBudgetFailsBeforeDestinationReplacement()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-cumulative-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-CUMULATIVE-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try
            {
                ExistingReviewWorkbookSurvivesBudgetFailure(path, sentinel);
            }
            finally
            {
                TryDelete(path);
                foreach (var temp in Directory.GetFiles(Path.GetDirectoryName(path)!, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }

        private static void ExistingReviewWorkbookSurvivesBudgetFailure(string path, string sentinel)
        {
            var chunk = new string('Q', 1024 * 1024);
            Action<TextWriter> largeSheet = writer => { for (var i = 0; i < 22; i++) writer.Write(chunk); };
            var sheets = new Action<TextWriter>[] { largeSheet, largeSheet, largeSheet, writer => writer.Write("<worksheet/>"), writer => writer.Write("<worksheet/>"), writer => writer.Write("<worksheet/>") };
            var method = XlsxType().GetMethod("WritePackage", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("QS3D Review bounded smoke: WritePackage not found.");
            try
            {
                method.Invoke(null, new object[] { path, sheets });
                throw new InvalidOperationException("QS3D Review bounded smoke: cumulative worksheet budget unexpectedly published.");
            }
            catch (TargetInvocationException error) when (error.InnerException is InvalidDataException)
            {
            }
            AssertDestinationSentinelPreserved(path, sentinel);
            AssertOwnedTempArtifactsCleared(path);
        }

        private static void HostileEncodingFailurePreservesDestination()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-review-bounded-" + Guid.NewGuid().ToString("N") + ".xlsx");
            const string sentinel = "KEEP-EXISTING-WORKBOOK";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try
            {
                var sheets = Enumerable.Range(0, 6)
                    .Select(index => index == 0
                        ? (Action<TextWriter>)(writer => writer.Write("\ud800"))
                        : writer => writer.Write("<worksheet/>"))
                    .ToArray();
                var method = XlsxType().GetMethod("WritePackage", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("QS3D Review bounded smoke: WritePackage not found.");
                try
                {
                    method.Invoke(null, new object[] { path, sheets });
                    throw new InvalidOperationException("QS3D Review bounded smoke: invalid UTF-16 unexpectedly published.");
                }
                catch (TargetInvocationException error) when (error.InnerException is EncoderFallbackException)
                {
                }
                AssertDestinationSentinelPreserved(path, sentinel);
                AssertOwnedTempArtifactsCleared(path);
            }
            finally
            {
                TryDelete(path);
                foreach (var temp in Directory.GetFiles(Path.GetDirectoryName(path)!, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }

        private static Stream CreateBoundedStream(string nestedName, Stream inner, long limit)
        {
            var type = XlsxType().GetNestedType(nestedName, BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("QS3D Review bounded smoke: " + nestedName + " not found.");
            return (Stream)(Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { inner, limit },
                culture: null)
                ?? throw new InvalidOperationException("QS3D Review bounded smoke: cannot construct " + nestedName + "."));
        }

        private static Type XlsxType() =>
            typeof(Qs3dReviewWorkbookExporter).Assembly.GetType("QS3D.Core.Export.Qs3dReviewXlsx", throwOnError: true)!;

        private static void AssertDestinationSentinelPreserved(string path, string sentinel)
        {
            var value = File.ReadAllText(path, Encoding.UTF8);
            if (!value.Contains(sentinel, StringComparison.Ordinal))
                throw new InvalidOperationException("QS3D Review bounded smoke: destination sentinel was replaced after failure.");
        }

        private static void AssertOwnedTempArtifactsCleared(string path)
        {
            var directory = Path.GetDirectoryName(path)!;
            var pattern = "." + Path.GetFileName(path) + ".*.tmp";
            if (Directory.GetFiles(directory, pattern).Length != 0)
                throw new InvalidOperationException("QS3D Review bounded smoke: owned temp artifact leaked after failure.");
        }

        private static void ThrowsInvalidData(Action action)
        {
            try
            {
                action();
                throw new InvalidOperationException("QS3D Review bounded smoke: expected InvalidDataException.");
            }
            catch (InvalidDataException)
            {
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
