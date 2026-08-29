using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    internal enum McpOnboardingPhase
    {
        EmbeddedServerStarting,
        CloudflaredMissing,
        CloudflareLoginRequired,
        NamedTunnelRequired,
        PublicEndpointReady,
        ChatGptRegistrationRequired,
        Ready,
        ErrorRecovery
    }

    internal sealed class McpOnboardingSnapshot
    {
        public McpOnboardingSnapshot(
            McpOnboardingPhase phase,
            string title,
            string detail,
            string nextStep,
            bool mcpRunning,
            bool cloudflaredInstalled,
            bool cloudflareAuthenticated,
            bool namedTunnelRunning,
            string publicUrl,
            bool chatGptRegistrationAcknowledged)
        {
            Phase = phase;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            NextStep = nextStep ?? string.Empty;
            McpRunning = mcpRunning;
            CloudflaredInstalled = cloudflaredInstalled;
            CloudflareAuthenticated = cloudflareAuthenticated;
            NamedTunnelRunning = namedTunnelRunning;
            PublicUrl = publicUrl ?? string.Empty;
            ChatGptRegistrationAcknowledged = chatGptRegistrationAcknowledged;
        }

        public McpOnboardingPhase Phase { get; private set; }
        public string Title { get; private set; }
        public string Detail { get; private set; }
        public string NextStep { get; private set; }
        public bool McpRunning { get; private set; }
        public bool CloudflaredInstalled { get; private set; }
        public bool CloudflareAuthenticated { get; private set; }
        public bool NamedTunnelRunning { get; private set; }
        public string PublicUrl { get; private set; }
        public bool ChatGptRegistrationAcknowledged { get; private set; }
    }

    internal sealed class McpExperienceEvent
    {
        public McpExperienceEvent(DateTime utc, string level, string category, string message, string currentAction, string nextStep)
        {
            Utc = utc;
            Level = level ?? string.Empty;
            Category = category ?? string.Empty;
            Message = message ?? string.Empty;
            CurrentAction = currentAction ?? string.Empty;
            NextStep = nextStep ?? string.Empty;
        }

        public DateTime Utc { get; private set; }
        public string Level { get; private set; }
        public string Category { get; private set; }
        public string Message { get; private set; }
        public string CurrentAction { get; private set; }
        public string NextStep { get; private set; }
    }

    /// <summary>
    /// Bounded local mirror of MCP/onboarding/recovery activity. This is intentionally
    /// operational metadata only; callers must not publish bearer/OAuth tokens, typed text,
    /// clipboard contents, screenshots or document contents into this timeline.
    /// </summary>
    internal static class McpAgentExperience
    {
        internal const int MaxEvents = 120;
        private const int MaxFieldLength = 1200;
        private static readonly object Sync = new object();
        private static readonly Queue<McpExperienceEvent> Events = new Queue<McpExperienceEvent>();
        private static string _currentAction = string.Empty;
        private static string _nextStep = string.Empty;
        private static string _lastError = string.Empty;
        private static DateTime _updatedUtc = DateTime.UtcNow;

        private static string StateDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "Experience");
            }
        }

        private static string RegistrationMarkerPath
        {
            get { return Path.Combine(StateDirectory, "chatgpt-registration.txt"); }
        }

        public static string CurrentAction { get { lock (Sync) return _currentAction; } }
        public static string NextStep { get { lock (Sync) return _nextStep; } }
        public static string LastError { get { lock (Sync) return _lastError; } }
        public static DateTime UpdatedUtc { get { lock (Sync) return _updatedUtc; } }

        public static void Info(string category, string message, string currentAction, string nextStep)
        {
            Publish("info", category, message, currentAction, nextStep, false);
        }

        public static void Success(string category, string message, string nextStep)
        {
            Publish("success", category, message, string.Empty, nextStep, false);
        }

        public static void Warning(string category, string message, string nextStep)
        {
            Publish("warning", category, message, string.Empty, nextStep, false);
        }

        public static void Error(string category, string message, string nextStep)
        {
            Publish("error", category, message, string.Empty, nextStep, true);
        }

        public static void ActionStarted(string category, string action, string nextStep)
        {
            Publish("active", category, action, action, nextStep, false);
        }

        public static void ActionFinished(string category, string message, string nextStep)
        {
            Publish("success", category, message, string.Empty, nextStep, false);
        }

        public static McpExperienceEvent[] Recent(int limit)
        {
            lock (Sync)
            {
                var snapshot = Events.ToArray();
                var count = Math.Max(0, Math.Min(limit, snapshot.Length));
                var result = new McpExperienceEvent[count];
                for (var i = 0; i < count; i++) result[i] = snapshot[snapshot.Length - count + i];
                return result;
            }
        }

        public static McpOnboardingSnapshot DetermineOnboarding()
        {
            var mcpRunning = McpEmbeddedServer.IsRunning;
            var cloudflaredInstalled = !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath);
            var authenticated = McpCloudflareAccountTunnelManager.IsAuthenticated;
            var namedTunnelRunning = McpCloudflareAccountTunnelManager.IsRunning;
            var publicUrl = McpPublicEndpointResolver.Resolve();
            var savedHostname = McpCloudflareAccountTunnelManager.SavedHostname;
            var registered = IsRegistrationAcknowledged(publicUrl);
            var serverError = McpEmbeddedServer.LastError;
            var tunnelError = McpCloudflareAccountTunnelManager.LastError;

            if (!mcpRunning)
                return Snapshot(McpOnboardingPhase.EmbeddedServerStarting, "MCP local chưa chạy",
                    "QS3D sẽ khởi động embedded MCP trên loopback; không cần cài MCP repo riêng.",
                    "Khởi động embedded MCP rồi Refresh.", mcpRunning, cloudflaredInstalled, authenticated, namedTunnelRunning, publicUrl, registered);

            if (!string.IsNullOrWhiteSpace(serverError) && !McpEmbeddedServer.IsRunning)
                return Snapshot(McpOnboardingPhase.ErrorRecovery, "Embedded MCP cần xử lý",
                    Bounded(serverError), "Mở Nâng cao và chạy kiểm tra MCP protocol.", mcpRunning, cloudflaredInstalled, authenticated, namedTunnelRunning, publicUrl, registered);

            if (!cloudflaredInstalled)
                return Snapshot(McpOnboardingPhase.CloudflaredMissing, "Cần Cloudflare Tunnel",
                    "Cài cloudflared chính thức bằng nút trong QS3D. QS3D kiểm tra binary trước khi nhận dùng.",
                    "Bấm “Cài Cloudflare Tunnel”.", mcpRunning, false, authenticated, namedTunnelRunning, publicUrl, registered);

            if (!authenticated)
                return Snapshot(McpOnboardingPhase.CloudflareLoginRequired, "Đăng nhập Cloudflare",
                    "Đăng nhập diễn ra trên browser do Cloudflare mở. QS3D không nhận hoặc lưu mật khẩu Cloudflare.",
                    "Bấm “Đăng nhập Cloudflare” và hoàn tất trên browser.", mcpRunning, true, false, namedTunnelRunning, publicUrl, registered);

            if (string.IsNullOrWhiteSpace(savedHostname) || string.IsNullOrWhiteSpace(publicUrl))
                return Snapshot(McpOnboardingPhase.NamedTunnelRequired, "Tạo Named Tunnel ổn định",
                    "Production nên dùng hostname HTTPS ổn định. Quick Tunnel chỉ dùng test.",
                    "Bấm “Tạo / sửa Named Tunnel” và chọn hostname public.", mcpRunning, true, true, namedTunnelRunning, publicUrl, registered);

            if (!namedTunnelRunning)
                return Snapshot(McpOnboardingPhase.PublicEndpointReady, "Named Tunnel đã cấu hình",
                    "QS3D đã có hostname lưu nhưng tunnel chưa chạy trong phiên này.",
                    "Bấm “Khởi động Named Tunnel”.", mcpRunning, true, true, false, publicUrl, registered);

            if (!registered)
                return Snapshot(McpOnboardingPhase.ChatGptRegistrationRequired, "Kết nối ChatGPT",
                    "Mở ChatGPT bằng browser hệ thống và thêm custom MCP bằng public URL. OAuth/DCR là đường khuyến nghị; không cần nhập mật khẩu ChatGPT vào QS3D.",
                    "Copy MCP URL, mở ChatGPT, thêm MCP rồi bấm “Đã thêm MCP trong ChatGPT”.", mcpRunning, true, true, true, publicUrl, false);

            if (!string.IsNullOrWhiteSpace(tunnelError))
                return Snapshot(McpOnboardingPhase.ErrorRecovery, "Tunnel có cảnh báo",
                    Bounded(tunnelError), "Refresh hoặc mở Cloudflare setup để kiểm tra tunnel.", mcpRunning, true, true, namedTunnelRunning, publicUrl, true);

            return Snapshot(McpOnboardingPhase.Ready, "MCP sẵn sàng",
                "Embedded MCP + Named Tunnel + đăng ký ChatGPT đã được người dùng xác nhận. Desktop-wide control vẫn mặc định OFF cho tới khi bật local consent.",
                "Prompt trong ChatGPT; bật quyền desktop trong tab Agent khi thật sự cần thao tác ngoài BricsCAD.", mcpRunning, true, true, true, publicUrl, true);
        }

        public static void MarkChatGptRegistrationAcknowledged()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Chưa có public MCP URL để ghi nhận đăng ký ChatGPT.");
            Directory.CreateDirectory(StateDirectory);
            File.WriteAllText(RegistrationMarkerPath, RegistrationFingerprint(url), new UTF8Encoding(false));
            Success("onboarding", "Đã ghi nhận user hoàn tất thêm MCP trong ChatGPT.", "Chạy protocol check hoặc bắt đầu prompt trong ChatGPT.");
        }

        public static void ForgetChatGptRegistrationAcknowledgement()
        {
            try { if (File.Exists(RegistrationMarkerPath)) File.Delete(RegistrationMarkerPath); } catch { }
        }

        private static bool IsRegistrationAcknowledged(string publicUrl)
        {
            if (string.IsNullOrWhiteSpace(publicUrl)) return false;
            try
            {
                if (!File.Exists(RegistrationMarkerPath)) return false;
                var saved = File.ReadAllText(RegistrationMarkerPath, Encoding.UTF8).Trim();
                return string.Equals(saved, RegistrationFingerprint(publicUrl), StringComparison.Ordinal);
            }
            catch { return false; }
        }

        private static string RegistrationFingerprint(string publicUrl)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes((publicUrl ?? string.Empty).Trim().ToLowerInvariant()));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static McpOnboardingSnapshot Snapshot(
            McpOnboardingPhase phase,
            string title,
            string detail,
            string nextStep,
            bool mcpRunning,
            bool cloudflaredInstalled,
            bool authenticated,
            bool namedTunnelRunning,
            string publicUrl,
            bool registered)
        {
            return new McpOnboardingSnapshot(phase, title, detail, nextStep, mcpRunning, cloudflaredInstalled,
                authenticated, namedTunnelRunning, publicUrl, registered);
        }

        private static void Publish(string level, string category, string message, string currentAction, string nextStep, bool error)
        {
            var item = new McpExperienceEvent(
                DateTime.UtcNow,
                Bounded(level),
                Bounded(category),
                Bounded(message),
                Bounded(currentAction),
                Bounded(nextStep));
            lock (Sync)
            {
                while (Events.Count >= MaxEvents) Events.Dequeue();
                Events.Enqueue(item);
                _currentAction = item.CurrentAction;
                _nextStep = item.NextStep;
                if (error) _lastError = item.Message;
                else if (string.Equals(level, "success", StringComparison.Ordinal)) _lastError = string.Empty;
                _updatedUtc = item.Utc;
            }
        }

        private static string Bounded(string value)
        {
            value = (value ?? string.Empty).Replace("\0", string.Empty).Trim();
            return value.Length <= MaxFieldLength ? value : value.Substring(0, MaxFieldLength);
        }
    }
}
