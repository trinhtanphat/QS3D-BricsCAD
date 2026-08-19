using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public readonly struct FeatureId : IEquatable<FeatureId>, IComparable<FeatureId>
    {
        public FeatureId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("FeatureId cannot be blank.", nameof(value));
            Value = value.Trim().ToLowerInvariant();
            for (var i = 0; i < Value.Length; i++)
            {
                var c = Value[i];
                if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'))
                    throw new ArgumentException("FeatureId may contain only letters, digits, '.', '-' and '_'.", nameof(value));
            }
        }

        public string Value { get; }
        public int CompareTo(FeatureId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public bool Equals(FeatureId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is FeatureId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(FeatureId left, FeatureId right) => left.Equals(right);
        public static bool operator !=(FeatureId left, FeatureId right) => !left.Equals(right);
    }

    public enum FeatureOnSelectBehavior { SelectContext, SelectAndRefresh }
    public enum CreateInputMode { Direct, ChooseRecipe, FormThenCreate, FormThenPick, PickThenForm, Wizard }
    public enum InteractionSurface { PrimaryInspector, SecondaryInspector, ModalSheet, RecipeChooser, FloatingTool }

    [Flags]
    public enum FeatureCapability
    {
        None = 0,
        Create = 1,
        EditParameters = 2,
        Material = 4,
        Quantity = 8,
        Regenerate = 16,
        Locate = 32,
        Delete = 64
    }

    public sealed class CreateRecipeDescriptor
    {
        public CreateRecipeDescriptor(string id, CreateInputMode inputMode, string schemaKey = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Recipe id cannot be blank.", nameof(id));
            Id = id.Trim();
            InputMode = inputMode;
            SchemaKey = NormalizeOptional(schemaKey);
        }

        public string Id { get; }
        public CreateInputMode InputMode { get; }
        public string SchemaKey { get; }
        public bool RequiresForm => InputMode == CreateInputMode.FormThenCreate || InputMode == CreateInputMode.FormThenPick || InputMode == CreateInputMode.PickThenForm || InputMode == CreateInputMode.Wizard;
        private static string NormalizeOptional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class InteractionProfile
    {
        public InteractionProfile(
            FeatureOnSelectBehavior onSelect,
            IEnumerable<CreateRecipeDescriptor> recipes,
            string primaryRecipeId,
            IEnumerable<InteractionSurface> persistentSurfaces,
            FeatureCapability capabilities,
            bool allowsModal = false,
            bool allowsFloatingTool = false,
            string propertySchemaKey = null,
            string dependencyPolicyKey = null,
            string semanticMappingKey = null)
        {
            OnSelect = onSelect;
            Recipes = Snapshot(recipes);
            PrimaryRecipeId = NormalizeOptional(primaryRecipeId);
            PersistentSurfaces = Snapshot(persistentSurfaces);
            Capabilities = capabilities;
            AllowsModal = allowsModal;
            AllowsFloatingTool = allowsFloatingTool;
            PropertySchemaKey = NormalizeOptional(propertySchemaKey);
            DependencyPolicyKey = NormalizeOptional(dependencyPolicyKey);
            SemanticMappingKey = NormalizeOptional(semanticMappingKey);
            Validate();
        }

        public FeatureOnSelectBehavior OnSelect { get; }
        public IReadOnlyList<CreateRecipeDescriptor> Recipes { get; }
        public string PrimaryRecipeId { get; }
        public IReadOnlyList<InteractionSurface> PersistentSurfaces { get; }
        public FeatureCapability Capabilities { get; }
        public bool AllowsModal { get; }
        public bool AllowsFloatingTool { get; }
        public string PropertySchemaKey { get; }
        public string DependencyPolicyKey { get; }
        public string SemanticMappingKey { get; }

        private void Validate()
        {
            if (Recipes.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new InvalidOperationException("InteractionProfile contains duplicate create recipe ids.");
            if ((Capabilities & FeatureCapability.Create) != 0 && Recipes.Count == 0)
                throw new InvalidOperationException("Create capability requires at least one create recipe.");
            if (Recipes.Count > 0 && string.IsNullOrWhiteSpace(PrimaryRecipeId))
                throw new InvalidOperationException("A profile with create recipes requires a primary recipe id.");
            if (!string.IsNullOrWhiteSpace(PrimaryRecipeId) && !Recipes.Any(x => string.Equals(x.Id, PrimaryRecipeId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Primary create recipe must exist in the recipe set.");
            if (Recipes.Any(x => x.RequiresForm && string.IsNullOrWhiteSpace(x.SchemaKey)))
                throw new InvalidOperationException("Form-driven create recipes require a schema key.");
            if (PersistentSurfaces.Count > 2)
                throw new InvalidOperationException("Normal Workspace interaction profiles support at most two persistent surfaces.");
            if (PersistentSurfaces.Any(x => x != InteractionSurface.PrimaryInspector && x != InteractionSurface.SecondaryInspector))
                throw new InvalidOperationException("Only primary/secondary inspector surfaces may be persistent.");
            if (!AllowsModal && Recipes.Any(x => x.RequiresForm || x.InputMode == CreateInputMode.ChooseRecipe))
                throw new InvalidOperationException("Profiles with chooser/form/wizard recipes must explicitly allow modal interaction.");
        }

        private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source)
        {
            return new ReadOnlyCollection<T>((source ?? Enumerable.Empty<T>()).ToArray());
        }

        private static string NormalizeOptional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class FeatureDescriptor
    {
        public FeatureDescriptor(FeatureId id, string groupKey, int order, string labelKey, InteractionProfile interactionProfile, string iconKey = null)
        {
            if (string.IsNullOrWhiteSpace(groupKey)) throw new ArgumentException("Group key cannot be blank.", nameof(groupKey));
            if (string.IsNullOrWhiteSpace(labelKey)) throw new ArgumentException("Label key cannot be blank.", nameof(labelKey));
            Id = id;
            GroupKey = groupKey.Trim();
            Order = order;
            LabelKey = labelKey.Trim();
            IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey.Trim();
            InteractionProfile = interactionProfile ?? throw new ArgumentNullException(nameof(interactionProfile));
        }

        public FeatureId Id { get; }
        public string GroupKey { get; }
        public int Order { get; }
        public string LabelKey { get; }
        public string IconKey { get; }
        public InteractionProfile InteractionProfile { get; }
    }

    public sealed class FeatureRegistry
    {
        private readonly IReadOnlyList<FeatureDescriptor> _descriptors;
        private readonly Dictionary<FeatureId, FeatureDescriptor> _byId;

        public FeatureRegistry(IEnumerable<FeatureDescriptor> descriptors)
        {
            if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));
            var materialized = descriptors.ToArray();
            if (materialized.Any(x => x == null)) throw new InvalidOperationException("Feature registry cannot contain null descriptors.");
            if (materialized.GroupBy(x => x.Id).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Feature registry contains duplicate FeatureId values.");

            _descriptors = new ReadOnlyCollection<FeatureDescriptor>(materialized
                .OrderBy(x => x.GroupKey, StringComparer.Ordinal)
                .ThenBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToArray());
            _byId = materialized.ToDictionary(x => x.Id);
        }

        public IReadOnlyList<FeatureDescriptor> Descriptors => _descriptors;

        public bool TryGet(FeatureId id, out FeatureDescriptor descriptor) => _byId.TryGetValue(id, out descriptor);

        public FeatureDescriptor GetRequired(FeatureId id)
        {
            if (!_byId.TryGetValue(id, out var descriptor))
                throw new KeyNotFoundException("Feature is not registered: " + id);
            return descriptor;
        }
    }
}
