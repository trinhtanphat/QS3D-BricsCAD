using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureActionBarSmoke
    {
        public static void Run()
        {
            CapabilitiesDriveVisibilityAndStableOrdering();
            AlternateRecipesDriveSplitAddOnlyWhenNeeded();
            DisabledActionsExplainMissingPreconditions();
            SimpleFeatureRemainsSimple();
            PilotProfilesExposeGeometry3DCapability();
            ActionMetadataIsConsistent();
            AvailabilityExactBoundaryIsAccepted();
            OversizedKnownAvailabilityIsRejectedBeforeEnumeration();
            KnownCountOverrunStopsBeforeExtraCurrent();
            KnownCountDriftAfterTraversalFailsClosed();
            HonestCountedAvailabilityPreservesTraversal();
            LazyAvailabilityStopsAtBoundaryPlusOne();
            InvalidAndDuplicateActionIdsFailClosed();
        }

        private static void CapabilitiesDriveVisibilityAndStableOrdering()
        {
            var profile = Profile(
                FeatureCapability.Create |
                FeatureCapability.EditParameters |
                FeatureCapability.Geometry3D |
                FeatureCapability.Quantity |
                FeatureCapability.Locate |
                FeatureCapability.Delete,
                Recipe("direct", CreateInputMode.Direct));

            var bar = FeatureActionBarBuilder.Build(profile);
            Equal("Add|EditParameters|Geometry3D|Quantity|Locate|Delete", string.Join("|", bar.Actions.Select(x => x.Id)));
            Equal(1, bar.Primary.Count);
            Equal(FeatureActionId.Add, bar.Primary[0].Id);
            Equal(FeatureActionId.Delete, bar.Overflow.Single().Id);
            False(bar.Actions.Any(x => x.Id == FeatureActionId.Material), "Unsupported Material action must not render.");
            False(bar.Actions.Any(x => x.Id == FeatureActionId.Regenerate), "Unsupported Regenerate action must not render.");
        }

        private static void AlternateRecipesDriveSplitAddOnlyWhenNeeded()
        {
            var single = FeatureActionBarBuilder.Build(Profile(
                FeatureCapability.Create,
                Recipe("direct", CreateInputMode.Direct)));
            False(single.Primary.Single().HasAlternateRecipes, "A single create recipe must not render an Add dropdown.");
            Equal("direct", single.Primary.Single().PrimaryRecipeId);

            var multiple = FeatureActionBarBuilder.Build(Profile(
                FeatureCapability.Create,
                Recipe("direct", CreateInputMode.Direct),
                Recipe("from-form", CreateInputMode.FormThenCreate, "create.schema")));
            var add = multiple.Primary.Single();
            True(add.HasAlternateRecipes, "Multiple create recipes must expose the adjacent Add dropdown.");
            Equal("direct", add.PrimaryRecipeId);
            Equal("from-form", add.AlternateRecipeIds.Single());
        }

        private static void DisabledActionsExplainMissingPreconditions()
        {
            var profile = Profile(
                FeatureCapability.EditParameters | FeatureCapability.Quantity,
                Array.Empty<CreateRecipeDescriptor>());
            var bar = FeatureActionBarBuilder.Build(profile, new[]
            {
                new FeatureActionAvailability(FeatureActionId.EditParameters, false, "Select an instance to edit parameters."),
                new FeatureActionAvailability(FeatureActionId.Quantity, true)
            });

            var edit = bar.Actions.Single(x => x.Id == FeatureActionId.EditParameters);
            False(edit.IsEnabled, "Edit Parameters must honor its current precondition state.");
            Equal("Select an instance to edit parameters.", edit.DisabledReason);
            True(bar.Actions.Single(x => x.Id == FeatureActionId.Quantity).IsEnabled, "Enabled capability action unexpectedly disabled.");

            ExpectInvalidArgument(
                () => new FeatureActionAvailability(FeatureActionId.Locate, false),
                "Disabled actions without a reason must fail closed.");
        }

        private static void SimpleFeatureRemainsSimple()
        {
            var bar = FeatureActionBarBuilder.Build(Profile(
                FeatureCapability.Quantity,
                Array.Empty<CreateRecipeDescriptor>()));
            Equal(1, bar.Actions.Count);
            Equal(0, bar.Primary.Count);
            Equal(FeatureActionId.Quantity, bar.Secondary.Single().Id);
            Equal(0, bar.Overflow.Count);
            False(bar.Actions.Any(x => x.Id == FeatureActionId.Geometry3D), "Features without Geometry3D capability must not render a dead 3D action.");
        }

        private static void PilotProfilesExposeGeometry3DCapability()
        {
            var room = RoomInteractionProfile.Descriptor.InteractionProfile;
            var finishes = RoomFinishInteractionProfiles.CreateRegistry();
            var floor = finishes.GetRequired(RoomFinishInteractionProfiles.FloorFinishId).InteractionProfile;
            var waterproofing = finishes.GetRequired(RoomFinishInteractionProfiles.WaterproofingId).InteractionProfile;
            var skirting = finishes.GetRequired(RoomFinishInteractionProfiles.SkirtingId).InteractionProfile;

            True((room.Capabilities & FeatureCapability.Geometry3D) != 0, "Room must expose its documented Geometry3D capability.");
            True((floor.Capabilities & FeatureCapability.Geometry3D) != 0, "Floor Finish must expose its documented Geometry3D capability.");
            True((waterproofing.Capabilities & FeatureCapability.Geometry3D) != 0, "Waterproofing must expose its documented Geometry3D capability.");
            True((skirting.Capabilities & FeatureCapability.Geometry3D) != 0, "Skirting must expose its documented Geometry3D capability.");
        }

        private static void ActionMetadataIsConsistent()
        {
            var profile = AllActionsProfile();
            var bar = FeatureActionBarBuilder.Build(profile);

            foreach (var action in bar.Actions)
            {
                True(!string.IsNullOrWhiteSpace(action.LabelKey), "Action label key must be stable and nonblank.");
                True(!string.IsNullOrWhiteSpace(action.AccessKey), "Action access key must be stable and nonblank.");
                True(!string.IsNullOrWhiteSpace(action.ToolTipKey), "Action tooltip key must be stable and nonblank.");
                True(!string.IsNullOrWhiteSpace(action.StatusHintKey), "Action status hint key must be stable and nonblank.");
            }
        }

        private static void AvailabilityExactBoundaryIsAccepted()
        {
            var states = Enum.GetValues(typeof(FeatureActionId))
                .Cast<FeatureActionId>()
                .Select(id => new FeatureActionAvailability(id, true))
                .ToArray();

            Equal(8, states.Length);
            var bar = FeatureActionBarBuilder.Build(AllActionsProfile(), states);
            Equal(8, bar.Actions.Count);
            True(bar.Actions.All(x => x.IsEnabled), "Exact-bound availability must preserve enabled state for every supported action.");
        }

        private static void OversizedKnownAvailabilityIsRejectedBeforeEnumeration()
        {
            var oversized = new OversizedReadOnlyAvailability();
            ExpectInvalidOperation(
                () => FeatureActionBarBuilder.Build(AllActionsProfile(), oversized),
                "Known availability count above the supported action set must fail before enumeration.");
            False(oversized.EnumerationAttempted, "Oversized known-count availability must be rejected before enumeration starts.");
        }

        private static void KnownCountOverrunStopsBeforeExtraCurrent()
        {
            var source = new TrackingCountedAvailability(
                initialCount: 1,
                reboundCount: 1,
                new FeatureActionAvailability(FeatureActionId.Add, true),
                new FeatureActionAvailability(FeatureActionId.Quantity, true));

            ExpectInvalidOperation(
                () => FeatureActionBarBuilder.Build(AllActionsProfile(), source),
                "Known-count availability overrun must fail before exposing Current beyond advertised Count.");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
            Equal(1, source.CountReads);
        }

        private static void KnownCountDriftAfterTraversalFailsClosed()
        {
            var source = new TrackingCountedAvailability(
                initialCount: 1,
                reboundCount: 2,
                new FeatureActionAvailability(FeatureActionId.Add, true));

            ExpectInvalidOperation(
                () => FeatureActionBarBuilder.Build(AllActionsProfile(), source),
                "Availability Count drift after an otherwise exact traversal must fail closed.");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
            Equal(2, source.CountReads);
        }

        private static void HonestCountedAvailabilityPreservesTraversal()
        {
            var source = new TrackingCountedAvailability(
                initialCount: 2,
                reboundCount: 2,
                new FeatureActionAvailability(FeatureActionId.Add, true),
                new FeatureActionAvailability(FeatureActionId.Quantity, true));

            var bar = FeatureActionBarBuilder.Build(AllActionsProfile(), source);
            Equal(8, bar.Actions.Count);
            Equal(3, source.MoveNextCalls);
            Equal(2, source.CurrentReads);
            Equal(2, source.CountReads);
        }

        private static void LazyAvailabilityStopsAtBoundaryPlusOne()
        {
            var lazy = new BoundaryPlusOneAvailability();
            ExpectInvalidOperation(
                () => FeatureActionBarBuilder.Build(AllActionsProfile(), lazy),
                "Unknown-count availability must fail at the first item beyond the supported action set.");
            Equal(9, lazy.YieldedCount);
        }

        private static void InvalidAndDuplicateActionIdsFailClosed()
        {
            ExpectInvalidArgument(
                () => new FeatureActionAvailability((FeatureActionId)999, true),
                "Undefined feature-action enum values must fail at construction.");

            ExpectInvalidOperation(
                () => FeatureActionBarBuilder.Build(AllActionsProfile(), new[]
                {
                    new FeatureActionAvailability(FeatureActionId.Quantity, true),
                    new FeatureActionAvailability(FeatureActionId.Quantity, false, "duplicate")
                }),
                "Duplicate availability ids must remain rejected.");
        }

        private static InteractionProfile AllActionsProfile() => Profile(
            FeatureCapability.Create |
            FeatureCapability.EditParameters |
            FeatureCapability.Material |
            FeatureCapability.Geometry3D |
            FeatureCapability.Quantity |
            FeatureCapability.Regenerate |
            FeatureCapability.Locate |
            FeatureCapability.Delete,
            Recipe("direct", CreateInputMode.Direct));

        private static InteractionProfile Profile(FeatureCapability capabilities, params CreateRecipeDescriptor[] recipes)
        {
            var primary = recipes.Length == 0 ? null : recipes[0].Id;
            var allowsModal = recipes.Any(x => x.RequiresForm || x.InputMode == CreateInputMode.ChooseRecipe);
            return new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                recipes,
                primary,
                Array.Empty<InteractionSurface>(),
                capabilities,
                allowsModal: allowsModal);
        }

        private static CreateRecipeDescriptor Recipe(string id, CreateInputMode mode, string? schema = null) =>
            new CreateRecipeDescriptor(id, mode, schema);

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Feature action bar smoke mismatch. Expected=" + expected + " Actual=" + actual);
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new Exception(message);
        }

        private static void ExpectInvalidArgument(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception(message);
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception(message);
        }

        private sealed class OversizedReadOnlyAvailability : IReadOnlyCollection<FeatureActionAvailability>
        {
            public int Count => 9;
            public bool EnumerationAttempted { get; private set; }

            public IEnumerator<FeatureActionAvailability> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new Exception("Oversized known-count availability must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class TrackingCountedAvailability : IReadOnlyCollection<FeatureActionAvailability>
        {
            private readonly FeatureActionAvailability[] _states;
            private readonly int _initialCount;
            private readonly int _reboundCount;

            public TrackingCountedAvailability(
                int initialCount,
                int reboundCount,
                params FeatureActionAvailability[] states)
            {
                _initialCount = initialCount;
                _reboundCount = reboundCount;
                _states = states ?? throw new ArgumentNullException(nameof(states));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return CountReads == 1 ? _initialCount : _reboundCount;
                }
            }

            public int CountReads { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<FeatureActionAvailability> GetEnumerator() => new TrackingEnumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<FeatureActionAvailability>
            {
                private readonly TrackingCountedAvailability _owner;
                private int _index = -1;

                public TrackingEnumerator(TrackingCountedAvailability owner)
                {
                    _owner = owner;
                }

                public FeatureActionAvailability Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._states.Length)
                            throw new InvalidOperationException("Current read outside the valid availability traversal boundary.");
                        return _owner._states[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_index + 1 >= _owner._states.Length)
                    {
                        _index = _owner._states.Length;
                        return false;
                    }

                    _index++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();

                public void Dispose()
                {
                }
            }
        }

        private sealed class BoundaryPlusOneAvailability : IEnumerable<FeatureActionAvailability>
        {
            public int YieldedCount { get; private set; }

            public IEnumerator<FeatureActionAvailability> GetEnumerator()
            {
                for (var index = 0; index < 9; index++)
                {
                    YieldedCount++;
                    var id = index < 8 ? (FeatureActionId)index : FeatureActionId.Add;
                    yield return new FeatureActionAvailability(id, true);
                }

                throw new Exception("Feature action availability read past boundary+1.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
