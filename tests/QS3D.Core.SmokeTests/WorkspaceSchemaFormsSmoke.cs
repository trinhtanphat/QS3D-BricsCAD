using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkspaceSchemaFormsSmoke
    {
        internal static void Run()
        {
            ReusesProjectFamilyQuickSchema();
            RequiredDefaultAndRangeValidation();
            ChoiceConditionalAndReadOnlyPlanning();
            SurfaceApplicabilityIsShared();
            UnsupportedKindFailsVisibly();
        }

        private static void ReusesProjectFamilyQuickSchema()
        {
            var source = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Skirting);
            var schema = ProjectFamilyQuickSchemaAdapter.Create(ElementCategory.Skirting, "finish.skirting");

            Require(schema.Key == "finish.skirting", "Adapter must preserve requested schema key.");
            Require(schema.Fields.Select(x => x.Key).SequenceEqual(source.FormKeys), "Adapter must consume #3107 field order instead of duplicating a category field table.");
            foreach (var field in schema.Fields)
            {
                Require(field.Kind == WorkspaceSchemaFieldKind.Number, "Quick-schema dimensions must adapt to numeric fields.");
                Require(field.Unit == "m", "Quick-schema internal dimensions must retain meter units.");
                Require(Equals(field.DefaultValue, source.DefaultsM[field.Key]), "Adapter defaults must come from ProjectFamilyQuickSchemaService.");
                Require(field.Required == source.IsIdentityKey(field.Key), "Identity keys must drive required-field behavior.");
            }
        }

        private static void RequiredDefaultAndRangeValidation()
        {
            var schema = new WorkspaceFormSchema("validation", new[]
            {
                new WorkspaceSchemaField("Name", WorkspaceSchemaFieldKind.Text, required: true),
                new WorkspaceSchemaField("ThicknessM", WorkspaceSchemaFieldKind.Number, required: true, defaultValue: 0.02d, minimum: 0.001d, maximum: 0.5d, unit: "m", precision: 3)
            });

            var missing = WorkspaceSchemaValidator.Validate(schema, WorkspaceSchemaSurface.CreateForm, new Dictionary<string, object>());
            Require(missing.Count == 1 && missing[0].FieldKey == "Name", "Required validation must be independent from WPF control text and allow schema defaults.");

            var invalid = WorkspaceSchemaValidator.Validate(schema, WorkspaceSchemaSurface.CreateForm, new Dictionary<string, object>
            {
                ["Name"] = "Finish",
                ["ThicknessM"] = 0.75d
            });
            Require(invalid.Count == 1 && invalid[0].FieldKey == "ThicknessM", "Numeric maximum must be enforced.");
        }

        private static void ChoiceConditionalAndReadOnlyPlanning()
        {
            var schema = new WorkspaceFormSchema("conditional", new[]
            {
                new WorkspaceSchemaField("Mode", WorkspaceSchemaFieldKind.Choice, required: true, defaultValue: "Auto", choices: new[] { "Auto", "Manual" }, order: 0),
                new WorkspaceSchemaField("Reference", WorkspaceSchemaFieldKind.Reference, visibleWhen: new WorkspaceSchemaCondition("Mode", "Manual"), enabledWhen: new WorkspaceSchemaCondition("Mode", "Manual"), order: 1),
                new WorkspaceSchemaField("ComputedArea", WorkspaceSchemaFieldKind.Number, readOnly: true, computed: true, unit: "m2", precision: 2, order: 2)
            });

            var auto = WorkspaceSchemaRenderer.Plan(schema, WorkspaceSchemaSurface.CreateForm, new Dictionary<string, object> { ["Mode"] = "Auto" });
            var hiddenReference = auto.Single(x => x.Field.Key == "Reference");
            Require(!hiddenReference.Visible && !hiddenReference.Enabled, "Conditional fields must hide and disable when condition is false.");

            var manual = WorkspaceSchemaRenderer.Plan(schema, WorkspaceSchemaSurface.Inspector, new Dictionary<string, object> { ["Mode"] = "Manual" });
            var visibleReference = manual.Single(x => x.Field.Key == "Reference");
            var computed = manual.Single(x => x.Field.Key == "ComputedArea");
            Require(visibleReference.Visible && visibleReference.Enabled, "Conditional field must render enabled when condition matches.");
            Require(computed.Visible && computed.ReadOnly && !computed.Enabled, "Computed fields must remain read-only in inspector rendering.");

            var invalidChoice = WorkspaceSchemaValidator.Validate(schema, WorkspaceSchemaSurface.CreateForm, new Dictionary<string, object> { ["Mode"] = "Other" });
            Require(invalidChoice.Any(x => x.FieldKey == "Mode"), "Enum/choice values must reject undeclared options.");
        }

        private static void SurfaceApplicabilityIsShared()
        {
            var schema = new WorkspaceFormSchema("surface", new[]
            {
                new WorkspaceSchemaField("CreateOnly", WorkspaceSchemaFieldKind.Text, applicability: WorkspaceSchemaApplicability.Create),
                new WorkspaceSchemaField("EditOnly", WorkspaceSchemaFieldKind.Text, applicability: WorkspaceSchemaApplicability.Edit),
                new WorkspaceSchemaField("Shared", WorkspaceSchemaFieldKind.Boolean, applicability: WorkspaceSchemaApplicability.CreateAndEdit)
            });

            var create = WorkspaceSchemaRenderer.Plan(schema, WorkspaceSchemaSurface.CreateForm, new Dictionary<string, object>());
            var inspect = WorkspaceSchemaRenderer.Plan(schema, WorkspaceSchemaSurface.Inspector, new Dictionary<string, object>());
            Require(create.Select(x => x.Field.Key).SequenceEqual(new[] { "CreateOnly", "Shared" }), "Create renderer must honor create applicability.");
            Require(inspect.Select(x => x.Field.Key).SequenceEqual(new[] { "EditOnly", "Shared" }), "Inspector renderer must use the same schema vocabulary with edit applicability.");
        }

        private static void UnsupportedKindFailsVisibly()
        {
            var schema = new WorkspaceFormSchema("unsupported", new[]
            {
                new WorkspaceSchemaField("Future", (WorkspaceSchemaFieldKind)999)
            });
            RequireThrows<NotSupportedException>(() => WorkspaceSchemaRenderer.Plan(schema, WorkspaceSchemaSurface.CreateForm, new Dictionary<string, object>()), "Unsupported field kinds must fail visibly rather than render blank content.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("WorkspaceSchemaFormsSmoke: " + message);
        }

        private static void RequireThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("WorkspaceSchemaFormsSmoke: " + message);
        }
    }
}
