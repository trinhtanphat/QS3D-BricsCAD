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

        public static void ErasePrevious(Document document, Transaction transaction, ProjectElement element)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!element.Properties.TryGetValue(HandleKey, out var text) || string.IsNullOrWhiteSpace(text)) return;
            RemoveFromSourceHandles(element, text);
            if (long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                try
                {
                    var id = document.Database.GetObjectId(false, new Handle(value), 0);
                    if (!id.IsNull && id.IsValid)
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                        if (entity != null && !entity.IsErased) entity.Erase();
                    }
                }
                catch { }
            }
            element.Properties.Remove(HandleKey);
            element.Properties.Remove(CategoryKey);
        }

        public static void Track(ProjectElement element, string handle, ElementCategory category)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.Properties[HandleKey] = handle ?? string.Empty;
            element.Properties[CategoryKey] = category.ToString();
            RemoveFromSourceHandles(element, handle);
        }

        private static void RemoveFromSourceHandles(ProjectElement element, string? handle)
        {
            if (string.IsNullOrWhiteSpace(handle)) return;
            for (var index = element.SourceHandles.Count - 1; index >= 0; index--)
                if (string.Equals(element.SourceHandles[index], handle, StringComparison.OrdinalIgnoreCase)) element.SourceHandles.RemoveAt(index);
        }
    }
}
