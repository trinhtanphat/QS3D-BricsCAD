using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleHealthSmoke
    {
        internal static void Run()
        {
            ValidAndZeroMatchSchedulesAreHealthy();
            NullCatalogEntriesRejectAtAdmissionAndElementNullFailsVisible();
            StaleAndAmbiguousReferencesAreReported();
            InvalidTemplateAndCatalogAreReportedReadOnly();
            ComprehensiveHealthIncludesSemanticScheduleProvider();
        }

        private static void ValidAndZeroMatchSchedulesAreHealthy()
        {
            var project = Project();
            SemanticScheduleCatalog.Save(project, new[]
            {
                Definition("S-EMPTY", "F2", "Z2", Array.Empty<string>(), Array.Empty<string>(), "{Id}")
            });
            var version = project.ChangeVersion;
            var issues = new SemanticScheduleHealthService().Inspect(project);
            Equal(0, issues.Count);
            Equal(version, project.ChangeVersion);
        }

        private static void NullCatalogEntriesRejectAtAdmissionAndElementNullFailsVisible()
        {
            var floorProject = Project();
            SaveIdentityProbe(floorProject, "S-NULL-FLOOR");
            var floorCount = floorProject.Floors.Count;
            var floorVersion = floorProject.ChangeVersion;
            var floorUpdatedUtc = floorProject.UpdatedUtc;
            Throws<ArgumentNullException>(() => floorProject.Floors.Add(null!));
            Equal(floorCount, floorProject.Floors.Count);
            Equal(floorVersion, floorProject.ChangeVersion);
            Equal(floorUpdatedUtc, floorProject.UpdatedUtc);

            var zoneProject = Project();
            SaveIdentityProbe(zoneProject, "S-NULL-ZONE");
            var zoneCount = zoneProject.Zones.Count;
            var zoneVersion = zoneProject.ChangeVersion;
            var zoneUpdatedUtc = zoneProject.UpdatedUtc;
            Throws<ArgumentNullException>(() => zoneProject.Zones.Add(null!));
            Equal(zoneCount, zoneProject.Zones.Count);
            Equal(zoneVersion, zoneProject.ChangeVersion);
            Equal(zoneUpdatedUtc, zoneProject.UpdatedUtc);

            var elementProject = Project();
            SaveIdentityProbe(elementProject, "S-NULL-ELEMENT");
            elementProject.Elements.Add(null!);
            Throws<InvalidOperationException>(() => new SemanticScheduleHealthService().Inspect(elementProject));
            HasProviderFailure(new ComprehensiveModelHealthService().Inspect(elementProject), "SemanticScheduleHealthService");
        }

        private static void SaveIdentityProbe(ProjectState project, string id)
        {
            SemanticScheduleCatalog.Save(project, new[]
            {
                Definition(id, "", "", Array.Empty<string>(), Array.Empty<string>(), "{Id}")
            });
        }

        private static void StaleAndAmbiguousReferencesAreReported()
        {
            var project = Project();
            project.Floors.Add(new FloorDefinition("f1", "Duplicate Floor", 9));
            project.Zones.Add(new ZoneDefinition("z1", "Duplicate Zone"));
            project.Elements.Add(Element("e1", "Duplicate"));
            SemanticScheduleCatalog.Save(project, new[]
            {
                Definition("S-STALE", "F1", "Z1", new[] { "E1", "MISSING" }, Array.Empty<string>(), "{Id}")
            });

            var issues = new SemanticScheduleHealthService().Inspect(project);
            Has(issues, "SEMANTIC_SCHEDULE_AMBIGUOUS_FLOOR");
            Has(issues, "SEMANTIC_SCHEDULE_AMBIGUOUS_ZONE");
            Has(issues, "SEMANTIC_SCHEDULE_AMBIGUOUS_ELEMENT");
            Has(issues, "SEMANTIC_SCHEDULE_MISSING_ELEMENT");
        }

        private static void InvalidTemplateAndCatalogAreReportedReadOnly()
        {
            var project = Project();

            Throws<FormatException>(() =>
                Definition("S-DIRECT-BAD-TEMPLATE", "", "", Array.Empty<string>(), Array.Empty<string>(), "{Unsupported}"));

            SemanticScheduleCatalog.Save(project, new[]
            {
                Definition("S-BAD-TEMPLATE", "", "", Array.Empty<string>(), Array.Empty<string>(), "{Id}")
            });
            var validPayload = project.Metadata[SemanticScheduleCatalog.MetadataKey];
            project.Metadata[SemanticScheduleCatalog.MetadataKey] = validPayload.Replace(
                "template=\"{Id}\"",
                "template=\"{Unsupported}\"",
                StringComparison.Ordinal);

            var version = project.ChangeVersion;
            var payload = project.Metadata[SemanticScheduleCatalog.MetadataKey];
            Has(new SemanticScheduleHealthService().Inspect(project), "SEMANTIC_SCHEDULE_TEMPLATE_INVALID");
            Equal(version, project.ChangeVersion);
            Equal(payload, project.Metadata[SemanticScheduleCatalog.MetadataKey]);

            project.Metadata[SemanticScheduleCatalog.MetadataKey] = "<semanticSchedules version='1'><schedule";
            var corruptPayload = project.Metadata[SemanticScheduleCatalog.MetadataKey];
            Has(new SemanticScheduleHealthService().Inspect(project), "SEMANTIC_SCHEDULE_CATALOG_INVALID");
            Equal(corruptPayload, project.Metadata[SemanticScheduleCatalog.MetadataKey]);
        }

        private static void ComprehensiveHealthIncludesSemanticScheduleProvider()
        {
            var project = Project();
            SemanticScheduleCatalog.Save(project, new[]
            {
                Definition("S-MISSING-FLOOR", "MISSING", "", Array.Empty<string>(), Array.Empty<string>(), "{Id}")
            });
            Has(new ComprehensiveModelHealthService().Inspect(project), "SEMANTIC_SCHEDULE_MISSING_FLOOR");
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("P-SCHEDULE-HEALTH", "Schedule Health");
            project.Floors.Add(new FloorDefinition("F1", "Level 1", 0));
            project.Floors.Add(new FloorDefinition("F2", "Level 2", 3.5));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("Z2", "Zone 2"));
            project.Elements.Add(Element("E1", "B1"));
            return project;
        }

        private static ProjectElement Element(string id, string mark)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, "F1", "Z1");
            element.Properties["Mark"] = mark;
            return element;
        }

        private static SemanticScheduleDefinition Definition(
            string id,
            string floorId,
            string zoneId,
            string[] include,
            string[] exclude,
            string template)
        {
            return new SemanticScheduleDefinition(
                id,
                id,
                id,
                new[] { ElementCategory.Beam },
                floorId,
                zoneId,
                include,
                exclude,
                new[] { new SemanticDocumentationColumn("Value", template) });
        }

        private static void Has(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new Exception("Expected Model Health code " + code + ".");
        }

        private static void HasProviderFailure(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string providerName)
        {
            if (!issues.Any(x =>
                    string.Equals(x.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                    (x.Message ?? string.Empty).IndexOf(providerName, StringComparison.Ordinal) >= 0))
                throw new Exception("Expected HEALTH_PROVIDER_FAILED for " + providerName + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class SemanticScheduleHealthSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticScheduleHealthSmoke.Run();
    }
}
