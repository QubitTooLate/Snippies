// To Run your application in the background with minimal impact on your system (Windows).
// You can run it on a lower priority, meaning taking away less CPU time of other processes.
// You can also run it in efficiency mode (Windows 11) with this feature the process gets
// run in a more efficient way, or core to reduce CPU power usage.
// You can also specify on which cores your process runs. (On my system, games run mostly on core 3,
// so this process is set to core 0)

// Error checking is removed from this example!

using System.Runtime.InteropServices;

const uint PROCESS_SET_INFORMATION = 0x0200;
const uint IDLE_PRIORITY_CLASS = 0x00000040;
const uint ProcessPowerThrottling = 4;
const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 1;

// Get our process handle
int id = GetCurrentProcessId();
IntPtr processHandle = OpenProcess(PROCESS_SET_INFORMATION, 0, id);

// To set the priority of the process
SetPriorityClass(processHandle, IDLE_PRIORITY_CLASS);

// To set efficiency mode
PROCESS_POWER_THROTTLING_STATE state = new()
{
    ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
    StateMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
    Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
};

unsafe { SetProcessInformation(processHandle, ProcessPowerThrottling, &state, sizeof(PROCESS_POWER_THROTTLING_STATE)); }

// To set the cores to run on
SetProcessAffinityMask(processHandle, 1); // In this case only core 0

//Don't forget to clean up after yourself!
CloseHandle(processHandle);

return;

// https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos/
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentprocessid
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-process_power_throttling_state
// https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setprocessaffinitymask
// https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle

[DllImport("kernel32", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
static extern int GetCurrentProcessId();

[DllImport("kernel32", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
static extern IntPtr OpenProcess(
    uint dwDesiredAccess,
    byte bInheritHandle,
    int dwProcessId
);

[DllImport("kernel32", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool SetPriorityClass(
    IntPtr hProcess,
    uint dwPriorityClass
);

[DllImport("kernel32", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern unsafe bool SetProcessInformation(
    IntPtr hProcess,
      uint ProcessInformationClass,
    void* ProcessInformation,
    int ProcessInformationSize
);

[DllImport("kernel32", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool SetProcessAffinityMask(
    IntPtr hProcess,
    nuint dwProcessAffinityMask
);

[DllImport("kernel32", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool CloseHandle(
    IntPtr hObject
);

[StructLayout(LayoutKind.Sequential)]
struct PROCESS_POWER_THROTTLING_STATE
{
    public uint Version;
    public uint ControlMask;
    public uint StateMask;
}
