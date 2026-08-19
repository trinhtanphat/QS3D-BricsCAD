using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkspaceModalPrimitivesSmoke
    {
        public static void Run()
        {
            RecipeChooserIsBoundedAndContextPreserved();
            SchemaModalCancelsWithoutMutationHandoff();
            DuplicateBlockingModalIsRejected();
            ConfirmationAndValidationBehaviorIsExplicit();
            WizardAndViewportPolicyRemainUsable();
        }

        private static void RecipeChooserIsBoundedAndContextPreserved()
        {
            var host = new WorkspaceModalHost();
            var session = host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.RecipeChooser,
                "feature:model.wall",
                "Create wall",
                recipeIds: new[] { "direct", "form-pick" },
                defaultFocusKey: "direct"));

            if (!session.IsOpen || !host.HasBlockingModal)
                throw new Exception("Recipe chooser must create exactly one active Workspace modal session.");
            if (!session.Descriptor.EnterAccepts || !session.Descriptor.EscapeCancels)
                throw new Exception("Recipe chooser must expose consistent Enter/Escape semantics.");

            var outcome = session.Accept("form-pick");
            if (outcome.Kind != WorkspaceModalOutcomeKind.Accepted || outcome.ContextKey != "feature:model.wall")
                throw new Exception("Accepted modal result must preserve the selected Workspace context.");
            if (host.HasBlockingModal)
                throw new Exception("Completed modal must release the Workspace blocking slot.");
        }

        private static void SchemaModalCancelsWithoutMutationHandoff()
        {
            var host = new WorkspaceModalHost();
            var session = host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.SchemaForm,
                "feature:model.finish",
                "Finish properties",
                schemaKey: "finish.material-thickness",
                defaultFocusKey: "material"));

            var outcome = session.Cancel();
            if (outcome.Kind != WorkspaceModalOutcomeKind.Cancelled || outcome.Value != null)
                throw new Exception("Cancellation must return an explicit mutation-free result.");
            if (host.ActiveSession != null)
                throw new Exception("Cancellation must leave no orphaned modal session.");
        }

        private static void DuplicateBlockingModalIsRejected()
        {
            var host = new WorkspaceModalHost();
            var first = host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.SchemaForm,
                "feature:model.column",
                "Column properties",
                schemaKey: "column.schema"));

            ExpectInvalid(() => host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.Confirmation,
                "feature:model.column",
                "Delete column")),
                "Workspace must reject overlapping blocking modal sessions.");

            first.Cancel();
            if (host.HasBlockingModal)
                throw new Exception("Cancelling the first modal must make the host reusable.");
        }

        private static void ConfirmationAndValidationBehaviorIsExplicit()
        {
            var host = new WorkspaceModalHost();
            var confirm = host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.Confirmation,
                "feature:model.wall",
                "Delete wall?",
                isDestructive: true,
                defaultFocusKey: "cancel"));

            if (!confirm.Descriptor.IsDestructive || !confirm.Descriptor.EnterAccepts)
                throw new Exception("Destructive confirmation must be represented explicitly.");
            confirm.Cancel();

            var validation = host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.ValidationError,
                "feature:model.wall",
                "Thickness must be greater than zero"));
            if (validation.Descriptor.EnterAccepts)
                throw new Exception("Validation presentation must not silently accept on Enter.");
            ExpectInvalid(() => validation.Accept(), "Validation error requires an explicit dismissal action.");
            validation.Cancel();
        }

        private static void WizardAndViewportPolicyRemainUsable()
        {
            var host = new WorkspaceModalHost(new WorkspaceModalLayoutPolicy(
                maxWidthDip: 640d,
                maxHeightRatio: 0.8d,
                minimumViewportWidthDip: 300d,
                minimumViewportHeightDip: 220d));

            var viewport = host.LayoutPolicy.Resolve(420d, 300d);
            if (viewport.WidthDip > 420d || viewport.HeightDip > 300d || !viewport.IsScrollable)
                throw new Exception("Modal viewport must remain bounded and scrollable in compact/high-DPI layouts.");

            var wizard = host.Open(new WorkspaceModalDescriptor(
                WorkspaceModalKind.Wizard,
                "feature:model.foundation",
                "Foundation wizard",
                schemaKey: "foundation.schema",
                wizardSteps: new[] { "shape", "placement", "review" },
                defaultFocusKey: "shape"));
            if (wizard.Descriptor.WizardSteps.Count != 3)
                throw new Exception("Wizard shell must preserve deterministic named steps.");
            wizard.Cancel();

            ExpectArgument(() => new WorkspaceModalDescriptor(
                WorkspaceModalKind.RecipeChooser,
                "feature:test",
                "Bad chooser",
                recipeIds: new[] { "only-one" }),
                "Recipe chooser outside 2-5 choices must fail closed.");
        }

        private static void ExpectInvalid(Action action, string message)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new Exception(message);
        }

        private static void ExpectArgument(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            throw new Exception(message);
        }
    }
}
