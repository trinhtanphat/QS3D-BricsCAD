using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingSmoke
    {
        internal static void Run()
        {
            DeterministicFrozenCatalog();
            MappedAndUnmappedResolution();
            DuplicateAndAmbiguousMappingsFailClosed();
            InvalidIdentityFailsClosed();
        }

        private static void DeterministicFrozenCatalog()
        {
            var wallArea = Mapping("map-wall-area", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-wall", "work-wall-area");
            var slabVolume = Mapping("map-slab-volume", ElementCategory.Slab, "NetVolumeM3", "class-slab", "work-slab-volume");
            var wallLength = Mapping("map-wall-length", ElementCategory.ArchitecturalWall, "LengthM", "class-wall", "work-wall-length");
            var source = new List<MeasurementWorkItemMapping> { wallArea, slabVolume, wallLength };
            var first = new MeasurementWorkItemMappingCatalog(source);
            source.Clear();

            Equal(3, first.Mappings.Count, "Catalog must detach from the caller-owned mapping list.");
            SequenceEqual(
                new[] { "map-wall-length", "map-wall-area", "map-slab-volume" },
                first.Mappings.Select(x => x.MappingId),
                "Catalog ordering must be deterministic by canonical category/item identity.");

            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var second = new MeasurementWorkItemMappingCatalog(new[] { wallLength, slabVolume, wallArea });
                SequenceEqual(
                    first.Mappings.Select(x => x.MappingId),
                    second.Mappings.Select(x => x.MappingId),
                    "Catalog ordering must not depend on caller order or current culture.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void MappedAndUnmappedResolution()
        {
            var canonical = Mapping("map-wall-area", ElementCategory.ArchitecturalWall, "NetWallAreaM2", "class-wall", "work-wall-area");
            var catalog = new MeasurementWorkItemMappingCatalog(new[] { canonical });

            var mapped = catalog.Resolve(ElementCategory.ArchitecturalWall, "netwallaream2");
            True(mapped.IsMapped, "Case-insensitive measurement-item lookup must resolve a declared mapping.");
            Equal(MeasurementWorkItemMappingResolutionKind.Mapped, mapped.Kind, "Mapped resolution kind mismatch.");
            True(ReferenceEquals(canonical, mapped.Mapping), "Mapped resolution must expose the canonical stored mapping entry.");
            Equal("NetWallAreaM2", mapped.MeasurementItemId, "Mapped resolution must preserve canonical stored measurement-item spelling.");
            Equal("class-wall", mapped.Mapping!.ClassificationId, "Mapped classification identity mismatch.");
            Equal("work-wall-area", mapped.Mapping.WorkItemId, "Mapped work-item identity mismatch.");

            var unmapped = catalog.Resolve(ElementCategory.ArchitecturalWall, "GrossWallAreaM2");
            True(!unmapped.IsMapped, "Unknown measurement item must remain explicitly unmapped.");
            Equal(MeasurementWorkItemMappingResolutionKind.Unmapped, unmapped.Kind, "Unmapped resolution kind mismatch.");
            True(unmapped.Mapping == null, "Unmapped resolution must not invent a mapping entry.");
            Equal(ElementCategory.ArchitecturalWall, unmapped.Category, "Unmapped category identity mismatch.");
            Equal("GrossWallAreaM2", unmapped.MeasurementItemId, "Unmapped measurement identity mismatch.");
        }

        private static void DuplicateAndAmbiguousMappingsFailClosed()
        {
            ExpectThrows<ArgumentException>(() => new MeasurementWorkItemMappingCatalog(new[]
            {
                Mapping("map-a", ElementCategory.Slab, "NetVolumeM3", "class-a", "work-a"),
                Mapping("MAP-A", ElementCategory.Beam, "LengthM", "class-b", "work-b")
            }));

            ExpectThrows<ArgumentException>(() => new MeasurementWorkItemMappingCatalog(new[]
            {
                Mapping("map-a", ElementCategory.Slab, "NetVolumeM3", "class-a", "work-a"),
                Mapping("map-b", ElementCategory.Slab, "netvolumem3", "class-b", "work-b")
            }));

            ExpectThrows<ArgumentException>(() => new MeasurementWorkItemMappingCatalog(new MeasurementWorkItemMapping[]
            {
                Mapping("map-a", ElementCategory.Slab, "NetVolumeM3", "class-a", "work-a"),
                null!
            }));
        }

        private static void InvalidIdentityFailsClosed()
        {
            ExpectThrows<ArgumentNullException>(() => new MeasurementWorkItemMappingCatalog(null!));
            ExpectThrows<ArgumentOutOfRangeException>(() => Mapping("map", (ElementCategory)int.MaxValue, "NetVolumeM3", "class", "work"));
            ExpectThrows<ArgumentException>(() => Mapping(" ", ElementCategory.Slab, "NetVolumeM3", "class", "work"));
            ExpectThrows<ArgumentException>(() => Mapping("map", ElementCategory.Slab, " NetVolumeM3", "class", "work"));
            ExpectThrows<ArgumentException>(() => Mapping("map", ElementCategory.Slab, "NetVolumeM3", "class\ninvalid", "work"));

            var catalog = new MeasurementWorkItemMappingCatalog(Array.Empty<MeasurementWorkItemMapping>());
            ExpectThrows<ArgumentOutOfRangeException>(() => catalog.Resolve((ElementCategory)(-1), "NetVolumeM3"));
            ExpectThrows<ArgumentException>(() => catalog.Resolve(ElementCategory.Slab, "  "));
            ExpectThrows<ArgumentException>(() => catalog.Resolve(ElementCategory.Slab, " NetVolumeM3"));
        }

        private static MeasurementWorkItemMapping Mapping(
            string id,
            ElementCategory category,
            string measurementItemId,
            string classificationId,
            string workItemId) =>
            new MeasurementWorkItemMapping(id, category, measurementItemId, classificationId, workItemId);

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void ExpectThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
