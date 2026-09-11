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