using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Ribbon-friendly Vietnamese entry point for raft-foundation authoring. Unlike the
    /// legacy point-pick Foundation command, Móng Bè consumes one existing exact closed
    /// Polyline/Region boundary and delegates semantic/native authoring to the guarded
    /// raft boundary pipeline without mutating the selected source entity.
    /// </summary>
    public sealed class RaftFoundationCommands
    {
        [CommandMethod("QS3DDRAWRAFTFOUNDATION", CommandFlags.Modal)]
        public void DrawRaftFoundation()
        {
            RaftFoundationBoundaryAuthoring.Execute();
        }
    }
}
