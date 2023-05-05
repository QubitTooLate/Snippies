
*Note: this example is a combination of C / C# how your code will actually look can be different depending on how you P/invoke it. This code is not complete and error handling may be improved.*

## Registering the window class.

```cs
WNDCLASSEXW windowClass = default;
windowClass.cbSize = sizeof(windowClass);
windowClass.hCursor = LoadCursorW(NULL, IDC_ARROW);
windowClass.lpfnWndProc = &UnmanagedProcedure;
windowClass.lpszClassName = className;

if (RegisterClassExW(&windowClass) == 0) { return; }
```

## Creating the window.

### Window properties

```cs
int styles = WS_OVERLAPPEDWINDOW;
int extendedStyles = 0;
```

### Use a layered window. 

It's better to make your window layered, or else you will get useless `WM_PAINT` events (worse perfomance).

There are 2 types of layered windows: One where the window can be made transparent (alpha 0-255), and the other where a specific color will show as completely transparent.

There is also an option to make a layered window click trough, this way the cursor can't interact with it but interacts with the window underneath instead.

```cs
bool isLayered = true;
bool isAlphaNotCutout = true;
int cutoutColor = 0;
byte alpha = 255;
bool isClickTrough = false;
```

### DWM settings

The DWM is responsible for drawing the window frame.

(Windows 11 only) Choose the material of the window frame (backdrop).

Set the window to use the dark theme.

```cs
bool useBackdrop = false;
int backdropType = 0; // 0, 1, 2 = Mica, 3 = Acrylic, 4 = Saturated Mica
bool useDarkTheme = true;
```

### Composing the window

A composed window has many advantages like the alpha value of the client area won't be ignored, resizing the client area works better and better performance overall.

```cs
bool isComposed = false;
```

```cs
extendedStyles |= isLayered ? WS_EX_LAYERED : 0;
extendedStyles |= isClickTrough ? WS_EX_TRANSPARENT : 0;
extendedStyles |= isComposed ? WS_EX_NOREDIRECTIONBITMAP : 0;

HWND windowHandle = CreateWindowEx(
    extendedStyles,
    className,
    title,
    styles,
    x,
    y,
    width, 
    height,
    null,
    null,
    null,
    null
);

if (windowHandle == 0) { return; }

// Don't forget to free the GCHandle when disposing this class.
// This is used to forward the window message from UnmanagedProcedure to Procedure
GCHandle gcHandle = GCHandle.Alloc(this, GCHandleType.Normal); 
void* param = GCHandle.ToIntPtr(_gcHandle).ToPointer();
_ = SetWindowLongPtr(windowHandle, GWLP_USERDATA, param);

if (isLayered)
{
    if (!SetLayeredWindowAttributes(
        windowHandle,
        cutoutColor,
        alpha | (isAlphaNotCutout ? 0 : 255),
        isAlphaNotCutout ? LWA_ALPHA : LWA_COLORKEY
    )) { return; }
}

if (useBackdrop)
{
    int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    if (S_OK != DwmSetWindowAttribute(
        windowHandle,
        DWMWA_SYSTEMBACKDROP_TYPE,
        &backdropType,
        4
    )) { return; }
}

if (useDarkTheme)
{
    int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    int yes = 1;
    if (S_OK != DwmSetWindowAttribute(
        windowHandle,
        DWMWA_USE_IMMERSIVE_DARK_MODE,
        &yes,
        4
    )) { return; }
}

// Create the graphics for the window here

ShowWindow(windowHandle, SW_NORMAL);
```

## The window procedure

A window procedure is needed to receive all the events of the window.

```cs
[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
private static unsafe nint UnmanagedProcedure(IntPtr window_handle, uint message, nuint w, nint l)
{
    void* userData = GetWindowLongPtr(window_handle, GWLP_USERDATA);

    if (userData != null)
    {
        var unmanagedReference = GCHandle.FromIntPtr((IntPtr)userData);
        var win32Window = (Win32Window)unmanagedReference.Target;
        return win32Window.Procedure(window_handle, message, w, l);
    }

    return DefWindowProc(window_handle, message, w, l);
}

private nint Procedure(IntPtr window_handle, uint message, nuint w, nint l)
{
    switch (message)
    {
        case WM_CLOSE:
        {
            _ = DestroyWindow(window_handle);
            break;
        }
        case WM_DESTROY:
        {
            PostQuitMessage(0);
            break;
        }
        default: { return DefWindowProc(window_handle, message, w, l); }
    }

    return 0;
}
```

## Pumping the window messages

There are 2 options, `GetMessage` and `PeekMessage`, one is blocking and the other is not.

```cs
MSG msg = default;
while (GetMessageW(&msg, IntPtr.Zero, 0, 0) != 0)
{
    _ = DispatchMessageW(&msg);
}
```

## Graphics

To keep it simple, use Direct2D. To make it better do it ontop of DirectX 11, and to do even better use that on Direct Composition.

How to do this might be added later.
