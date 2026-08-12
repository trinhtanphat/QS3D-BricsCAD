using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryReferenceCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-BROWSER-QUERY-REF", "Browser query reference smoke");
            project.Families.Add(new ProjectFamily("FAM1", "Beam Family", ElementCategory.Beam));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var element = new ProjectElement("E1", ElementCategory.Beam, "FAM1", "L1", "Z1");
            project.Elements.Add(element);
            var filtered = new ProjectBrowserQueryOptions(dirtyOnly: true);

            var canonical = ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, filtered);
            Equal(1, canonical.MatchedCount, "canonical filtered match");

            element.FamilyId = "fam1";
            element.FloorId = "l1";
            element.ZoneId = "z1";
            Equal(1, ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, filtered).MatchedCount, "case-insensitive canonical references");

            element.FamilyId = " FAM1 ";
            element.FloorId = "L1";
            element.ZoneId = "Z1";
            Throws<InvalidOperationException>(() => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, filtered), "padded family reference");

            element.FamilyId = "FAM1";
            element.FloorId = " L1 ";
            Throws<InvalidOperationException>(() => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, filtered), "padded floor reference");

            element.FloorId = "L1";
            element.ZoneId = " Z1 ";
            Throws<InvalidOperationException>(() => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, filtered), "padded zone reference");

            element.ZoneId = "Z1";
            element.FamilyId = "   ";
            Throws<InvalidOperationException>(() => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, filtered), "whitespace-only family reference");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectBrowserQueryReferenceCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectBrowserQueryReferenceCanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
