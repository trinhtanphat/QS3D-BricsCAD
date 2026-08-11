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
            if (handles == null) throw new ArgumentNullException(nameof(handles));

            var candidates = new List<ObjectId>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var text in handles)
            {
                var normalized = NormalizeHexHandle(text);
                if (normalized == null || !seen.Add(normalized) || !long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                try
                {
                    var id = document.Database.GetObjectId(false, new Handle(value), 0);
                    if (!id.IsNull && id.IsValid) candidates.Add(id);
                }
                catch { }
            }

            var result = new List<ObjectId>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in candidates)
                {
                    try
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity != null && !entity.IsErased) result.Add(id);
                    }
                    catch { }
                }
                transaction.Commit();
            }
            return result;
        }

        public static string? NormalizeHexHandle(string? text)
        {
            var normalized = (text ?? string.Empty).Trim();
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(2);
            if (normalized.Length == 0 || !long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value <= 0L) return null;
            return value.ToString("X", CultureInfo.InvariantCulture);
        }

        public static int Select(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var ids = Resolve(document, handles);
            document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());
            return ids.Count;
        }

        public static int SelectIfAny(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var ids = Resolve(document, handles);
            if (ids.Count == 0) return 0;
            document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());
            return ids.Count;
        }

        public static ISet<string> GetLiveHandles(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in Resolve(document, handles))
            {
                try { result.Add(id.Handle.ToString()); } catch { }
            }
            return result;
        }

        public static ISet<string> GetLiveSolidHandles(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = Resolve(document, handles);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                        if (solid != null && !solid.IsErased) result.Add(id.Handle.ToString());
                    }
                    catch { }
                }
                transaction.Commit();
            }
            return result;
        }
    }
}
