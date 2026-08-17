using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSchedulePlacementIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalIdsRemainCaseInsensitive();
            PaddedPlacementIdsFailClosed();
            PaddedAvailableScheduleIdsFailClosed();
        }

        private static void CanonicalIdsRemainCaseInsensitive()
        {
            var plan = SemanticSchedulePlacementPlanner.Build(
                EmptySheet(),
                new[] { Schedule("SCH-1") },
                new[] { new SemanticSchedulePlacementItem("sch-1", 60d, 30d) });

            if (plan.Placements.Count != 1 ||
                !string.Equals(plan.Placements[0].ScheduleId, "sch-1", StringComparison.Ordinal))
                throw new Exception("Canonical semantic schedule ids must retain case-insensitive matching without rewriting caller identity text.");
        }

        private static void PaddedPlacementIdsFailClosed()
        {
            foreach (var padded in new[] { " SCH-1", "SCH-1 ", "\tSCH-1", "SCH-1\t", "\nSCH-1", "SCH-1\n" })
            {
                MustFail(
                    () => SemanticSchedulePlacementPlanner.Build(
                        EmptySheet(),
                        new[] { Schedule("SCH-1") },
                        new[] { new SemanticSchedulePlacementItem(padded, 60d, 30d) }),
                    "Padded semantic schedule placement ids must fail closed before lookup/deduplication: " + Escape(padded));
            }
        }

        private static void PaddedAvailableScheduleIdsFailClosed()
        {
            foreach (var padded in new[] { " SCH-1", "SCH-1 ", "\tSCH-1", "SCH-1\t", "\nSCH-1", "SCH-1\n" })
            {
                MustFail(
                    () => SemanticSchedulePlacementPlanner.Build(
                        EmptySheet(),
                        new[] { Schedule(padded) },
                        new[] { new SemanticSchedulePlacementItem("SCH-1", 60d, 30d) }),
                    "Padded available semantic schedule ids must fail closed before index insertion: " + Escape(padded));
            }
        }

        private static SemanticSheetPlan EmptySheet()
        {
            return SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-CANONICAL-SCHEDULE",
                    "A-01",
                    "Schedule Canonicality",
                    297d,
                    210d,
                    Array.Empty<SemanticSheetPlacementDefinition>()),
                Array.Empty<SemanticViewPlan>());
        }

        private static SemanticScheduleDefinition Schedule(string id)
        {
            return new SemanticScheduleDefinition(
                id,
                " Schedule name remains descriptive ",
                " Schedule title remains descriptive ",
                Array.Empty<ElementCategory>(),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("ID", "{Id}") });
        }

        private static void MustFail(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception(message);
        }

        private static string Escape(string value) =>
            value.Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
