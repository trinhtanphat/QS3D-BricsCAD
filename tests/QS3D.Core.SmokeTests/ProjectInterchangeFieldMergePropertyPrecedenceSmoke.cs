using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeFieldMergePropertyPrecedenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KeepTargetElementPropertiesSurviveFamilyDefaultMerge();
            EqualElementPropertiesSurviveFamilyDefaultMergeWithUseSource();
        }

        private static void KeepTargetElementPropertiesSurviveFamilyDefaultMerge()
        {
            var target = BuildTarget();
            var source = BuildSource();
            var policy = new ProjectInterchangeFieldMergePolicy
            {
                FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource,
                ElementProperties = InterchangeFieldPrecedenceChoice.KeepTarget
            };
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);

            True(plan.CanExecute);
            Equal(1, plan.FieldPlan.SourceChoiceCount);
            True(FindDecision(plan, InterchangeIdentityKind.Element, "E-1", "properties.Material") == null);

            ProjectInterchangeFieldMergeImporter.Import(target, json, policy, plan.CreateAuthorization());

            Equal("C40", target.FindFamily("FAM-B")!.Properties["Material"]);
            Equal("C30", target.FindElement("E-1")!.Properties["Material"]);
            Equal("TARGET", target.FindElement("E-1")!.Properties["KeepMe"]);
        }

        private static void EqualElementPropertiesSurviveFamilyDefaultMergeWithUseSource()
        {
            var target = BuildTarget();
            var source = BuildSource();
            var policy = new ProjectInterchangeFieldMergePolicy
            {
                FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource,
                ElementProperties = InterchangeFieldPrecedenceChoice.UseSource
            };
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);

            True(plan.CanExecute);
            True(FindDecision(plan, InterchangeIdentityKind.Element, "E-1", "properties.Material") == null);

            ProjectInterchangeFieldMergeImporter.Import(target, json, policy, plan.CreateAuthorization());

            Equal("C40", target.FindFamily("FAM-B")!.Properties["Material"]);
            Equal("C30", target.FindElement("E-1")!.Properties["Material"]);
            Equal("TARGET", target.FindElement("E-1")!.Properties["KeepMe"]);
        }

        private static ProjectState BuildTarget()
        {
            var target = new ProjectState("TARGET-PROP-PRECEDENCE", "Target") { DrawingFingerprint = "target-fp" };
            var family = new ProjectFamily("FAM-B", "Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            target.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            element.Properties["Material"] = "C30";
            element.Properties["KeepMe"] = "TARGET";
            target.Elements.Add(element);
            return target;
        }

        private static ProjectState BuildSource()
        {
            var source = new ProjectState("SOURCE-PROP-PRECEDENCE", "Source") { DrawingFingerprint = "source-fp" };
            var family = new ProjectFamily("FAM-B", "Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C40";
            source.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            element.Properties["Material"] = "C30";
            element.Properties["KeepMe"] = "TARGET";
            source.Elements.Add(element);
            return source;
        }

        private static InterchangeFieldMergeDecision? FindDecision(
            ProjectInterchangeFieldMergeExecutionPlan plan,
            InterchangeIdentityKind kind,
            string id,
            string field)
        {
            foreach (var decision in plan.FieldPlan.Decisions)
            {
                if (decision.Kind == kind &&
                    string.Equals(decision.Id, id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(decision.Field, field, StringComparison.OrdinalIgnoreCase))
                    return decision;
            }
            return null;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeFieldMergePropertyPrecedenceSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("ProjectInterchangeFieldMergePropertyPrecedenceSmoke assertion failed.");
        }
    }
}
