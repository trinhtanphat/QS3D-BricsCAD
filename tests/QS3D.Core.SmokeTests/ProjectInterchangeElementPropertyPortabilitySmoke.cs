using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeElementPropertyPortabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportOmitsElementHandleMetadataButKeepsFamilySemantics();
            LegacyHandlePropertyIsAcceptedButNotMaterialized();
            AppendOnlyDoesNotRebindLegacyHandleProperty();
            KeepTargetDoesNotRebindLegacyHandleProperty();
            FieldMergeDoesNotReviewOrAdoptLegacyHandleProperty();
        }

        private static void ExportOmitsElementHandleMetadataButKeepsFamilySemantics()
        {
            var project = new ProjectState("EXPORT-PORTABLE", "Export") { DrawingFingerprint = "export-fp" };
            var family = new ProjectFamily("FAM-B", "Beam", ElementCategory.Beam);
            family.Properties["HandleHeight"] = "0.12";
            project.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            element.Properties["SemanticMark"] = "B1";
            element.Properties["CadHandle"] = "AA11";
            project.Elements.Add(element);

            var json = ProjectInterchangeJsonExporter.Build(project);
            Contains(json, "\"HandleHeight\":\"0.12\"");
            Contains(json, "\"SemanticMark\":\"B1\"");
            DoesNotContain(json, "\"CadHandle\"");
        }

        private static void LegacyHandlePropertyIsAcceptedButNotMaterialized()
        {
            var json = LegacySourceJson();
            True(ProjectInterchangeJsonValidator.Validate(json).IsValid);
            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var element = snapshot.Elements.Single(x => string.Equals(x.Id, "E-1", StringComparison.OrdinalIgnoreCase));
            Equal("SOURCE", element.Properties["SemanticMark"]);
            False(element.Properties.ContainsKey("CadHandle"));
        }

        private static void AppendOnlyDoesNotRebindLegacyHandleProperty()
        {
            var target = new ProjectState("APPEND-TARGET", "Target") { DrawingFingerprint = "append-target-fp" };
            ProjectInterchangeAppendOnlyImporter.Import(target, LegacySourceJson());
            AssertImportedElementIsPortable(target);
        }

        private static void KeepTargetDoesNotRebindLegacyHandleProperty()
        {
            var target = new ProjectState("KEEP-TARGET", "Target") { DrawingFingerprint = "keep-target-fp" };
            ProjectInterchangeKeepTargetImporter.Import(target, LegacySourceJson());
            AssertImportedElementIsPortable(target);
        }

        private static void FieldMergeDoesNotReviewOrAdoptLegacyHandleProperty()
        {
            var target = new ProjectState("FIELD-TARGET", "Target") { DrawingFingerprint = "field-target-fp" };
            var targetElement = new ProjectElement("E-1", ElementCategory.Beam);
            targetElement.Properties["SemanticMark"] = "TARGET";
            targetElement.Properties["CadHandle"] = "TARGET-CAD";
            target.Elements.Add(targetElement);

            var policy = new ProjectInterchangeFieldMergePolicy
            {
                ElementProperties = InterchangeFieldPrecedenceChoice.UseSource
            };
            var json = LegacySourceJson();
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);

            True(plan.CanExecute);
            True(plan.FieldPlan.Decisions.Any(x =>
                x.Kind == InterchangeIdentityKind.Element &&
                string.Equals(x.Id, "E-1", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Field, "properties.SemanticMark", StringComparison.OrdinalIgnoreCase)));
            False(plan.FieldPlan.Decisions.Any(x =>
                x.Kind == InterchangeIdentityKind.Element &&
                string.Equals(x.Field, "properties.CadHandle", StringComparison.OrdinalIgnoreCase)));

            ProjectInterchangeFieldMergeImporter.Import(target, json, policy, plan.CreateAuthorization());
            var element = target.FindElement("E-1") ?? throw new InvalidOperationException("Field-merge target element disappeared.");
            Equal("SOURCE", element.Properties["SemanticMark"]);
            False(element.Properties.ContainsKey("CadHandle"));
        }

        private static string LegacySourceJson()
        {
            var source = new ProjectState("SOURCE-PORTABLE", "Source") { DrawingFingerprint = "source-fp" };
            var element = new ProjectElement("E-1", ElementCategory.Beam);
            element.Properties["SemanticMark"] = "SOURCE";
            source.Elements.Add(element);
            var json = ProjectInterchangeJsonExporter.Build(source);
            const string needle = "\"SemanticMark\":\"SOURCE\"";
            if (!json.Contains(needle)) throw new InvalidOperationException("Portability smoke could not locate the semantic property in exported JSON.");
            return json.Replace(needle, "\"CadHandle\":\"SOURCE-CAD\",\n        \"SemanticMark\":\"SOURCE\"");
        }

        private static void AssertImportedElementIsPortable(ProjectState target)
        {
            var element = target.FindElement("E-1") ?? throw new InvalidOperationException("Imported element is missing.");
            Equal("SOURCE", element.Properties["SemanticMark"]);
            False(element.Properties.ContainsKey("CadHandle"));
        }

        private static void Contains(string value, string token)
        {
            if (value.IndexOf(token, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("ProjectInterchangeElementPropertyPortabilitySmoke expected token: " + token);
        }

        private static void DoesNotContain(string value, string token)
        {
            if (value.IndexOf(token, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("ProjectInterchangeElementPropertyPortabilitySmoke found forbidden token: " + token);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeElementPropertyPortabilitySmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("ProjectInterchangeElementPropertyPortabilitySmoke assertion expected true.");
        }

        private static void False(bool condition)
        {
            if (condition) throw new InvalidOperationException("ProjectInterchangeElementPropertyPortabilitySmoke assertion expected false.");
        }
    }
}
