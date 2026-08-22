using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rules;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRollbackFailureMatrixSmoke
    {
        private enum InjectionStage
        {
            Catalog,
            Element,
            RulesAuditMetadata,
            Validation
        }

        [ModuleInitializer]
        internal static void Initialize()
        {
            MutationStagesRestoreWholeProjectState();
            ValidationFailureRestoresWholeProjectState();
            AssertionHarnessDetectsDrift();
        }

        private static void MutationStagesRestoreWholeProjectState()
        {
            foreach (var stage in new[] { InjectionStage.Catalog, InjectionStage.Element, InjectionStage.RulesAuditMetadata })
            {
                var project = Fixture();
                var baseline = ProjectRollbackAssert.Capture(project);
                var journal = new ProjectSemanticMutationJournal();

                Throws<InjectedFailureException>(() => ProjectSemanticMutationExecutor.Execute(
                    project,
                    "rollback.matrix." + stage,
                    () =>
                    {
                        MutateCatalog(project);
                        Inject(stage, InjectionStage.Catalog);
                        MutateElement(project);
                        Inject(stage, InjectionStage.Element);
                        MutateRulesAuditMetadata(project);
                        Inject(stage, InjectionStage.RulesAuditMetadata);
                        return 1;
                    },
                    journal));

                ProjectRollbackAssert.Equivalent(baseline, project, "rollback after " + stage);
                True(journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.RollingBack));
                True(journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.RolledBack));
                False(journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.Committed));
            }
        }

        private static void ValidationFailureRestoresWholeProjectState()
        {
            var project = Fixture();
            var baseline = ProjectRollbackAssert.Capture(project);
            var journal = new ProjectSemanticMutationJournal();

            Throws<InjectedFailureException>(() => ProjectSemanticMutationExecutor.Execute(
                project,
                "rollback.matrix.validation",
                () =>
                {
                    MutateCatalog(project);
                    MutateElement(project);
                    MutateRulesAuditMetadata(project);
                    return 1;
                },
                () => throw new InjectedFailureException(InjectionStage.Validation),
                journal));

            ProjectRollbackAssert.Equivalent(baseline, project, "rollback after validation");
            True(journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.Validating));
            True(journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.RolledBack));
            False(journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.Committed));
        }

        private static void AssertionHarnessDetectsDrift()
        {
            var project = Fixture();
            var baseline = ProjectRollbackAssert.Capture(project);
            project.Metadata["Drift"] = "yes";
            Throws<InvalidOperationException>(() => ProjectRollbackAssert.Equivalent(baseline, project, "intentional drift"));
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-ROLLBACK-MATRIX", "Rollback Matrix")
            {
                SchemaVersion = ProjectState.CurrentSchemaVersion,
                DrawingPath = @"C:\Projects\rollback-matrix.dwg",
                DrawingFingerprint = "DWG-FINGERPRINT-BASE",
                ActiveZoneId = "Z1",
                ActiveFloorId = "F1"
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 3.25));

            var family = new ProjectFamily("FAM1", "Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var element = new ProjectElement("E1", ElementCategory.Beam, "FAM1", "F1", "Z1")
            {
                DrawingFingerprint = "ELEMENT-DWG-BASE"
            };
            element.SourceHandles.Add("1A");
            element.SourceHandles.Add("1B");
            element.DependsOn.Add("UPSTREAM-1");
            element.Properties["Width"] = "300";
            element.SetQuantity("Volume", 1.25);
            element.MarkClean(ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations);
            project.Elements.Add(element);

            project.QuantityRules.Add(new QuantityRule("QR1", ElementCategory.Beam, "Mass", "Volume*2400", "1"));
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc),
                Action = "seed",
                ElementId = "E1",
                Detail = "baseline",
                Actor = "smoke",
                CorrelationId = "C0"
            });
            project.Metadata["Profile"] = "baseline";
            project.Metadata["Owner"] = "smoke";
            project.Touch();
            return project;
        }

        private static void MutateCatalog(ProjectState project)
        {
            project.Name = "Mutated Project";
            project.DrawingPath = @"C:\Projects\mutated.dwg";
            project.DrawingFingerprint = "DWG-FINGERPRINT-MUTATED";
            project.ActiveZoneId = "Z2";
            project.ActiveFloorId = "F2";
            project.Zones[0].Name = "Mutated Zone";
            project.Zones.Add(new ZoneDefinition("Z2", "Zone 2"));
            project.Floors[0].Name = "Mutated Floor";
            project.Floors[0].ElevationM = 9.5;
            project.Floors.Add(new FloorDefinition("F2", "Floor 2", 12.0));
            project.Families[0].Name = "Mutated Family";
            project.Families[0].Category = ElementCategory.Column;
            project.Families[0].Properties["Material"] = "C50";
            project.Families[0].Properties["Added"] = "yes";
            project.Families.Add(new ProjectFamily("FAM2", "Column Family", ElementCategory.Column));
        }

        private static void MutateElement(ProjectState project)
        {
            var element = project.Elements[0];
            element.Category = ElementCategory.Column;
            element.FamilyId = "FAM2";
            element.FloorId = "F2";
            element.ZoneId = "Z2";
            element.DrawingFingerprint = "ELEMENT-DWG-MUTATED";
            element.SourceHandles.Clear();
            element.SourceHandles.Add("9A");
            element.DependsOn.Clear();
            element.DependsOn.Add("UPSTREAM-2");
            element.Properties["Width"] = "900";
            element.Properties["Added"] = "yes";
            element.SetQuantity("Volume", 9.75);
            element.SetQuantity("Mass", 23400);
            element.MarkDirty(ElementDirtyFlags.All);
            project.Elements.Add(new ProjectElement("E2", ElementCategory.Column, "FAM2", "F2", "Z2"));
        }

        private static void MutateRulesAuditMetadata(ProjectState project)
        {
            project.QuantityRules.Clear();
            project.QuantityRules.Add(new QuantityRule("QR2", ElementCategory.Column, "Area", "Width*Width", "2"));
            project.AuditEvents.Clear();
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 11, 1, 0, 0, DateTimeKind.Utc),
                Action = "mutated",
                ElementId = "E2",
                Detail = "injected matrix state",
                Actor = "smoke2",
                CorrelationId = "C1"
            });
            project.Metadata.Clear();
            project.Metadata["Profile"] = "mutated";
            project.Metadata["Added"] = "yes";
            project.Touch();
        }

        private static void Inject(InjectionStage actual, InjectionStage expected)
        {
            if (actual == expected) throw new InjectedFailureException(actual);
        }

        private sealed class InjectedFailureException : Exception
        {
            internal InjectedFailureException(InjectionStage stage) : base("Injected rollback matrix failure at " + stage + ".") { }
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
