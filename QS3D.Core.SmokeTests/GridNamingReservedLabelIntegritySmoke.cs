using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingReservedLabelIntegritySmoke
    {
        public static void Run()
        {
            DuplicateNonTargetLabelsBlockWholeBatchAtomically();
            TargetOwnedDuplicateCanBeRepaired();
        }

        private static void DuplicateNonTargetLabelsBlockWholeBatchAtomically()
        {
            var project = Project();
            var target = Grid(project, "G-TARGET");
            target.SetProperty(GridNamingService.GridLabelKey, "OLD");
            target.SetProperty(GridNamingService.GridSequenceIndexKey, "77");

            var externalA = Grid(project, "G-EXT-A");
            var externalB = Grid(project, "G-EXT-B");
            externalA.SetProperty(GridNamingService.GridLabelKey, "  KEEP  ");
            externalB.SetProperty(GridNamingService.GridLabelKey, "keep");

            var beforeVersion = project.ChangeVersion;
            Throws<InvalidOperationException>(() => GridNamingService.Renumber(
                project,
                new[] { target.Id },
                new GridNamingOptions { StartIndex = 1 }));

            Equal(beforeVersion, project.ChangeVersion);
            Equal("OLD", target.Properties[GridNamingService.GridLabelKey]);
            Equal("77", target.Properties[GridNamingService.GridSequenceIndexKey]);
        }

        private static void TargetOwnedDuplicateCanBeRepaired()
        {
            var project = Project();
            var target = Grid(project, "G-TARGET");
            var external = Grid(project, "G-EXT");
            target.SetProperty(GridNamingService.GridLabelKey, "A");
            external.SetProperty(GridNamingService.GridLabelKey, " a ");

            var plan = GridNamingService.Renumber(
                project,
                new[] { target.Id },
                new GridNamingOptions { StartIndex = 2 });

            Equal(1, plan.Count);
            Equal("2", target.Properties[GridNamingService.GridLabelKey]);
            Equal("2", target.Properties[GridNamingService.GridSequenceIndexKey]);
            Equal(" a ", external.Properties[GridNamingService.GridLabelKey]);
        }

        private static ProjectState Project() => new ProjectState("grid-reserved-label-integrity", "Grid reserved-label integrity");

        private static ProjectElement Grid(ProjectState project, string id)
        {
            var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return element;
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
