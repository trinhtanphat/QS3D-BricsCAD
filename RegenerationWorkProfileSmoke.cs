using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectProfileIsDeterministicAndReadOnly();
            SubsetProfileMirrorsTargetSemantics();
            GeometryOnlyDirtyWorkIsVisibleButNotSemantic();
            MalformedTargetsFailClosed();
            DeepChainProfilesWithoutRecursion();
        }

        private static void ProjectProfileIsDeterministicAndReadOnly()
        {
            var project = Fixture();
            var version = project.ChangeVersion;
            var updated = project.UpdatedUtc;
            var dirty = project.Elements.ToDictionary(x => x.Id, x => x.Dirty, StringComparer.OrdinalIgnoreCase);
            var elementUpdated = project.Elements.ToDictionary(x => x.Id, x => x.UpdatedUtc, StringComparer.OrdinalIgnoreCase);

            var profiler = new RegenerationWorkProfiler();
            var first = profiler.Profile(project);
            var second = profiler.Profile(project);

            Equal(RegenerationWorkScope.Project, first.Scope);
            Equal(4, first.ProjectElementCount);
            Equal(3, first.DirtyProjectElementCount);
            Equal(3, first.PlannedElementCount);
            Equal(2, first.InternalDependencyEdgeCount);
            Equal(2, first.MaxDependencyDepth);
            Equal("A,B,C", string.Join(",", first.Items.Select(x => x.ElementId)));
            Equal("A,B,C", string.Join(",", second.Items.Select(x => x.ElementId)));
            Equal(0, first.Items.Single(x => x.ElementId == "A").DependencyDepth);
            Equal(1, first.Items.Single(x => x.ElementId == "B").DependencyDepth);
            Equal(2, first.Items.Single(x => x.ElementId == "C").DependencyDepth);
            Equal(1, first.Items.Single(x => x.ElementId == "A").DirectPlannedDependentCount);
            Equal(1, first.Items.Single(x => x.ElementId == "B").DirectPlannedDependencyCount);
            Equal(2, first.Categories.Count);
            Equal(2, first.Categories.Single(x => x.Category == ElementCategory.Beam).PlannedElementCount);
            Equal(1, first.Categories.Single(x => x.Category == ElementCategory.Column).PlannedElementCount);

            Equal(version, project.ChangeVersion);
            Equal(updated, project.UpdatedUtc);
            foreach (var element in project.Elements)
            {
                Equal(dirty[element.Id], element.Dirty);
                Equal(elementUpdated[element.Id], element.UpdatedUtc);
            }
        }

        private static void SubsetProfileMirrorsTargetSemantics()
        {
            var project = Fixture();
            var profiler = new RegenerationWorkProfiler();

            var profile = profiler.ProfileSubset(project, new[] { "C", "B" });
            Equal(RegenerationWorkScope.Subset, profile.Scope);
            Equal("B,C", string.Join(",", profile.TargetElementIds));
            Equal("B,C", string.Join(",", profile.Items.Select(x => x.ElementId)));
            Equal(1, profile.InternalDependencyEdgeCount);
            Equal(1, profile.MaxDependencyDepth);
            Equal(0, profile.Items.Single(x => x.ElementId == "B").DependencyDepth);
            Equal(1, profile.Items.Single(x => x.ElementId == "C").DependencyDepth);

            var empty = profiler.ProfileSubset(project, Array.Empty<string>());
            Equal(RegenerationWorkScope.Subset, empty.Scope);
            Equal(0, empty.TargetElementIds.Count);
            Equal(0, empty.PlannedElementCount);
            Equal(4, empty.ProjectElementCount);
        }

        private static void GeometryOnlyDirtyWorkIsVisibleButNotSemantic()
        {
            var project = Fixture();
            var geometryOnly = project.FindElement("D")!;
            geometryOnly.MarkDirty(ElementDirtyFlags.Geometry);

            var profile = new RegenerationWorkProfiler().ProfileSubset(project, new[] { "D" });
            Equal(1, profile.PlannedElementCount);
            Equal(0, profile.SemanticDirtyElementCount);
            Equal(1, profile.GeometryOnlyDirtyElementCount);
            True(!profile.Items[0].HasSemanticDirtyWork);
        }

        private static void MalformedTargetsFailClosed()
        {
            var project = Fixture();
            var profiler = new RegenerationWorkProfiler();
            Throws<ArgumentException>(() => profiler.ProfileSubset(project, new[] { " " }));
            Throws<ArgumentException>(() => profiler.ProfileSubset(project, new[] { " A " }));
            Throws<ArgumentException>(() => profiler.ProfileSubset(project, new[] { "A", "a" }));
            Throws<System.Collections.Generic.KeyNotFoundException>(() => profiler.ProfileSubset(project, new[] { "MISSING" }));
        }

        private static void DeepChainProfilesWithoutRecursion()
        {
            const int count = 2048;
            var project = new ProjectState("P-REGEN-WORK-DEEP", "Deep regeneration work");
            for (var i = 0; i < count; i++)
            {
                var id = "E" + i.ToString("D4");
                var element = new ProjectElement(id, ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
                if (i > 0) element.DependsOn.Add("E" + (i - 1).ToString("D4"));
                project.Elements.Add(element);
            }

            var profile = new RegenerationWorkProfiler().Profile(project);
            Equal(count, profile.PlannedElementCount);
            Equal(count - 1, profile.InternalDependencyEdgeCount);
            Equal(count - 1, profile.MaxDependencyDepth);
            Equal("E0000", profile.Items[0].ElementId);
            Equal("E2047", profile.Items[count - 1].ElementId);
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-REGEN-WORK", "Regeneration work profile");
            var a = new ProjectElement("A", ElementCategory.Beam, "", "", "");
            var b = new ProjectElement("B", ElementCategory.Beam, "", "", "");
            var c = new ProjectElement("C", ElementCategory.Column, "", "", "");
            var d = new ProjectElement("D", ElementCategory.Column, "", "", "");
            b.DependsOn.Add("A");
            c.DependsOn.Add("B");
            d.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(c);
            project.Elements.Add(d);
            project.Elements.Add(b);
            project.Elements.Add(a);
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
