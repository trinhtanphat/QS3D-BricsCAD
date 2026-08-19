using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedFoundationMeshLiveHandleCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NegativeCountFailsBeforeEnumeration();
            AdvertisedCountGreaterThanTraversalFails();
            AdvertisedCountLessThanTraversalFails();
            HonestCountEnumeratesOnceAndKeepsNumericAlias();
        }

        private static void NegativeCountFailsBeforeEnumeration()
        {
            var setup = Create("NEG", "A", "1");
            var source = new CountedSet(-1, new[] { "A" }, throwOnEnumeration: true);

            ExpectCountFailure(() => new GeneratedFoundationMeshHealthService().Inspect(setup.Project, source));
            Require(source.EnumerationCount == 0, "Negative live-handle Count reached enumeration before rejection.");
        }

        private static void AdvertisedCountGreaterThanTraversalFails()
        {
            var setup = Create("UNDER", "A", "1");
            var source = new CountedSet(2, new[] { "A" });

            ExpectCountFailure(() => new GeneratedFoundationMeshHealthService().Inspect(setup.Project, source));
            Require(source.EnumerationCount == 1, "Under-enumerating live-handle input was not traversed exactly once.");
        }

        private static void AdvertisedCountLessThanTraversalFails()
        {
            var setup = Create("OVER", "A;B", "2");
            var source = new CountedSet(1, new[] { "A", "B" });

            ExpectCountFailure(() => new GeneratedFoundationMeshHealthService().Inspect(setup.Project, source));
            Require(source.EnumerationCount == 1, "Over-enumerating live-handle input was not traversed exactly once.");
        }

        private static void HonestCountEnumeratesOnceAndKeepsNumericAlias()
        {
            var setup = Create("HONEST", "A;B", "2");
            var source = new CountedSet(2, new[] { "000A", "B" });

            var issues = new GeneratedFoundationMeshHealthService().Inspect(setup.Project, source);

            Require(source.EnumerationCount == 1, "Honest live-handle input must be normalized in exactly one traversal.");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "FOUNDATION_MESH_GENERATED_COUNT_MISMATCH");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-FOUNDATION-MESH-LIVE-COUNT-" + suffix, "Foundation Mesh live-handle Count integrity");
            var element = new ProjectElement("E-FOUNDATION-MESH-LIVE-COUNT-" + suffix, ElementCategory.Foundation);
            element.Properties["GeneratedFoundationMeshHandles"] = handles;
            element.Properties["GeneratedFoundationMeshCount"] = count;
            element.Properties["GeneratedFoundationMeshXDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshYDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshCoverM"] = "0.03";
            element.Properties["GeneratedFoundationMeshFaces"] = "Both";
            element.Properties["GeneratedFoundationMeshMode"] = "FoundationMeshXY";
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void ExpectCountFailure(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.IndexOf("Count", StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected Foundation Mesh live-handle failure: " + exception.Message, exception);
            }

            throw new InvalidOperationException("Expected Foundation Mesh live-handle Count contract rejection.");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("GeneratedFoundationMeshLiveHandleCountIntegritySmoke reported unexpected issue: " + code + ".");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class CountedSet : ISet<string>
        {
            private readonly List<string> _items;
            private readonly HashSet<string> _set;
            private readonly int _reportedCount;
            private readonly bool _throwOnEnumeration;

            internal CountedSet(int reportedCount, IEnumerable<string> items, bool throwOnEnumeration = false)
            {
                _reportedCount = reportedCount;
                _items = new List<string>(items ?? throw new ArgumentNullException(nameof(items)));
                _set = new HashSet<string>(_items, StringComparer.OrdinalIgnoreCase);
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal int EnumerationCount { get; private set; }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Live-handle source must not be enumerated for an invalid known Count.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(string item) => _set.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public bool IsProperSubsetOf(IEnumerable<string> other) => _set.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<string> other) => _set.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<string> other) => _set.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<string> other) => _set.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<string> other) => _set.Overlaps(other);
            public bool SetEquals(IEnumerable<string> other) => _set.SetEquals(other);

            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
        }

        private sealed class Setup
        {
            internal Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal ProjectElement Element { get; }
        }
    }
}
