using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using QS3D.Core.Interoperability.Sap2000;

namespace QS3D.BricsCAD.Sap2000
{
    /// <summary>
    /// Late-bound CSI OAPI adapter. Keeping SAP2000v1 out of compile-time references allows
    /// QS3D to build without redistributing CSI proprietary assemblies.
    /// </summary>
    internal sealed class Sap2000ComBridge : IDisposable
    {
        private const string HelperProgId = "SAP2000v1.Helper";
        private const string SapObjectProgId = "CSI.SAP2000.API.SapObject";
        private const int KnMetreCelsiusUnits = 6;

        private object _helper;
        private object _sapObject;
        private object _sapModel;
        private bool _disposed;

        private Sap2000ComBridge(object helper, object sapObject, object sapModel, bool startedNewInstance)
        {
            _helper = helper;
            _sapObject = sapObject;
            _sapModel = sapModel;
            StartedNewInstance = startedNewInstance;
        }

        public bool StartedNewInstance { get; }

        public static Sap2000ComBridge ConnectOrStart()
        {
            var helperType = Type.GetTypeFromProgID(HelperProgId);
            if (helperType == null)
            {
                throw new InvalidOperationException(
                    "SAP2000 OAPI is not registered. Install a supported SAP2000 version and run it once before using QS3D SAP commands.");
            }

            var helper = Activator.CreateInstance(helperType);
            if (helper == null)
            {
                throw new InvalidOperationException("Could not create the SAP2000 OAPI helper.");
            }

            object sapObject = null;
            var startedNewInstance = false;

            try
            {
                try
                {
                    sapObject = Invoke(helper, "GetObject", SapObjectProgId);
                }
                catch (Exception ex) when (IsAttachFailure(ex))
                {
                    sapObject = null;
                }

                if (sapObject == null)
                {
                    sapObject = Invoke(helper, "CreateObjectProgID", SapObjectProgId);
                    if (sapObject == null)
                    {
                        throw new InvalidOperationException("SAP2000 OAPI could not create a SAP2000 application object.");
                    }

                    EnsureSuccess(InvokeStatus(sapObject, "ApplicationStart"), "start SAP2000");
                    startedNewInstance = true;
                }

                var sapModel = GetProperty(sapObject, "SapModel");
                if (sapModel == null)
                {
                    throw new InvalidOperationException("SAP2000 OAPI returned no SapModel object.");
                }

                return new Sap2000ComBridge(helper, sapObject, sapModel, startedNewInstance);
            }
            catch
            {
                ReleaseComObject(sapObject);
                ReleaseComObject(helper);
                throw;
            }
        }

        public void InitializeBlankModel()
        {
            ThrowIfDisposed();
            EnsureSuccess(InvokeStatus(_sapModel, "InitializeNewModel"), "initialize a SAP2000 model");

            var file = GetProperty(_sapModel, "File");
            try
            {
                EnsureSuccess(InvokeStatus(file, "NewBlank"), "create a blank SAP2000 model");
            }
            finally
            {
                ReleaseComObject(file);
            }

            EnsureSuccess(InvokeStatus(_sapModel, "SetPresentUnits", KnMetreCelsiusUnits), "set SAP2000 units to kN-m-C");
        }

        public string AddFrame(Sap2000FrameMember frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            ThrowIfDisposed();
            UnlockForEditing();

            var frameObject = GetProperty(_sapModel, "FrameObj");
            try
            {
                var arguments = new object[]
                {
                    frame.Start.X,
                    frame.Start.Y,
                    frame.Start.Z,
                    frame.End.X,
                    frame.End.Y,
                    frame.End.Z,
                    string.Empty,
                    frame.SectionName,
                    frame.UserName,
                    "Global"
                };

                var status = InvokeStatusWithArguments(frameObject, "AddByCoord", arguments);
                EnsureSuccess(status, "add frame " + frame.UserName);
                return Convert.ToString(arguments[6], CultureInfo.InvariantCulture) ?? frame.UserName;
            }
            finally
            {
                ReleaseComObject(frameObject);
            }
        }

