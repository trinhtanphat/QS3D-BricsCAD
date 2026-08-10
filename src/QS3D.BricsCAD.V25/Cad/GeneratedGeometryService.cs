using System;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedGeometryService
    {
        private const string HandleKey = "GeneratedSolidHandle";
        private const string CategoryKey = "GeneratedSolidCategory";

        public static string PrepareReplacement(Document document, Transaction transaction, ProjectElement element)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!element.Properties.TryGetValue(HandleKey, out var text) || string.IsNullOrWhiteSpace(text)) return string.Empty;

            if (long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                try
                {
                    var id = document.Database.GetObjectId(false, new Handle(value), 0);
                    if (!id.IsNull && id.IsValid)
                    {
                        var solid = transaction.GetObject(id, OpenMode.ForWrite, false) as Solid3d;
                        if (solid != null && !solid.IsErased) solid.Erase();
                    }
                }
                catch
                {
                    // A stale generated handle is safe to ignore. Metadata is replaced only after the CAD transaction commits.
                }
            }
            return text.Trim();
        }

        public static void CommitReplacement(ProjectElement element, string previousHandle, string generatedHandle, ElementCategory category)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(generatedHandle)) throw new ArgumentException("Generated solid handle is required.", nameof(generatedHandle));

            RemoveFromSourceHandles(element, previousHandle);
            RemoveFromSourceHandles(element, generatedHandle);
            element.Properties[HandleKey] = generatedHandle.Trim();
            element.Properties[CategoryKey] = category.ToString();
        }

        private static void RemoveFromSourceHandles(ProjectElement element, string? handle)
        {
            if (string.IsNullOrWhiteSpace(handle)) return;
            for (var index = element.SourceHandles.Count - 1; index >= 0; index--)
                if (string.Equals(element.SourceHandles[index], handle, StringComparison.OrdinalIgnoreCase)) element.SourceHandles.RemoveAt(index);
        }
    }
}
