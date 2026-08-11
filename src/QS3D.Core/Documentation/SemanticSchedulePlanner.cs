using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticScheduleDefinition
    {
        public SemanticScheduleDefinition(
            string id,
            string name,
            string viewId,
            IEnumerable<SemanticDocumentationColumn> columns)
        {
            Id = id;
            Name = name;
            ViewId = viewId;
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            Columns = new List<SemanticDocumentationColumn>(columns).AsReadOnly();
        }

        public string Id { get; }
        public string Name { get; }
        public string ViewId { get; }
        public IReadOnlyList<SemanticDocumentationColumn> Columns { get; }
    }

    public sealed class SemanticSchedulePlan
    {
        internal SemanticSchedulePlan(
            string id,
            string name,
            string viewId,
            IReadOnlyList<string> elementIds,
            IReadOnlyList<SemanticDocumentationColumn> columns)
        {
            Id = id;
            Name = name;
            ViewId = viewId;
            ElementIds = new List<string>(elementIds).AsReadOnly();
            Columns = new List<SemanticDocumentationColumn>(columns).AsReadOnly();
        }

        public string Id { get; }
        public string Name { get; }
        public string ViewId { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<SemanticDocumentationColumn> Columns { get; }
    }

    internal static class SemanticDocumentationColumnPolicy
    {
        internal const int MaxColumns = 32;
        internal const int MaxHeaderLength = 96;
        internal const int MaxTemplateLength = 512;

        internal static IReadOnlyList<SemanticDocumentationColumn> Normalize(
            IEnumerable<SemanticDocumentationColumn> columns,
            string parameterName)
        {
            if (columns == null) throw new ArgumentNullException(parameterName);
            var materialized = new List<SemanticDocumentationColumn>();
            using (var enumerator = columns.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (materialized.Count >= MaxColumns)
                        throw new InvalidOperationException("Semantic documentation supports at most " + MaxColumns + " columns.");
                    materialized.Add(enumerator.Current);
                }
            }
            if (materialized.Count == 0)
                throw new InvalidOperationException("Semantic documentation requires at least one column.");

            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<SemanticDocumentationColumn>(materialized.Count);
            for (var index = 0; index < materialized.Count; index++)
            {
                var column = materialized[index]
                    ?? throw new ArgumentException("Semantic documentation column cannot be null at index " + index + ".", parameterName);
                var header = Required(column.Header, parameterName + "[" + index + "].Header", MaxHeaderLength);
                var template = Required(column.Template, parameterName + "[" + index + "].Template", MaxTemplateLength);
                if (!headers.Add(header))
                    throw new InvalidOperationException("Semantic documentation contains duplicate column header: " + header + ".");
                result.Add(new SemanticDocumentationColumn(header, template));
            }
            return result.AsReadOnly();
        }

        private static string Required(string value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }
    }

    public static class SemanticSchedulePlanner
    {
        private const int MaxSchedules = 2000;
        private const int MaxIdLength = 128;
        private const int MaxNameLength = 160;

        public static SemanticSchedulePlan Build(
            ProjectState project,
            SemanticScheduleDefinition definition,
            IEnumerable<SemanticViewPlan> viewPlans)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (viewPlans == null) throw new ArgumentNullException(nameof(viewPlans));

            var id = Required(definition.Id, nameof(definition.Id), MaxIdLength);
            var name = Required(definition.Name, nameof(definition.Name), MaxNameLength);
            var viewId = Required(definition.ViewId, nameof(definition.ViewId), MaxIdLength);
            var columns = SemanticDocumentationColumnPolicy.Normalize(definition.Columns, nameof(definition.Columns));

            var matchingViews = viewPlans
                .Where(x => x != null && string.Equals(x.Id, viewId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matchingViews.Length == 0)
                throw new InvalidOperationException("Semantic schedule references missing semantic view id: " + viewId + ".");
            if (matchingViews.Length > 1)
                throw new InvalidOperationException("Semantic schedule references ambiguous semantic view id: " + viewId + ".");

            var view = matchingViews[0];
            if (view.Kind != SemanticViewKind.Schedule)
                throw new InvalidOperationException("Semantic schedule view must use SemanticViewKind.Schedule: " + view.Id + ".");

            return new SemanticSchedulePlan(id, name, view.Id, view.ElementIds, columns);
        }

        public static IReadOnlyList<SemanticSchedulePlan> BuildCatalog(
            ProjectState project,
            IEnumerable<SemanticScheduleDefinition> definitions,
            IEnumerable<SemanticViewPlan> viewPlans)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (viewPlans == null) throw new ArgumentNullException(nameof(viewPlans));

            var materialized = definitions.ToList();
            if (materialized.Count > MaxSchedules)
                throw new InvalidOperationException("Semantic documentation catalog supports at most " + MaxSchedules + " schedules.");
            var views = viewPlans.ToList();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plans = new List<SemanticSchedulePlan>(materialized.Count);

            foreach (var definition in materialized)
            {
                if (definition == null) throw new ArgumentException("Semantic schedule definition cannot be null.", nameof(definitions));
                var plan = Build(project, definition, views);
                if (!ids.Add(plan.Id)) throw new InvalidOperationException("Semantic schedule catalog contains duplicate schedule id: " + plan.Id + ".");
                if (!names.Add(plan.Name)) throw new InvalidOperationException("Semantic schedule catalog contains duplicate schedule name: " + plan.Name + ".");
                plans.Add(plan);
            }

            return plans
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static SemanticDocumentationTable BuildTable(
            ProjectState project,
            SemanticScheduleDefinition definition,
            IEnumerable<SemanticViewDefinition> views)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (views == null) throw new ArgumentNullException(nameof(views));
            var viewPlans = SemanticViewPlanner.BuildCatalog(project, views);
            var plan = Build(project, definition, viewPlans);
            return SemanticDocumentationTableBuilder.Build(project, plan.Name, plan.ElementIds, plan.Columns, allowEmpty: true);
        }

        private static string Required(string value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            var normalized = value.Trim();
            if (normalized.Length > maxLength) throw new ArgumentException("Value exceeds " + maxLength + " characters.", name);
            return normalized;
        }
    }
}
