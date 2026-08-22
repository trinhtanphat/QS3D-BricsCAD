using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyCreateNullPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var malformed = new ProjectState("FAMILY-NULL-CREATE", "Family null create");
            malformed.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Room));
            malformed.Families.Add(null!);

            var familyCount = malformed.Families.Count;
            var changeVersion = malformed.ChangeVersion;
            var updatedUtc = malformed.UpdatedUtc;

            try
            {
                ProjectFamilyService.Create(malformed, "F2", "Family 2", ElementCategory.Room);
                throw new InvalidOperationException("Create must reject a project family collection containing a null entry.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project family collection contains a null family.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Create must fail closed with the canonical null-family integrity error.", ex);
            }

            if (malformed.Families.Count != familyCount)
                throw new InvalidOperationException("Rejected Family creation must not change the Family collection.");
            if (malformed.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Rejected Family creation must not advance project ChangeVersion.");
            if (malformed.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected Family creation must not change UpdatedUtc.");

            var valid = new ProjectState("FAMILY-CREATE-OK", "Family create ok");
            var created = ProjectFamilyService.Create(valid, "F1", "Family 1", ElementCategory.Room);
            if (!ReferenceEquals(valid.FindFamily("F1"), created))
                throw new InvalidOperationException("Ordinary Family creation must publish the created Family into the project.");
            if (valid.Families.Count != 1 || valid.ChangeVersion != 1)
                throw new InvalidOperationException("Ordinary Family creation must add one Family and advance ChangeVersion once.");
        }
    }
}
