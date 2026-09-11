using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbCanonicalPersistenceSmoke
    {
        public static void Run()
        {
            PaddedMapKeyFailsBeforePersistence();
            PaddedMapKeyFailsOnLoad();
            PaddedQuantityNameFailsBeforePersistence();
            DuplicateQuantityNameFailsOnLoad();
            NegativeQuantityFailsClosed();
            NonCanonicalHandleAndDependencyFailBeforePersistence();
            NullAuditEventFailsClosed();
            NonUtcTimestampFailsBeforePersistence();
            UndefinedCategoryFailsClosed();
        }

        private static void PaddedMapKeyFailsBeforePersistence()
        {
            var project = NewProject("map-key");
            var rejectedAtDomainBoundary = false;
            try { project.Metadata[" padded "] = "value"; }
            catch (ArgumentException) { rejectedAtDomainBoundary = true; }
            if (!rejectedAtDomainBoundary)
                throw new Exception("Padded metadata key reached persistence instead of failing at the domain boundary.");
            if (project.Metadata.ContainsKey(" padded "))
                throw new Exception("Rejected padded metadata key was retained after public mutation failure.");

            var metadataItemsField = project.Metadata.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Project metadata backing dictionary field is unavailable.");
            var metadataItems = metadataItemsField.GetValue(project.Metadata) as IDictionary<string, string>
                ?? throw new InvalidOperationException("Project metadata backing dictionary is unavailable.");
            metadataItems[" padded "] = "value";
            RejectSave(project, "Padded metadata key was silently persisted/normalized.");
            metadataItems.Remove(" padded ");

            project = NewProject("property-key");
            var element = AddElement(project);
            var propertiesField = typeof(ProjectElement).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Project element property backing dictionary field is unavailable.");
            var propertyItems = propertiesField.GetValue(element) as IDictionary<string, string>
                ?? throw new InvalidOperationException("Project element property backing dictionary is unavailable.");
            propertyItems[" WidthM "] = "1.2";
            RejectSave(project, "Padded element property key was silently persisted/normalized.");
        }

        private static void PaddedMapKeyFailsOnLoad()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-map-key-load-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = NewProject("map-key-load");
                project.Metadata["CanonicalKey"] = "value";
                var store = new QsdbProjectStore();
                store.Save(project, path);

                var original = File.ReadAllText(path);
                var tampered = original.Replace("name=\"CanonicalKey\"", "name=\" CanonicalKey \"");
                if (string.Equals(original, tampered, StringComparison.Ordinal))
                    throw new Exception("Canonical metadata key fixture was not found in serialized QSDB.");
                File.WriteAllText(path, tampered);

                var rejected = false;
                try { store.Load(path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception("Padded persisted metadata key was silently normalized while loading QSDB.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void PaddedQuantityNameFailsBeforePersistence()
        {
            var project = NewProject("quantity-key");
            var element = AddElement(project);
            element.Quantities[" AreaM2 "] = 3d;
            var semanticItems = QuantityBacking(element);
            if (!semanticItems.ContainsKey("AreaM2") || semanticItems.ContainsKey(" AreaM2 ") || semanticItems["AreaM2"] != 3d)
                throw new Exception("Padded semantic quantity input was not canonicalized before persistence.");

            project = NewProject("quantity-key-persisted-corruption");
            element = AddElement(project);
            SeedPersistedQuantity(element, " AreaM2 ", 3d);
            RejectSave(project, "Padded persisted quantity name was silently persisted/normalized.");
        }

        private static void DuplicateQuantityNameFailsOnLoad()
        {
            RejectDuplicateQuantityOnLoad("AreaM2");
            RejectDuplicateQuantityOnLoad("aream2");
        }

        private static void RejectDuplicateQuantityOnLoad(string duplicateName)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-duplicate-quantity-load-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = NewProject("duplicate-quantity-load");
                var first = AddElement(project);
                first.SetQuantity("AreaM2", 3d);
                var second = new ProjectElement("E2", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
                second.SetQuantity("AreaM2", 4d);
                project.Elements.Add(second);

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var baseline = store.Load(path);
                if (baseline.Elements.Count != 2 || baseline.Elements[0].Quantities["AreaM2"] != 3d || baseline.Elements[1].Quantities["AreaM2"] != 4d)
                    throw new Exception("Unique per-element quantities did not roundtrip before duplicate-quantity tampering.");

                var document = XDocument.Load(path, LoadOptions.None);
                var firstPersistedElement = document.Root?.Element("elements")?.Element("element")
                    ?? throw new Exception("Serialized QSDB element fixture was not found.");
                var quantities = firstPersistedElement.Element("quantities")
                    ?? throw new Exception("Serialized QSDB quantity fixture was not found.");
                quantities.Add(new XElement("q", new XAttribute("name", duplicateName), new XAttribute("value", "99")));
                document.Save(path, SaveOptions.DisableFormatting);

                var rejected = false;
                try { store.Load(path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception("Duplicate persisted element quantity name was silently overwritten while loading QSDB: " + duplicateName);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void NegativeQuantityFailsClosed()
        {
            var rejectedProject = NewProject("negative-quantity-save");
            var rejectedElement = AddElement(rejectedProject);
            var rejectedAtDomainBoundary = false;
            try { rejectedElement.Quantities["AreaM2"] = -1d; }
            catch (ArgumentOutOfRangeException) { rejectedAtDomainBoundary = true; }
            if (!rejectedAtDomainBoundary)
                throw new Exception("Negative element quantity bypassed semantic dictionary admission.");
            if (rejectedElement.Quantities.Count != 0)
                throw new Exception("Rejected negative semantic quantity changed element state.");

            SeedPersistedQuantity(rejectedElement, "AreaM2", -1d);
            RejectSave(rejectedProject, "Negative persisted element quantity was published after semantic admission rejected it.");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-negative-quantity-fallback-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = NewProject("negative-quantity-fallback");
                var element = AddElement(project);
                element.SetQuantity("AreaM2", 0d);
                var store = new QsdbProjectStore();
                store.Save(project, path);

                element.SetQuantity("AreaM2", 2d);
                store.Save(project, path);
                if (!File.Exists(path + ".bak"))
                    throw new Exception("QSDB backup fixture was not created before negative-quantity tampering.");

                var positive = store.Load(path);
                if (positive.Elements.Count != 1 || positive.Elements[0].Quantities["AreaM2"] != 2d)
                    throw new Exception("Positive element quantity did not roundtrip before negative-quantity tampering.");

                var document = XDocument.Load(path, LoadOptions.None);
                var persistedQuantity = document.Root?.Element("elements")?.Element("element")?.Element("quantities")?.Element("q")
                    ?? throw new Exception("Serialized QSDB quantity fixture was not found for negative-value tampering.");
                persistedQuantity.SetAttributeValue("value", "-1");
                document.Save(path, SaveOptions.DisableFormatting);

                var recovered = store.LoadWithBackupFallback(path);
                if (!recovered.RecoveredFromBackup)
                    throw new Exception("Negative persisted element quantity bypassed QSDB backup recovery.");
                if (!string.Equals(recovered.SourcePath, Path.GetFullPath(path + ".bak"), StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Negative persisted element quantity recovered from an unexpected QSDB source.");
                if (recovered.Project.Elements.Count != 1 || recovered.Project.Elements[0].Quantities["AreaM2"] != 0d)
                    throw new Exception("Zero element quantity did not roundtrip from the validated QSDB backup.");
                if (string.IsNullOrWhiteSpace(recovered.PrimaryFailureMessage))
                    throw new Exception("QSDB backup recovery did not retain the primary negative-quantity validation failure.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void NonCanonicalHandleAndDependencyFailBeforePersistence()
        {
            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("handle-key"),
                true,
                " 1A ",
                false,
                "Padded source handle was silently persisted/normalized.");

            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("dependency-key"),
                false,
                " E2 ",
                false,
                "Padded dependency id was silently persisted/normalized.");

            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("blank-handle"),
                true,
                "   ",
                false,
                "Blank source handle was silently dropped during persistence.");

            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("duplicate-handle-exact"),
                true,
                "1A",
                true,
                "Exact duplicate source handles were persisted even though the QSDB reader rejects them.");

            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("duplicate-handle-case"),
                true,
                "1a",
                true,
                "Case-only duplicate source handles were persisted even though source identity is case-insensitive.");

            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("duplicate-dependency-exact"),
                false,
                "E2",
                true,
                "Exact duplicate dependency ids were persisted even though the QSDB reader rejects them.");

            VerifySemanticBoundaryThenPersistedCorruption(
                NewProject("duplicate-dependency-case"),
                false,
                "e2",
                true,
                "Case-only duplicate dependency ids were persisted even though dependency identity is case-insensitive.");
        }

        private static void VerifySemanticBoundaryThenPersistedCorruption(
            ProjectState project,
            bool sourceHandle,
            string value,
            bool duplicate,
            string persistenceMessage)
        {
            var element = AddElement(project);
            var relation = sourceHandle ? element.SourceHandles : element.DependsOn;
            var canonical = value.Trim();

            if (duplicate)
            {
                var existing = sourceHandle && string.Equals(value, "1a", StringComparison.Ordinal) ? "1A" :
                    !sourceHandle && string.Equals(value, "e2", StringComparison.Ordinal) ? "E2" : canonical;
                relation.Add(existing);
                var countBeforeRejectedMutation = relation.Count;
                var rejectedAtDomainBoundary = false;
                try { relation.Add(value); }
                catch (ArgumentException) { rejectedAtDomainBoundary = true; }
                if (!rejectedAtDomainBoundary)
                    throw new Exception("Duplicate relation identity was accepted by the semantic domain boundary.");
                if (relation.Count != countBeforeRejectedMutation)
                    throw new Exception("Rejected duplicate relation mutation changed the public collection.");
            }
            else if (canonical.Length == 0)
            {
                var rejectedAtDomainBoundary = false;
                try { relation.Add(value); }
                catch (ArgumentException) { rejectedAtDomainBoundary = true; }
                if (!rejectedAtDomainBoundary)
                    throw new Exception("Blank relation identity was accepted by the semantic domain boundary.");
                if (relation.Count != 0)
                    throw new Exception("Rejected blank relation mutation changed the public collection.");
            }
            else
            {
                relation.Add(value);
                if (relation.Count != 1 || !string.Equals(relation[0], canonical, StringComparison.Ordinal))
                    throw new Exception("Padded semantic relation input was not canonicalized deterministically.");
                relation.Clear();
            }

            AddPersistedRelation(element, sourceHandle, value);
            RejectSave(project, persistenceMessage);
        }

        private static void AddPersistedRelation(ProjectElement element, bool sourceHandle, string value)
        {
            var relation = sourceHandle ? element.SourceHandles : element.DependsOn;
            var valuesField = relation.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement relation backing list field is unavailable.");
            var values = valuesField.GetValue(relation) as List<string>
                ?? throw new InvalidOperationException("ProjectElement relation backing list is unavailable.");
            values.Add(value);
        }

        private static IDictionary<string, double> QuantityBacking(ProjectElement element)
        {
            var field = typeof(ProjectElement).GetField("_quantityValues", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary field is unavailable.");
            return field.GetValue(element) as IDictionary<string, double>
                ?? throw new InvalidOperationException("ProjectElement quantity backing dictionary is unavailable.");
        }

        private static void SeedPersistedQuantity(ProjectElement element, string name, double value)
        {
            QuantityBacking(element).Add(name, value);
        }

        private static void NullAuditEventFailsClosed()
        {
            var project = NewProject("null-audit");
            project.AuditEvents.Add(null!);
            RejectSave(project, "Null audit event reached serialization instead of failing validation.");
        }

        private static void NonUtcTimestampFailsBeforePersistence()
        {
            var project = NewProject("project-time");
            var priorUtc = project.UpdatedUtc;
            var rejectedAtDomainBoundary = false;
            try { project.UpdatedUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Unspecified); }
            catch (ArgumentException) { rejectedAtDomainBoundary = true; }
            if (!rejectedAtDomainBoundary)
                throw new Exception("Unspecified project UpdatedUtc reached persistence instead of failing at the domain boundary.");
            if (project.UpdatedUtc != priorUtc)
                throw new Exception("Rejected non-UTC project timestamp changed the prior canonical UTC value.");

            project = NewProject("audit-time");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Local),
                Action = "test"
            });
            RejectSave(project, "Local audit timestamp was converted using machine timezone during persistence.");
        }

        private static void UndefinedCategoryFailsClosed()
        {
            ThrowsArgumentOutOfRange(
                () => new ProjectFamily("F1", "Family", (ElementCategory)999),
                "Undefined family category reached persistence instead of failing at the domain boundary.");

            ThrowsArgumentOutOfRange(
                () => new ProjectElement("E1", (ElementCategory)999, string.Empty, string.Empty, string.Empty),
                "Undefined element category reached persistence instead of failing at the domain boundary.");

            ThrowsArgumentOutOfRange(
                () => new QuantityRule("R1", (ElementCategory)999, "Area", "1", "v1"),
                "Undefined quantity-rule category reached persistence instead of failing at the domain boundary.");

            var project = NewProject("load-category");
            project.Families.Add(new ProjectFamily("F1", "Family", ElementCategory.ArchitecturalWall));
            var path = Path.Combine(Path.GetTempPath(), "qs3d-category-load-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(project, path);
                File.WriteAllText(path, File.ReadAllText(path).Replace("category=\"ArchitecturalWall\"", "category=\"999\""));
                var rejected = false;
                try { store.Load(path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception("Undefined numeric category was accepted while loading QSDB.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static ProjectState NewProject(string id)
        {
            return new ProjectState(id, "Canonical persistence");
        }

        private static ProjectElement AddElement(ProjectState project)
        {
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return element;
        }

        private static void ThrowsArgumentOutOfRange(Action action, string message)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new Exception(message);
        }

        private static void RejectSave(ProjectState project, string message)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-canonical-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var rejected = false;
                try { new QsdbProjectStore().Save(project, path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception(message);
                if (File.Exists(path)) throw new Exception("Rejected non-canonical project still created a QSDB file.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }
    }
}
