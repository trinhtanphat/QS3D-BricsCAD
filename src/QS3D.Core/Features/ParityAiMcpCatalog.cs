namespace QS3D.Core.Features
{
    public static class ParityAiMcpCatalog
    {
        public static readonly FeatureId AiLunaId = new FeatureId("ai.luna");
        public static readonly FeatureId McpDirectCadId = new FeatureId("mcp.direct-cad");

        public static ParityWorkflowRegistry CreateRegistry()
        {
            return new ParityWorkflowRegistry(new[]
            {
                new ParityWorkflowBinding(
                    AiLunaId,
                    AiLunaId.ToString(),
                    ParityWorkflowKind.Infrastructure,
                    ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                    ParityWorkflowRequirement.None),
                new ParityWorkflowBinding(
                    McpDirectCadId,
                    McpDirectCadId.ToString(),
                    ParityWorkflowKind.Infrastructure,
                    ParityWorkflowSurface.Mcp,
                    ParityWorkflowRequirement.ActiveDocument)
            });
        }
    }
}
