using System;
using System.IO;
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
            NonCanonicalHandleAndDependencyFailBeforePersistence();
            NullAuditEventFailsClosed();
            NonUtcTimestampFailsBeforePersistence();
            UndefinedCategoryFailsClosed();
        }

        private static void PaddedMapKeyFailsBeforePersistence()
        {
            var project = NewProject("map-key");
            project.Metadata[" padded "] = "value";
            RejectSave(project, "Padded metadata key was silently persisted/normalized.");

            project = NewProject("property-key");
            var element = AddElement(project);
            element.Properties[" WidthM "] = "1.2";
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
            RejectSave(project, "Padded quantity name was silently persisted/normalized.");
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

        private static void NonCanonicalHandleAndDependencyFailBeforePersistence()
        {
            var project = NewProject("handle-key");
            var element = AddElement(project);
            element.SourceHandles.Add(" 1A ");
            RejectSave(project, "Padded source handle was silently persisted/normalized.");

            project = NewProject("dependency-key");
            element = AddElement(project);
            element.DependsOn.Add(" E2 ");
            RejectSave(project, "Padded dependency id was silently persisted/normalized.");

            project = NewProject("blank-handle");
            element = AddElement(project);
            element.SourceHandles.Add("   ");
            RejectSave(project, "Blank source handle was silently dropped during persistence.");

            project = NewProject("duplicate-handle-exact");
            element = AddElement(project);
            element.SourceHandles.Add("1A");
            element.SourceHandles.Add("1A");
            RejectSave(project, "Exact duplicate source handles were persisted even though the QSDB reader rejects them.");

            project = NewProject("duplicate-handle-case");
            element = AddElement(project);
            element.SourceHandles.Add("1A");
            element.SourceHandles.Add("1a");
            RejectSave(project, "Case-only duplicate source handles were persisted even though source identity is case-insensitive.");

            project = NewProject("duplicate-dependency-exact");
            element = AddElement(project);
            element.DependsOn.Add("E2");
            element.DependsOn.Add("E2");
            RejectSave(project, "Exact duplicate dependency ids were persisted even though the QSDB reader rejects them.");

            project = NewProject("duplicate-dependency-case");
            element = AddElement(project);
            element.DependsOn.Add("E2");
            element.DependsOn.Add("e2");
            RejectSave(project, "Case-only duplicate dependency ids were persisted even though dependency identity is case-insensitive.");
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
            project.UpdatedUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Unspecified);
            RejectSave(project, "Unspecified project UpdatedUtc was converted using machine timezone during persistence.");

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

            var project = NewProject("rule-category");
            project.QuantityRules.Add(new QuantityRule("R1", (ElementCategory)999, "Area", "1", "v1"));
            RejectSave(project, "Undefined quantity-rule category was persisted.");

            project = NewProject("load-category");
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
