using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookTraceRowIntegritySmoke
    {
        private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        [ModuleInitializer]
        internal static void Initialize()
        {
            StableHeaderAndTargetResolve();
            DuplicateHeaderFailsClosed();
            DuplicateTargetFailsClosed();
            UnrelatedMalformedTrailingRowFailsClosed();
            UnrelatedOutOfRangeTrailingRowFailsClosed();
        }

        private static void StableHeaderAndTargetResolve()
        {
            InvokeFindRequiredRows(Document("1", "2", "99"), 2);
        }

        private static void DuplicateHeaderFailsClosed()
        {
            ThrowsInvalidData(() => InvokeFindRequiredRows(Document("1", "1", "2"), 2), "row 1 is missing or duplicated");
        }

        private static void DuplicateTargetFailsClosed()
        {
            ThrowsInvalidData(() => InvokeFindRequiredRows(Document("1", "2", "2"), 2), "row 2 is missing or duplicated");
        }

        private static void UnrelatedMalformedTrailingRowFailsClosed()
        {
            ThrowsInvalidData(() => InvokeFindRequiredRows(Document("1", "2", "not-a-row"), 2), "invalid row number");
        }

        private static void UnrelatedOutOfRangeTrailingRowFailsClosed()
        {
            ThrowsInvalidData(() => InvokeFindRequiredRows(Document("1", "2", "1048577"), 2), "invalid row number");
        }

        private static XDocument Document(params string[] rows)
        {
            var sheetData = new XElement(Ns + "sheetData");
            foreach (var row in rows) sheetData.Add(new XElement(Ns + "row", new XAttribute("r", row)));
            return new XDocument(new XElement(Ns + "worksheet", sheetData));
        }

        private static void InvokeFindRequiredRows(XDocument document, int rowNumber)
        {
            var method = typeof(Qs3dReviewWorkbookTraceReader).GetMethod(
                "FindRequiredRows", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Trace row selector was not found.");
            var args = new object?[] { document, Ns, rowNumber, null, null };
            try
            {
                method.Invoke(null, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
            if (args[3] is not XElement header || (string?)header.Attribute("r") != "1")
                throw new InvalidOperationException("Trace row selector did not retain the unique header row.");
            if (args[4] is not XElement target || (string?)target.Attribute("r") != rowNumber.ToString())
                throw new InvalidOperationException("Trace row selector did not retain the requested target row.");
        }

        private static void ThrowsInvalidData(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Trace row integrity failed with unexpected message: " + ex.Message, ex);
            }
            throw new InvalidOperationException("Trace row integrity expected InvalidDataException containing: " + expectedMessage);
        }
    }
}
