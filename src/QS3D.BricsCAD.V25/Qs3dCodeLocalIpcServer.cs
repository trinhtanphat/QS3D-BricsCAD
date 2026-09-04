using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Single-process local IPC listener with bounded messages and per-session capability authentication.
    /// </summary>
    internal sealed class Qs3dCodeLocalIpcServer : IDisposable
    {
        private const int MaxRequestBytes = 131072;
        private const int MaxResponseBytes = 524288;
        private readonly object _gate = new object();
        private readonly string _pipeName;
        private readonly string _capability;
        private readonly Func<string, string> _handler;
        private Thread _thread;
        private NamedPipeServerStream _activePipe;
        private volatile bool _stopping;

        internal Qs3dCodeLocalIpcServer(string pipeName, string capability, Func<string, string> handler)
        {
            if (string.IsNullOrWhiteSpace(pipeName)) throw new ArgumentException("Local pipe name is required.", nameof(pipeName));
            if (string.IsNullOrWhiteSpace(capability)) throw new ArgumentException("Local capability is required.", nameof(capability));
            _pipeName = pipeName;
            _capability = capability;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        internal void Start()
        {
            lock (_gate)
            {
                if (_thread != null) return;
                _stopping = false;
                _thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "QS3D Code local IPC"
                };
                _thread.Start();
            }
        }

        internal void Stop()
        {
            Thread thread;
            NamedPipeServerStream pipe;
            lock (_gate)
            {
                _stopping = true;
                thread = _thread;
                _thread = null;
                pipe = _activePipe;
                _activePipe = null;
            }

            try { if (pipe != null) pipe.Dispose(); } catch { }
            if (thread != null && thread != Thread.CurrentThread)
            {
                try { thread.Join(2000); } catch { }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void Run()
        {
            while (!_stopping)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    var options = PipeOptions.Asynchronous | PipeOptions.WriteThrough;
#if BRICSCAD_V26
                    options |= PipeOptions.CurrentUserOnly;
#endif
                    pipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        options,
                        4096,
                        4096);
                    lock (_gate)
                    {
                        if (_stopping)
                        {
                            pipe.Dispose();
                            break;
                        }
                        _activePipe = pipe;
                    }

                    pipe.WaitForConnection();
                    if (_stopping) break;

                    var request = ReadBoundedLine(pipe, MaxRequestBytes);
                    string response;
                    try
                    {
                        var supplied = McpTopLevelJson.ExtractString(request, "capability");
                        if (!FixedTimeEquals(supplied, _capability))
                            response = "{\"ok\":false,\"errorCode\":\"authentication_failed\"}";
                        else
                            response = _handler(request) ?? "{\"ok\":false,\"errorCode\":\"host_error\"}";
                    }
                    catch (Exception)
                    {
                        response = "{\"ok\":false,\"errorCode\":\"bad_request\"}";
                    }
                    WriteBoundedLine(pipe, response, MaxResponseBytes);
                }
                catch (ObjectDisposedException)
                {
                    if (!_stopping) Thread.Sleep(50);
                }
                catch (IOException)
                {
                    if (!_stopping) Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    if (!_stopping)
                    {
                        McpDiagnosticHub.Record("qs3d-code", "warning", "local-ipc-failure", ex.GetType().Name);
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    lock (_gate)
                    {
                        if (ReferenceEquals(_activePipe, pipe)) _activePipe = null;
                    }
                    try { if (pipe != null) pipe.Dispose(); } catch { }
                }
            }
        }

        private static string ReadBoundedLine(Stream stream, int maxBytes)
        {
            using (var memory = new MemoryStream())
            {
                while (memory.Length <= maxBytes)
                {
                    var value = stream.ReadByte();
                    if (value < 0 || value == '\n') break;
                    if (value == '\r') continue;
                    memory.WriteByte((byte)value);
                }
                if (memory.Length > maxBytes)
                    throw new InvalidDataException("Local IPC request exceeds the bounded message size.");
                return Encoding.UTF8.GetString(memory.ToArray());
            }
        }

        private static void WriteBoundedLine(Stream stream, string response, int maxBytes)
        {
            var bytes = Encoding.UTF8.GetBytes(response ?? string.Empty);
            if (bytes.Length > maxBytes)
                bytes = Encoding.UTF8.GetBytes("{\"ok\":false,\"errorCode\":\"response_too_large\"}");
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte((byte)'\n');
            stream.Flush();
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var b = Encoding.UTF8.GetBytes(right ?? string.Empty);
            var difference = a.Length ^ b.Length;
            var count = Math.Max(a.Length, b.Length);
            for (var i = 0; i < count; i++)
            {
                var av = i < a.Length ? a[i] : (byte)0;
                var bv = i < b.Length ? b[i] : (byte)0;
                difference |= av ^ bv;
            }
            return difference == 0;
        }
    }
}
