using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadSelectionGuard
    {
        public static ObjectId[] ReadImpliedSelection(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
                return Array.Empty<ObjectId>();

            return selection.Value.GetObjectIds();
        }

        public static ObjectId[] AcquireCurrentSelection(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var editor = document.Editor;
            var objectIds = ReadImpliedSelection(document);
            if (objectIds.Length > 0) return objectIds;

            var selection = editor.GetSelection();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
                return Array.Empty<ObjectId>();

            objectIds = selection.Value.GetObjectIds();
            if (objectIds.Length == 0) return Array.Empty<ObjectId>();

            // Preserve the interactive pick as PICKFIRST so the existing native builder consumes
            // exactly this selection without opening a second prompt after project binding.
            editor.SetImpliedSelection(objectIds);
            return objectIds;
        }
    }
}
