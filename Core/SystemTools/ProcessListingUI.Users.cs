using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Core.SystemTools
{
    public sealed partial class ProcessListingUI
    {
        // Service configuration is readable by normal local users even when the
        // service's process token is not. It describes the configured logon
        // account (which can change before a restart), so label this fallback *.
        // Capture it afresh with each sample; never cache an owner by PID alone.
        private static Dictionary<int, string> CaptureServiceAccounts()
        {
            const uint managerConnectAndEnumerate = 0x0001 | 0x0004;
            const uint win32Services = 0x0030, activeServices = 0x0001;
            const int moreData = 234, bufferSize = 256 * 1024;
            var accounts = new Dictionary<int, string>();
            var conflicts = new HashSet<int>();
            IntPtr manager = IntPtr.Zero, buffer = IntPtr.Zero;
            try
            {
                manager = OpenSCManager(null, null, managerConnectAndEnumerate);
                if (manager == IntPtr.Zero) return accounts;
                buffer = Marshal.AllocHGlobal(bufferSize);
                uint resume = 0;
                int entrySize = Marshal.SizeOf<ServiceStatusEntry>();
                do
                {
                    uint previousResume = resume;
                    bool complete = EnumServicesStatusEx(manager, 0, win32Services, activeServices,
                        buffer, bufferSize, out _, out int count, ref resume, null);
                    int error = complete ? 0 : Marshal.GetLastWin32Error();
                    if (!complete && error != moreData) break;
                    for (int i = 0; i < count; i++)
                    {
                        var entry = Marshal.PtrToStructure<ServiceStatusEntry>(IntPtr.Add(buffer, i * entrySize));
                        // Start/stop-pending PIDs are not reliable. Per-user service
                        // templates do not describe the instance's actual account.
                        if (!HasServiceProcess(entry.Status) || (entry.Status.ServiceType & 0xC0) != 0) continue;
                        string account = ReadServiceAccount(manager, Marshal.PtrToStringUni(entry.Name), entry.Status.ProcessId);
                        if (account == null) continue;
                        int pid = entry.Status.ProcessId;
                        if (accounts.TryGetValue(pid, out string existing) &&
                            !string.Equals(existing, account, StringComparison.OrdinalIgnoreCase))
                            conflicts.Add(pid);
                        else
                            accounts[pid] = account;
                    }
                    if (complete || resume == previousResume) break;
                } while (resume != 0);
            }
            catch { } // Restricted SCM access must not prevent process enumeration.
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
                if (manager != IntPtr.Zero) CloseServiceHandle(manager);
            }
            foreach (int pid in conflicts) accounts.Remove(pid);
            return accounts;
        }

        private static bool HasServiceProcess(ServiceProcessStatus status)
            => status.ProcessId > 0 && status.CurrentState is 4 or 5 or 6 or 7;

        private static string ReadServiceAccount(IntPtr manager, string name, int processId)
        {
            const uint queryConfigAndStatus = 0x0001 | 0x0004;
            const int configBufferSize = 8192;
            IntPtr service = IntPtr.Zero, config = IntPtr.Zero;
            try
            {
                service = OpenService(manager, name, queryConfigAndStatus);
                if (service == IntPtr.Zero) return null;
                config = Marshal.AllocHGlobal(configBufferSize);
                if (!QueryServiceConfig(service, config, configBufferSize, out _)) return null;
                var settings = Marshal.PtrToStructure<ServiceConfig>(config);
                if (!QueryServiceStatusEx(service, 0, out ServiceProcessStatus status,
                        Marshal.SizeOf<ServiceProcessStatus>(), out _) ||
                    !HasServiceProcess(status) || status.ProcessId != processId)
                    return null; // It stopped or restarted while its configuration was read.
                return NormalizeServiceAccount(Marshal.PtrToStringUni(settings.StartName));
            }
            finally
            {
                if (config != IntPtr.Zero) Marshal.FreeHGlobal(config);
                if (service != IntPtr.Zero) CloseServiceHandle(service);
            }
        }

        private static string NormalizeServiceAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account)) return null;
            if (account.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                account.Equals(@"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase)) return "SYSTEM";
            if (account.Equals(@"NT AUTHORITY\LocalService", StringComparison.OrdinalIgnoreCase)) return "LOCAL SERVICE";
            if (account.Equals(@"NT AUTHORITY\NetworkService", StringComparison.OrdinalIgnoreCase)) return "NETWORK SERVICE";
            int slash = account.IndexOf('\\');
            return slash >= 0 ? account[(slash + 1)..] : account;
        }

        private static string ServiceAccountForProcess(int pid, long started, long captured,
            Dictionary<int, string> accounts)
        {
            // A newly created process may have reused a PID from the service sample.
            return started <= captured ? accounts.GetValueOrDefault(pid) : null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ServiceProcessStatus
        {
            public uint ServiceType, CurrentState, ControlsAccepted, Win32ExitCode;
            public uint ServiceSpecificExitCode, CheckPoint, WaitHint;
            public int ProcessId;
            public uint ServiceFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ServiceStatusEntry
        {
            public IntPtr Name, DisplayName;
            public ServiceProcessStatus Status;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ServiceConfig
        {
            public uint ServiceType, StartType, ErrorControl;
            public IntPtr BinaryPathName, LoadOrderGroup;
            public uint TagId;
            public IntPtr Dependencies, StartName, DisplayName;
        }

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machine, string database, uint access);

        [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenService(IntPtr manager, string name, uint access);

        [DllImport("advapi32.dll", EntryPoint = "EnumServicesStatusExW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumServicesStatusEx(IntPtr manager, int level, uint type, uint state,
            IntPtr services, int size, out int needed, out int count, ref uint resume, string group);

        [DllImport("advapi32.dll", EntryPoint = "QueryServiceConfigW", ExactSpelling = true, SetLastError = true)]
        private static extern bool QueryServiceConfig(IntPtr service, IntPtr config, int size, out int needed);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool QueryServiceStatusEx(IntPtr service, int level, out ServiceProcessStatus status, int size, out int needed);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr service);
    }
}