        public string AddArea(Sap2000AreaMember area)
        {
            if (area == null)
            {
                throw new ArgumentNullException(nameof(area));
            }

            ThrowIfDisposed();
            UnlockForEditing();

            var x = new double[area.Vertices.Count];
            var y = new double[area.Vertices.Count];
            var z = new double[area.Vertices.Count];
            for (var i = 0; i < area.Vertices.Count; i++)
            {
                x[i] = area.Vertices[i].X;
                y[i] = area.Vertices[i].Y;
                z[i] = area.Vertices[i].Z;
            }

            var areaObject = GetProperty(_sapModel, "AreaObj");
            try
            {
                var arguments = new object[]
                {
                    area.Vertices.Count,
                    x,
                    y,
                    z,
                    string.Empty,
                    area.PropertyName,
                    area.UserName,
                    "Global"
                };

                var status = InvokeStatusWithArguments(areaObject, "AddByCoord", arguments);
                EnsureSuccess(status, "add area " + area.UserName);
                return Convert.ToString(arguments[4], CultureInfo.InvariantCulture) ?? area.UserName;
            }
            finally
            {
                ReleaseComObject(areaObject);
            }
        }

        public void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A SAP2000 .sdb path is required.", nameof(path));
            }

            ThrowIfDisposed();
            var file = GetProperty(_sapModel, "File");
            try
            {
                EnsureSuccess(InvokeStatus(file, "Save", path), "save SAP2000 model");
            }
            finally
            {
                ReleaseComObject(file);
            }
        }

        public void RunAnalysis()
        {
            ThrowIfDisposed();
            var analyze = GetProperty(_sapModel, "Analyze");
            try
            {
                EnsureSuccess(InvokeStatus(analyze, "RunAnalysis"), "run SAP2000 analysis");
            }
            finally
            {
                ReleaseComObject(analyze);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ReleaseComObject(_sapModel);
            ReleaseComObject(_sapObject);
            ReleaseComObject(_helper);
            _sapModel = null;
            _sapObject = null;
            _helper = null;
        }

        private void UnlockForEditing()
        {
            try
            {
                var status = InvokeStatus(_sapModel, "SetModelIsLocked", false);
                EnsureSuccess(status, "unlock SAP2000 model for editing");
            }
            catch (MissingMethodException)
            {
                // Older supported OAPI builds can expose editing without this convenience method.
            }
        }

        private static object GetProperty(object target, string propertyName)
        {
            if (target == null)
            {
                throw new InvalidOperationException("SAP2000 OAPI object is unavailable.");
            }

            return target.GetType().InvokeMember(
                propertyName,
                BindingFlags.GetProperty,
                null,
                target,
                null,
                CultureInfo.InvariantCulture);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            if (target == null)
            {
                throw new InvalidOperationException("SAP2000 OAPI object is unavailable.");
            }

            return target.GetType().InvokeMember(
                methodName,
                BindingFlags.InvokeMethod,
                null,
                target,
                arguments,
                CultureInfo.InvariantCulture);
        }

        private static int InvokeStatus(object target, string methodName, params object[] arguments)
        {
            return Convert.ToInt32(Invoke(target, methodName, arguments), CultureInfo.InvariantCulture);
        }

        private static int InvokeStatusWithArguments(object target, string methodName, object[] arguments)
        {
            return Convert.ToInt32(Invoke(target, methodName, arguments), CultureInfo.InvariantCulture);
        }

        private static void EnsureSuccess(int status, string operation)
        {
            if (status != 0)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "SAP2000 OAPI failed to {0} (return code {1}).", operation, status));
            }
        }

        private static bool IsAttachFailure(Exception exception)
        {
            var current = exception;
            while (current != null)
            {
                if (current is COMException || current is TargetInvocationException || current is InvalidOperationException)
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

        private static void ReleaseComObject(object value)
        {
            if (value == null)
            {
                return;
            }

            try
            {
                if (Marshal.IsComObject(value))
                {
                    Marshal.FinalReleaseComObject(value);
                }
            }
            catch
            {
                // Releasing an RCW is best-effort and must not close a user-owned SAP2000 process.
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Sap2000ComBridge));
            }
        }
    }

    internal static class Sap2000Session
    {
        private static readonly object Gate = new object();
        private static Sap2000ComBridge _current;

        public static Sap2000ComBridge GetOrConnect()
        {
            lock (Gate)
            {
                if (_current != null)
                {
                    return _current;
                }

                _current = Sap2000ComBridge.ConnectOrStart();
                return _current;
            }
        }

        public static void Reset()
        {
            lock (Gate)
            {
                if (_current != null)
                {
                    _current.Dispose();
                    _current = null;
                }
            }
        }
    }
}
