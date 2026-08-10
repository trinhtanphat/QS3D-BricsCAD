using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25
{
    internal static class GeneratedHandleOwnershipPolicy
    {
        public static bool IsOwnerSlot(string key) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsOwnerSlot(key);

        public static IEnumerable<KeyValuePair<string, string>> EnumerateOwnerHandles(ProjectElement element) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element);

        public static IReadOnlyList<string> CollectOwnerHandles(ProjectState project) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project);
    }
}
