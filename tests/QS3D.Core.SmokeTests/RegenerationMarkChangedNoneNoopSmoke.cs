using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationMarkChangedNoneNoopSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            NoneDoesNotMutateSourceDependentsOrProject();
            NoneStillRejectsUnknownTarget();
            RealFlagsStillPropagate();
        }

        private static void NoneDoesNotMutateSourceDependentsOrProject()
        {
            var project = CreateProject(out var source, out var dependent);
            var sourceUpdatedUtc = source.UpdatedUtc;
            var dependentUpdatedUtc = dependent.UpdatedUtc;
            var projectUpdatedUtc = project.UpdatedUtc;
            var changeVersion = project.ChangeVersion;

            new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>())
                .MarkChanged(project, source.Id, ElementDirtyFlags.None);

            Require(source.Dirty == ElementDirtyFlags.None, "MarkChanged(None) dirtied the source element.");
            Require(dependent.Dirty == ElementDirtyFlags.None, "MarkChanged(None) dirtied a dependent element.");
            Require(source.UpdatedUtc == sourceUpdatedUtc, "MarkChanged(None) changed the source persistence timestamp.");
            Require(dependent.UpdatedUtc == dependentUpdatedUtc, "MarkChanged(None) changed the dependent persistence timestamp.");
            Require(project.ChangeVersion == changeVersion, "MarkChanged(None) advanced ProjectState.ChangeVersion.");
            Require(project.UpdatedUtc == projectUpdatedUtc, "MarkChanged(None) changed ProjectState.UpdatedUtc.");
        }

        private static void NoneStillRejectsUnknownTarget()
        {
            var project = CreateProject(out var source, out var dependent);
            var changeVersion = project.ChangeVersion;

            try
            {
                new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>())
                    .MarkChanged(project, "missing", ElementDirtyFlags.None);
            }
            catch (KeyNotFoundException)
            {
                Require(source.Dirty == ElementDirtyFlags.None, "Unknown-target rejection dirtied the source.");
                Require(dependent.Dirty == ElementDirtyFlags.None, "Unknown-target rejection dirtied the dependent.");
                Require(project.ChangeVersion == changeVersion, "Unknown-target rejection advanced ChangeVersion.");
                return;
            }

            throw new InvalidOperationException("MarkChanged(None) must still reject an unknown target.");
        }

        private static void RealFlagsStillPropagate()
        {
            var project = CreateProject(out var source, out var dependent);
            var changeVersion = project.ChangeVersion;

            new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>())
                .MarkChanged(project, source.Id, ElementDirtyFlags.Properties);

            Require((source.Dirty & ElementDirtyFlags.Properties) != 0, "MarkChanged(Properties) did not dirty the source.");
            Require((dependent.Dirty & (ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)) ==
                    (ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity),
                "MarkChanged(Properties) did not preserve dependent propagation.");
            Require(project.ChangeVersion == changeVersion + 1L, "Real MarkChanged mutation must advance ChangeVersion exactly once.");
        }

        private static ProjectState CreateProject(out ProjectElement source, out ProjectElement dependent)
        {
            var project = new ProjectState("REGEN-NONE", "Regeneration None no-op");
            source = new ProjectElement("SOURCE", ElementCategory.Beam);
            dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
            dependent.DependsOn.Add(source.Id);
            source.MarkClean(ElementDirtyFlags.All);
            dependent.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(source);
            project.Elements.Add(dependent);
            return project;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
