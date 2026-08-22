using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationAtomicitySmoke
    {
        public static void Run()
        {
            FailedBatchRestoresWholeProjectSnapshot();
        }

        private static void FailedBatchRestoresWholeProjectSnapshot()
        {
            var project = new ProjectState("regen-atomic", "Atomic regeneration");
            var first = new ProjectElement("A", ElementCategory.Wall, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("B", ElementCategory.Wall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(first);
            project.Elements.Add(second);
            project.Metadata["Stable"] = "before";
            var beforeUpdated = project.UpdatedUtc;

            var engine = new RegenerationEngine(new DependencyGraph(), new IElementRegenerator[] { new MutateThenFailRegenerator() });
            Throws<InvalidOperationException>(() => engine.RegenerateDirty(project));

            var restoredFirst = project.FindElement("A") ?? throw new Exception("Rollback lost first semantic element.");
            var restoredSecond = project.FindElement("B") ?? throw new Exception("Rollback lost second semantic element.");
            if (restoredFirst.Properties.ContainsKey("Probe") || restoredSecond.Properties.ContainsKey("Probe"))
                throw new Exception("Failed regeneration must restore semantic properties from before the batch.");
            if (restoredFirst.Dirty != ElementDirtyFlags.All || restoredSecond.Dirty != ElementDirtyFlags.All)
                throw new Exception("Failed regeneration must restore dirty flags from before the batch.");
            if (!project.Metadata.TryGetValue("Stable", out var stable) || stable != "before" || project.Metadata.ContainsKey("Transient"))
                throw new Exception("Failed regeneration must restore project metadata from before the batch.");
            if (project.UpdatedUtc != beforeUpdated)
                throw new Exception("Failed regeneration must restore project UpdatedUtc from before the batch.");
            if (project.Elements.Count != 2 || project.Elements.Select(x => x.Id).OrderBy(x => x).SequenceEqual(new[] { "A", "B" }) == false)
                throw new Exception("Failed regeneration must restore the original semantic element set.");
        }

        private sealed class MutateThenFailRegenerator : IElementRegenerator
        {
            public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Wall;

            public void Regenerate(ProjectState project, ProjectElement element)
            {
                element.SetProperty("Probe", "mutated-" + element.Id);
                project.Metadata["Transient"] = element.Id;
                project.Touch();
                if (string.Equals(element.Id, "B", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("synthetic regeneration failure");
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
