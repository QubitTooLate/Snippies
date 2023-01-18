
// The quickest way to take screenshots in Windows!
// 
// A very simple wrapper for a complicated api.
// 
// https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/desktop-dup-api

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Take sceenshots of the first monitor (output) connected to the first GPU (adapter)
using var desktopDuplicator = new DesktopDuplicator(0, 0); 
var buffer = new byte[desktopDuplicator.RequiredBufferSizeInBytes];

var stopwatch = Stopwatch.StartNew();
desktopDuplicator.WriteFrameIntoBuffer(buffer.AsSpan(), out var frameInfo);
stopwatch.Stop();
Console.WriteLine(stopwatch.Elapsed);

unsafe
{
    fixed (byte* scan0 = buffer)
    {
        using var bitmap = new Bitmap(
            desktopDuplicator.Width,
            desktopDuplicator.Height,
            desktopDuplicator.Stride,
            PixelFormat.Format32bppArgb,
            (nint)scan0
        );

        bitmap.Save(@"Screenshot.png");
    }
}

return;

/// <summary>
/// Source: https://github.com/QubitTooLate/Snippies
/// </summary>
public unsafe sealed class DesktopDuplicator : IDisposable
{
    private readonly void*** _d3d11DeviceContext;
    private readonly void*** _dxgiOutputDuplication;
    private readonly void*** _d3d11Texture2DFrameBuffer;
    private readonly void* _frameBuffer;

    private readonly int _width;
    private readonly int _height;

    public DesktopDuplicator(int adapter, int output)
    {
        var dxgiFactory1 = default(void***);
        var dxgiAdapter1 = default(void***);
        var dxgiOutput = default(void***);
        var dxgiOutput1 = default(void***); 
        var d3d11Device = default(void***); 
        var d3d11DeviceContext = default(void***); 
        var dxgiOutputDuplication = default(void***);
        var d3d11Texture2DFirstFrame = default(void***);
        var d3d11Texture2DFrameBuffer = default(void***);
        var frameBuffer = default(void*);

        try
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-createdxgifactory1
            var dxgiFactory1Guid = new Guid("770AAE78-F26F-4DBA-A829-253C83D1B387");
            CreateDXGIFactory1(
                &dxgiFactory1Guid,
                &dxgiFactory1
            ).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgifactory1-enumadapters1
            ((delegate* unmanaged[Stdcall]<void***, int, void****, int>)dxgiFactory1[0][12])(dxgiFactory1, adapter, &dxgiAdapter1).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiadapter-enumoutputs
            ((delegate* unmanaged[Stdcall]<void***, int, void****, int>)dxgiAdapter1[0][7])(dxgiAdapter1, output, &dxgiOutput).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)
            var dxgiOutput1Guid = new Guid("00CDDEA8-939B-4B83-A340-A685226666CC");
            ((delegate* unmanaged[Stdcall]<void***, Guid*, void****, int>)dxgiOutput[0][0])(dxgiOutput, &dxgiOutput1Guid, &dxgiOutput1).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-d3d11createdevice
            D3D11CreateDevice(
                dxgiAdapter1,
                D3D_DRIVER_TYPE_UNKNOWN,
                IntPtr.Zero,
#if DEBUG
                CREATE_DEVICE_FLAGS_DEBUG |
#endif
                CREATE_DEVICE_FLAGS_RGBA_SUPPORT,
                null,
                0,
                D3D11_SDK_VERSION,
                &d3d11Device,
                null,
                &d3d11DeviceContext
            ).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgioutput1-duplicateoutput
            ((delegate* unmanaged[Stdcall]<void***, void***, void****, int>)dxgiOutput1[0][22])(dxgiOutput1, d3d11Device, &dxgiOutputDuplication).Assert();

            var frameInfo = default(FrameInfo);
            AcquireNextFrame(dxgiOutputDuplication, &frameInfo, &d3d11Texture2DFirstFrame);

            // https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11texture2d-getdesc
            var description = default(Texture2DDescription);
            ((delegate* unmanaged[Stdcall]<void***, Texture2DDescription*, void>)d3d11Texture2DFirstFrame[0][10])(d3d11Texture2DFirstFrame, &description);

            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgioutputduplication-releaseframe
            ((delegate* unmanaged[Stdcall]<void***, int>)dxgiOutputDuplication[0][14])(dxgiOutputDuplication).Assert();

            _width = (int)description.Width;
            _height = (int)description.Height;

            description.BindFlags = 0;
            description.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG_READ;
            description.Usage = D3D11_USAGE_STAGING;
            description.MiscFlags = 0;

            // https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-createtexture2d
            ((delegate* unmanaged[Stdcall]<void***, Texture2DDescription*, SubresourceData*, void****, int>)d3d11Device[0][5])(d3d11Device, &description, null, &d3d11Texture2DFrameBuffer).Assert();

