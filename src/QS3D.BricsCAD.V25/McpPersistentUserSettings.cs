using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// User-scoped persistence for MCP secrets that must survive BricsCAD restarts.
    /// Secrets are stored as Windows Generic Credentials, never as plaintext QS3D files.
    /// Non-secret tunnel/provider/client-path/autostart metadata remains owned by the existing
    /// transport managers under the QS3D application-data directory.
    /// </summary>
    internal static class McpPersistentUserSettings
    {
        private const string OpenAiRuntimeKeyTarget = "QS3D.BricsCAD.MCP.OpenAI.RuntimeApiKey";
        private const uint CredTypeGeneric = 1;
        private const uint CredPersistLocalMachine = 2;
        private const int ErrorNotFound = 1168;
        private const int MaxSecretCharacters = 4096;

        public static bool HasSavedOpenAiRuntimeApiKey
        {
            get
            {
                string ignored;
                return TryReadOpenAiRuntimeApiKey(out ignored);
            }
        }

        /// <summary>
        /// Called once during plugin startup before transport auto-start. Explicit process/user
        /// environment variables remain authoritative; otherwise a previously saved Windows
        /// credential is projected into the current process so the child tunnel client inherits it.
        /// </summary>
        public static void ApplyStartupSecretsToProcessEnvironment()
        {
            var external = ReadEnvironmentKey();
            if (!string.IsNullOrWhiteSpace(external))
            {
                // Best effort: make a valid environment-backed setup restart-safe as well.
                TrySaveOpenAiRuntimeApiKey(external);
                return;
            }

            string saved;
            if (!TryReadOpenAiRuntimeApiKey(out saved) || string.IsNullOrWhiteSpace(saved)) return;
            Environment.SetEnvironmentVariable("CONTROL_PLANE_API_KEY", saved, EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// Capture a Runtime API key typed locally in Agent Center. This is intentionally callable
        /// only by local plugin/UI code; it is not exposed as an MCP tool.
        /// </summary>
        public static void SaveOpenAiRuntimeApiKey(string value)
        {
            var secret = NormalizeSecret(value);
            if (secret.Length == 0) throw new InvalidOperationException("Runtime API key is empty.");
            WriteCredential(OpenAiRuntimeKeyTarget, secret);
            Environment.SetEnvironmentVariable("CONTROL_PLANE_API_KEY", secret, EnvironmentVariableTarget.Process);
        }

        public static bool TrySaveOpenAiRuntimeApiKey(string value)
        {
            try
            {
                SaveOpenAiRuntimeApiKey(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryReadOpenAiRuntimeApiKey(out string value)
        {
            value = string.Empty;
            IntPtr credentialPtr = IntPtr.Zero;
            try
            {
                if (!CredRead(OpenAiRuntimeKeyTarget, CredTypeGeneric, 0, out credentialPtr))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == ErrorNotFound) return false;
                    throw new Win32Exception(error, "Windows Credential Manager could not read the QS3D MCP credential.");
                }

                var credential = (NativeCredential)Marshal.PtrToStructure(credentialPtr, typeof(NativeCredential));
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0) return false;
                if (credential.CredentialBlobSize > MaxSecretCharacters * 4)
                    throw new InvalidOperationException("Saved QS3D MCP credential is unexpectedly large.");

                var bytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
                value = NormalizeSecret(Encoding.UTF8.GetString(bytes));
                Array.Clear(bytes, 0, bytes.Length);
                return value.Length > 0;
            }
            finally
            {
                if (credentialPtr != IntPtr.Zero) CredFree(credentialPtr);
            }
        }

        public static void DeleteOpenAiRuntimeApiKey()
        {
            if (CredDelete(OpenAiRuntimeKeyTarget, CredTypeGeneric, 0)) return;
            var error = Marshal.GetLastWin32Error();
            if (error != ErrorNotFound)
                throw new Win32Exception(error, "Windows Credential Manager could not delete the QS3D MCP credential.");
        }

        private static string ReadEnvironmentKey()
        {
            var value = Environment.GetEnvironmentVariable("CONTROL_PLANE_API_KEY");
            if (!string.IsNullOrWhiteSpace(value)) return NormalizeSecret(value);
            value = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            return NormalizeSecret(value);
        }

        private static string NormalizeSecret(string value)
        {
            var secret = (value ?? string.Empty).Replace("\0", string.Empty).Trim();
            if (secret.Length > MaxSecretCharacters)
                throw new InvalidOperationException("Runtime API key exceeds the supported length.");
            return secret;
        }

        private static void WriteCredential(string target, string secret)
        {
            var bytes = Encoding.UTF8.GetBytes(secret);
            IntPtr blob = IntPtr.Zero;
            try
            {
                blob = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                var credential = new NativeCredential
                {
                    Flags = 0,
                    Type = CredTypeGeneric,
                    TargetName = target,
                    Comment = "QS3D BricsCAD MCP OpenAI Runtime API key",
                    CredentialBlobSize = (uint)bytes.Length,
                    CredentialBlob = blob,
                    Persist = CredPersistLocalMachine,
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    TargetAlias = null,
                    UserName = Environment.UserName
                };

                if (!CredWrite(ref credential, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows Credential Manager could not save the QS3D MCP credential.");
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
                if (blob != IntPtr.Zero)
                {
                    for (var i = 0; i < secret.Length && i < MaxSecretCharacters; i++) Marshal.WriteByte(blob, i, 0);
                    Marshal.FreeCoTaskMem(blob);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
            public NativeFileTime LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeFileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);
    }
}
