using System;
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
            ActionMetadataIsConsistent();
        }

        private static void CapabilitiesDriveVisibilityAndStableOrdering()
        {
            var profile = Profile(
                FeatureCapability.Create |
                FeatureCapability.EditParameters |
                FeatureCapability.Quantity |
                FeatureCapability.Locate |
                FeatureCapability.Delete,
                Recipe("direct", CreateInputMode.Direct));

            var bar = FeatureActionBarBuilder.Build(profile);
            Equal("Add|EditParameters|Quantity|Locate|Delete", string.Join("|", bar.Actions.Select(x => x.Id)));
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
        }

        private static void ActionMetadataIsConsistent()
        {
            var profile = Profile(
                FeatureCapability.Create |
                FeatureCapability.EditParameters |
                FeatureCapability.Material |
                FeatureCapability.Quantity |
                FeatureCapability.Regenerate |
                FeatureCapability.Locate |
                FeatureCapability.Delete,
                Recipe("direct", CreateInputMode.Direct));
            var bar = FeatureActionBarBuilder.Build(profile);

            foreach (var action in bar.Actions)
            {
                True(!string.IsNullOrWhiteSpace(action.LabelKey), "Action label key must be stable and nonblank.");
                True(!string.IsNullOrWhiteSpace(action.AccessKey), "Action access key must be stable and nonblank.");
                True(!string.IsNullOrWhiteSpace(action.ToolTipKey), "Action tooltip key must be stable and nonblank.");
                True(!string.IsNullOrWhiteSpace(action.StatusHintKey), "Action status hint key must be stable and nonblank.");
            }
        }

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
    }
}
