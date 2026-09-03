using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationPreviewSmoke
    {
        private sealed class EnumerationTouchedException : Exception
        {
        }

        private sealed class CountedTargetSource : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly IReadOnlyList<string> _values;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal CountedTargetSource(
                IEnumerable<string> values,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration = false)
            {
                _values = (values ?? Array.Empty<string>()).ToList().AsReadOnly();
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool Enumerated { get; private set; }

            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                Enumerated = true;
                if (_throwOnEnumeration) throw new EnumerationTouchedException();
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => _values.Contains(item);
            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                for (var i = 0; i < _values.Count; i++) array[arrayIndex + i] = _values[i];
            }
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _values.Count; i++) array.SetValue(_values[i], index + i);
            }
        }

        [ModuleInitializer]
        internal static void Initialize()
        {
            PreviewRunsOnDetachedState();
            SubsetPreviewAndApplyRespectScope();
            MalformedSubsetTargetsFailClosed();
            KnownCountIntegrityFailsClosed();
            MutationDuringSubsetTargetEnumerationFailsFreshness();
            StalePreviewFailsBeforeLiveMutation();
            ChangeVersionInvalidatesEquivalentPreview();
            FreshPreviewCanApplyWithoutNewHealthErrors();
        }

        private static void PreviewRunsOnDetachedState()
        {
            var project = Fixture();
            var beam = project.FindElement("B1")!;
            var originalDirty = beam.Dirty;
            var originalUpdated = beam.UpdatedUtc;
            var projectUpdated = project.UpdatedUtc;
            var projectVersion = project.ChangeVersion;

            var preview = new RegenerationPreviewService().Preview(project);
            True(preview.RegeneratedElementCount >= 1);
            True(preview.HasSemanticChanges);
            True(!preview.IsSubset);
            Equal(0, preview.TargetElementIds.Count);
            Equal(projectVersion, preview.SourceChangeVersion);
            True(preview.Deltas.Any(x => x.ElementId == "B1" && x.Fields.Any(f => f.Field == "Quantity:NetVolumeM3")));
            True(!beam.Quantities.ContainsKey("NetVolumeM3"));
            Equal(originalDirty, beam.Dirty);
            Equal(originalUpdated, beam.UpdatedUtc);
            Equal(projectUpdated, project.UpdatedUtc);
            Equal(projectVersion, project.ChangeVersion);
        }

        private static void SubsetPreviewAndApplyRespectScope()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var preview = service.PreviewSubset(project, new[] { "B1" });
            True(preview.IsSubset);
            Equal(1, preview.TargetElementIds.Count);
            Equal("B1", preview.TargetElementIds[0]);
            True(preview.Deltas.Any(x => x.ElementId == "B1"));
            True(!preview.Deltas.Any(x => x.ElementId == "B2"));
            True(!project.FindElement("B1")!.Quantities.ContainsKey("NetVolumeM3"));
            True(!project.FindElement("B2")!.Quantities.ContainsKey("NetVolumeM3"));

            var result = service.Apply(project, preview);
            True(result.RegeneratedElementCount >= 1);
            Near(0.9d, project.FindElement("B1")!.Quantities["NetVolumeM3"]);
            True(!project.FindElement("B2")!.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void MalformedSubsetTargetsFailClosed()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            Throws<ArgumentException>(() => service.PreviewSubset(project, Array.Empty<string>()));
            Throws<ArgumentException>(() => service.PreviewSubset(project, new[] { " " }));
            Throws<ArgumentException>(() => service.PreviewSubset(project, new[] { " B1 " }));
            Throws<ArgumentException>(() => service.PreviewSubset(project, new[] { "B1", "b1" }));

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { " " }));
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { " B1 " }));
            Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { "B1", "b1" }));
        }

        private static void KnownCountIntegrityFailsClosed()
        {
            var service = new RegenerationPreviewService();

            var negative = new CountedTargetSource(new[] { "B1" }, -1, -1, -1, throwOnEnumeration: true);
            Throws<InvalidOperationException>(() => service.PreviewSubset(Fixture(), negative));
            True(!negative.Enumerated);

            var conflicting = new CountedTargetSource(new[] { "B1" }, 1, 2, 1, throwOnEnumeration: true);
            Throws<InvalidOperationException>(() => service.PreviewSubset(Fixture(), conflicting));
            True(!conflicting.Enumerated);

            var underYield = new CountedTargetSource(new[] { "B1" }, 2, 2, 2);
            Throws<InvalidOperationException>(() => service.PreviewSubset(Fixture(), underYield));
            True(underYield.Enumerated);

            var overYield = new CountedTargetSource(new[] { "B1", "B2" }, 1, 1, 1);
            Throws<InvalidOperationException>(() => service.PreviewSubset(Fixture(), overYield));
            True(overYield.Enumerated);

            var duplicateBeforeMismatch = new CountedTargetSource(new[] { "B1", "b1" }, 3, 3, 3);
            ThrowsMessage<ArgumentException>(
                () => service.PreviewSubset(Fixture(), duplicateBeforeMismatch),
                "Duplicate regeneration preview target");

            var cardinalityBeforeMismatch = new CountedTargetSource(new[] { "B1", "B2", "B3" }, 1, 1, 1);
            ThrowsMessage<InvalidOperationException>(
                () => service.PreviewSubset(Fixture(), cardinalityBeforeMismatch),
                "more entries than its known count");

            var honest = new CountedTargetSource(new[] { "B1" }, 1, 1, 1);
            var honestPreview = service.PreviewSubset(Fixture(), honest);
            Equal(1, honestPreview.TargetElementIds.Count);
            Equal("B1", honestPreview.TargetElementIds[0]);

            IEnumerable<string> StreamingTargets()
            {
                yield return "B2";
            }

            var streamingPreview = service.PreviewSubset(Fixture(), StreamingTargets());
            Equal(1, streamingPreview.TargetElementIds.Count);
            Equal("B2", streamingPreview.TargetElementIds[0]);
        }

        private static void MutationDuringSubsetTargetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;

            IEnumerable<string> Targets()
            {
                project.Touch();
                yield return "B1";
            }

            Throws<InvalidOperationException>(() => new RegenerationPreviewService().PreviewSubset(project, Targets()));
            Equal(beforeVersion + 1L, project.ChangeVersion);
            True(!project.FindElement("B1")!.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void StalePreviewFailsBeforeLiveMutation()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var preview = service.Preview(project);
            var beam = project.FindElement("B1")!;
            beam.SetProperty("LengthM", "8");
            Throws<InvalidOperationException>(() => service.Apply(project, preview));
            True(!beam.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void ChangeVersionInvalidatesEquivalentPreview()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var preview = service.Preview(project);
            var oldVersion = preview.SourceChangeVersion;
            project.Touch();
            True(project.ChangeVersion > oldVersion);
            Throws<InvalidOperationException>(() => service.Apply(project, preview));
            True(!project.FindElement("B1")!.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void FreshPreviewCanApplyWithoutNewHealthErrors()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var preview = service.Preview(project);
            True(!preview.IntroducesHealthErrors);

            var result = service.Apply(project, preview);
            True(result.RegeneratedElementCount >= 1);
            Equal(0, result.HealthDiff.NewErrorCount);
            Near(0.9d, project.FindElement("B1")!.Quantities["NetVolumeM3"]);
            Near(0.48d, project.FindElement("B2")!.Quantities["NetVolumeM3"]);
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-REGEN-PREVIEW", "Regen Preview");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            var family = new ProjectFamily("FAM", "Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var beam = new ProjectElement("B1", ElementCategory.Beam, "FAM", "F", "Z");
            beam.Properties["LengthM"] = "6";
            beam.Properties["WidthM"] = "0.3";
            beam.Properties["HeightM"] = "0.5";
            project.Elements.Add(beam);

            var second = new ProjectElement("B2", ElementCategory.Beam, "FAM", "F", "Z");
            second.Properties["LengthM"] = "4";
            second.Properties["WidthM"] = "0.3";
            second.Properties["HeightM"] = "0.4";
            project.Elements.Add(second);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void ThrowsMessage<T>(Action action, string expectedFragment) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new Exception("Expected " + typeof(T).Name + " containing '" + expectedFragment + "', got: " + ex.Message);
            }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
