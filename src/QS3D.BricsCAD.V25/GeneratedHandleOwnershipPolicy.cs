using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25
{
    internal static class GeneratedHandleOwnershipPolicy
    {
        public static IReadOnlyList<string> RebarHandleKeys => QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.RebarHandleKeys;
        public static bool IsOwnerSlot(string key) => QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsOwnerSlot(key);
        public static bool IsRebarOwnerSlot(string key) => QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsRebarOwnerSlot(key);
        public static string CanonicalOwnerSlot(string key) => QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(key);
        public static IEnumerable<KeyValuePair<string, string>> EnumerateOwnerHandles(ProjectElement element) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element);
        public static IEnumerable<KeyValuePair<string, string>> EnumerateLogicalOwnerHandles(ProjectElement element) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element);
        public static IReadOnlyList<string> CollectOwnerHandles(ProjectState project) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project);
        public static bool TryFindOwner(ProjectState project, string handle, out ProjectElement? owner, out string propertyKey) =>
            QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.TryFindOwner(project, handle, out owner, out propertyKey);
    }
}
