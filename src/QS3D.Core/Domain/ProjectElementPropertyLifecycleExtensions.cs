using System;

namespace QS3D.Core.Domain
{
    /// <summary>
    /// Lifecycle-aware property removal for adapter callers. The canonical validation,
    /// dirty-flag, and generated-output invalidation policy remains owned by ProjectElement.
    /// </summary>
    public static class ProjectElementPropertyLifecycleExtensions
    {
        public static bool RemovePropertyLifecycle(this ProjectElement element, string name)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return element.RemoveProperty(name);
        }
    }
}
