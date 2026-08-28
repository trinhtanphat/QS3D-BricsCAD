using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// One-window, click-first operations center for non-technical MCP users.
    /// It deliberately keeps Cloudflare credentials in the provider browser and
    /// never asks the user to open PowerShell/CMD.
    /// </summary>
    public sealed class McpAgentControlCenterCommands
    {
        [CommandMethod("QS3DMCPAGENTCENTER", CommandFlags.Modal)]
        public void ShowControlCenter()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                McpEmbeddedServer.EnsureStarted();
                new McpAgentControlCenterWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3D MCP Agent Center lỗi: " + ex.Message);
            }
        }
    }

    internal sealed class McpAgentControlCenterWindow : Window
    {
        private readonly TextBlock _status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 10)
        };
        private readonly TextBlock _activity = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 8)
        };
        private int _localOperationActive;
        private DispatcherTimer? _quickUrlTimer;
        private int _quickUrlPollTicks;

        public McpAgentControlCenterWindow()
        {
            Title = "QS3D - ChatGPT MCP Agent Center";
            Width = 720;
            Height = 760;
            MinWidth = 620;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Closed += (_, __) => StopQuickUrlPolling();

            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock
            {
                Text = "ChatGPT ↔ QS3D ↔ BricsCAD",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Mọi thao tác setup chính đều bằng nút bấm. Cloudflare username/password chỉ nhập trên trang đăng nhập Cloudflare trong browser; QS3D không hỏi và không lưu password.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            panel.Children.Add(Section("1. Cài đặt / kết nối"));
            panel.Children.Add(Button("Cài / cập nhật Cloudflare Tunnel tự động", InstallCloudflared));
            panel.Children.Add(Button("Đăng nhập Cloudflare + tạo Named Tunnel", (_, __) => OpenAccountSetup()));
            panel.Children.Add(Button("Khởi động lại Named Tunnel đã lưu", (_, __) => StartNamedTunnel()));
            panel.Children.Add(Button("Quick Tunnel (chỉ test)", (_, __) => StartQuickTunnel()));
            panel.Children.Add(Button("Dừng tất cả tunnel", (_, __) => StopTunnels()));

            panel.Children.Add(Section("2. ChatGPT connector"));
            panel.Children.Add(Button("Copy MCP URL", (_, __) => CopyUrl()));
            panel.Children.Add(Button("Copy Bearer Token", (_, __) => CopyToken()));
            panel.Children.Add(Button("Copy URL + Authorization", (_, __) => CopyConfig()));
            panel.Children.Add(Button("Mở ChatGPT", (_, __) => McpCloudflareAccountTunnelManager.OpenChatGpt()));
            panel.Children.Add(Button("Kiểm tra MCP protocol", (_, __) => CheckProtocol()));
            panel.Children.Add(Button("Tự kiểm tra Agent (read-only)", (_, __) => RunReadOnlySelfTest()));

            panel.Children.Add(Section("3. Điều khiển Agent"));
            var stop = Button("EMERGENCY STOP AGENT", (_, __) => InvokeControlTool("cad_agent_stop", "{}"));
            stop.FontWeight = FontWeights.Bold;
            stop.MinHeight = 42;
            panel.Children.Add(stop);
            panel.Children.Add(Button("Hủy command BricsCAD hiện tại (ESC x2)", (_, __) => InvokeControlTool("cad_cancel_command", "{}")));
            panel.Children.Add(Button("Resume Agent", (_, __) => InvokeControlTool("cad_agent_resume", "{\"confirmMutation\":true}")));
            panel.Children.Add(Button("Mở thư mục audit MCP", (_, __) => OpenAuditFolder()));

            panel.Children.Add(Section("Trạng thái"));
            panel.Children.Add(_status);
            panel.Children.Add(_activity);
            panel.Children.Add(Button("Refresh", (_, __) => RefreshStatus()));
            panel.Children.Add(Button("Đóng", (_, __) => Close()));

            Content = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            RefreshStatus();
        }

        private static TextBlock Section(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                Margin = new Thickness(0, 12, 0, 4)
            };
        }

        private static Button Button(string text, RoutedEventHandler handler)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 3, 0, 3),
                MinHeight = 32,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 4, 10, 4)
            };
            button.Click += handler;
            return button;
        }

        private void InstallCloudflared(object? sender, RoutedEventArgs args)
        {
            _activity.Text = "Đang tải cloudflared chính thức và kiểm tra Authenticode...";
            McpCloudflaredBootstrapper.BeginInstall((ok, message) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _activity.Text = message;
                    RefreshStatus();
                    if (!ok)
                    {
                        MessageBox.Show(
                            message + "\n\nBạn có thể thử lại nút cài tự động hoặc kiểm tra chính sách mạng/chứng thư trên máy.",
                            "QS3D MCP",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                })));
        }

        private void OpenAccountSetup()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                {
                    string adopted;
                    McpCloudflaredBootstrapper.AdoptExistingManagedBinary(out adopted);
                }
                new McpCloudflareAccountSetupWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            RefreshStatus();
        }

        private void StartNamedTunnel()
        {
            StopQuickUrlPolling();
            string error;
            if (!McpCloudflareAccountTunnelManager.StartSaved(out error))
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                _activity.Text = "Named Tunnel đang khởi động.";
            RefreshStatus();
        }

        private void StartQuickTunnel()
        {
            string error;
            if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
            {
                StopQuickUrlPolling();
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshStatus();
                return;
            }
            _activity.Text = "Quick Tunnel đang khởi động; đang chờ public URL...";
            StartQuickUrlPolling();
            RefreshStatus();
        }

        private void StartQuickUrlPolling()
        {
            StopQuickUrlPolling();
            _quickUrlPollTicks = 0;
            _quickUrlTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _quickUrlTimer.Tick += QuickUrlTimerOnTick;
            _quickUrlTimer.Start();
        }

        private void QuickUrlTimerOnTick(object? sender, EventArgs e)
        {
            _quickUrlPollTicks++;
            RefreshStatus();
            var publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                _activity.Text = "Quick Tunnel sẵn sàng: " + publicUrl;
                StopQuickUrlPolling();
                return;
            }
            if (!McpCloudflareTunnelManager.IsRunning || _quickUrlPollTicks >= 20)
            {
                if (McpCloudflareTunnelManager.IsRunning)
                    _activity.Text = "Quick Tunnel đang chạy nhưng chưa nhận được public URL trong thời gian chờ; có thể bấm Refresh để kiểm tra lại.";
                StopQuickUrlPolling();
            }
        }

        private void StopQuickUrlPolling()
        {
            var timer = _quickUrlTimer;
            _quickUrlTimer = null;
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= QuickUrlTimerOnTick;
        }

        private void StopTunnels()
        {
            StopQuickUrlPolling();
            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            McpCloudflareTunnelManager.StopForHostShutdown();
            _activity.Text = "Đã dừng tunnel của QS3D trong phiên BricsCAD này.";
            RefreshStatus();
        }

        private void CopyUrl()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Chưa có public MCP URL. Hãy tạo Named Tunnel hoặc Quick Tunnel trước.", "QS3D MCP");
                return;
            }
            Clipboard.SetText(url);
            _activity.Text = "Đã copy MCP URL.";
        }

        private void CopyToken()
        {
            McpEmbeddedServer.EnsureStarted();
            Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
            _activity.Text = "Đã copy Bearer Token. Không chia sẻ token này công khai.";
        }

        private void CopyConfig()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Chưa có public MCP URL.", "QS3D MCP");
                return;
            }
            Clipboard.SetText("MCP URL: " + url + Environment.NewLine
                              + "Authorization: Bearer " + McpEmbeddedServer.GetBearerToken());
            _activity.Text = "Đã copy URL + Authorization cho ChatGPT.";
        }

        private void CheckProtocol()
        {
            RunLocalOperation("Đang kiểm tra MCP protocol...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 5000).Message;
            }, true);
        }

        private void RunReadOnlySelfTest()
        {
            RunLocalOperation("Đang chạy Agent self-test read-only...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpLocalAgentClient.RunReadOnlySelfTest(McpEmbeddedServer.Endpoint, 6000);
            }, true);
        }

        private void InvokeControlTool(string tool, string arguments)
        {
            // Emergency stop/cancel must not be blocked by an observation self-test that is already
            // waiting on CAD context. They run on a worker too, but deliberately bypass the UI-only slot.
            RunLocalOperation("Đang gọi " + tool + "...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpLocalAgentClient.CallOne(McpEmbeddedServer.Endpoint, 6000, tool, arguments);
            }, false);
        }

        private void RunLocalOperation(string pendingMessage, Func<string> action, bool serialize)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var ownsSlot = false;
            if (serialize)
            {
                if (Interlocked.CompareExchange(ref _localOperationActive, 1, 0) != 0)
                {
                    _activity.Text = "Một MCP local check khác đang chạy; Emergency Stop/ESC vẫn luôn khả dụng.";
                    return;
                }
                ownsSlot = true;
            }

            _activity.Text = pendingMessage;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string message;
                try { message = action(); }
                catch (Exception ex) { message = "MCP local operation FAIL: " + ex.Message; }
                finally
                {
                    if (ownsSlot) Interlocked.Exchange(ref _localOperationActive, 0);
                }

                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _activity.Text = message;
                        RefreshStatus();
                    }));
                }
                catch { }
            });
        }

        private void OpenAuditFolder()
        {
            try
            {
                var path = McpEmbeddedServer.AuditFilePath;
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshStatus()
        {
            var publicUrl = McpPublicEndpointResolver.Resolve();
            _status.Text =
                "MCP embedded: " + (McpEmbeddedServer.IsRunning ? "RUNNING" : "STOPPED")
                + "\nLocal endpoint: " + McpEmbeddedServer.Endpoint
                + "\nCloudflare installed: " + (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                + "\nCloudflare browser login: " + McpCloudflareAccountTunnelManager.IsAuthenticated
                + "\nNamed Tunnel: " + (McpCloudflareAccountTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                + "\nFallback/Quick Tunnel: " + (McpCloudflareTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                + "\nPublic MCP: " + (string.IsNullOrWhiteSpace(publicUrl) ? "chưa có" : publicUrl)
                + "\nAgent: " + McpEmbeddedServer.Describe();
        }
    }

    /// <summary>
    /// Local-only MCP client used by the GUI for emergency controls and a read-only
    /// end-to-end self-test. It talks to the same loopback endpoint ChatGPT uses.
    /// </summary>
    internal static class McpLocalAgentClient
    {
        private const string ProtocolVersion = "2025-06-18";

        public static string CallOne(Uri endpoint, int timeoutMilliseconds, string tool, string argumentsJson)
        {
            string? session = null;
            try
            {
                session = Initialize(endpoint, timeoutMilliseconds);
                NotifyInitialized(endpoint, timeoutMilliseconds, session);
                return Call(endpoint, timeoutMilliseconds, session, tool, argumentsJson);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(session))
                {
                    try { Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, session); } catch { }
                }
            }
        }

        public static string RunReadOnlySelfTest(Uri endpoint, int timeoutMilliseconds)
        {
            string? session = null;
            try
            {
                session = Initialize(endpoint, timeoutMilliseconds);
                NotifyInitialized(endpoint, timeoutMilliseconds, session);
                var list = Send(endpoint, "POST",
                    "{\"jsonrpc\":\"2.0\",\"id\":20,\"method\":\"tools/list\",\"params\":{}}",
                    timeoutMilliseconds,
                    session);
                RequireSuccess(list, "tools/list");

                var required = new[]
                {
                    "connector_info", "qs3d_status", "cad_active_document", "cad_selection",
                    "cad_database_snapshot", "cad_entity_inspect", "cad_view_state", "cad_wait_idle",
                    "cad_create_line", "cad_create_circle", "cad_create_polyline", "cad_create_text",
                    "cad_entity_transform", "cad_entity_delete", "cad_layer", "cad_command_sequence",
                    "qs3d_run_command", "cad_ui_click", "cad_ui_type", "cad_ui_key",
                    "cad_agent_stop", "cad_agent_resume", "cad_audit_tail", "cad_cancel_command"
                };
                var missing = new List<string>();
                foreach (var name in required)
                    if (list.Body.IndexOf("\\\"name\\\":\\\"" + name + "\\\"", StringComparison.Ordinal) < 0
                        && list.Body.IndexOf("\"name\":\"" + name + "\"", StringComparison.Ordinal) < 0)
                        missing.Add(name);
                if (missing.Count > 0)
                    throw new InvalidOperationException("tools/list thiếu: " + string.Join(", ", missing));

                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "connector_info", "{}"), "connector_info");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "qs3d_status", "{}"), "qs3d_status");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_active_document", "{}"), "cad_active_document");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_view_state", "{}"), "cad_view_state");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_database_snapshot", "{\"limit\":20}"), "cad_database_snapshot");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_command_catalog", "{}"), "cad_command_catalog");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_audit_tail", "{\"limit\":3}"), "cad_audit_tail");

                return "SELF-TEST PASS: MCP initialize/session/tools/list + 7 read-only CAD/agent calls đều thành công; mutation không được chạy.";
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(session))
                {
                    try { Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, session); } catch { }
                }
            }
        }

        private static string Initialize(Uri endpoint, int timeoutMilliseconds)
        {
            var body = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
                       + ProtocolVersion
                       + "\",\"capabilities\":{},\"clientInfo\":{\"name\":\"QS3D-Agent-Center\",\"version\":\"1\"}}}";
            var result = Send(endpoint, "POST", body, timeoutMilliseconds, null);
            RequireSuccess(result, "initialize");
            if (string.IsNullOrWhiteSpace(result.SessionId))
                throw new InvalidOperationException("initialize không trả Mcp-Session-Id.");
            return result.SessionId!;
        }

        private static void NotifyInitialized(Uri endpoint, int timeoutMilliseconds, string session)
        {
            var result = Send(endpoint, "POST",
                "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}",
                timeoutMilliseconds,
                session);
            if (result.StatusCode != 202 && result.StatusCode != 204 && result.StatusCode != 200)
                throw new InvalidOperationException("notifications/initialized HTTP " + result.StatusCode + ".");
        }

        private static string Call(Uri endpoint, int timeoutMilliseconds, string session, string tool, string argumentsJson)
        {
            var safeArguments = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson.Trim();
            if (!safeArguments.StartsWith("{", StringComparison.Ordinal) || !safeArguments.EndsWith("}", StringComparison.Ordinal))
                throw new InvalidOperationException("Local MCP tool arguments must be a JSON object.");
            var request = "{\"jsonrpc\":\"2.0\",\"id\":30,\"method\":\"tools/call\",\"params\":{\"name\":\""
                          + McpEmbeddedServer.JsonEscape(tool)
                          + "\",\"arguments\":" + safeArguments + "}}";
            var result = Send(endpoint, "POST", request, timeoutMilliseconds, session);
            RequireSuccess(result, tool);
            RequireToolSuccess(result.Body, tool);
            return tool + ": OK";
        }

        private static void RequireToolSuccess(string body, string operation)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException(operation + " returned an empty response.");
            if (body.IndexOf("\\\"isError\\\":true", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("\"isError\":true", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(operation + " returned MCP isError=true.");
        }

        private static void RequireSuccess(LocalHttpResult result, string operation)
        {
            if (result.StatusCode < 200 || result.StatusCode >= 300)
                throw new InvalidOperationException(operation + " HTTP " + result.StatusCode + ".");
            if (!string.IsNullOrWhiteSpace(result.Body)
                && Regex.IsMatch(result.Body, "\\\"error\\\"\\s*:", RegexOptions.IgnoreCase))
                throw new InvalidOperationException(operation + " returned JSON-RPC error.");
        }

        private static LocalHttpResult Send(Uri endpoint, string method, string body, int timeoutMilliseconds, string? session)
        {
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
#pragma warning restore SYSLIB0014
            request.Method = method;
            request.Accept = "application/json, text/event-stream";
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + McpEmbeddedServer.GetBearerToken();
            if (!string.IsNullOrWhiteSpace(session)) request.Headers["Mcp-Session-Id"] = session;

            if (!string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                request.ContentType = "application/json";
                var payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
                request.ContentLength = payload.Length;
                using (var stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
            }
            else request.ContentLength = 0;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var responseBody = string.Empty;
                if (response.ContentLength != 0)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        using (var reader = new StreamReader(stream, Encoding.UTF8)) responseBody = NormalizeBody(reader.ReadToEnd());
                    }
                }
                return new LocalHttpResult((int)response.StatusCode, response.Headers["Mcp-Session-Id"], responseBody);
            }
        }

        private static string NormalizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || body.IndexOf("data:", StringComparison.OrdinalIgnoreCase) < 0)
                return body == null ? string.Empty : body.Trim();
            var builder = new StringBuilder();
            using (var reader = new StringReader(body))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                    if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) builder.Append(line.Substring(5).Trim());
            }
            return builder.Length == 0 ? body.Trim() : builder.ToString();
        }

        private sealed class LocalHttpResult
        {
            public LocalHttpResult(int statusCode, string? sessionId, string body)
            {
                StatusCode = statusCode;
                SessionId = sessionId;
                Body = body ?? string.Empty;
            }

            public int StatusCode { get; private set; }
            public string? SessionId { get; private set; }
            public string Body { get; private set; }
        }
    }
}
