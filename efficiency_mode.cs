// To Run your application in the background with minimal impact on your system (Windows).
// You can run it on a lower priority, meaning taking away less CPU time of other processes.
// You can also run it in efficiency mode (Windows 11) with this feature the process gets
// run in a more efficient way, or core to reduce CPU power usage.
// You can also specify on which cores your process runs. (On my system, games run mostly on core 3,
// so this process is set to core 0)

// Error checking is removed from this example!

const uint PROCESS_SET_INFORMATION = 0x0200;
const uint IDLE_PRIORITY_CLASS = 0x00000040;
const uint ProcessPowerThrottling = 4;
const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 1;

// Get our process handle
int id = GetCurrentProcessId();
IntPtr processHandle = OpenProcess(PROCESS_SET_INFORMATION, false, id));

// To set the priority of the process
SetPriorityClass(processHandle, IDLE_PRIORITY_CLASS);

// To set efficiency mode
PROCESS_POWER_THROTTLING_STATE state =
{
	ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
	StateMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
	Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
};

SetProcessInformation(processHandle, ProcessPowerThrottling, &state, sizeof(PROCESS_POWER_THROTTLING_STATE)));

// To set the cores to run on
SetProcessAffinityMask(processHandle, 1);

//Don't forget to clean up after yourself!
CloseHandle(processHandle);

// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocessid
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass
// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-process_power_throttling_state
// https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setprocessaffinitymask
// https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle
