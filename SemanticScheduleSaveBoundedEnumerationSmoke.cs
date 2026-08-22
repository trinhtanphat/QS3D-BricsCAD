using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleSaveBoundedEnumerationSmoke
    {
        public static void Run()
        {
            OversizeLazyCatalogStopsAtFirstItemBeyondCapacity();
        }

        private static void OversizeLazyCatalogStopsAtFirstItemBeyondCapacity()
        {
            var project = new ProjectState("P-SCHEDULE-SAVE-BOUND", "Semantic schedule save bound");
            var source = new GuardedInfiniteDefinitions();
            var beforeVersion = project.ChangeVersion;

            try
            {
                SemanticScheduleCatalog.Save(project, source.Values());
            }
            catch (InvalidOperationException ex)
            {
                Equal("Semantic schedule catalog exceeds the supported 128 definitions.", ex.Message);
                Equal(129, source.YieldCount);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(false, project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));
                return;
            }

            throw new Exception("Expected semantic schedule catalog capacity rejection.");
        }

        private sealed class GuardedInfiniteDefinitions
        {
            public int YieldCount { get; private set; }

            public IEnumerable<SemanticScheduleDefinition> Values()
            {
                while (true)
                {
                    YieldCount++;
                    if (YieldCount > 129)
                        throw new Exception("Semantic schedule save enumerated beyond the first item over capacity.");
                    yield return Definition(YieldCount);
                }
            }

            private static SemanticScheduleDefinition Definition(int index)
            {
                return new SemanticScheduleDefinition(
                    "S-" + index,
                    "Schedule " + index,
                    "Schedule " + index,
                    Array.Empty<ElementCategory>(),
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    new[] { new SemanticDocumentationColumn("Id", "{Id}") });
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
