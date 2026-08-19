using System;
using System.Collections.Generic;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class AddCreateStateMachineSmoke
    {
        public static void Run()
        {
            RoomDirectDoesNotRequestPopup();
            FormValidationPrecedesCreateHandoff();
            FormThenPickAndPickThenFormAreOrdered();
            CancellationClearsTransientDirective();
            CreateFailureRequiresRollbackAcknowledgement();
            DuplicateInvocationCannotOverlap();
            ChooseRecipeAndWizardAreRepresentable();
        }

        private static void RoomDirectDoesNotRequestPopup()
        {
            var machine = new AddCreateStateMachine(Feature("model.room", new CreateRecipeDescriptor("room.direct", CreateInputMode.Direct)));
            var directive = machine.Begin();

            if (machine.State != AddCreateState.Creating || directive.Kind != AddCreateDirectiveKind.Create)
                throw new Exception("Direct Room creation must hand off create without opening a popup.");
            if (directive.Kind == AddCreateDirectiveKind.ShowForm || directive.Kind == AddCreateDirectiveKind.ChooseRecipe)
                throw new Exception("Direct Room creation must not request modal UI.");

            machine.CompleteCreate();
            if (machine.State != AddCreateState.Created)
                throw new Exception("Successful create handoff must reach Created state explicitly.");
        }

        private static void FormValidationPrecedesCreateHandoff()
        {
            var machine = new AddCreateStateMachine(Feature(
                "model.finish",
                new CreateRecipeDescriptor("finish.form", CreateInputMode.FormThenCreate, "finish.material-thickness"),
                allowsModal: true));

            if (machine.Begin().Kind != AddCreateDirectiveKind.ShowForm || machine.State != AddCreateState.Preparing)
                throw new Exception("FormThenCreate must enter Preparing and request its schema form.");

            ExpectInvalid(() => machine.SubmitForm(
                new[] { new KeyValuePair<string, string>("thickness", "-1") },
                _ => false),
                "Validation failure must fail before mutation handoff.");

            if (machine.State != AddCreateState.Preparing || machine.CreateWasHandedOff)
                throw new Exception("Validation failure must leave the session usable and must not hand off mutation.");

            var directive = machine.SubmitForm(
                new[]
                {
                    new KeyValuePair<string, string>("material", "Concrete"),
                    new KeyValuePair<string, string>("thickness", "100")
                },
                request => request.FormValues.ContainsKey("material") && request.FormValues.ContainsKey("thickness"));

            if (directive.Kind != AddCreateDirectiveKind.Create || machine.State != AddCreateState.Creating)
                throw new Exception("Validated schema input must be captured before create handoff.");
            if (machine.GetCreateRequest().FormValues["material"] != "Concrete")
                throw new Exception("Validated form values must be preserved in the immutable create request snapshot.");
        }

        private static void FormThenPickAndPickThenFormAreOrdered()
        {
            var formThenPick = new AddCreateStateMachine(Feature(
                "model.wall",
                new CreateRecipeDescriptor("wall.form-pick", CreateInputMode.FormThenPick, "wall.material-thickness"),
                allowsModal: true));
            formThenPick.Begin();
            var afterForm = formThenPick.SubmitForm(
                new[] { new KeyValuePair<string, string>("thickness", "200") },
                _ => true);
            if (afterForm.Kind != AddCreateDirectiveKind.RequestCadInput || formThenPick.State != AddCreateState.WaitingForCadInput)
                throw new Exception("FormThenPick must validate the form before requesting CAD input.");
            if (formThenPick.SubmitCadInput("picked-line").Kind != AddCreateDirectiveKind.Create)
                throw new Exception("FormThenPick must create only after CAD input arrives.");

            var pickThenForm = new AddCreateStateMachine(Feature(
                "model.column",
                new CreateRecipeDescriptor("column.pick-form", CreateInputMode.PickThenForm, "column.material-size"),
                allowsModal: true));
            if (pickThenForm.Begin().Kind != AddCreateDirectiveKind.RequestCadInput)
                throw new Exception("PickThenForm must request CAD input before showing a form.");
            var afterPick = pickThenForm.SubmitCadInput("picked-point");
            if (afterPick.Kind != AddCreateDirectiveKind.ShowForm || pickThenForm.State != AddCreateState.Preparing)
                throw new Exception("PickThenForm must return to Preparing for schema input after CAD input.");
            pickThenForm.SubmitForm(new[] { new KeyValuePair<string, string>("size", "300x300") }, _ => true);
            if (pickThenForm.GetCreateRequest().CadInput as string != "picked-point")
                throw new Exception("PickThenForm must preserve CAD input through validated create handoff.");
        }

        private static void CancellationClearsTransientDirective()
        {
            var machine = new AddCreateStateMachine(Feature(
                "model.beam",
                new CreateRecipeDescriptor("beam.pick-form", CreateInputMode.PickThenForm, "beam.schema"),
                allowsModal: true));
            machine.Begin();
            machine.Cancel();

            if (machine.State != AddCreateState.Cancelled || machine.Directive.Kind != AddCreateDirectiveKind.None)
                throw new Exception("Cancellation must clear transient CAD/form surface directives.");
            if (machine.CreateWasHandedOff || machine.RequiresRollback)
                throw new Exception("Cancellation before mutation must leave no orphaned mutation state.");
        }

        private static void CreateFailureRequiresRollbackAcknowledgement()
        {
            var machine = new AddCreateStateMachine(Feature("model.room", new CreateRecipeDescriptor("room.direct", CreateInputMode.Direct)));
            machine.Begin();
            machine.FailCreate(new InvalidOperationException("host rejected mutation"));

            if (machine.State != AddCreateState.Error || !machine.RequiresRollback)
                throw new Exception("Create failure after mutation handoff must expose an explicit rollback boundary.");

            machine.AcknowledgeRollback();
            if (machine.RequiresRollback || machine.CreateWasHandedOff)
                throw new Exception("Rollback acknowledgement must close the failed mutation handoff boundary.");
        }

        private static void DuplicateInvocationCannotOverlap()
        {
            var machine = new AddCreateStateMachine(Feature("model.room", new CreateRecipeDescriptor("room.direct", CreateInputMode.Direct)));
            machine.Begin();
            ExpectInvalid(() => machine.Begin(), "An active create session must reject duplicate invocation.");
        }

        private static void ChooseRecipeAndWizardAreRepresentable()
        {
            var chooserProfile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[]
                {
                    new CreateRecipeDescriptor("choose", CreateInputMode.ChooseRecipe),
                    new CreateRecipeDescriptor("direct", CreateInputMode.Direct)
                },
                "choose",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create,
                allowsModal: true);
            var chooser = new AddCreateStateMachine(new FeatureDescriptor(new FeatureId("model.multi"), "model", 0, "Feature.Multi", chooserProfile));
            if (chooser.Begin().Kind != AddCreateDirectiveKind.ChooseRecipe)
                throw new Exception("ChooseRecipe mode must expose recipe selection as an explicit directive.");
            if (chooser.SelectRecipe("direct").Kind != AddCreateDirectiveKind.Create)
                throw new Exception("Recipe selection must continue the same create session without overlap.");

            var wizard = new AddCreateStateMachine(Feature(
                "model.foundation",
                new CreateRecipeDescriptor("foundation.wizard", CreateInputMode.Wizard, "foundation.wizard-schema"),
                allowsModal: true));
            if (wizard.Begin().Kind != AddCreateDirectiveKind.ShowForm)
                throw new Exception("Wizard mode must be representable as schema preparation before CAD handoff.");
            if (wizard.SubmitForm(new[] { new KeyValuePair<string, string>("step", "1") }, _ => true).Kind != AddCreateDirectiveKind.RequestCadInput)
                throw new Exception("Wizard mode must support a validated form-to-CAD transition.");
        }

        private static FeatureDescriptor Feature(string id, CreateRecipeDescriptor recipe, bool allowsModal = false)
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { recipe },
                recipe.Id,
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create,
                allowsModal: allowsModal);
            return new FeatureDescriptor(new FeatureId(id), "model", 0, "Feature.Test", profile);
        }

        private static void ExpectInvalid(Action action, string message)
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
    }
}
