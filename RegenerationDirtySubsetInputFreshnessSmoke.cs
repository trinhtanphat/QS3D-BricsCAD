using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationDirtySubsetInputFreshnessSmoke
    {
        public static void Run()
        {
            StableLazyInputStillRegenerates();
            MutatingLazyInputFailsBeforeRegeneration();
            MutatingEmptyInputFailsBeforeNoOp();
        }

        private static void StableLazyInputStillRegenerates()
        {
            var project = new ProjectState("P-REGEN-FRESH-1", "Stable regeneration input");
            var element = DirtyElement("E-STABLE");
            project.Elements.Add(element);
            var regenerator = new CountingRegenerator();
            var engine = new RegenerationEngine(new DependencyGraph(), new IElementRegenerator[] { regenerator });

            Equal(1, engine.RegenerateDirtySubset(project, LazyId(element.Id)));
            Equal(1, regenerator.Calls);
            Equal("regenerated", element.Properties[CountingRegenerator.MarkerKey]);
        }

        private static void MutatingLazyInputFailsBeforeRegeneration()
        {
            var project = new ProjectState("P-REGEN-FRESH-2", "Mutating regeneration input");
            var element = DirtyElement("E-STALE");
            project.Elements.Add(element);
            var regenerator = new CountingRegenerator();
            var engine = new RegenerationEngine(new DependencyGraph(), new IElementRegenerator[] { regenerator });
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                engine.RegenerateDirtySubset(project, TouchThenYield(project, element.Id)));

            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(0, regenerator.Calls);
            False(element.Properties.ContainsKey(CountingRegenerator.MarkerKey));
            True((element.Dirty & ElementDirtyFlags.Properties) != 0);
        }

        private static void MutatingEmptyInputFailsBeforeNoOp()
        {
            var project = new ProjectState("P-REGEN-FRESH-3", "Mutating empty regeneration input");
            var engine = new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>());
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                engine.RegenerateDirtySubset(project, TouchThenStop(project)));

            Equal(beforeVersion + 1L, project.ChangeVersion);
        }

        private static ProjectElement DirtyElement(string id)
        {
            var element = new ProjectElement(id, ElementCategory.Room);
            element.MarkClean(ElementDirtyFlags.All);
            element.MarkDirty(ElementDirtyFlags.Properties);
            return element;
        }

        private static IEnumerable<string> LazyId(string id)
        {
            yield return id;
        }

        private static IEnumerable<string> TouchThenYield(ProjectState project, string id)
        {
            project.Touch();
            yield return id;
        }

        private static IEnumerable<string> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private sealed class CountingRegenerator : IElementRegenerator
        {
            internal const string MarkerKey = "FreshnessSmoke";

            public int Calls { get; private set; }

            public bool CanRegenerate(ElementCategory category) => true;

            public void Regenerate(ProjectState project, ProjectElement element)
            {
                Calls++;
                element.Properties[MarkerKey] = "regenerated";
            }
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
