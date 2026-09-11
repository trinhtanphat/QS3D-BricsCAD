using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Licensed-runtime proof for LOCAL-003. The PowerShell runner opens only a
    /// disposable synthetic DWG and kills the host without saving it. The marker
    /// contains aggregate counts and Z measurements only; Handles and paths are
    /// deliberately excluded.
    /// </summary>
    public sealed class LevelZRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_LEVEL_Z_RESULT";
        private const string NonceVariable = "QS3D_LEVEL_Z_NONCE";
