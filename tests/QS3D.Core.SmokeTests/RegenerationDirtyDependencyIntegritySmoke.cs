using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationDirtyDependencyIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsDanglingFullProjectDependencyBeforeMutation();
            PreservesCleanInProjectDependency();
        }

        private static void RejectsDanglingFullProjectDependencyBeforeMutation()
        {
            var project = new ProjectState("REGEN-DANGLING", "Regeneration dangling dependency");
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add("MISSING");
            dependent.Quantities["Sentinel"] = 17d;
            project.Elements.Add(dependent);

            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var dirty = dependent.Dirty;
            var quantityCount = dependent.Quantities.Count;

            try
            {
                Engine(new TrackingRegenerator()).RegenerateDirty(project);
            }
            catch (InvalidOperationException ex)
            {
                var expected = "Semantic element DEPENDENT depends on missing semantic element: MISSING. Repair semantic relations before graph evaluation.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected full-regeneration dependency-integrity error.", ex);

                Require(project.ChangeVersion == changeVersion, "Dangling dependency rejection advanced ChangeVersion.");
                Require(project.UpdatedUtc == updatedUtc, "Dangling dependency rejection changed project UpdatedUtc.");
                Require(dependent.Dirty == dirty, "Dangling dependency rejection changed dirty flags.");
                Require(dependent.Quantities.Count == quantityCount &&
                        dependent.Quantities.TryGetValue("Sentinel", out var sentinel) && sentinel == 17d,
                    "Dangling dependency rejection partially mutated quantities.");
                return;
            }

            throw new InvalidOperationException("Full RegenerateDirty must reject a dangling semantic dependency.");
        }

        private static void PreservesCleanInProjectDependency()
        {
            var project = new ProjectState("REGEN-CLEAN-DEP", "Regeneration clean dependency");
            var source = new ProjectElement("SOURCE", ElementCategory.CustomQuantity);
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add(source.Id);
            source.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(source);
            project.Elements.Add(dependent);

            var regenerator = new TrackingRegenerator();
            var regenerated = Engine(regenerator).RegenerateDirty(project);

            Require(regenerated == 1, "A dirty element with a clean in-project dependency must still regenerate.");
            Require(regenerator.CallCount == 1, "Valid full regeneration called the regenerator an unexpected number of times.");
            Require(dependent.Quantities.TryGetValue("Tracked", out var tracked) && tracked == 1d,
                "Valid dependent regeneration did not preserve regenerator output.");
            Require(dependent.Dirty == ElementDirtyFlags.None, "Valid dependent regeneration did not clean semantic dirty flags.");
            Require(source.Dirty == ElementDirtyFlags.None, "Clean dependency was unexpectedly dirtied.");
        }

        private static RegenerationEngine Engine(IElementRegenerator regenerator) =>
            new RegenerationEngine(new DependencyGraph(), new[] { regenerator });

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class TrackingRegenerator : IElementRegenerator
        {
            public int CallCount { get; private set; }

            public bool CanRegenerate(ElementCategory category) => category == ElementCategory.CustomQuantity;

            public void Regenerate(ProjectState project, ProjectElement element)
            {
                CallCount++;
                element.SetQuantity("Tracked", 1d);
            }
        }
    }
}
