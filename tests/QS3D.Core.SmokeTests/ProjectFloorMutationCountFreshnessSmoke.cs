using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorMutationCountFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            var project = new ProjectState("floor-count-drift", "Floor count drift");
            var sourceFloor = new FloorDefinition("F1", "Floor 1", 0d);
            var targetFloor = new FloorDefinition("F2", "Floor 2", 3d);
            project.Floors.Add(sourceFloor);
            project.Floors.Add(targetFloor);
            project.ActiveFloorId = sourceFloor.Id;

            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, sourceFloor.Id, string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeVersion = project.ChangeVersion;
            var beforeFloorId = element.FloorId;
            var source = new VersionMutatingCountCollection(project, element);

            var error = Capture<InvalidOperationException>(() =>
                ProjectFloorService.Assign(project, targetFloor.Id, source));

            Contains("being counted", error.Message, "Floor mutation must report Count-time project drift.");
            Equal(1, source.CountReads, "Floor mutation must read the known Count exactly once before rejecting drift.");
            Equal(0, source.EnumerationAttempts, "Floor mutation enumerated targets after Count changed the project.");
            Equal(beforeVersion + 1L, project.ChangeVersion, "Count fixture must be the only project version mutation.");
            Equal(beforeFloorId, element.FloorId, "Count-time drift must not assign the target Floor.");
            Equal(ElementDirtyFlags.None, element.Dirty, "Count-time drift must not dirty the target element.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class VersionMutatingCountCollection : ICollection<ProjectElement>
        {
            private readonly ProjectState _project;
            private readonly ProjectElement _element;

            internal VersionMutatingCountCollection(ProjectState project, ProjectElement element)
            {
                _project = project;
                _element = element;
            }

            internal int CountReads { get; private set; }
            internal int EnumerationAttempts { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    if (CountReads == 1) _project.Touch();
                    return 1;
                }
            }

            public bool IsReadOnly => true;

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new Exception("Count-time project drift must be rejected before target enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
    }
}
