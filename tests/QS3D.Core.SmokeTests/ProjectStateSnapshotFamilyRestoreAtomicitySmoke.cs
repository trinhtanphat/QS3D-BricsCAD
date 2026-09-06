using System;
using System.ComponentModel;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotFamilyRestoreAtomicitySmoke
    {
        public static void Run()
        {
            ThrowingFamilySubscriberCannotPublishPartialRestore();
        }

        private static void ThrowingFamilySubscriberCannotPublishPartialRestore()
        {
            var project = new ProjectState("snapshot-family-atomicity", "Captured project");
            var family = new ProjectFamily("F-01", "Captured family", ElementCategory.Beam);
            family.Properties["Grade"] = "C30";
            family.Properties["NullableNote"] = null!;
            project.Families.Add(family);
            var familyProperties = family.Properties;

            var snapshot = ProjectStateSnapshot.Capture(project);

            project.Name = "Mutated project";
            family.Name = "Mutated family";
            family.Category = ElementCategory.Column;
            family.Properties["Grade"] = "C40";
            family.Properties["NullableNote"] = "mutated";

            PropertyChangedEventHandler throwingSubscriber = (_, __) => throw new InvalidOperationException("subscriber failure");
            family.PropertyChanged += throwingSubscriber;
            try
            {
                snapshot.Restore(project);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Snapshot restore must not let a ProjectFamily PropertyChanged subscriber leave the project partially restored.",
                    ex);
            }
            finally
            {
                family.PropertyChanged -= throwingSubscriber;
            }

            if (!string.Equals(project.Name, "Captured project", StringComparison.Ordinal))
                throw new InvalidOperationException("Project scalar state was not restored consistently.");
            if (project.Families.Count != 1)
                throw new InvalidOperationException("Snapshot restore lost the preserved family after a throwing subscriber.");
            if (!ReferenceEquals(project.Families[0], family))
                throw new InvalidOperationException("Snapshot restore must preserve captured ProjectFamily object identity.");
            if (!ReferenceEquals(family.Properties, familyProperties))
                throw new InvalidOperationException("Snapshot restore must preserve the captured ProjectFamily property-store object identity.");
            if (!string.Equals(family.Name, "Captured family", StringComparison.Ordinal))
                throw new InvalidOperationException("Family name was not restored.");
            if (family.Category != ElementCategory.Beam)
                throw new InvalidOperationException("Family category was not restored.");
            if (!family.Properties.TryGetValue("Grade", out var grade) || !string.Equals(grade, "C30", StringComparison.Ordinal))
                throw new InvalidOperationException("Family properties were not restored.");
            if (!family.Properties.TryGetValue("NullableNote", out var nullableNote) || !string.Equals(nullableNote, string.Empty, StringComparison.Ordinal))
                throw new InvalidOperationException("Snapshot restore must preserve the canonical empty-string representation of an admitted null family property value.");
        }
    }
}
