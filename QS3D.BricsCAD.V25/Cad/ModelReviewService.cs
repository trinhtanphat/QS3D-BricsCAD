using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class ModelReviewService
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<Document, List<string>> Highlighted = new Dictionary<Document, List<string>>();
        public static int HighlightSelection(Document document, bool promptIfEmpty)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var editor = document.Editor; var selection = editor.SelectImplied();
            if ((selection.Status != PromptStatus.OK || selection.Value == null) && promptIfEmpty) { selection = editor.GetSelection(); if (selection.Status == PromptStatus.OK && selection.Value != null) editor.SetImpliedSelection(selection.Value.GetObjectIds()); }
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            editor.SetImpliedSelection(selection.Value.GetObjectIds()); ClearHighlight(document);
            var handles = new List<string>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds()) { var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; if (entity == null || entity.IsErased) continue; entity.Highlight(); handles.Add(entity.Handle.ToString()); }
                transaction.Commit();
            }
            lock (Gate) Highlighted[document] = handles; document.Editor.Regen(); return handles.Count;
        }
        public static int ClearHighlight(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            List<string> handles; lock (Gate) { if (!Highlighted.TryGetValue(document, out handles)) return 0; Highlighted.Remove(document); }
            var count = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var text in handles)
                {
                    if (!long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                    try { var id = document.Database.GetObjectId(false, new Handle(value), 0); if (id.IsNull || !id.IsValid) continue; var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; if (entity == null || entity.IsErased) continue; entity.Unhighlight(); count++; } catch { }
                }
                transaction.Commit();
            }
            document.Editor.Regen(); return count;
        }
        public static void ForgetByName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            lock (Gate)
            {
                foreach (var document in Highlighted.Keys.ToList()) { string name; try { name = document.Name; } catch { Highlighted.Remove(document); continue; } if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)) Highlighted.Remove(document); }
            }
        }
    }
}
