using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Features
{
    public enum WorkspaceSchemaFieldKind
    {
        Number,
        Choice,
        Boolean,
        Text,
        Reference
    }

    [Flags]
    public enum WorkspaceSchemaApplicability
    {
        None = 0,
        Create = 1,
        Edit = 2,
        CreateAndEdit = Create | Edit
    }

    public enum WorkspaceSchemaSurface
    {
        CreateForm,
        Inspector
    }

    public sealed class WorkspaceSchemaCondition
    {
        public WorkspaceSchemaCondition(string fieldKey, object? expectedValue)
        {
            if (string.IsNullOrWhiteSpace(fieldKey)) throw new ArgumentException("Condition field key cannot be blank.", nameof(fieldKey));
            FieldKey = fieldKey.Trim();
            ExpectedValue = expectedValue;
        }

        public string FieldKey { get; }
        public object? ExpectedValue { get; }

        public bool Matches(IReadOnlyDictionary<string, object>? values)
        {
            if (values == null || !values.TryGetValue(FieldKey, out var actual)) return false;
            return EqualsNormalized(actual, ExpectedValue);
        }

        private static bool EqualsNormalized(object? left, object? right)
        {
            if (left == null || right == null) return left == null && right == null;
            if (left is string || right is string)
                return string.Equals(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
            return object.Equals(left, right);
        }
    }

    public sealed class WorkspaceSchemaField
    {
        public WorkspaceSchemaField(
            string key,
            WorkspaceSchemaFieldKind kind,
            bool required = false,
            object? defaultValue = null,
            double? minimum = null,
            double? maximum = null,
            string? unit = null,
            int? precision = null,
            IEnumerable<string>? choices = null,
            bool readOnly = false,
            bool computed = false,
            WorkspaceSchemaCondition? visibleWhen = null,
            WorkspaceSchemaCondition? enabledWhen = null,
            string? groupKey = null,
            int order = 0,
            string? helpText = null,
            WorkspaceSchemaApplicability applicability = WorkspaceSchemaApplicability.CreateAndEdit)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Schema field key cannot be blank.", nameof(key));
            if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value) throw new ArgumentException("Minimum cannot exceed maximum.");
            if (precision.HasValue && precision.Value < 0) throw new ArgumentOutOfRangeException(nameof(precision));
            Key = key.Trim();
            Kind = kind;
            Required = required;
            DefaultValue = defaultValue;
            Minimum = minimum;
            Maximum = maximum;
            Unit = Normalize(unit);
            Precision = precision;
            Choices = new ReadOnlyCollection<string>((choices ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
            ReadOnly = readOnly;
            Computed = computed;
            VisibleWhen = visibleWhen;
            EnabledWhen = enabledWhen;
            GroupKey = Normalize(groupKey);
            Order = order;
            HelpText = Normalize(helpText);
            Applicability = applicability;
            ValidateShape();
        }

        public string Key { get; }
        public WorkspaceSchemaFieldKind Kind { get; }
        public bool Required { get; }
        public object? DefaultValue { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }
        public string? Unit { get; }
        public int? Precision { get; }
        public IReadOnlyList<string> Choices { get; }
        public bool ReadOnly { get; }
        public bool Computed { get; }
        public WorkspaceSchemaCondition? VisibleWhen { get; }
        public WorkspaceSchemaCondition? EnabledWhen { get; }
        public string? GroupKey { get; }
        public int Order { get; }
        public string? HelpText { get; }
        public WorkspaceSchemaApplicability Applicability { get; }

        public bool AppliesTo(WorkspaceSchemaSurface surface)
        {
            var flag = surface == WorkspaceSchemaSurface.CreateForm ? WorkspaceSchemaApplicability.Create : WorkspaceSchemaApplicability.Edit;
            return (Applicability & flag) != 0;
        }

        private void ValidateShape()
        {
            if (Kind != WorkspaceSchemaFieldKind.Number && (Minimum.HasValue || Maximum.HasValue || Precision.HasValue || !string.IsNullOrWhiteSpace(Unit)))
                throw new InvalidOperationException("Numeric range/unit/precision metadata requires a Number field: " + Key);
            if (Kind == WorkspaceSchemaFieldKind.Choice && Choices.Count == 0)
                throw new InvalidOperationException("Choice fields require at least one choice: " + Key);
            if (Kind != WorkspaceSchemaFieldKind.Choice && Choices.Count > 0)
                throw new InvalidOperationException("Only Choice fields may declare choices: " + Key);
            if (Computed && !ReadOnly)
                throw new InvalidOperationException("Computed fields must be read-only: " + Key);
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class WorkspaceFormSchema
    {
        public WorkspaceFormSchema(string key, IEnumerable<WorkspaceSchemaField>? fields)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Schema key cannot be blank.", nameof(key));
            Key = key.Trim();
            var snapshot = (fields ?? Enumerable.Empty<WorkspaceSchemaField>()).ToArray();
            if (snapshot.Any(x => x == null)) throw new InvalidOperationException("Schema cannot contain null fields.");
            if (snapshot.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                throw new InvalidOperationException("Schema contains duplicate field keys.");
            Fields = new ReadOnlyCollection<WorkspaceSchemaField>(snapshot.OrderBy(x => x.GroupKey, StringComparer.Ordinal).ThenBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray());
        }

        public string Key { get; }
        public IReadOnlyList<WorkspaceSchemaField> Fields { get; }
    }

    public sealed class WorkspaceSchemaValidationMessage
    {
        public WorkspaceSchemaValidationMessage(string? fieldKey, string? message)
        {
            FieldKey = fieldKey ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string FieldKey { get; }
        public string Message { get; }
    }

    public sealed class WorkspaceSchemaRenderField
    {
        internal WorkspaceSchemaRenderField(WorkspaceSchemaField field, bool visible, bool enabled)
        {
            Field = field;
            Visible = visible;
            Enabled = enabled;
        }

        public WorkspaceSchemaField Field { get; }
        public bool Visible { get; }
        public bool Enabled { get; }
        public bool ReadOnly => Field.ReadOnly || Field.Computed;
    }

    public static class WorkspaceSchemaRenderer
    {
        public static IReadOnlyList<WorkspaceSchemaRenderField> Plan(
            WorkspaceFormSchema schema,
            WorkspaceSchemaSurface surface,
            IReadOnlyDictionary<string, object>? values)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            values = values ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var result = new List<WorkspaceSchemaRenderField>();
            foreach (var field in schema.Fields.Where(x => x.AppliesTo(surface)))
            {
                EnsureSupported(field);
                var visible = field.VisibleWhen == null || field.VisibleWhen.Matches(values);
                var enabled = visible && !field.ReadOnly && !field.Computed && (field.EnabledWhen == null || field.EnabledWhen.Matches(values));
                result.Add(new WorkspaceSchemaRenderField(field, visible, enabled));
            }
            return new ReadOnlyCollection<WorkspaceSchemaRenderField>(result);
        }

        private static void EnsureSupported(WorkspaceSchemaField field)
        {
            switch (field.Kind)
            {
                case WorkspaceSchemaFieldKind.Number:
                case WorkspaceSchemaFieldKind.Choice:
                case WorkspaceSchemaFieldKind.Boolean:
                case WorkspaceSchemaFieldKind.Text:
                case WorkspaceSchemaFieldKind.Reference:
                    return;
                default:
                    throw new NotSupportedException("Unsupported Workspace schema field kind for '" + field.Key + "': " + field.Kind);
            }
        }
    }

    public static class WorkspaceSchemaValidator
    {
        public static IReadOnlyList<WorkspaceSchemaValidationMessage> Validate(
            WorkspaceFormSchema schema,
            WorkspaceSchemaSurface surface,
            IReadOnlyDictionary<string, object>? values)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            values = values ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var rendered = WorkspaceSchemaRenderer.Plan(schema, surface, values);
            var messages = new List<WorkspaceSchemaValidationMessage>();

            foreach (var row in rendered.Where(x => x.Visible))
            {
                var field = row.Field;
                values.TryGetValue(field.Key, out var raw);
                object? candidate = raw;
                if (IsMissing(candidate)) candidate = field.DefaultValue;

                if (field.Required && IsMissing(candidate))
                {
                    messages.Add(new WorkspaceSchemaValidationMessage(field.Key, field.Key + " is required."));
                    continue;
                }
                if (IsMissing(candidate)) continue;

                if (field.Kind == WorkspaceSchemaFieldKind.Number)
                {
                    if (!TryFiniteDouble(candidate, out var number))
                    {
                        messages.Add(new WorkspaceSchemaValidationMessage(field.Key, field.Key + " must be a finite number."));
                        continue;
                    }
                    if (field.Minimum.HasValue && number < field.Minimum.Value)
                        messages.Add(new WorkspaceSchemaValidationMessage(field.Key, field.Key + " must be at least " + field.Minimum.Value.ToString(CultureInfo.InvariantCulture) + "."));
                    if (field.Maximum.HasValue && number > field.Maximum.Value)
                        messages.Add(new WorkspaceSchemaValidationMessage(field.Key, field.Key + " must be at most " + field.Maximum.Value.ToString(CultureInfo.InvariantCulture) + "."));
                }
                else if (field.Kind == WorkspaceSchemaFieldKind.Choice)
                {
                    var text = Convert.ToString(candidate, CultureInfo.InvariantCulture) ?? string.Empty;
                    if (!field.Choices.Contains(text, StringComparer.OrdinalIgnoreCase))
                        messages.Add(new WorkspaceSchemaValidationMessage(field.Key, field.Key + " must be one of the declared choices."));
                }
                else if (field.Kind == WorkspaceSchemaFieldKind.Boolean && !(candidate is bool))
                {
                    messages.Add(new WorkspaceSchemaValidationMessage(field.Key, field.Key + " must be a boolean."));
                }
            }
            return new ReadOnlyCollection<WorkspaceSchemaValidationMessage>(messages);
        }

        private static bool IsMissing(object? value) => value == null || (value is string text && string.IsNullOrWhiteSpace(text));

        private static bool TryFiniteDouble(object? value, out double number)
        {
            if (value is double d) number = d;
            else if (value is float f) number = f;
            else if (value is decimal m) number = (double)m;
            else if (!double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
    }

    public static class ProjectFamilyQuickSchemaAdapter
    {
        public static WorkspaceFormSchema Create(ElementCategory category, string? schemaKey = null)
        {
            var source = ProjectFamilyQuickSchemaService.GetSchema(category);
            if (!source.SupportsQuickForm)
                throw new InvalidOperationException("No explicit ProjectFamily quick schema exists for category: " + category);

            var fields = source.FormKeys.Select((key, index) => new WorkspaceSchemaField(
                key,
                WorkspaceSchemaFieldKind.Number,
                required: source.IsIdentityKey(key),
                defaultValue: source.DefaultsM.TryGetValue(key, out var defaultMeters) ? (object)defaultMeters : null,
                minimum: IsPositiveDimension(key) ? 0d : (double?)null,
                unit: "m",
                precision: 3,
                groupKey: "geometry",
                order: index,
                helpText: "Adapted from ProjectFamilyQuickSchemaService for " + category + ".",
                applicability: WorkspaceSchemaApplicability.CreateAndEdit));

            return new WorkspaceFormSchema(schemaKey ?? ("project-family." + category.ToString().ToLowerInvariant()), fields);
        }

        private static bool IsPositiveDimension(string key)
        {
            return !string.Equals(key, "BottomOffsetM", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(key, "TopOffsetM", StringComparison.OrdinalIgnoreCase);
        }
    }
}
