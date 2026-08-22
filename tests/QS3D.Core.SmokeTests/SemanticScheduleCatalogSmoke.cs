using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCatalogSmoke
    {
        internal static void Run()
        {
            SaveLoadRoundTripIsDeterministic();
            PersistedCategoriesRequireCanonicalNames();
            PersistedSchemaRequiresCanonicalShape();
            UpsertAndRemoveSupportMultipleDefinitions();
            BuildFiltersAndUsesCanonicalTemplateRenderer();
            EmptySelectionBuildsHeaderOnlyTable();
            DefinitionCollectionsAreDefensivelyImmutable();
            NullProjectElementsFailClosed();
            StaleReferencesFailClosedAtRenderTime();
            DuplicateDefinitionsAndOverlappingListsFailClosed();
            LoadAcceptsCapacityAndRejectsMalformedExcessByCapacity();
            MalformedScheduleWithinCapacityKeepsSchemaFailure();
        }

        private static void SaveLoadRoundTripIsDeterministic()
        {
            var project = Project();
            var definition = Definition("S1", "Beam schedule", "BEAMS", "F1", "Z1", new[] { "E1" }, new[] { "E2" });
            SemanticScheduleCatalog.Save(project, new[] { definition });
            var payload = project.Metadata[SemanticScheduleCatalog.MetadataKey];
            var version = project.ChangeVersion;

            var loaded = SemanticScheduleCatalog.Load(project);
            Equal(1, loaded.Count);
            Equal("S1", loaded[0].Id);
            Equal("Beam schedule", loaded[0].Name);
            Equal("BEAMS", loaded[0].Title);
            Equal(ElementCategory.Beam, loaded[0].Categories.Single());
            Equal("Id|Mark|Length", string.Join("|", loaded[0].Columns.Select(x => x.Header)));

            SemanticScheduleCatalog.Save(project, loaded);
            Equal(payload, project.Metadata[SemanticScheduleCatalog.MetadataKey]);
            Equal(version, project.ChangeVersion);
        }

        private static void PersistedCategoriesRequireCanonicalNames()
        {
            var project = Project();
            SemanticScheduleCatalog.Save(project, new[] { Definition("S1", "Beam schedule", "BEAMS", "", "", Array.Empty<string>(), Array.Empty<string>()) });
            var canonical = project.Metadata[SemanticScheduleCatalog.MetadataKey];
            var categoryName = ElementCategory.Beam.ToString();
            var canonicalToken = "value=\"" + categoryName + "\"";

            project.Metadata[SemanticScheduleCatalog.MetadataKey] = canonical.Replace(canonicalToken, "value=\"" + categoryName.ToLowerInvariant() + "\"");
            Throws<InvalidDataException>(() => SemanticScheduleCatalog.Load(project));

            project.Metadata[SemanticScheduleCatalog.MetadataKey] = canonical.Replace(
                canonicalToken,
                "value=\"" + Convert.ToInt64(ElementCategory.Beam, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) + "\"");
            Throws<InvalidDataException>(() => SemanticScheduleCatalog.Load(project));

            project.Metadata[SemanticScheduleCatalog.MetadataKey] = canonical.Replace(canonicalToken, "value=\" " + categoryName + " \"");
            Throws<InvalidDataException>(() => SemanticScheduleCatalog.Load(project));
        }

        private static void PersistedSchemaRequiresCanonicalShape()
        {
            var project = Project();
            SemanticScheduleCatalog.Save(project, new[] { Definition("S1", "Beam schedule", "BEAMS", "", "", Array.Empty<string>(), Array.Empty<string>()) });
            var canonical = project.Metadata[SemanticScheduleCatalog.MetadataKey];

            foreach (var containerName in new[] { "categories", "include", "exclude", "columns" })
            {
                var root = XDocument.Parse(canonical).Root ?? throw new Exception("Catalog root missing.");
                var schedule = root.Element("schedule") ?? throw new Exception("Schedule missing.");
                (schedule.Element(containerName) ?? throw new Exception("Canonical container missing.")).Remove();
                project.Metadata[SemanticScheduleCatalog.MetadataKey] = root.ToString(SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => SemanticScheduleCatalog.Load(project));
            }

            foreach (var attributeName in new[] { "id", "name", "title", "floorId", "zoneId" })
            {
                var root = XDocument.Parse(canonical).Root ?? throw new Exception("Catalog root missing.");
                var schedule = root.Element("schedule") ?? throw new Exception("Schedule missing.");
                (schedule.Attribute(attributeName) ?? throw new Exception("Canonical attribute missing.")).Remove();
                project.Metadata[SemanticScheduleCatalog.MetadataKey] = root.ToString(SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => SemanticScheduleCatalog.Load(project));
            }
        }

        private static void UpsertAndRemoveSupportMultipleDefinitions()
        {
            var project = Project();
            SemanticScheduleCatalog.Upsert(project, Definition("S1", "One", "ONE", "", "", Array.Empty<string>(), Array.Empty<string>()));
            SemanticScheduleCatalog.Upsert(project, Definition("S2", "Two", "TWO", "", "", Array.Empty<string>(), Array.Empty<string>()));
            Equal(2, SemanticScheduleCatalog.Load(project).Count);

            SemanticScheduleCatalog.Upsert(project, Definition("S1", "One updated", "ONE UPDATED", "F1", "", Array.Empty<string>(), Array.Empty<string>()));
            var loaded = SemanticScheduleCatalog.Load(project);
            Equal(2, loaded.Count);
            Equal("One updated", loaded.Single(x => x.Id == "S1").Name);
            Equal("F1", loaded.Single(x => x.Id == "S1").FloorId);

            True(SemanticScheduleCatalog.Remove(project, "S2"));
            Equal(1, SemanticScheduleCatalog.Load(project).Count);
            False(SemanticScheduleCatalog.Remove(project, "missing"));
        }

        private static void BuildFiltersAndUsesCanonicalTemplateRenderer()
        {
            var project = Project();
            var definition = Definition("S1", "Beam schedule", "BEAMS", "F1", "Z1", new[] { "E1" }, new[] { "E2" });
            var table = SemanticScheduleCatalog.Build(project, definition);

            Equal("BEAMS", table.Title);
            Equal(1, table.Rows.Count);
            Equal("E1", table.Rows[0].ElementId);
            Equal("E1", table.Rows[0].Cells[0]);
            Equal("B1", table.Rows[0].Cells[1]);
            Equal("4.5", table.Rows[0].Cells[2]);
            Equal("Id|Mark|Length", string.Join("|", table.Headers));

            var source = project.FindElement("E1") ?? throw new Exception("Element missing.");
            Equal(4.5, source.Quantities["LengthM"]);
        }

        private static void EmptySelectionBuildsHeaderOnlyTable()
        {
            var project = Project();
            var definition = Definition(
                "S-EMPTY",
                "Empty schedule",
                "EMPTY",
                "F2",
                "Z2",
                Array.Empty<string>(),
                Array.Empty<string>());

            var table = SemanticScheduleCatalog.Build(project, definition);
            Equal("EMPTY", table.Title);
            Equal("Id|Mark|Length", string.Join("|", table.Headers));
            Equal(0, table.Rows.Count);
        }

        private static void DefinitionCollectionsAreDefensivelyImmutable()
        {
            var definition = Definition(
                "S-IMMUTABLE",
                "Immutable",
                "IMMUTABLE",
                "F1",
                "Z1",
                new[] { "E1" },
                Array.Empty<string>());

            Throws<NotSupportedException>(() => ((IList<ElementCategory>)definition.Categories)[0] = ElementCategory.Column);
            Throws<NotSupportedException>(() => ((IList<string>)definition.IncludeElementIds)[0] = "E2");
            Throws<NotSupportedException>(() => ((IList<SemanticDocumentationColumn>)definition.Columns)[0] = new SemanticDocumentationColumn("Other", "{Id}"));
            Equal(ElementCategory.Beam, definition.Categories[0]);
            Equal("E1", definition.IncludeElementIds[0]);
            Equal("Id", definition.Columns[0].Header);
        }

        private static void NullProjectElementsFailClosed()
        {
            var project = Project();
            project.Elements.Add(null!);
            var definition = Definition("S-NULL", "Null guard", "NULL GUARD", "", "", Array.Empty<string>(), Array.Empty<string>());
            Throws<InvalidOperationException>(() => SemanticScheduleCatalog.Build(project, definition));
        }

        private static void StaleReferencesFailClosedAtRenderTime()
        {
            var project = Project();
            var staleFloor = Definition("S1", "Stale", "STALE", "MISSING", "", Array.Empty<string>(), Array.Empty<string>());
            SemanticScheduleCatalog.Save(project, new[] { staleFloor });
            Throws<InvalidOperationException>(() => SemanticScheduleCatalog.Build(project, SemanticScheduleCatalog.Load(project)[0]));

            var staleElement = Definition("S2", "Stale element", "STALE ELEMENT", "", "", new[] { "MISSING" }, Array.Empty<string>());
            Throws<InvalidOperationException>(() => SemanticScheduleCatalog.Build(project, staleElement));
        }

        private static void DuplicateDefinitionsAndOverlappingListsFailClosed()
        {
            var project = Project();
            Throws<InvalidOperationException>(() => SemanticScheduleCatalog.Save(project, new[]
            {
                Definition("S1", "Same", "A", "", "", Array.Empty<string>(), Array.Empty<string>()),
                Definition("s1", "Other", "B", "", "", Array.Empty<string>(), Array.Empty<string>())
            }));

            Throws<InvalidOperationException>(() => SemanticScheduleCatalog.Save(project, new[]
            {
                Definition("S3", "Overlap", "OVERLAP", "", "", new[] { "E1" }, new[] { "e1" })
            }));
        }

        private static void LoadAcceptsCapacityAndRejectsMalformedExcessByCapacity()
        {
            var project = Project();
            var definitions = Enumerable.Range(1, 128)
                .Select(index => Definition(
                    "S-CAP-" + index,
                    "Capacity " + index,
                    "CAPACITY " + index,
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>()))
                .ToArray();
            SemanticScheduleCatalog.Save(project, definitions);
            Equal(128, SemanticScheduleCatalog.Load(project).Count);

            var root = XDocument.Parse(project.Metadata[SemanticScheduleCatalog.MetadataKey]).Root
                ?? throw new Exception("Catalog root missing.");
            root.Add(new XElement(
                "schedule",
                new XAttribute("unsupported-excess-detail", "must-not-be-validated")));
            project.Metadata[SemanticScheduleCatalog.MetadataKey] = root.ToString(SaveOptions.DisableFormatting);

            try
            {
                SemanticScheduleCatalog.Load(project);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Semantic schedule catalog exceeds the supported 128 definitions.", ex.Message);
                return;
            }
            catch (InvalidDataException ex)
            {
                throw new Exception("The malformed 129th schedule reached detailed schema validation before the catalog capacity guard.", ex);
            }

            throw new Exception("Expected Semantic Schedule load capacity rejection.");
        }

        private static void MalformedScheduleWithinCapacityKeepsSchemaFailure()
        {
            var project = Project();
            SemanticScheduleCatalog.Save(project, new[]
            {
                Definition(
                    "S-SCHEMA",
                    "Schema failure",
                    "SCHEMA FAILURE",
                    string.Empty,
                    string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<string>())
            });
            var root = XDocument.Parse(project.Metadata[SemanticScheduleCatalog.MetadataKey]).Root
                ?? throw new Exception("Catalog root missing.");
            (root.Element("schedule") ?? throw new Exception("Schedule missing."))
                .Add(new XAttribute("unsupported-within-capacity", "must-fail-schema"));
            project.Metadata[SemanticScheduleCatalog.MetadataKey] = root.ToString(SaveOptions.DisableFormatting);

            Throws<InvalidDataException>(() => SemanticScheduleCatalog.Load(project));
        }

        private static SemanticScheduleDefinition Definition(
            string id,
            string name,
            string title,
            string floorId,
            string zoneId,
            string[] include,
            string[] exclude)
        {
            return new SemanticScheduleDefinition(
                id,
                name,
                title,
                new[] { ElementCategory.Beam },
                floorId,
                zoneId,
                include,
                exclude,
                new[]
                {
                    new SemanticDocumentationColumn("Id", "{Id}"),
                    new SemanticDocumentationColumn("Mark", "{P:Mark}"),
                    new SemanticDocumentationColumn("Length", "{Q:LengthM}")
                });
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("P", "Project");
            project.Floors.Add(new FloorDefinition("F1", "Level 1", 0));
            project.Floors.Add(new FloorDefinition("F2", "Level 2", 3.5));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("Z2", "Zone 2"));

            project.Elements.Add(Element("E1", ElementCategory.Beam, "F1", "Z1", "B1", 4.5));
            project.Elements.Add(Element("E2", ElementCategory.Beam, "F1", "Z1", "B2", 5.5));
            project.Elements.Add(Element("E3", ElementCategory.Beam, "F2", "Z1", "B3", 6.5));
            project.Elements.Add(Element("E4", ElementCategory.Column, "F1", "Z1", "C1", 3.0));
            return project;
        }

        private static ProjectElement Element(string id, ElementCategory category, string floor, string zone, string mark, double length)
        {
            var element = new ProjectElement(id, category, "", floor, zone);
            element.Properties["Mark"] = mark;
            element.Quantities["LengthM"] = length;
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
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
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class SemanticScheduleCatalogSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticScheduleCatalogSmoke.Run();
    }
}
