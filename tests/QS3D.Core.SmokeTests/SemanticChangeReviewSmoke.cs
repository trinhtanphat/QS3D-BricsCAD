using System;
using System.Linq;
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
            True(item.Fields.All(x => x.Before.IndexOf("HANDLE-", StringComparison.OrdinalIgnoreCase) < 0 && x.After.IndexOf("HANDLE-", StringComparison.OrdinalIgnoreCase) < 0));
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
                new RevisionSnapshot { Id = "R\uD800" }, new RevisionSnapshot { Id = "R2" }));
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = "R1" }, new RevisionSnapshot { Id = "R\uDC00" }));
            Throws<InvalidOperationException>(() => new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = "R\u0001" }, new RevisionSnapshot { Id = "R2" }));
        }

        private static void SupplementaryRevisionIdsRemainExact()
        {
            const string beforeId = "R\U0001F600";
            const string afterId = "R\U0001F680";
            var review = new SemanticChangeReviewBuilder().Build(
                new RevisionSnapshot { Id = beforeId }, new RevisionSnapshot { Id = afterId });
            Equal(beforeId, review.BeforeRevisionId);
            Equal(afterId, review.AfterRevisionId);
        }

        private static RevisionElementSnapshot ChangedElement(string id, params string[] propertyPairs)
        {
            var element = Element(id, "Beam", "F", "L", "Z");
            for (var i = 0; i + 1 < propertyPairs.Length; i += 2) element.Properties[propertyPairs[i]] = propertyPairs[i + 1];
            return element;
        }

        private static RevisionElementSnapshot Element(string id, string category, string family, string floor, string zone)
        {
            return new RevisionElementSnapshot { ElementId = id, Category = category, FamilyId = family, FloorId = floor, ZoneId = zone };
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
    }
}
