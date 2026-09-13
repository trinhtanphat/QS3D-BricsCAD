from pathlib import Path
p=Path('src/QS3D.BricsCAD.V25/McpAgentControlCenter.cs')
s=p.read_text(encoding='utf-8')
checks={
'operation generation field':'_backgroundOperationGeneration' in s,
'generation capture':'Volatile.Read(ref _backgroundOperationGeneration)' in s,
'late callback fence':'operationGeneration != Volatile.Read(ref _backgroundOperationGeneration)' in s,
'closed publication fence':'if (_closed || operationGeneration' in s,
'sanitized worker failure':'SanitizeBackgroundFailure(ex)' in s,
'no raw exception publication':'message = "MCP local operation FAIL: " + ex.Message' not in s,
}
failed=[k for k,v in checks.items() if not v]
if failed:
    raise SystemExit('FAIL: '+', '.join(failed))
print('PASS: MCP Agent Center background status lifecycle guard')
