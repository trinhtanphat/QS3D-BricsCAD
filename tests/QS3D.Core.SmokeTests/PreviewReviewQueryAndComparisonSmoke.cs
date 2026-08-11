using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Review;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewQueryAndComparisonSmoke
    {
        public static void Run()
        {
            QueryFiltersAndFacetsAreDeterministic();
            SnapshotComparisonClassifiesRows();
            ComparisonRejectsDifferentProjects();
            ResultCollectionsAreImmutable();
        }

        private static void QueryFiltersAndFacetsAreDeterministic()
        {
            var project = new ProjectState("P-REVIEW", "Review");
            project.Elements.Add(new ProjectElement("E-BEAM", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("E-COLUMN", ElementCategory.Column, string.Empty, string.Empty, string.Empty));
            project.QuantityRules.Add(new QuantityRule("R-B", ElementCategory.Beam, "NetVolume", "2", "1"));
            project.QuantityRules.Add(new QuantityRule("R-C", ElementCategory.Column, "NetArea", "3", "1"));
            var snapshot = CreateSnapshot("query", project);

            var result = new PreviewReviewQueryService().Query(
                snapshot,
                new PreviewReviewQueryOptions("e-beam", "beam", "added", "quantity:"));

            Equal(1, result.Count);
            Equal("E-BEAM", result.Entries[0].ElementId);
            Equal("Quantity:NetVolume", result.Entries[0].Field);
            Equal(1, result.ChangeFacets.Count);
            Equal("Added", result.ChangeFacets[0].Key);
            Equal(1, result.ChangeFacets[0].Count);
            Equal("Beam", result.CategoryFacets[0].Key);

            var all = new PreviewReviewQueryService().Query(snapshot);
            Equal(2, all.Count);
            Equal("E-BEAM", all.Entries[0].ElementId);
            Equal("E-COLUMN", all.Entries[1].ElementId);
            Equal(2, all.CategoryFacets.Count);
            Equal("Beam", all.CategoryFacets[0].Key);
            Equal("Column", all.CategoryFacets[1].Key);
        }

        private static void SnapshotComparisonClassifiesRows()
        {
            var baseline = CreateSnapshot("baseline", ProjectWithRules(
                "P-REVIEW",
                new RuleSpec("R-A", "A", "2"),
                new RuleSpec("R-B", "B", "5"),
                new RuleSpec("R-D", "D", "9")));
            var candidate = CreateSnapshot("candidate", ProjectWithRules(
                "P-REVIEW",
                new RuleSpec("R-A", "A", "3"),
                new RuleSpec("R-C", "C", "7"),
                new RuleSpec("R-D", "D", "9")));

            var diff = new PreviewReviewSnapshotComparisonService().Compare(baseline, candidate);

            True(diff.HasChanges);
            Equal(1, diff.AddedCount);
            Equal(1, diff.RemovedCount);
            Equal(1, diff.ChangedCount);
            Equal(1, diff.UnchangedCount);
            Equal(0, diff.SummaryChanges.Count);
            Equal(PreviewReviewDeltaKind.Changed, diff.Rows.Single(x => x.Field == "Quantity:A").Kind);
            Equal(PreviewReviewDeltaKind.Removed, diff.Rows.Single(x => x.Field == "Quantity:B").Kind);
            Equal(PreviewReviewDeltaKind.Added, diff.Rows.Single(x => x.Field == "Quantity:C").Kind);
            Equal(PreviewReviewDeltaKind.Unchanged, diff.Rows.Single(x => x.Field == "Quantity:D").Kind);
        }

        private static void ComparisonRejectsDifferentProjects()
        {
            var baseline = CreateSnapshot("left", ProjectWithRules("P-LEFT", new RuleSpec("R-A", "A", "2")));
            var candidate = CreateSnapshot("right", ProjectWithRules("P-RIGHT", new RuleSpec("R-A", "A", "2")));
            Throws<InvalidOperationException>(() => new PreviewReviewSnapshotComparisonService().Compare(baseline, candidate));
        }

        private static void ResultCollectionsAreImmutable()
        {
            var snapshot = CreateSnapshot("immutable", ProjectWithRules("P-REVIEW", new RuleSpec("R-A", "A", "2")));
            var query = new PreviewReviewQueryService().Query(snapshot);
            Throws<NotSupportedException>(() => ((IList<PreviewReviewEntry>)query.Entries).Clear());
            Throws<NotSupportedException>(() => ((IList<PreviewReviewFacet>)query.ChangeFacets).Clear());

            var diff = new PreviewReviewSnapshotComparisonService().Compare(snapshot, snapshot);
            Throws<NotSupportedException>(() => ((IList<PreviewReviewRowDelta>)diff.Rows).Clear());
            Throws<NotSupportedException>(() => ((IList<PreviewReviewSummaryDelta>)diff.SummaryChanges).Clear());
        }

        private static ProjectState ProjectWithRules(string projectId, params RuleSpec[] rules)
        {
            var project = new ProjectState(projectId, "Review");
            project.Elements.Add(new ProjectElement("E-1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));
            foreach (var rule in rules)
                project.QuantityRules.Add(new QuantityRule(rule.Id, ElementCategory.Beam, rule.Output, rule.Expression, "1"));
            return project;
        }

        private static PreviewReviewSnapshot CreateSnapshot(string name, ProjectState project)
        {
            var preview = new QuantityRulePreviewService().PreviewProject(project);
            return new PreviewReviewSnapshotService().Create(name, preview);
        }

        private sealed class RuleSpec
        {
            public RuleSpec(string id, string output, string expression)
            {
                Id = id;
                Output = output;
                Expression = expression;
            }

            public string Id { get; }
            public string Output { get; }
            public string Expression { get; }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
