
// This example Console application stops running when CTRL+P is pressed.

using System.Runtime.InteropServices;

// Remember the ids of the registered hot keys (don't use an id with value 0, -1 or -2)
const int HOTKEY_ID = 1;

// Modify the TryRegisterHotKeys method to the hot keys YOU want.
if (!TryRegisterHotKeys()) { return; }

var i = 0;
while (true)
{
    Console.WriteLine($"[{i++}] Hello, world!");
    Thread.Sleep(100);

    if (TryGetPressedHotKeyNonBlocking(out var id))
    {
        if (id == HOTKEY_ID)
        {
            break;
        }
    }
}

return;

// These methods and variables can be put in a class in another file.
const int MOD_CONTROL = 0x2;
const int MOD_NOREPEAT = 0x4000;
const int PM_REMOVE = 0x1;
const int WM_HOTKEY = 0x0312;

static bool TryRegisterHotKeys()
{
    // Register the hot key CTRL+P, no repeat on holding it down.
    if (!RegisterHotKey(
        IntPtr.Zero,
        HOTKEY_ID,
        MOD_CONTROL | MOD_NOREPEAT,
        'P'
    )) { return false; }

    return true;
}

static bool TryGetPressedHotKeyNonBlocking(out int id)
{
    // Check if there are messages available. If there is, remove them from the message queue. (You might not want to remove the messages from the queue)
    do
    {
        if (!PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE)) { break; }

        if (msg.Message == WM_HOTKEY)
        {
            id = (int)msg.W;
            return true;
        }
    } while (true);

    // There was no hot key message.
    id = 0;
    return false;
}

// DllImported Win32 Functions

// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
[DllImport("user32")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool RegisterHotKey(
  IntPtr hWnd,
  int id,
  uint fsModifiers,
  uint vk
);

// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-peekmessagew
[DllImport("user32", CharSet = CharSet.Unicode, EntryPoint = "PeekMessageW")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool PeekMessage(
    out Msg lpMsg,
    IntPtr hWnd,
    uint wMsgFilterMin,
    uint wMsgFilterMax,
    uint wRemoveMsg
);

// https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msg
[StructLayout(LayoutKind.Sequential)]
struct Msg
{
    public IntPtr WindowHandle;
    public uint Message;
    public nuint W;
    public nuint L;
    public uint Time;
    public int PointX;
    public int PointY;
    public uint Private;
}
