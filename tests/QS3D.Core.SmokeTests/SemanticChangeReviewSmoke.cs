using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticChangeReviewSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GroupsStableSemanticChangesWithoutHandleAuthority();
            ReviewOrderingIsDeterministic();
            MalformedSnapshotsFailClosed();
            MalformedRevisionIdsFailClosed();
            SupplementaryRevisionIdsRemainExact();
            ReviewUsesOneDetachedCategoryGeneration();
        }

        private static void GroupsStableSemanticChangesWithoutHandleAuthority()
        {
            var before = new RevisionSnapshot { Id = "R-BEFORE", CreatedUtc = DateTime.UtcNow.AddMinutes(-1) };
            var a = Element("A", "Beam", "F1", "L1", "Z1");
            a.Properties["Mark"] = "B-01";
            a.Properties["GeneratedSolidHandle"] = "GEN-HANDLE-BEFORE";
            a.Properties["BoundarySourceHandles"] = "ROOM-HANDLE-BEFORE";
            a.Properties["QS3D.GeneratedSolid.StaleSnapshot"] = "STALE-HANDLE-BEFORE";
            a.Properties["PhysicalOpeningCutHostHandle"] = "CUT-HANDLE-BEFORE";
            a.Quantities["NetVolumeM3"] = 1d;
            a.SourceHandles.Add("SOURCE-HANDLE-BEFORE");
            before.Elements.Add(a);
            before.Elements.Add(Element("B", "Column", "C1", "L1", "Z1"));

            var after = new RevisionSnapshot { Id = "R-AFTER", CreatedUtc = DateTime.UtcNow };
            var changed = Element("A", "Beam", "F2", "L1", "Z1");
            changed.Properties["Mark"] = "B-02";
            changed.Properties["GeneratedSolidHandle"] = "GEN-HANDLE-AFTER";
            changed.Properties["BoundarySourceHandles"] = "ROOM-HANDLE-AFTER";
            changed.Properties["QS3D.GeneratedSolid.StaleSnapshot"] = "STALE-HANDLE-AFTER";
            changed.Properties["PhysicalOpeningCutHostHandle"] = "CUT-HANDLE-AFTER";
            changed.Quantities["NetVolumeM3"] = 1.5d;
            changed.SourceHandles.Add("SOURCE-HANDLE-AFTER");
            after.Elements.Add(changed);
            after.Elements.Add(Element("C", "Room", "R1", "L1", "Z2"));

            var review = new SemanticChangeReviewBuilder().Build(before, after);

            Equal("R-BEFORE", review.BeforeRevisionId);
            Equal("R-AFTER", review.AfterRevisionId);
            Equal(3, review.Elements.Count);
            Equal(1, review.Summary.AddedElementCount);
            Equal(1, review.Summary.RemovedElementCount);
            Equal(1, review.Summary.ChangedElementCount);
            Equal(1, review.Summary.IdentityChangeCount);
            Equal(1, review.Summary.PropertyChangeCount);
            Equal(1, review.Summary.QuantityChangeCount);
            Equal(5, review.Summary.OmittedSourceReferenceChangeCount);

            var item = review.Elements.Single(x => x.ElementId == "A");
            Equal("Beam", item.Category);
            Equal("Changed", item.Change);
            Equal(3, item.Fields.Count);
            Equal(SemanticChangeFieldKind.Identity, item.Fields.Single(x => x.Field == "FamilyId").Kind);
            Equal(SemanticChangeFieldKind.Property, item.Fields.Single(x => x.Field == "Property:Mark").Kind);
            Equal(SemanticChangeFieldKind.Quantity, item.Fields.Single(x => x.Field == "Quantity:NetVolumeM3").Kind);
            Equal(5, item.OmittedSourceReferenceChangeCount);
            True(item.Fields.All(x => x.Field.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) < 0));
            True(item.Fields.All(x => x.Field.IndexOf("Generated", StringComparison.OrdinalIgnoreCase) < 0));
            True(item.Fields.All(x => x.Field.IndexOf("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase) < 0));
            True(item.Fields.All(x =>
                x.Before.IndexOf("HANDLE-", StringComparison.OrdinalIgnoreCase) < 0 &&
                x.After.IndexOf("HANDLE-", StringComparison.OrdinalIgnoreCase) < 0));
        }

        private static void ReviewOrderingIsDeterministic()
        {
            var before1 = new RevisionSnapshot { Id = "R1" };
            before1.Elements.Add(Element("Z", "Beam", "F", "L", "Z"));
            before1.Elements.Add(Element("A", "Beam", "F", "L", "Z"));
            var after1 = new RevisionSnapshot { Id = "R2" };
            after1.Elements.Add(ChangedElement("Z", "B", "2", "A", "1"));
            after1.Elements.Add(ChangedElement("A", "C", "3"));

            var before2 = new RevisionSnapshot { Id = "R1" };
            before2.Elements.Add(Element("A", "Beam", "F", "L", "Z"));
            before2.Elements.Add(Element("Z", "Beam", "F", "L", "Z"));
            var after2 = new RevisionSnapshot { Id = "R2" };
            after2.Elements.Add(ChangedElement("A", "C", "3"));
            after2.Elements.Add(ChangedElement("Z", "A", "1", "B", "2"));

            var first = new SemanticChangeReviewBuilder().Build(before1, after1);
            var second = new SemanticChangeReviewBuilder().Build(before2, after2);

            Equal(string.Join("|", first.Elements.Select(x => x.ElementId)), string.Join("|", second.Elements.Select(x => x.ElementId)));
            Equal("A|Z", string.Join("|", first.Elements.Select(x => x.ElementId)));
            Equal("Property:A|Property:B", string.Join("|", first.Elements.Single(x => x.ElementId == "Z").Fields.Select(x => x.Field)));
        }

        private static void MalformedSnapshotsFailClosed()
        {
            var padded = new RevisionSnapshot { Id = "R1" };
            padded.Elements.Add(Element(" A ", "Beam", "F", "L", "Z"));
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(padded, new RevisionSnapshot { Id = "R2" }));

            var duplicate = new RevisionSnapshot { Id = "R1" };
            duplicate.Elements.Add(Element("A", "Beam", "F", "L", "Z"));
            duplicate.Elements.Add(Element("a", "Beam", "F", "L", "Z"));
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(duplicate, new RevisionSnapshot { Id = "R2" }));
        }

        private static void MalformedRevisionIdsFailClosed()
        {
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = "R\uD800" },
                new RevisionSnapshot { Id = "R2" }));
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = "R1" },
                new RevisionSnapshot { Id = "R\uDC00" }));
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = "R\u0001" },
                new RevisionSnapshot { Id = "R2" }));
        }

        private static void SupplementaryRevisionIdsRemainExact()
        {
            const string beforeId = "R\U0001F600";
            const string afterId = "R\U0001F680";
            var review = new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = beforeId },
                new RevisionSnapshot { Id = afterId });
            Equal(beforeId, review.BeforeRevisionId);
            Equal(afterId, review.AfterRevisionId);
        }

        private static void ReviewUsesOneDetachedCategoryGeneration()
        {
            var before = new RevisionSnapshot { Id = "R1", ProjectId = "P1" };
            var beforeElement = Element("E1", "StructuralWall", "F", "L", "Z");
            beforeElement.Properties["Mark"] = "BEFORE";
            before.Elements.Add(beforeElement);

            var after = new RevisionSnapshot { Id = "R2", ProjectId = "P1" };
            var afterElement = Element("E1", "StructuralWall", "F", "L", "Z");
            var hostile = new MutatingDictionary(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Mark"] = "AFTER" },
                () => afterElement.Category = "StructuralColumn");
            SetBackingField(afterElement, "<Properties>k__BackingField", hostile);
            after.Elements.Add(afterElement);

            var review = new SemanticChangeReviewBuilder().Build(before, after);
            Equal("StructuralColumn", afterElement.Category);
            var item = review.Elements.Single(x => x.ElementId == "E1");
            Equal("StructuralWall", item.Category);
            Equal("Property:Mark", item.Fields.Single().Field);
            Equal("BEFORE", item.Fields.Single().Before);
            Equal("AFTER", item.Fields.Single().After);
        }

        private static RevisionElementSnapshot ChangedElement(string id, params string[] propertyPairs)
        {
            var element = Element(id, "Beam", "F", "L", "Z");
            for (var i = 0; i + 1 < propertyPairs.Length; i += 2)
                element.Properties[propertyPairs[i]] = propertyPairs[i + 1];
            return element;
        }

        private static RevisionElementSnapshot Element(string id, string category, string family, string floor, string zone)
        {
            return new RevisionElementSnapshot
            {
                ElementId = id,
                Category = category,
                FamilyId = family,
                FloorId = floor,
                ZoneId = zone
            };
        }

        private static void SetBackingField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("Backing field not found: " + name);
            field.SetValue(target, value);
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

        private sealed class MutatingDictionary : IDictionary<string, string>
        {
            private readonly IDictionary<string, string> _inner;
            private readonly Action _mutation;
            private bool _mutated;

            internal MutatingDictionary(IDictionary<string, string> inner, Action mutation)
            {
                _inner = inner;
                _mutation = mutation;
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                if (!_mutated)
                {
                    _mutated = true;
                    _mutation();
                }
                return _inner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => _inner.Count;
            public bool IsReadOnly => _inner.IsReadOnly;
            public ICollection<string> Keys => _inner.Keys;
            public ICollection<string> Values => _inner.Values;
            public string this[string key] { get => _inner[key]; set => _inner[key] = value; }
            public void Add(string key, string value) => _inner.Add(key, value);
            public void Add(KeyValuePair<string, string> item) => _inner.Add(item);
            public void Clear() => _inner.Clear();
            public bool Contains(KeyValuePair<string, string> item) => _inner.Contains(item);
            public bool ContainsKey(string key) => _inner.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            public bool Remove(string key) => _inner.Remove(key);
            public bool Remove(KeyValuePair<string, string> item) => _inner.Remove(item);
            public bool TryGetValue(string key, out string value) => _inner.TryGetValue(key, out value!);
        }
    }
}
