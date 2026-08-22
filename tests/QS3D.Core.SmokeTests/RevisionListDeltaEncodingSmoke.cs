using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionListDeltaEncodingSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CommaBearingListsRemainDistinct();
            BackslashesRemainUnambiguous();
            OrdinaryListsKeepReadableFormat();
        }

        private static void CommaBearingListsRemainDistinct()
        {
            var before = Snapshot(new[] { "A,B", "C" }, new[] { "D,E", "F" });
            var after = Snapshot(new[] { "A", "B,C" }, new[] { "D", "E,F" });

            var delta = new RevisionService().Compare(before, after).Single();
            var handles = delta.Fields.Single(x => string.Equals(x.Field, "SourceHandles", StringComparison.Ordinal));
            var dependencies = delta.Fields.Single(x => string.Equals(x.Field, "Dependencies", StringComparison.Ordinal));

            Equal("A\\,B,C", handles.Before, "comma handles before");
            Equal("A,B\\,C", handles.After, "comma handles after");
            NotEqual(handles.Before, handles.After, "comma handles must remain distinguishable");
            Equal("D\\,E,F", dependencies.Before, "comma dependencies before");
            Equal("D,E\\,F", dependencies.After, "comma dependencies after");
            NotEqual(dependencies.Before, dependencies.After, "comma dependencies must remain distinguishable");
        }

        private static void BackslashesRemainUnambiguous()
        {
            var before = Snapshot(new[] { "A\\B", "C" }, Array.Empty<string>());
            var after = Snapshot(new[] { "A", "B\\C" }, Array.Empty<string>());

            var handles = new RevisionService().Compare(before, after)
                .Single()
                .Fields
                .Single(x => string.Equals(x.Field, "SourceHandles", StringComparison.Ordinal));

            Equal("A\\\\B,C", handles.Before, "backslash handles before");
            Equal("A,B\\\\C", handles.After, "backslash handles after");
            NotEqual(handles.Before, handles.After, "backslash handles must remain distinguishable");
        }

        private static void OrdinaryListsKeepReadableFormat()
        {
            var before = Snapshot(new[] { "H1", "H2" }, new[] { "E1", "E2" });
            var after = Snapshot(new[] { "H1", "H3" }, new[] { "E1", "E3" });

            var delta = new RevisionService().Compare(before, after).Single();
            var handles = delta.Fields.Single(x => string.Equals(x.Field, "SourceHandles", StringComparison.Ordinal));
            var dependencies = delta.Fields.Single(x => string.Equals(x.Field, "Dependencies", StringComparison.Ordinal));

            Equal("H1,H2", handles.Before, "ordinary handles before");
            Equal("H1,H3", handles.After, "ordinary handles after");
            Equal("E1,E2", dependencies.Before, "ordinary dependencies before");
            Equal("E1,E3", dependencies.After, "ordinary dependencies after");
        }

        private static RevisionSnapshot Snapshot(string[] handles, string[] dependencies)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = "R-LIST-DELTA",
                CreatedUtc = DateTime.UtcNow
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E-LIST-DELTA",
                Category = ElementCategory.Beam.ToString()
            };
            foreach (var handle in handles) element.SourceHandles.Add(handle);
            foreach (var dependency in dependencies) element.Dependencies.Add(dependency);
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void NotEqual(string left, string right, string label)
        {
            if (string.Equals(left, right, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ".");
        }
    }
}
