using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Ribbon-friendly Vietnamese entry point for raft-foundation authoring. The canonical
    /// Foundation Direct Draw command remains the single geometry/semantic/native pipeline:
    /// select the footprint by picking its boundary points, then QS3D captures and builds it
    /// atomically with the current/default Foundation Family parameters.
    /// </summary>
    public sealed class RaftFoundationCommands
    {
        [CommandMethod("QS3DDRAWRAFTFOUNDATION", CommandFlags.Modal)]
        public void DrawRaftFoundation()
        {
            new DirectDrawP1Commands().DrawFoundation();
        }
    }
}