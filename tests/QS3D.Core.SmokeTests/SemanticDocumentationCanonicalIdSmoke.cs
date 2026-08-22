using System;
using System.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCanonicalIdSmoke
    {
        public static void Run()
        {
            var project = new ProjectState("P-DOC-ID", "Documentation Canonical ID Smoke");
            var store = new SemanticDocumentationCatalogStore();
            store.Save(
                project,
                new[]
                {
                    new SemanticViewDefinition(" V-1 ", "Model 1", SemanticViewKind.Model),
                    new SemanticViewDefinition("V-2", "Model 2", SemanticViewKind.Model)
                },
                new[]
                {
                    new SemanticSheetDefinition(
                        "S-1",
                        "A-01",
                        "Sheet 1",
                        297d,
                        210d,
                        new[]
                        {
                            new SemanticSheetPlacementDefinition("V-1", 10d, 10d, 100d, 80d),
                            new SemanticSheetPlacementDefinition("V-2", 120d, 10d, 100d, 80d)
                        },
                        "A3")
                });

            var editor = new SemanticDocumentationCatalogEditor();
            var result = editor.ReplaceView(
                project,
                "V-1",
                new SemanticViewDefinition("V-100", "Model 100", SemanticViewKind.Model),
                true);

            if (!result.Changed) throw new Exception("Canonical view-id replacement must mutate the catalog.");
            if (result.RewrittenPlacementCount != 1) throw new Exception("Canonical view-id replacement must rewrite exactly one placement.");

            var catalog = store.Load(project);
            if (!catalog.Views.Any(x => x.Id == "V-100")) throw new Exception("Replacement view was not persisted.");
            if (catalog.Views.Any(x => string.Equals(x.Id.Trim(), "V-1", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Whitespace-padded source view remained after canonical replacement.");
            if (catalog.Sheets[0].Placements[0].ViewId != "V-100")
                throw new Exception("Canonical replacement did not rewrite the sheet placement.");
            if (catalog.Sheets[0].Placements[1].ViewId != "V-2")
                throw new Exception("Canonical replacement rewrote an unrelated sheet placement.");
        }
    }
}
