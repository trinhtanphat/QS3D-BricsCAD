using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionCompareReadonlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var before = Snapshot("before");
            var after = Snapshot("after");
            var result = new RevisionService().Compare(before, after);

            if (result.Count != 1 ||
                !string.Equals(result[0].ElementId, "E-REV-READONLY", StringComparison.Ordinal) ||
                !string.Equals(result[0].Change, "Changed", StringComparison.Ordinal) ||
                result[0].Fields.Count != 1)
                throw new InvalidOperationException("Revision Compare readonly smoke produced an unexpected delta shape.");

            if (result is not IList<RevisionDelta> mutableView || !mutableView.IsReadOnly)
                throw new InvalidOperationException("Revision Compare must return a read-only outer collection.");

            try
            {
                mutableView.Add(new RevisionDelta { ElementId = "INJECTED", Change = "Added" });
                throw new InvalidOperationException("Revision Compare result accepted mutation through IList<RevisionDelta>.");
            }
            catch (NotSupportedException)
            {
            }

            if (result.Count != 1)
                throw new InvalidOperationException("Rejected mutation changed Revision Compare result contents.");
        }

        private static RevisionSnapshot Snapshot(string note)
        {
            var snapshot = new RevisionSnapshot();
            var element = new RevisionElementSnapshot
            {
                ElementId = "E-REV-READONLY",
                Category = ElementCategory.Beam.ToString()
            };
            element.Properties["Note"] = note;
            snapshot.Elements.Add(element);
            return snapshot;
        }
    }
}
