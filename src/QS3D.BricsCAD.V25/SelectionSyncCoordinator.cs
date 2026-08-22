using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;

namespace QS3D.BricsCAD.V25
{
    internal static class SelectionSyncCoordinator
    {
        private static readonly HashSet<Document> Attached = new HashSet<Document>();

        public static void Attach(Document? document)
        {
            if (document == null || !Attached.Add(document)) return;
            document.ImpliedSelectionChanged += OnImpliedSelectionChanged;
            Refresh(document);
        }

        public static void DetachByName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            foreach (var document in Attached.Where(x => string.Equals(x.Name, fileName, StringComparison.OrdinalIgnoreCase)).ToArray()) Detach(document);
        }

        public static void Refresh(Document? document)
        {
            if (document == null || !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            try { PaletteCoordinator.SetInspection(EntitySnapshotReader.ReadImpliedSelection(document)); }
            catch (Exception ex) { PaletteCoordinator.SetStatus("Selection sync lỗi: " + ex.Message); }
        }

        public static void Stop()
        {
            foreach (var document in Attached.ToArray()) Detach(document);
        }

        private static void Detach(Document document)
        {
            try { document.ImpliedSelectionChanged -= OnImpliedSelectionChanged; }
            catch { }
            Attached.Remove(document);
        }

        private static void OnImpliedSelectionChanged(object sender, EventArgs e)
        {
            var document = sender as Document ?? Application.DocumentManager.MdiActiveDocument;
            if (document == null || !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            Refresh(document);
        }
    }
}
