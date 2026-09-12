using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityWorkflowRegistrySmoke
    {
        internal static void Run()
        {
            Throws<ArgumentException>(() => new ParityWorkflowBinding(
                new FeatureId("bim.draw.rectangle"), "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project));

            var id = new FeatureId("bim.draw.rectangle");
            var safe = new ParityWorkflowBinding(id, "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project |
                ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit);
            var registry = new ParityWorkflowRegistry(new[] { safe });
            if (!ReferenceEquals(safe, registry.GetRequired(id)))
                throw new InvalidOperationException("Registry returned the wrong binding.");

            Throws<InvalidOperationException>(() => new ParityWorkflowRegistry(new[] { safe, safe }));
            P2ShellProjectBindings();
            P3BimEditingModelingBindings();
            P4RecognitionBinding();
            P5RebarBinding();
            P6SpecialCategoryBindings();
            P7ReviewDocumentationBindings();
            P8DrawingInteroperabilityBindings();
        }

        private static void P2ShellProjectBindings()
        {
            var registry = ParityShellProjectNavigationCatalog.CreateRegistry();
            if (registry.Bindings.Count != 7)
                throw new InvalidOperationException("P2 shell/project catalog must contain exactly seven bindings.");

            AssertBinding(registry, "shell.start", ParityWorkflowKind.Infrastructure,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Launcher, ParityWorkflowRequirement.None);
            AssertBinding(registry, "shell.workspace", ParityWorkflowKind.ReadOnly,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument);
            AssertBinding(registry, "shell.ribbon", ParityWorkflowKind.ReadOnly,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.None);
            AssertBinding(registry, "project.setup", ParityWorkflowKind.Infrastructure,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument);

            var mutationRequirements = ParityWorkflowRequirement.ActiveDocument |
                ParityWorkflowRequirement.Project | ParityWorkflowRequirement.AtomicMutation |
                ParityWorkflowRequirement.Audit;
            AssertBinding(registry, "project.zone", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutationRequirements);
            AssertBinding(registry, "project.floor", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutationRequirements);
            AssertBinding(registry, "project.family", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutationRequirements);
        }

        private static void P3BimEditingModelingBindings()
        {
            var registry = ParityBimEditingModelingCatalog.CreateRegistry();
            if (registry.Bindings.Count != 4)
                throw new InvalidOperationException("P3 BIM/edit/modeling catalog must contain exactly four bindings.");

            var mutation = ParityWorkflowRequirement.ActiveDocument |
                ParityWorkflowRequirement.Project | ParityWorkflowRequirement.AtomicMutation |
                ParityWorkflowRequirement.Audit;
            AssertBinding(registry, "bim.authoring", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutation | ParityWorkflowRequirement.Zone |
                ParityWorkflowRequirement.Floor | ParityWorkflowRequirement.Family);
            AssertBinding(registry, "draw", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutation);
            AssertBinding(registry, "tool.editing", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutation | ParityWorkflowRequirement.Selection);
            AssertBinding(registry, "modeling", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutation | ParityWorkflowRequirement.Selection);
        }

        private static void P4RecognitionBinding()
        {
            var registry = ParityRecognitionCatalog.CreateRegistry();
            if (registry.Bindings.Count != 1)
                throw new InvalidOperationException("P4 recognition catalog must contain exactly one binding.");

            var mutation = ParityWorkflowRequirement.ActiveDocument |
                ParityWorkflowRequirement.Project | ParityWorkflowRequirement.AtomicMutation |
                ParityWorkflowRequirement.Audit;
            AssertBinding(registry, "recognition", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutation);
        }
        private static void P5RebarBinding()
        {
            var registry = ParityRebarCatalog.CreateRegistry();
            if (registry.Bindings.Count != 1)
                throw new InvalidOperationException("P5 Rebar catalog must contain exactly one binding.");

            var mutation = ParityWorkflowRequirement.ActiveDocument |
                ParityWorkflowRequirement.Project | ParityWorkflowRequirement.Selection |
                ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit;
            AssertBinding(registry, "rebar", ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui, mutation);
        }
        private static void P6SpecialCategoryBindings()
        {
            var registry = ParitySpecialCategoriesCatalog.CreateRegistry();
            if (registry.Bindings.Count != 8)
                throw new InvalidOperationException("P6 special-category catalog must contain exactly eight bindings.");

            var mutation = ParityWorkflowRequirement.ActiveDocument |
                ParityWorkflowRequirement.Project | ParityWorkflowRequirement.Selection |
                ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit;
            foreach (var id in new[] { "room", "room.finish", "earthwork", "stair", "railing", "curtain", "door-opening", "grid" })
            {
                AssertBinding(registry, id, ParityWorkflowKind.SemanticMutation,
                    ParityWorkflowSurface.Ui, mutation);
            }
        }
        private static void P7ReviewDocumentationBindings()
        {
            var registry = ParityReviewDocumentationCatalog.CreateRegistry();
            if (registry.Bindings.Count != 3)
                throw new InvalidOperationException("P7 review/documentation catalog must contain exactly three bindings.");

            AssertBinding(registry, "view", ParityWorkflowKind.ReadOnly,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument);
            AssertBinding(registry, "quantity", ParityWorkflowKind.ReadOnly,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument);
            AssertBinding(registry, "revision", ParityWorkflowKind.ReadOnly,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project);
            if (registry.TryGet(new FeatureId("drawing-manager"), out _))
                throw new InvalidOperationException("P7 must keep Drawing Manager unbound until a canonical QS3D workflow exists.");
        }
        private static void P8DrawingInteroperabilityBindings()
        {
            var registry = ParityDrawingInteroperabilityCatalog.CreateRegistry();
            if (registry.Bindings.Count != 2)
                throw new InvalidOperationException("P8 Drawing Manager/IFC catalog must contain exactly two bindings.");

            AssertBinding(registry, "drawing-manager", ParityWorkflowKind.Infrastructure,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument);
            AssertBinding(registry, "ifc", ParityWorkflowKind.Infrastructure,
                ParityWorkflowSurface.Ui, ParityWorkflowRequirement.ActiveDocument);
        }
        private static void AssertBinding(ParityWorkflowRegistry registry, string id,
            ParityWorkflowKind kind, ParityWorkflowSurface surfaces, ParityWorkflowRequirement requirements)
        {
            var binding = registry.GetRequired(new FeatureId(id));
            if (!string.Equals(binding.WorkflowKey, id, StringComparison.Ordinal) ||
                binding.Kind != kind || binding.Surfaces != surfaces || binding.Requirements != requirements)
            {
                throw new InvalidOperationException("Unexpected P2 workflow binding: " + id + ".");
            }
        }
        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}