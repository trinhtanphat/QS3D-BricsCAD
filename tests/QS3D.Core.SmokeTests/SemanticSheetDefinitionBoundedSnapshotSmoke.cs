using System;
using System.Collections.Generic;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetDefinitionBoundedSnapshotSmoke
    {
        internal static void Run()
        {
            PlacementsStopAtFirstOverBoundItem();
            AcceptedPlacementsRemainDefensiveSnapshot();
        }

        private static void PlacementsStopAtFirstOverBoundItem()
        {
            try
            {
                _ = new SemanticSheetDefinition("S-BOUND", "A-1", "Bound", 420d, 297d, OverBoundedPlacements());
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Semantic sheet supports at most 128 view placements.", StringComparison.Ordinal))
                    throw new Exception("Unexpected placement capacity error: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected bounded Semantic Sheet placement failure, got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("Expected bounded Semantic Sheet placement failure.");
        }

        private static void AcceptedPlacementsRemainDefensiveSnapshot()
        {
            var source = new List<SemanticSheetPlacementDefinition>
            {
                new SemanticSheetPlacementDefinition("V1", 10d, 10d, 100d, 80d)
            };
            var definition = new SemanticSheetDefinition("S-SNAPSHOT", "A-2", "Snapshot", 420d, 297d, source);
            source.Clear();
            if (definition.Placements.Count != 1 || definition.Placements[0].ViewId != "V1")
                throw new Exception("Semantic Sheet placements must remain a defensive snapshot.");
        }

        private static IEnumerable<SemanticSheetPlacementDefinition> OverBoundedPlacements()
        {
            for (var i = 0; i <= 128; i++)
                yield return new SemanticSheetPlacementDefinition("V", 0d, 0d, 1d, 1d);
            throw new ApplicationException("Semantic Sheet constructor enumerated beyond the first over-bound placement.");
        }
    }
}
