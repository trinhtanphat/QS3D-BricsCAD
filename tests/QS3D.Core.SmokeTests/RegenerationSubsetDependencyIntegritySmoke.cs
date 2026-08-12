using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationSubsetDependencyIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsDanglingSelectedDependencyBeforeMutation();
            AllowsDependencyOutsideSelectedSubsetWhenItExists();
            UnknownRequestedTargetStillWins();
        }

        private static void RejectsDanglingSelectedDependencyBeforeMutation()
        {
            var project = new ProjectState("REGEN-SUBSET-DANGLING", "Subset dangling dependency");
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
                Engine(new TrackingRegenerator()).RegenerateDirtySubset(project, new[] { dependent.Id });
            }
            catch (InvalidOperationException ex)
            {
                var expected = "Semantic element DEPENDENT depends on missing semantic element: MISSING. Repair semantic relations before graph evaluation.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected subset dependency-integrity error.", ex);

                Require(project.ChangeVersion == changeVersion, "Dangling subset dependency rejection advanced ChangeVersion.");
                Require(project.UpdatedUtc == updatedUtc, "Dangling subset dependency rejection changed project UpdatedUtc.");
                Require(dependent.Dirty == dirty, "Dangling subset dependency rejection changed dirty flags.");
                Require(dependent.Quantities.Count == quantityCount &&
                        dependent.Quantities.TryGetValue("Sentinel", out var sentinel) && sentinel == 17d,
                    "Dangling subset dependency rejection partially mutated quantities.");
                return;
            }

            throw new InvalidOperationException("RegenerateDirtySubset must reject a selected element with a dangling dependency.");
        }

        private static void AllowsDependencyOutsideSelectedSubsetWhenItExists()
        {
            var project = new ProjectState("REGEN-SUBSET-EXTERNAL", "Subset external dependency");
            var source = new ProjectElement("SOURCE", ElementCategory.CustomQuantity);
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add(source.Id);
            source.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(source);
            project.Elements.Add(dependent);

            var regenerator = new TrackingRegenerator();
            var regenerated = Engine(regenerator).RegenerateDirtySubset(project, new[] { dependent.Id });

            Require(regenerated == 1, "A selected dirty element must allow an existing dependency outside the selected subset.");
            Require(regenerator.CallCount == 1, "Valid subset regeneration called the regenerator an unexpected number of times.");
            Require(dependent.Quantities.TryGetValue("Tracked", out var tracked) && tracked == 1d,
                "Valid subset regeneration did not preserve regenerator output.");
            Require(dependent.Dirty == ElementDirtyFlags.None, "Valid selected dependent was not cleaned.");
            Require(source.Dirty == ElementDirtyFlags.None, "Unselected clean dependency was unexpectedly dirtied.");
        }

        private static void UnknownRequestedTargetStillWins()
        {
            var project = new ProjectState("REGEN-SUBSET-UNKNOWN", "Subset unknown target precedence");
            var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            var unrelated = new ProjectElement("UNRELATED", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add("MISSING");
            unrelated.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(dependent);
            project.Elements.Add(unrelated);

            try
            {
                Engine(new TrackingRegenerator()).RegenerateDirtySubset(project, new[] { dependent.Id, "UNKNOWN" });
            }
            catch (KeyNotFoundException ex)
            {
                if (!string.Equals(ex.Message, "Unknown regeneration target: UNKNOWN", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected unknown-target precedence error.", ex);
                return;
            }

            throw new InvalidOperationException("Unknown requested target must be rejected before selected dependency integrity.");
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
