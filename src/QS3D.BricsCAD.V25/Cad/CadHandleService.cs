using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadHandleService
    {
        public static IReadOnlyList<ObjectId> Resolve(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new List<ObjectId>();
            foreach (var text in handles)
            {
                if (string.IsNullOrWhiteSpace(text) || !long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                try
                {
                    var id = document.Database.GetObjectId(false, new Handle(value), 0);
                    if (!id.IsNull && id.IsValid) result.Add(id);
                }
                catch { }
            }
            return result;
        }

        public static int Select(Document document, IEnumerable<string> handles)
        {
            var ids = Resolve(document, handles);
            document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());
            return ids.Count;
        }

        public static ISet<string> GetLiveHandles(Document document, IEnumerable<string> handles)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in Resolve(document, handles))
            {
                try { result.Add(id.Handle.ToString()); } catch { }
            }
            return result;
        }
    }
}
