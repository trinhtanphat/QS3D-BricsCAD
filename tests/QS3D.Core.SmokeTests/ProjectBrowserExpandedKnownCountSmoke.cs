using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserExpandedKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InvalidKnownCountsFailBeforeEnumeration();
            AdvertisedCountGreaterThanTraversalFails();
            AdvertisedCountLessThanTraversalFails();
            AdvertisedCountOverrunWinsBeforeThrowingTail();
            HonestCountRemainsAccepted();
            EnumerableWithoutKnownCountRemainsAccepted();
        }

        private static void InvalidKnownCountsFailBeforeEnumeration()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);

            FailsBeforeEnumeration(
                root,
                new CountedExpandedPaths(-1, -1, -1, new[] { rootPath }, throwOnEnumeration: true));
            FailsBeforeEnumeration(
                root,
                new CountedExpandedPaths(50001, 50001, 50001, new[] { rootPath }, throwOnEnumeration: true));
            FailsBeforeEnumeration(
                root,
                new CountedExpandedPaths(1, 2, 1, new[] { rootPath }, throwOnEnumeration: true));
        }

        private static void AdvertisedCountGreaterThanTraversalFails()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var source = new CountedExpandedPaths(2, 2, 2, new[] { rootPath });

            ThrowsCountMismatch(() => ProjectBrowserVirtualizationPlanner.BuildViewport(root, source));
        }

        private static void AdvertisedCountLessThanTraversalFails()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var firstLevel = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath }, 0, 10);
            var childPath = firstLevel.Rows.First(x => x.Depth == 1).Path;
            var source = new CountedExpandedPaths(1, 1, 1, new[] { rootPath, childPath });

            ThrowsCountMismatch(() => ProjectBrowserVirtualizationPlanner.BuildViewport(root, source));
        }

        private static void AdvertisedCountOverrunWinsBeforeThrowingTail()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var firstLevel = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath }, 0, 10);
            var childPath = firstLevel.Rows.First(x => x.Depth == 1).Path;
            var source = new ThrowingTailCountedExpandedPaths(rootPath, childPath);

            ThrowsCountMismatch(() => ProjectBrowserVirtualizationPlanner.BuildViewport(root, source));
            Require(source.EnumerationCount == 1,
                "Known-Count overrun regression source must be enumerated exactly once.");
        }

        private static void HonestCountRemainsAccepted()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var source = new CountedExpandedPaths(1, 1, 1, new[] { rootPath });

            var viewport = ProjectBrowserVirtualizationPlanner.BuildViewport(root, source, 0, 10);
            Require(source.EnumerationCount == 1, "Honest counted expanded-path input was not enumerated exactly once.");
            Require(viewport.TotalVisibleRows > 1, "Honest counted expanded-path input no longer expands the root node.");
        }

        private static void EnumerableWithoutKnownCountRemainsAccepted()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var source = new EnumerableOnly<string>(new[] { rootPath });

            var viewport = ProjectBrowserVirtualizationPlanner.BuildViewport(root, source, 0, 10);
            Require(viewport.TotalVisibleRows > 1, "Enumerable-only expanded-path input no longer expands the root node.");
        }

        private static void FailsBeforeEnumeration(ProjectBrowserNode root, CountedExpandedPaths source)
        {
            try
            {
                ProjectBrowserVirtualizationPlanner.BuildViewport(root, source);
                throw new Exception("Expected invalid expanded-path Count contract rejection.");
            }
            catch (InvalidOperationException)
            {
                Require(source.EnumerationCount == 0,
                    "Invalid expanded-path Count contract reached enumeration before rejection.");
            }
        }

        private static void ThrowsCountMismatch(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected expanded-path Count/traversal mismatch rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "Project browser expanded node path Count does not match traversal count.",
                        StringComparison.Ordinal),
                    "Unexpected expanded-path Count/traversal diagnostic: " + exception.Message);
            }
        }

        private static ProjectBrowserNode BuildRoot()
        {
            var project = new ProjectState("P-EXPANDED-COUNT", "Expanded Count Browser");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, string.Empty, "F-01", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, string.Empty, "F-02", "Z-A"));
            return ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class CountedExpandedPaths : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly List<string> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal CountedExpandedPaths(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                IEnumerable<string> items,
                bool throwOnEnumeration = false)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = new List<string>(items ?? throw new ArgumentNullException(nameof(items)));
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal int EnumerationCount { get; private set; }

            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Expanded-path source must not be enumerated for an invalid known Count.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => _items.Contains(item);
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private sealed class ThrowingTailCountedExpandedPaths : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string _first;
            private readonly string _second;

            internal ThrowingTailCountedExpandedPaths(string first, string second)
            {
                _first = first ?? throw new ArgumentNullException(nameof(first));
                _second = second ?? throw new ArgumentNullException(nameof(second));
            }

            internal int EnumerationCount { get; private set; }

            int ICollection<string>.Count => 1;
            int IReadOnlyCollection<string>.Count => 1;
            int ICollection.Count => 1;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                return Enumerate().GetEnumerator();
            }

            private IEnumerable<string> Enumerate()
            {
                yield return _first;
                yield return _second;
                throw new InvalidOperationException(
                    "Expanded-path throwing tail must not win after the declared Count is exceeded.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) =>
                string.Equals(item, _first, StringComparison.Ordinal) || string.Equals(item, _second, StringComparison.Ordinal);
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class EnumerableOnly<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _items;

            internal EnumerableOnly(IEnumerable<T> items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
