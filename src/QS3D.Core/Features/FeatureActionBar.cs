using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public enum FeatureActionId
    {
        Add,
        EditParameters,
        Material,
        Quantity,
        Regenerate,
        Locate,
        Delete,
        Geometry3D
    }

    public enum FeatureActionPlacement
    {
        Primary,
        Secondary,
        Overflow
    }

    public sealed class FeatureActionAvailability
    {
        public FeatureActionAvailability(FeatureActionId actionId, bool isEnabled, string? disabledReason = null)
        {
            if (!Enum.IsDefined(typeof(FeatureActionId), actionId))
                throw new ArgumentOutOfRangeException(nameof(actionId), "Feature action id is not supported.");
            if (!isEnabled && string.IsNullOrWhiteSpace(disabledReason))
                throw new ArgumentException("A disabled feature action must explain its missing precondition.", nameof(disabledReason));

            ActionId = actionId;
            IsEnabled = isEnabled;
            DisabledReason = isEnabled ? null : disabledReason!.Trim();
        }

        public FeatureActionId ActionId { get; }
        public bool IsEnabled { get; }
        public string? DisabledReason { get; }
    }

    public sealed class FeatureActionItem
    {
        internal FeatureActionItem(
            FeatureActionId id,
            FeatureActionPlacement placement,
            int order,
            string labelKey,
            string accessKey,
            string toolTipKey,
            string statusHintKey,
            bool isEnabled,
            string? disabledReason,
            string? primaryRecipeId,
            IReadOnlyList<string> alternateRecipeIds)
        {
            Id = id;
            Placement = placement;
            Order = order;
            LabelKey = labelKey;
            AccessKey = accessKey;
            ToolTipKey = toolTipKey;
            StatusHintKey = statusHintKey;
            IsEnabled = isEnabled;
            DisabledReason = disabledReason;
            PrimaryRecipeId = primaryRecipeId;
            AlternateRecipeIds = alternateRecipeIds;
        }

        public FeatureActionId Id { get; }
        public FeatureActionPlacement Placement { get; }
        public int Order { get; }
        public string LabelKey { get; }
        public string AccessKey { get; }
        public string ToolTipKey { get; }
        public string StatusHintKey { get; }
        public bool IsEnabled { get; }
        public string? DisabledReason { get; }
        public string? PrimaryRecipeId { get; }
        public IReadOnlyList<string> AlternateRecipeIds { get; }
        public bool HasAlternateRecipes => AlternateRecipeIds.Count > 0;
    }

    public sealed class FeatureActionBarSnapshot
    {
        internal FeatureActionBarSnapshot(IEnumerable<FeatureActionItem> actions)
        {
            var ordered = actions.OrderBy(x => x.Order).ThenBy(x => x.Id).ToArray();
            Actions = new ReadOnlyCollection<FeatureActionItem>(ordered);
            Primary = new ReadOnlyCollection<FeatureActionItem>(ordered.Where(x => x.Placement == FeatureActionPlacement.Primary).ToArray());
            Secondary = new ReadOnlyCollection<FeatureActionItem>(ordered.Where(x => x.Placement == FeatureActionPlacement.Secondary).ToArray());
            Overflow = new ReadOnlyCollection<FeatureActionItem>(ordered.Where(x => x.Placement == FeatureActionPlacement.Overflow).ToArray());
        }

        public IReadOnlyList<FeatureActionItem> Actions { get; }
        public IReadOnlyList<FeatureActionItem> Primary { get; }
        public IReadOnlyList<FeatureActionItem> Secondary { get; }
        public IReadOnlyList<FeatureActionItem> Overflow { get; }
    }

    public static class FeatureActionBarBuilder
    {
        private sealed class Definition
        {
            public Definition(
                FeatureActionId id,
                FeatureCapability capability,
                FeatureActionPlacement placement,
                int order,
                string labelKey,
                string accessKey,
                string toolTipKey,
                string statusHintKey)
            {
                Id = id;
                Capability = capability;
                Placement = placement;
                Order = order;
                LabelKey = labelKey;
                AccessKey = accessKey;
                ToolTipKey = toolTipKey;
                StatusHintKey = statusHintKey;
            }

            public FeatureActionId Id { get; }
            public FeatureCapability Capability { get; }
            public FeatureActionPlacement Placement { get; }
            public int Order { get; }
            public string LabelKey { get; }
            public string AccessKey { get; }
            public string ToolTipKey { get; }
            public string StatusHintKey { get; }
        }

        private static readonly Definition[] Definitions =
        {
            new Definition(FeatureActionId.Add, FeatureCapability.Create, FeatureActionPlacement.Primary, 0,
                "FeatureAction.Add", "A", "FeatureAction.Add.ToolTip", "FeatureAction.Add.StatusHint"),
            new Definition(FeatureActionId.EditParameters, FeatureCapability.EditParameters, FeatureActionPlacement.Secondary, 10,
                "FeatureAction.EditParameters", "P", "FeatureAction.EditParameters.ToolTip", "FeatureAction.EditParameters.StatusHint"),
            new Definition(FeatureActionId.Material, FeatureCapability.Material, FeatureActionPlacement.Secondary, 20,
                "FeatureAction.Material", "M", "FeatureAction.Material.ToolTip", "FeatureAction.Material.StatusHint"),
            new Definition(FeatureActionId.Geometry3D, FeatureCapability.Geometry3D, FeatureActionPlacement.Secondary, 30,
                "FeatureAction.Geometry3D", "G", "FeatureAction.Geometry3D.ToolTip", "FeatureAction.Geometry3D.StatusHint"),
            new Definition(FeatureActionId.Quantity, FeatureCapability.Quantity, FeatureActionPlacement.Secondary, 40,
                "FeatureAction.Quantity", "Q", "FeatureAction.Quantity.ToolTip", "FeatureAction.Quantity.StatusHint"),
            new Definition(FeatureActionId.Regenerate, FeatureCapability.Regenerate, FeatureActionPlacement.Secondary, 50,
                "FeatureAction.Regenerate", "R", "FeatureAction.Regenerate.ToolTip", "FeatureAction.Regenerate.StatusHint"),
            new Definition(FeatureActionId.Locate, FeatureCapability.Locate, FeatureActionPlacement.Secondary, 60,
                "FeatureAction.Locate", "L", "FeatureAction.Locate.ToolTip", "FeatureAction.Locate.StatusHint"),
            new Definition(FeatureActionId.Delete, FeatureCapability.Delete, FeatureActionPlacement.Overflow, 90,
                "FeatureAction.Delete", "D", "FeatureAction.Delete.ToolTip", "FeatureAction.Delete.StatusHint")
        };

        public static FeatureActionBarSnapshot Build(
            InteractionProfile profile,
            IEnumerable<FeatureActionAvailability>? availability = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var byAction = SnapshotAvailability(availability);
            var actions = new List<FeatureActionItem>();
            foreach (var definition in Definitions)
            {
                if ((profile.Capabilities & definition.Capability) == 0)
                    continue;

                var state = ResolveAvailability(definition.Id, byAction);
                string? primaryRecipeId = null;
                IReadOnlyList<string> alternateRecipeIds = Array.Empty<string>();
                if (definition.Id == FeatureActionId.Add)
                {
                    primaryRecipeId = profile.PrimaryRecipeId;
                    alternateRecipeIds = new ReadOnlyCollection<string>(profile.Recipes
                        .Where(x => !string.Equals(x.Id, profile.PrimaryRecipeId, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Id)
                        .ToArray());
                }

                actions.Add(new FeatureActionItem(
                    definition.Id,
                    definition.Placement,
                    definition.Order,
                    definition.LabelKey,
                    definition.AccessKey,
                    definition.ToolTipKey,
                    definition.StatusHintKey,
                    state.IsEnabled,
                    state.DisabledReason,
                    primaryRecipeId,
                    alternateRecipeIds));
            }

            var snapshot = new FeatureActionBarSnapshot(actions);
            if ((profile.Capabilities & FeatureCapability.Create) != 0 && snapshot.Primary.Count != 1)
                throw new InvalidOperationException("Creation-oriented features must expose exactly one primary Add action.");
            return snapshot;
        }

        private static Dictionary<FeatureActionId, FeatureActionAvailability> SnapshotAvailability(
            IEnumerable<FeatureActionAvailability>? availability)
        {
            var result = new Dictionary<FeatureActionId, FeatureActionAvailability>();
            if (availability == null) return result;

            var expectedCount = SnapshotKnownCount(availability);
            var observed = 0;
            using (var enumerator = availability.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (observed == Definitions.Length)
                        throw TooManyAvailabilityStates();
                    if (expectedCount.HasValue && observed >= expectedCount.Value)
                        throw AvailabilityCountMismatch(expectedCount.Value, observed + 1);

                    var state = enumerator.Current;
                    observed++;
                    if (state == null) throw new InvalidOperationException("Feature action availability cannot contain null values.");
                    if (!Enum.IsDefined(typeof(FeatureActionId), state.ActionId))
                        throw new InvalidOperationException("Feature action availability contains an unsupported action id: " + state.ActionId + ".");
                    if (result.ContainsKey(state.ActionId))
                        throw new InvalidOperationException("Feature action availability contains duplicate action ids: " + state.ActionId);
                    result.Add(state.ActionId, state);
                }
            }

            if (expectedCount.HasValue && observed != expectedCount.Value)
                throw AvailabilityCountMismatch(expectedCount.Value, observed);

            var reboundCount = SnapshotKnownCount(availability);
            if (expectedCount != reboundCount)
                throw new InvalidOperationException("Feature action availability count changed during enumeration.");
            return result;
        }

        private static int? SnapshotKnownCount(IEnumerable<FeatureActionAvailability> availability)
        {
            int? expected = null;
            if (availability is ICollection<FeatureActionAvailability> genericCollection)
                AcceptKnownCount(genericCollection.Count, ref expected);
            if (availability is IReadOnlyCollection<FeatureActionAvailability> readOnlyCollection)
                AcceptKnownCount(readOnlyCollection.Count, ref expected);
            if (availability is System.Collections.ICollection nonGenericCollection)
                AcceptKnownCount(nonGenericCollection.Count, ref expected);
            return expected;
        }

        private static void AcceptKnownCount(int count, ref int? expected)
        {
            if (count < 0)
                throw new InvalidOperationException("Feature action availability exposes an invalid negative count.");
            if (count > Definitions.Length)
                throw TooManyAvailabilityStates();
            if (expected.HasValue && expected.Value != count)
                throw new InvalidOperationException("Feature action availability exposes conflicting known counts.");
            expected = count;
        }

        private static InvalidOperationException AvailabilityCountMismatch(int reportedCount, int observedCount) =>
            new InvalidOperationException(
                "Feature action availability count changed during enumeration; Count reported " + reportedCount +
                " states but traversal observed " + observedCount + ".");

        private static InvalidOperationException TooManyAvailabilityStates() =>
            new InvalidOperationException("Feature action availability supports at most " + Definitions.Length + " states.");

        private static FeatureActionAvailability ResolveAvailability(
            FeatureActionId actionId,
            IReadOnlyDictionary<FeatureActionId, FeatureActionAvailability> availability)
        {
            if (availability.TryGetValue(actionId, out var state))
                return state;
            return new FeatureActionAvailability(actionId, true);
        }
    }
}
