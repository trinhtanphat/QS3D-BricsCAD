using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeRemapProvenanceImporterSmoke
    {
        internal static void Run()
        {
            PlanBuildsOneToOneSourceTargetLineage();
            ImportKeepsRawHandlesOutsideMappedTargetOwnership();
            MissingSourceFingerprintFailsBeforeMutation();
            TargetMapRejectsMissingTargetElement();
            TargetMapRejectsNegativeKnownCountBeforeEnumeration();
            TargetMapRejectsOversizedKnownCountBeforeEnumeration();
        }

        private static void PlanBuildsOneToOneSourceTargetLineage()
        {
            var target = TargetProject();
            var updated = new DateTime(2026, 8, 11, 0, 50, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true));

            var plan = ProjectInterchangeRemapProvenanceImporter.Plan(target, json);

            True(plan.CanImport);
            Equal(2, plan.MappingCount);
            Equal(2, plan.ProvenanceHandleCount);
            var mappedCollision = plan.ElementMappings["E1"];
            True(!string.Equals("E1", mappedCollision, StringComparison.OrdinalIgnoreCase));
            Equal("E2", plan.ElementMappings["E2"]);
            Equal(1, target.Elements.Count);
            Equal(0, target.Metadata.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportKeepsRawHandlesOutsideMappedTargetOwnership()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: true));
            var plan = ProjectInterchangeRemapProvenanceImporter.Plan(target, json);
            var mappedE1 = plan.ElementMappings["E1"];
            var mappedE2 = plan.ElementMappings["E2"];

            var result = ProjectInterchangeRemapProvenanceImporter.Import(target, json);

            Equal(2, result.SemanticResult.ElementsAdded);
            Equal(2, result.ProvenanceResult.SourceHandlesStored);
            Equal(2, result.TargetMapResult.MappingsStored);
            Equal(mappedE1, ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E1"));
            Equal(mappedE2, ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "E2"));
            Equal("SOURCE-H1|SOURCE-H2", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE", "E1")));

            var original = target.FindElement("E1") ?? throw new Exception("Original target element missing.");
            Equal("TARGET-H", original.SourceHandles.Single());
            Equal("TARGET-DWG", original.DrawingFingerprint);

            var importedCollision = target.FindElement(mappedE1) ?? throw new Exception("Mapped collision element missing.");
            var importedNew = target.FindElement(mappedE2) ?? throw new Exception("Mapped new element missing.");
            Equal(0, importedCollision.SourceHandles.Count);
            Equal(string.Empty, importedCollision.DrawingFingerprint);
            Equal(0, importedNew.SourceHandles.Count);
            Equal(string.Empty, importedNew.DrawingFingerprint);
            Equal(mappedE1, importedNew.DependsOn.Single());
            Equal(ElementDirtyFlags.All, importedCollision.Dirty);
            Equal(ElementDirtyFlags.All, importedNew.Dirty);

            Equal(ProjectInterchangeRemapProvenanceImporter.ImportMode, target.Metadata["Interchange.LastImport.Mode"]);
            Equal("2", target.Metadata[ProjectInterchangeRemapProvenanceImporter.LastMappingCountKey]);
            Equal("2", target.Metadata[ProjectInterchangeRemapProvenanceImporter.LastProvenanceHandleCountKey]);
            Equal("ImportInterchangeRemapWithSourceHandleProvenance", target.AuditEvents.Last().Action);

            var portableTarget = ProjectInterchangeJsonExporter.Build(target);
            False(portableTarget.Contains("SOURCE-H1", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains("SOURCE-H2", StringComparison.OrdinalIgnoreCase));
            False(portableTarget.Contains(ProjectInterchangeProvenanceTargetMap.MetadataPrefix, StringComparison.Ordinal));
        }

        private static void MissingSourceFingerprintFailsBeforeMutation()
        {
            var target = TargetProject();
            var json = ProjectInterchangeJsonExporter.Build(SourceProject(withFingerprint: false));
            var beforeCount = target.Elements.Count;

            Throws<InvalidOperationException>(() => ProjectInterchangeRemapProvenanceImporter.Plan(target, json));
            Throws<InvalidOperationException>(() => ProjectInterchangeRemapProvenanceImporter.Import(target, json));
            Equal(beforeCount, target.Elements.Count);
            Equal(0, target.Metadata.Count);
        }

        private static void TargetMapRejectsMissingTargetElement()
        {
            var target = TargetProject();
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SOURCE-E"] = "MISSING-TARGET"
            };
            Throws<InvalidOperationException>(() => ProjectInterchangeProvenanceTargetMap.Store(target, "SOURCE", "SOURCE-DWG", mapping));
            Equal(string.Empty, ProjectInterchangeProvenanceTargetMap.ReadTargetElementId(target, "SOURCE", "SOURCE-E"));
        }

        private static void TargetMapRejectsNegativeKnownCountBeforeEnumeration()
        {
            var target = TargetProject();
            var mapping = new KnownCountDictionary(-1);

            Throws<InvalidOperationException>(() => ProjectInterchangeProvenanceTargetMap.Store(target, "SOURCE", "SOURCE-DWG", mapping));

            Equal(1, mapping.CountReads);
            False(mapping.EnumerationAttempted);
            Equal(0, target.Metadata.Count);
        }

        private static void TargetMapRejectsOversizedKnownCountBeforeEnumeration()
        {
            var target = TargetProject();
            var mapping = new KnownCountDictionary(50001);

            Throws<InvalidOperationException>(() => ProjectInterchangeProvenanceTargetMap.Store(target, "SOURCE", "SOURCE-DWG", mapping));

            Equal(1, mapping.CountReads);
            False(mapping.EnumerationAttempted);
            Equal(0, target.Metadata.Count);
        }

        private static ProjectState TargetProject()
        {
            var target = new ProjectState("TARGET", "Target")
            {
                DrawingFingerprint = "TARGET-DWG"
            };
            var existing = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = target.DrawingFingerprint
            };
            existing.SourceHandles.Add("TARGET-H");
            existing.Properties["Mark"] = "TARGET";
            target.Elements.Add(existing);
            return target;
        }

        private static ProjectState SourceProject(bool withFingerprint)
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = withFingerprint ? "SOURCE-DWG" : string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 11, 0, 48, 0, DateTimeKind.Utc)
            };
            var collision = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            collision.SourceHandles.Add("SOURCE-H2");
            collision.SourceHandles.Add("SOURCE-H1");
            collision.Properties["Mark"] = "SOURCE-1";
            source.Elements.Add(collision);

            var added = new ProjectElement("E2", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            added.DependsOn.Add("E1");
            added.Properties["Mark"] = "SOURCE-2";
            source.Elements.Add(added);
            return source;
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

        private sealed class KnownCountDictionary : IReadOnlyDictionary<string, string>
        {
            private readonly int _count;

            public KnownCountDictionary(int count)
            {
                _count = count;
            }

            public bool EnumerationAttempted { get; private set; }
            public int CountReads { get; private set; }
            public int Count
            {
                get
                {
                    CountReads++;
                    return _count;
                }
            }

            public IEnumerable<string> Keys => throw new Exception("Keys must not be read for an invalid known Count.");
            public IEnumerable<string> Values => throw new Exception("Values must not be read for an invalid known Count.");
            public string this[string key] => throw new Exception("Indexer must not be read for an invalid known Count.");

            public bool ContainsKey(string key) => throw new Exception("ContainsKey must not be called for an invalid known Count.");
            public bool TryGetValue(string key, out string value) => throw new Exception("TryGetValue must not be called for an invalid known Count.");

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new Exception("Enumeration must not start for an invalid known Count.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class ProjectInterchangeRemapProvenanceImporterSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapProvenanceImporterSmoke.Run();
    }
}
