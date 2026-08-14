namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Makes the presentation-only reference tree augmenter reachable whenever the WorkspacePanel
    /// type is initialized. WorkspacePanel already owns an explicit static constructor in the
    /// compact-shell partial, so this field initializer runs before the first panel instance is
    /// constructed without touching PluginEntry/Palette startup lifecycle.
    /// </summary>
    public partial class WorkspacePanel
    {
        internal static readonly bool ReferenceWorkspaceTreeRegistrationReady = RegisterReferenceWorkspaceTree();

        private static bool RegisterReferenceWorkspaceTree()
        {
            ReferenceWorkspaceTreeAugmenter.EnsureRegistered();
            return true;
        }
    }
}
