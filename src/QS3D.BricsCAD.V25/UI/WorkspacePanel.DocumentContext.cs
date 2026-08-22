using System;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        // Workspace UI survives active-document switches. Keep authoring filters scoped to the
        // document that established them so a subtype/category from another DWG cannot steer Add,
        // Capture or View3D in the newly-active drawing.
        private Document? _workspaceContextDocument = Application.DocumentManager.MdiActiveDocument;

        internal void RefreshProjectForActiveDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (!ReferenceEquals(_workspaceContextDocument, document))
            {
                _workspaceContextDocument = document;
                ResetWorkspaceAuthoringFilters();
            }

            RefreshProject();
        }

        internal void ClearProjectForUnavailableDocument(string status)
        {
            _workspaceContextDocument = null;
            ResetWorkspaceAuthoringFilters();
            ClearProject(status);
        }

        private void ResetWorkspaceAuthoringFilters()
        {
            _categoryFilter = null;
            _familySubtypeFilter = string.Empty;
        }
    }
}
