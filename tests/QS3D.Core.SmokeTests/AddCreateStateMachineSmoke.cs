using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
            FormFieldBoundaryIsBoundedAndTransactional();
            KnownOversizeFormIsRejectedBeforeEnumeration();
            LazyFormStopsAtBoundaryPlusOne();
            TraversalFailureDoesNotLeakPartialFormState();
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
            if (StoredFormValueCount(machine) != 0)
                throw new Exception("Validation failure must clear transient form values before retry.");

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

        private static void FormFieldBoundaryIsBoundedAndTransactional()
        {
            var machine = FormMachine("model.boundary");
            machine.Begin();

            var values = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < AddCreateStateMachine.MaximumFormFields; i++)
                values.Add(new KeyValuePair<string, string>("field-" + i, i.ToString()));

            machine.SubmitForm(values, request => request.FormValues.Count == AddCreateStateMachine.MaximumFormFields);
            if (machine.GetCreateRequest().FormValues.Count != AddCreateStateMachine.MaximumFormFields)
                throw new Exception("The exact create-form field ceiling must remain valid.");
        }

        private static void KnownOversizeFormIsRejectedBeforeEnumeration()
        {
            var machine = FormMachine("model.known-oversize");
            machine.Begin();
            var values = new KnownCountFormValues(AddCreateStateMachine.MaximumFormFields + 1);

            ExpectInvalid(() => machine.SubmitForm(values, _ => true),
                "A known oversized create form must fail before enumeration.");

            if (values.EnumeratorCalls != 0)
                throw new Exception("Known oversized create forms must be rejected before traversal starts.");
            if (machine.State != AddCreateState.Preparing || machine.Directive.Kind != AddCreateDirectiveKind.ShowForm)
                throw new Exception("Known oversized form refusal must preserve the form-entry lifecycle state.");
            if (StoredFormValueCount(machine) != 0)
                throw new Exception("Known oversized form refusal must not mutate stored form state.");
        }

        private static void LazyFormStopsAtBoundaryPlusOne()
        {
            var machine = FormMachine("model.lazy-boundary");
            machine.Begin();
            var observed = 0;

            IEnumerable<KeyValuePair<string, string>> Values()
            {
                for (var i = 0; i < AddCreateStateMachine.MaximumFormFields + 10; i++)
                {
                    observed++;
                    yield return new KeyValuePair<string, string>("field-" + i, i.ToString());
                }
            }

            ExpectInvalid(() => machine.SubmitForm(Values(), _ => true),
                "A lazy create-form stream must stop at boundary+1.");

            if (observed != AddCreateStateMachine.MaximumFormFields + 1)
                throw new Exception("Lazy create-form traversal must stop exactly at boundary+1 without over-reading.");
            if (StoredFormValueCount(machine) != 0)
                throw new Exception("Lazy boundary refusal must not publish a partial form snapshot.");
        }

        private static void TraversalFailureDoesNotLeakPartialFormState()
        {
            var machine = FormMachine("model.throwing-form");
            machine.Begin();
            var validateCalls = 0;

            IEnumerable<KeyValuePair<string, string>> Values()
            {
                yield return new KeyValuePair<string, string>("partial", "should-not-stick");
                throw new InvalidOperationException("simulated UI form provider failure");
            }

            ExpectInvalid(() => machine.SubmitForm(Values(), _ =>
            {
                validateCalls++;
                return true;
            }), "Traversal failure must propagate before validation or mutation handoff.");

            if (validateCalls != 0)
                throw new Exception("Form validation must not run against a partially enumerated payload.");
            if (StoredFormValueCount(machine) != 0)
                throw new Exception("Traversal failure must preserve the previously committed form snapshot.");
            if (machine.State != AddCreateState.Preparing || machine.Directive.Kind != AddCreateDirectiveKind.ShowForm || machine.CreateWasHandedOff)
                throw new Exception("Traversal failure must leave the same form-entry lifecycle retryable.");

            machine.SubmitForm(
                new[]
                {
                    new KeyValuePair<string, string>("material", "Concrete"),
                    new KeyValuePair<string, string>("material", "Steel")
                },
                request => request.FormValues.Count == 1 && request.FormValues["material"] == "Steel");

            if (machine.GetCreateRequest().FormValues["material"] != "Steel")
                throw new Exception("Bounded ingestion must preserve the existing case-insensitive last-value-wins duplicate semantics.");
        }

        private static AddCreateStateMachine FormMachine(string id)
            => new AddCreateStateMachine(Feature(
                id,
                new CreateRecipeDescriptor(id + ".form", CreateInputMode.FormThenCreate, id + ".schema"),
                allowsModal: true));

        private static int StoredFormValueCount(AddCreateStateMachine machine)
        {
            var field = typeof(AddCreateStateMachine).GetField("_formValues", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || !(field.GetValue(machine) is IDictionary<string, string> values))
                throw new Exception("Unable to inspect create-form state for regression coverage.");
            return values.Count;
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

        private sealed class KnownCountFormValues : ICollection<KeyValuePair<string, string>>
        {
            public KnownCountFormValues(int count)
            {
                Count = count;
            }

            public int EnumeratorCalls { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                EnumeratorCalls++;
                throw new Exception("Known oversized form must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }
    }
}