            frameBuffer = NativeMemory.Alloc(description.Width * 4 * description.Height);
        }
        catch
        {
            Release(d3d11DeviceContext); 
            Release(dxgiOutputDuplication);
            Release(d3d11Texture2DFrameBuffer);
            NativeMemory.Free(frameBuffer);
            throw;
        }
        finally
        {
            Release(dxgiFactory1);
            Release(dxgiAdapter1);
            Release(dxgiOutput);
            Release(dxgiOutput1);
            Release(d3d11Device);
            Release(d3d11Texture2DFirstFrame);
        }

        _d3d11DeviceContext = d3d11DeviceContext;
        _dxgiOutputDuplication = dxgiOutputDuplication; 
        _d3d11Texture2DFrameBuffer = d3d11Texture2DFrameBuffer;
        _frameBuffer = frameBuffer;
    }

    public int Width => _width;
    public int Height => _height;
    public int RequiredBufferSizeInBytes => _width * 4 * _height;
    public int Stride => _width * 4;

    public void WriteFrameIntoBuffer(Span<byte> buffer, out FrameInfo frame_info) 
    {
        var d3d11Texture2DFrame = default(void***);
        var frameInfo = default(FrameInfo);

        try
        {
            AcquireNextFrame(_dxgiOutputDuplication, &frameInfo, &d3d11Texture2DFrame);

            // https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11devicecontext-copyresource
            ((delegate* unmanaged[Stdcall]<void***, void***, void***, void>)_d3d11DeviceContext[0][47])(_d3d11DeviceContext, _d3d11Texture2DFrameBuffer, d3d11Texture2DFrame);

            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgioutputduplication-releaseframe
            ((delegate* unmanaged[Stdcall]<void***, int>)_dxgiOutputDuplication[0][14])(_dxgiOutputDuplication).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11devicecontext-map
            var mapped = default(MappedSubresource);
            ((delegate* unmanaged[Stdcall]<void***, void***, uint, int, uint, MappedSubresource*, int>)_d3d11DeviceContext[0][14])(
                _d3d11DeviceContext,
                _d3d11Texture2DFrameBuffer,
                0,
                D3D11_MAP_READ,
                0,
                &mapped
            ).Assert();

            new Span<byte>(mapped.Data, (int)(mapped.RowPitch * _height)).CopyTo(buffer);

            // https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11devicecontext-unmap
            ((delegate* unmanaged[Stdcall]<void***, void***, uint, void>)_d3d11DeviceContext[0][15])(_d3d11DeviceContext, _d3d11Texture2DFrameBuffer, 0);
        }
        finally
        {
            Release(d3d11Texture2DFrame);
        }

        frame_info = frameInfo;
    }

    public void Dispose()
    {
        Release(_d3d11DeviceContext);
        Release(_dxgiOutputDuplication);
        Release(_d3d11Texture2DFrameBuffer);
        NativeMemory.Free(_frameBuffer);
    }

    private static void Release(void*** unknown)
    {
        if (unknown == null) { return; }
        _ = ((delegate* unmanaged[Stdcall]<void***, uint>)unknown[0][2])(unknown);
    }

    private static void AcquireNextFrame(void*** dxgi_output_duplication, FrameInfo* out_frame_info, void**** out_d3d11_texture_2D, uint max_wait_milliseconds = 1000)
    {
        var dxgiResource = default(void***);
        var d3d11Texture2D = default(void***);

        try
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgioutputduplication-acquirenextframe
            ((delegate* unmanaged[Stdcall]<void***, uint, FrameInfo*, void****, int>)dxgi_output_duplication[0][8])(dxgi_output_duplication, max_wait_milliseconds, out_frame_info, &dxgiResource).Assert();

            // https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)
            var guid = new Guid("6F15AAF2-D208-4E89-9AB4-489535D34F9C");
            ((delegate* unmanaged[Stdcall]<void***, Guid*, void****, int>)dxgiResource[0][0])(dxgiResource, &guid, &d3d11Texture2D).Assert();
        }
        catch
        {
            Release(d3d11Texture2D);
            *out_d3d11_texture_2D = null;
            throw;
        }
        finally
        {
            Release(dxgiResource);
        }

        *out_d3d11_texture_2D = d3d11Texture2D;
    }

    [DllImport("dxgi", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int CreateDXGIFactory1(Guid* riid, void**** ppFactory);

    private const int D3D_DRIVER_TYPE_UNKNOWN = 0;
    private const int CREATE_DEVICE_FLAGS_DEBUG = 2;
    private const int CREATE_DEVICE_FLAGS_RGBA_SUPPORT = 32;
    private const int D3D11_SDK_VERSION = 7;

    [DllImport("d3d11", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int D3D11CreateDevice(
        [Optional] void*** pAdapter,
        int DriverType,
        IntPtr Software,
        int Flags,
        [Optional] int* pFeatureLevels,
        uint FeatureLevels,
        uint SDKVersion,
        [Optional] void**** ppDevice,
        [Optional] int* pFeatureLevel,
        [Optional] void**** ppImmediateContext
    );

    private const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    private const int D3D11_USAGE_STAGING = 3;
    private const int D3D11_CPU_ACCESS_FLAG_READ = 131072;

    private struct Texture2DDescription
    {
        public uint Width;
        public uint Height;
        public uint MipLevels;
        public uint ArraySize;
        public int Format;
        public uint Count;
        public uint Quality;
        public int Usage;
        public int BindFlags;
        public int CPUAccessFlags;
        public int MiscFlags;
    }

    private const int D3D11_MAP_READ = 1;

    private struct SubresourceData
    {
        public void* Memory;
        public uint Pitch;
        public uint SlicePitch;
    }

    private struct MappedSubresource
    {
        public void* Data;
        public uint RowPitch;
        public uint DepthPitch;
    }
}

public struct FrameInfo
{
    public long LastPresentTime;
    public long LastMouseUpdateTime;
    public uint AccumulatedFrames;
    public int RectsCoalesced;
    public int ProtectedContentMaskedOut;
    public PointerPosition PointerPosition;
    public uint TotalMetadataBufferSize;
    public uint PointerShapeBufferSize;
}

public struct PointerPosition
{
    public int PositionX;
    public int PositionY;
    public int Visible;
}

public static class HResultExtensions
{
    private const int S_OK = 0;

    public static void Assert(this int hresult)
    {
        if (hresult == S_OK) { return; }
        throw Marshal.GetExceptionForHR(hresult) ?? new Exception($"HRESULT: 0x{hresult:x8}.");
    }
}
