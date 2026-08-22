using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewCatalogStructuralFreshnessSmoke
    {
        internal static void Run()
        {
            SameIdElementReplacementFailsClosed();
            SameInstanceCategoryDriftFailsClosed();
            SameInstanceRelationDriftFailsClosed();
            RevisionDriftFailsClosed();
            StableCatalogRemainsDeterministicAndReadOnly();
        }

        private static void SameIdElementReplacementFailsClosed()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var original = project.Elements[0];

            Throws<InvalidOperationException>(() => SemanticViewPlanner.BuildCatalog(
                project,
                ReplaceElementWhileEnumerating(project)));

            Equal(beforeVersion, project.ChangeVersion);
            if (ReferenceEquals(original, project.Elements[0]))
                throw new InvalidOperationException("Structural-freshness fixture did not replace the semantic element instance.");
        }

        private static void SameInstanceCategoryDriftFailsClosed()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var element = project.Elements[0];

            Throws<InvalidOperationException>(() => SemanticViewPlanner.BuildCatalog(
                project,
                ChangeCategoryWhileEnumerating(project)));

            Equal(beforeVersion, project.ChangeVersion);
            Same(element, project.Elements[0]);
            Equal(ElementCategory.Column, element.Category);
        }

        private static void SameInstanceRelationDriftFailsClosed()
        {
            var project = BuildProject();
            project.Floors.Add(new FloorDefinition("F-02", "Floor 02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-02", "Zone 02"));
            var beforeVersion = project.ChangeVersion;
            var element = project.Elements[0];

            Throws<InvalidOperationException>(() => SemanticViewPlanner.BuildCatalog(
                project,
                ChangeRelationsWhileEnumerating(project)));

            Equal(beforeVersion, project.ChangeVersion);
            Same(element, project.Elements[0]);
            Equal("F-02", element.FloorId);
            Equal("Z-02", element.ZoneId);
        }

        private static void RevisionDriftFailsClosed()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SemanticViewPlanner.BuildCatalog(
                project,
                TouchProjectWhileEnumerating(project)));

            Equal(checked(beforeVersion + 1L), project.ChangeVersion);
        }

        private static void StableCatalogRemainsDeterministicAndReadOnly()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var element = project.Elements[0];
            var floor = project.Floors[0];
            var zone = project.Zones[0];

            var catalog = SemanticViewPlanner.BuildCatalog(
                project,
                new[]
                {
                    new SemanticViewDefinition("VIEW-Z", "Zulu"),
                    new SemanticViewDefinition("VIEW-A", "Alpha", SemanticViewKind.Plan, "F-01", "Z-01")
                });

            Equal(2, catalog.Count);
            Equal("VIEW-A", catalog[0].Id);
            Equal("VIEW-Z", catalog[1].Id);
            Equal("E-01", catalog[0].ElementIds[0]);
            Equal(beforeVersion, project.ChangeVersion);
            Same(element, project.Elements[0]);
            Same(floor, project.Floors[0]);
            Same(zone, project.Zones[0]);

            if (!(catalog is IList<SemanticViewPlan> mutable))
                throw new InvalidOperationException("Semantic view catalog must expose the standard read-only IList contract.");
            Throws<NotSupportedException>(() => mutable[0] = catalog[1]);
        }

        private static IEnumerable<SemanticViewDefinition> ReplaceElementWhileEnumerating(ProjectState project)
        {
            project.Elements[0] = new ProjectElement("E-01", ElementCategory.Column, "", "F-01", "Z-01");
            yield return new SemanticViewDefinition(
                "VIEW-COLUMN",
                "Column view",
                SemanticViewKind.Model,
                categories: new[] { ElementCategory.Column });
        }

        private static IEnumerable<SemanticViewDefinition> TouchProjectWhileEnumerating(ProjectState project)
        {
            project.Touch();
            yield return new SemanticViewDefinition("VIEW-ALL", "All elements");
        }

        private static IEnumerable<SemanticViewDefinition> ChangeCategoryWhileEnumerating(ProjectState project)
        {
            project.Elements[0].Category = ElementCategory.Column;
            yield return new SemanticViewDefinition(
                "VIEW-COLUMN",
                "Column view",
                SemanticViewKind.Model,
                categories: new[] { ElementCategory.Column });
        }

        private static IEnumerable<SemanticViewDefinition> ChangeRelationsWhileEnumerating(ProjectState project)
        {
            project.Elements[0].FloorId = "F-02";
            project.Elements[0].ZoneId = "Z-02";
            yield return new SemanticViewDefinition(
                "VIEW-F02-Z02",
                "Floor 02 Zone 02",
                SemanticViewKind.Plan,
                "F-02",
                "Z-02");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("VIEW-CATALOG-FRESHNESS", "View catalog freshness");
            project.Floors.Add(new FloorDefinition("F-01", "Floor 01", 0d));
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));
            project.Elements.Add(new ProjectElement("E-01", ElementCategory.Beam, "", "F-01", "Z-01"));
            return project;
        }

        private static void Same(object expected, object actual)
        {
            if (!ReferenceEquals(expected, actual))
                throw new InvalidOperationException("Expected the same object instance.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
