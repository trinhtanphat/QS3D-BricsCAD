using System;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementKeyControlSmoke
    {
        public static void Run()
        {
            RejectMalformedPropertyKeysWithoutMutation();
            RejectMalformedQuantityKeysWithoutMutation();
            RejectMalformedRemoveKeysWithoutMutation();
            PreserveOrdinarySpaceNormalization();
            RelationSettersMarkRelationsDirty();
            RelationSettersMarkGeneratedGeometryStale();
            NormalizedRelationNoOpsStayClean();
        }

        private static void RejectMalformedPropertyKeysWithoutMutation()
        {
            foreach (var key in MalformedKeys("LengthM"))
            {
                var element = CreateCleanElement();
                AssertArgumentException(() => element.SetProperty(key, "12"), "SetProperty should reject control characters before trimming.");
                Assert(element.Properties.Count == 0, "Rejected property key must not mutate Properties.");
                Assert(element.Dirty == ElementDirtyFlags.None, "Rejected property key must not dirty the element.");
            }
        }

        private static void RejectMalformedQuantityKeysWithoutMutation()
        {
            foreach (var key in MalformedKeys("VolumeM3"))
            {
                var element = CreateCleanElement();
                AssertArgumentException(() => element.SetQuantity(key, 3.5d), "SetQuantity should reject control characters before trimming.");
                Assert(element.Quantities.Count == 0, "Rejected quantity key must not mutate Quantities.");
                Assert(element.Dirty == ElementDirtyFlags.None, "Rejected quantity key must not dirty the element.");
            }
        }

        private static void RejectMalformedRemoveKeysWithoutMutation()
        {
            var removeProperty = typeof(ProjectElement).GetMethod("RemoveProperty", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(removeProperty != null, "ProjectElement.RemoveProperty reflection hook is required by this regression.");

            foreach (var key in MalformedKeys("LengthM"))
            {
                var element = CreateCleanElement();
                element.SetProperty("LengthM", "12");
                element.MarkClean(ElementDirtyFlags.All);

                try
                {
                    removeProperty!.Invoke(element, new object[] { key });
                    throw new InvalidOperationException("RemoveProperty should reject control characters before trimming.");
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
                {
                }

                Assert(element.Properties.TryGetValue("LengthM", out var value) && value == "12", "Malformed remove key must not delete the canonical property.");
                Assert(element.Dirty == ElementDirtyFlags.None, "Rejected remove key must not dirty the element.");
            }
        }

        private static void PreserveOrdinarySpaceNormalization()
        {
            var element = CreateCleanElement();
            element.SetProperty("  LengthM  ", "12");
            element.SetQuantity("  VolumeM3  ", 3.5d);

            Assert(element.Properties.ContainsKey("LengthM"), "Ordinary surrounding-space property normalization must remain supported.");
            Assert(element.Quantities.ContainsKey("VolumeM3"), "Ordinary surrounding-space quantity normalization must remain supported.");
        }

        private static void RelationSettersMarkRelationsDirty()
        {
            AssertRelationMutationMarksDirty(element => element.FamilyId = "family-2", "FamilyId");
            AssertRelationMutationMarksDirty(element => element.FloorId = "floor-2", "FloorId");
            AssertRelationMutationMarksDirty(element => element.ZoneId = "zone-2", "ZoneId");
        }

        private static void RelationSettersMarkGeneratedGeometryStale()
        {
            var element = new ProjectElement("element-1", ElementCategory.Beam, "family-1", "floor-1", "zone-1");
            element.SetProperty("GeneratedSolidHandle", "AA");
            element.ClearGeneratedGeometryStale();
            element.MarkClean(ElementDirtyFlags.All);

            element.FloorId = "floor-2";

            Assert(element.IsGeneratedSolidStale(), "A relation mutation must mark existing generated output stale.");
        }

        private static void NormalizedRelationNoOpsStayClean()
        {
            var element = new ProjectElement("element-1", ElementCategory.Beam, "family-1", "floor-1", "zone-1");
            element.MarkClean(ElementDirtyFlags.All);

            element.FamilyId = "  family-1  ";
            element.FloorId = " floor-1 ";
            element.ZoneId = "zone-1";

            Assert(element.Dirty == ElementDirtyFlags.None, "Normalized relation no-op assignments must not dirty a clean element.");
        }

        private static void AssertRelationMutationMarksDirty(Action<ProjectElement> mutate, string relationName)
        {
            var element = new ProjectElement("element-1", ElementCategory.Beam, "family-1", "floor-1", "zone-1");
            element.MarkClean(ElementDirtyFlags.All);

            mutate(element);

            Assert(element.Dirty == ElementDirtyFlags.Relations, relationName + " mutation must mark Relations dirty and no unrelated dirty flags.");
        }

        private static ProjectElement CreateCleanElement()
        {
            var element = new ProjectElement("element-1", ElementCategory.Beam);
            element.MarkClean(ElementDirtyFlags.All);
            return element;
        }

        private static string[] MalformedKeys(string canonical)
        {
            return new[]
            {
                "\t" + canonical,
                canonical + "\t",
                "\r" + canonical,
                canonical + "\r",
                "\n" + canonical,
                canonical + "\n",
                canonical.Substring(0, 1) + "\t" + canonical.Substring(1)
            };
        }

        private static void AssertArgumentException(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}