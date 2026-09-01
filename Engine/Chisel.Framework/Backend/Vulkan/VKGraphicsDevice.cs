using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Hexa.NET.SDL3;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Vma;

namespace Chisel.Framework;

public class VKGraphicsDevice : Disposable, IGraphicsDevice
{
    public uint FrameIndex => FrameIndexInternal;
    public uint SampleCount => SampleCountInternal;
    public uint BufferCount => BufferCountInternal;
    public ImageFormat[] ColorFormats => ColorFormatsInternal;
    public ImageFormat? DepthStencilFormat => DepthStencilFormatInternal;
    public GraphicsBackend Backend => GraphicsBackend.Vulkan; // This will never change obv

    internal uint FrameIndexInternal { get; set; }
    internal uint SampleCountInternal { get; set; }
    internal uint BufferCountInternal { get; set; }
    internal ImageFormat[] ColorFormatsInternal { get; set; }
    internal ImageFormat? DepthStencilFormatInternal { get; set; }
    internal static readonly ImageFormat[] BackBufferColorFormats = { ImageFormat.R8G8B8A8UNorm };

    private readonly Vk _vk;
    private static readonly uint _vkVersion = Vk.Version13;
    private bool _isDebug;

    // Vulkan Main
    private Instance _instance;
    private PhysicalDevice _physDevice;
    private Device _logiDevice;

    private uint _graphicsQueueFamily, _presentQueueFamily;
    private Queue _graphicsQueue, _presentQueue;
    private Semaphore[] _imageAvailableSemas;   // len = frames in flight
    private Semaphore[] _renderFinishedSemas;   // len = swapchain images
    private Fence[] _inFlightFences;            // len = frames in flight

    private CommandBuffer[] _cmdBuffers;

    private SurfaceKHR _surface;
    private KhrSurface _surfaceExt;

    private SwapchainKHR _swapChain;
    private KhrSwapchain _swapChainExt;

    private Image[] _swapImages;
    private ImageView[] _swapImageViews;
    private Format _swapFormat;
    private Extent2D _swapExtent;

    private CommandPool _cmdPool;

    private uint _currentFrame;
    private uint _currentImage;

    // Vulkan Debug
    private ExtDebugUtils _dbgUtilities;
    private DebugUtilsMessengerEXT _dbgMessenger;
    private DebugUtilsMessengerCallbackFunctionEXT? _dbgCallback;
    private static readonly string[] _validation = { "VK_LAYER_KHRONOS_validation" };

    public VKGraphicsDevice(bool debug)
    {
        _vk = Vk.GetApi();
        _isDebug = debug;

        FrameIndexInternal = 0;
        SampleCountInternal = 1;
        BufferCountInternal = 2;
        ColorFormatsInternal = BackBufferColorFormats;
        DepthStencilFormatInternal = ImageFormat.D24UNormS8UInt;

        InitInstance();
        InitDebugMessenger();
        InitSurface();
        InitPhysicalDevice();
        InitLogicalDevice();
        InitSwapChain();
        InitImageViews();
        InitCommandPool();
        InitCommandBuffer();
        InitSemaphores();
        InitFences();
        QueryGpuInfo();
    }

    public unsafe void BeginFrame()
    {
        Fence fence = _inFlightFences[_currentFrame];
        Result waitResult = _vk.WaitForFences(_logiDevice, 1, &fence, true, 10000000000); // 10 second wait

        if (waitResult != Result.Success)
        {
            throw new InvalidOperationException($"WaitForFences failed! VkResult: {waitResult}");
        }

        Result acquireResult = _swapChainExt.AcquireNextImage(_logiDevice, _swapChain, ulong.MaxValue, _imageAvailableSemas[_currentFrame], default, ref _currentImage);

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            UtilRecreateSwapChain();
            return;
        }

        if (acquireResult != Result.Success)
        {
            throw new InvalidOperationException($"Failed to acquire VK swapchain image! VkResult: {acquireResult}");
        }

        _vk.ResetFences(_logiDevice, 1, &fence);
        CommandBuffer cmd = _cmdBuffers[_currentFrame];
        _vk.ResetCommandBuffer(cmd, CommandBufferResetFlags.None);

        CommandBufferBeginInfo beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Result beginResult = _vk.BeginCommandBuffer(cmd, &beginInfo);

        if (beginResult != Result.Success)
        {
            throw new InvalidOperationException($"Failed to begin VK command buffer! VkResult: {beginResult}");
        }

        UtilTransitionImageLayout(cmd, _swapImages[_currentImage], ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
    }

    public unsafe void EndFrame()
    {
        CommandBuffer cmd = _cmdBuffers[_currentFrame];
        UtilTransitionImageLayout(cmd, _swapImages[_currentImage], ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr);

        Result endResult = _vk.EndCommandBuffer(cmd);

        if (endResult != Result.Success)
        {
            throw new InvalidOperationException($"Failed to end VK command buffer! VkResult: {endResult}");
        }

        Semaphore waitSema = _imageAvailableSemas[_currentFrame];
        Semaphore signalSema = _renderFinishedSemas[_currentImage];
        PipelineStageFlags waitStage = PipelineStageFlags.TransferBit;

        SubmitInfo submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSema,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSema,
        };

        Result submitResult = _vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, _inFlightFences[_currentFrame]);

        if (submitResult != Result.Success)
        {
            throw new InvalidOperationException($"Failed to submit VK command buffer! VkResult: {submitResult}");
        }

        SwapchainKHR swapchain = _swapChain;
        uint imageIndex = _currentImage;

        PresentInfoKHR presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSema,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
        };

        Result presentResult = _swapChainExt.QueuePresent(_presentQueue, in presentInfo);

        if (presentResult == Result.ErrorOutOfDateKhr || presentResult == Result.SuboptimalKhr)
        {
            UtilRecreateSwapChain();
        }
        else if (presentResult != Result.Success)
        {
            throw new InvalidOperationException($"Failed to present VK swapchain image! VkResult: {presentResult}");
        }

        _currentFrame = (_currentFrame + 1) % BufferCountInternal;
        FrameIndexInternal++;
    }

    public void BeginDrawing(IRenderTarget target)
    {

    }

    public void EndDrawing()
    {

    }

    public void Clear(Color clearColor)
    {
        Clear(clearColor, 1.0f, 0, GraphicsClearFlags.Color | GraphicsClearFlags.Depth);
    }

    public unsafe void Clear(Color clearColor, float clearDepth, int clearStencil, GraphicsClearFlags flags)
    {
        CommandBuffer cmd = _cmdBuffers[_currentFrame];
        Vector4 color = clearColor.ToVector4Normalized();
        ClearColorValue colorValue = new ClearColorValue(color.X, color.Y, color.Z, color.W);

        ImageAspectFlags aspect = ImageAspectFlags.None;
        bool depthClear = flags.HasFlag(GraphicsClearFlags.Depth);

        if (flags.HasFlag(GraphicsClearFlags.Color))
        {
            aspect |= ImageAspectFlags.ColorBit;
        }

        if (flags.HasFlag(GraphicsClearFlags.Stencil))
        {
            aspect |= ImageAspectFlags.StencilBit;
        }

        if (depthClear)
        {
            aspect |= ImageAspectFlags.DepthBit;
        }

        ImageSubresourceRange range = new ImageSubresourceRange
        {
            AspectMask = aspect,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1,
        };

        _vk.CmdClearColorImage(cmd, _swapImages[_currentImage], ImageLayout.TransferDstOptimal, &colorValue, 1, &range);

        if (depthClear)
        {
            ClearDepthStencilValue depthStencilValue = new ClearDepthStencilValue(clearDepth, (uint)clearStencil);
            _vk.CmdClearDepthStencilImage(cmd, _swapImages[_currentImage], ImageLayout.TransferDstOptimal, &depthStencilValue, 1, &range);
        }
    }

    public void Resize(int width, int height)
    {
        UtilRecreateSwapChain();
    }

    public void Draw(uint vertexCount)
    {

    }

    public void DrawIndexed(uint indexCount)
    {

    }

    public void DrawIndexed(uint indexCount, uint startIndex, int baseVertex)
    {

    }

    public void DrawInstanced(uint vertexCount, uint instCount)
    {

    }

    public void DrawIndexedInstanced(uint indexCount, uint instCount)
    {

    }

    public void DrawIndexedInstanced(uint indexCount, uint instCount, uint startIndex, int baseVertex)
    {

    }

    public void DrawIndirect(IBuffer buffer, uint drawCount)
    {

    }

    public void DrawIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {

    }

    public void DrawIndexedIndirect(IBuffer buffer, uint drawCount)
    {

    }

    public void DrawIndexedIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {

    }

    public void Dispatch(uint groupX, uint groupY, uint groupZ)
    {

    }

    public void DispatchIndirect(IBuffer buffer)
    {

    }

    public void DispatchIndirect(IBuffer buffer, ulong offset)
    {

    }

    public void SetViewport(Vector2 position, Vector2 size)
    {

    }

    public void SetScissor(Vector2 position, Vector2 size)
    {

    }

    public void SetScissorEnabled(bool enabled)
    {

    }

    public void SetConstants<T>(in T value, uint slot)
        where T : unmanaged
    {

    }

    public void SetVertexLayout(VertexLayoutDescription layout, uint slot)
    {

    }

    public void BindVertexBuffer(IBuffer buffer, uint slot)
    {

    }

    public void BindIndexBuffer(IBuffer buffer)
    {

    }

    public void BindConstantBuffer(IBuffer buffer, uint slot)
    {

    }

    public void BindConstantBuffer(IBuffer buffer, ulong offset, uint size, uint slot)
    {

    }

    public void BindStorageBuffer(IBuffer buffer)
    {

    }

    public void BindImage(IImage image, uint slot)
    {

    }

    public void BindSampler(ISampler sampler, uint slot)
    {

    }

    public void BindGraphicsState(IGraphicsState graphicsState)
    {

    }

    public void BindComputeState(IComputeState computeState)
    {

    }

    public void BindMaterialTable(IMaterialTable materialTable)
    {

    }

    public void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data)
    {

    }

    public void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data, ulong offset)
    {

    }

    public void SuballocBuffer(ReadOnlySpan<byte> data, out IBuffer arena, out ulong offset)
    {
        arena = null;
        offset = 0;
    }

    public void CopyBuffer(IBuffer bufferSrc, IBuffer bufferDst)
    {

    }

    public void CopyBuffer(IBuffer bufferSrc, IBuffer bufferDst, BufferCopyRegion region)
    {

    }

    public void CopyBufferToImage(IBuffer bufferSrc, IImage imageDst)
    {

    }

    public void CopyBufferToImage(IBuffer bufferSrc, IImage imageDst, ImageBufferCopyRegion region)
    {

    }

    public void ResolveImage(IImage imageSrc, IImage imageDst)
    {

    }

    public void CopyImage(IImage imageSrc, IImage imageDst)
    {

    }

    public void CopyImage(IImage imageSrc, IImage imageDst, ImageCopyRegion region)
    {

    }

    public void CopyImageToBuffer(IImage imageSrc, IBuffer bufferDst)
    {

    }

    public void CopyImageToBuffer(IImage imageSrc, IBuffer bufferDst, ImageBufferCopyRegion region)
    {

    }

    public IBuffer CreateBuffer(BufferDescription bufferDesc)
    {
        return null;
    }

    public IImage CreateImage(ImageDescription imageDesc)
    {
        return null;
    }

    public ISampler CreateSampler(SamplerDescription samplerDesc)
    {
        return null;
    }

    public IShader CreateShader(ShaderDescription shaderDesc)
    {
        return null;
    }

    public IRenderTarget CreateRenderTarget(RenderTargetDescription targetDesc)
    {
        return null;
    }

    public IGraphicsState CreateGraphicsState(GraphicsStateDescription graphicsDesc)
    {
        return null;
    }

    public IComputeState CreateComputeState(ComputeStateDescription computeDesc)
    {
        return null;
    }

    public IMaterialTable CreateMaterialTable(MaterialTableDescription materialDesc)
    {
        return null;
    }

    public void GenerateMipmaps(IImage image, ReadOnlySpan<byte> baseLevelData)
    {

    }

    protected override unsafe void Dispose(bool disposing)
    {
        _vk.DeviceWaitIdle(_logiDevice); // Just in case
        _vk.DestroyInstance(_instance, null);

        if (_isDebug)
        {
            _dbgUtilities.DestroyDebugUtilsMessenger(_instance, _dbgMessenger, null);
        }

        UtilDestroySwapChain();

        _vk.DestroyDevice(_logiDevice, null);
        _surfaceExt.DestroySurface(_instance, _surface, null);
        _vk.DestroyCommandPool(_logiDevice, _cmdPool, null);

        for (int i = 0; i < _imageAvailableSemas.Length; i++)
        {
            _vk.DestroySemaphore(_logiDevice, _imageAvailableSemas[i], null);
        }

        for (int i = 0; i < _renderFinishedSemas.Length; i++)
        {
            _vk.DestroySemaphore(_logiDevice, _renderFinishedSemas[i], null);
        }

        for (int i = 0; i < _inFlightFences.Length; i++)
        {
            _vk.DestroyFence(_logiDevice, _inFlightFences[i], null);
        }
    }

    #region VK Util

    private void UtilLogMessage(string msg)
    {
        Logger.AppendLog("VK", msg, ConsoleColor.DarkCyan, 1);
    }

    private unsafe uint UtilOnDebugMessage(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT type,
        DebugUtilsMessengerCallbackDataEXT* callbackData, void* userData)
    {
        UtilLogMessage(Marshal.PtrToStringUTF8((nint)callbackData->PMessage) ?? string.Empty);
        return Vk.False;
    }

    private unsafe string[] UtilGetRequiredExtensions()
    {
        uint count;
        byte** raws = SDL.VulkanGetInstanceExtensions(&count);
        List<string> exts = new List<string>((int)count);

        for (uint i = 0; i < count; i++)
        {
            exts.Add(Marshal.PtrToStringUTF8((nint)raws[i])!);
        }

        if (_isDebug)
        {
            exts.Add(ExtDebugUtils.ExtensionName);
        }

        return exts.ToArray();
    }

    private unsafe bool UtilGetValidationSupport()
    {
        uint count;
        _vk.EnumerateInstanceLayerProperties(&count, null);

        LayerProperties[] layers = new LayerProperties[count];

        fixed (LayerProperties* lptr = layers)
        {
            _vk.EnumerateInstanceLayerProperties(&count, lptr);
        }

        foreach (string r in _validation)
        {
            bool found = false;

            foreach (LayerProperties l in layers)
            {
                string name = Marshal.PtrToStringAnsi((nint)l.LayerName)!;

                if (name == r)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private unsafe bool UtilGetQueueFamilies(PhysicalDevice device, out uint graphicsFamily, out uint presentFamily)
    {
        graphicsFamily = 0;
        presentFamily = 0;
        bool foundGraphics = false;
        bool foundPresent = false;

        uint count;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &count, null);
        QueueFamilyProperties[] families = new QueueFamilyProperties[count];

        fixed (QueueFamilyProperties* fptr = families)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &count, fptr);
        }

        for (uint i = 0; i < count; i++)
        {
            if (families[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                graphicsFamily = i;
                foundGraphics = true;
            }

            _surfaceExt.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out Bool32 presentSupport);

            if (presentSupport)
            {
                presentFamily = i;
                foundPresent = true;
            }

            if (foundGraphics && foundPresent)
            {
                break;
            }
        }

        return foundGraphics && foundPresent;
    }

    private unsafe bool UtilGetDeviceSupport(PhysicalDevice device)
    {
        uint count;
        _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, null);
        ExtensionProperties[] properties = new ExtensionProperties[count];

        fixed (ExtensionProperties* pptr = properties)
        {
            _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, pptr);
        }

        foreach (ExtensionProperties p in properties)
        {
            string name = Marshal.PtrToStringAnsi((nint)p.ExtensionName)!;

            if (name == KhrSwapchain.ExtensionName)
            {
                return true;
            }
        }

        return false;
    }

    private unsafe void UtilDestroySwapChain()
    {
        for (int i = 0; i < _swapImageViews.Length; i++)
        {
            _vk.DestroyImageView(_logiDevice, _swapImageViews[i], null);
        }

        if (_swapChain.Handle != default)
        {
            _swapChainExt.DestroySwapchain(_logiDevice, _swapChain, null);
        }
    }

    private void UtilRecreateSwapChain()
    {
        _vk.DeviceWaitIdle(_logiDevice);
        UtilDestroySwapChain();
        InitSwapChain();
        InitImageViews();
    }

    private unsafe void UtilTransitionImageLayout(CommandBuffer cmdBuffer, Image image, ImageLayout oldLayout, ImageLayout newLayout)
    {
        ImageMemoryBarrier barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
        };

        PipelineStageFlags srcStage, dstStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.None;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.PresentSrcKhr)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.None;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.BottomOfPipeBit;
        }
        else
        {
            throw new NotImplementedException($"Unhandled Vulkan layout transition! ({oldLayout} -> {newLayout})");
        }

        _vk.CmdPipelineBarrier(cmdBuffer, srcStage, dstStage, DependencyFlags.None, 0, null, 0, null, 1, in barrier);
    }

#endregion

    // RIP the funny comment elgen put here in the old D3D12 renderer, in partial reference to how bad D3D setup is in C#.
    // Although Vulkan's init really isn't all that much more legible... at least we're not doing a bunch of weird COM stuff this time

    #region VK Init

    private unsafe void InitInstance()
    {
        ApplicationInfo appInfo = new ApplicationInfo()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(Path.GetFileNameWithoutExtension(Environment.ProcessPath)),
            ApplicationVersion = Vk.MakeVersion(67, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Chisel"),
            EngineVersion = Vk.MakeVersion(67, 0, 0),
            ApiVersion = Vk.Version13
        };

        string[] exts = UtilGetRequiredExtensions();
        byte** eptr = (byte**)SilkMarshal.StringArrayToPtr(exts);

        InstanceCreateInfo instInfo = new InstanceCreateInfo()
        {
            SType = StructureType.InstanceCreateInfo,
            Flags = InstanceCreateFlags.None,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)exts.Length,
            PpEnabledExtensionNames = eptr,
            EnabledLayerCount = 0,
            PpEnabledLayerNames = null,
        };

        byte** lptr = null;

        if (_isDebug && UtilGetValidationSupport())
        {
            lptr = (byte**)SilkMarshal.StringArrayToPtr(_validation);
            instInfo.EnabledLayerCount = (uint)_validation.Length;
            instInfo.PpEnabledLayerNames = lptr;
        }
        else if (_isDebug)
        {
            Logger.AppendWarn("VK debug was requested but 'VK_LAYER_KHRONOS_validation' is not available!");
        }

        Result result = _vk.CreateInstance(&instInfo, null, out _instance);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create VK instance! VkResult: {result}");
        }

        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
        SilkMarshal.Free((nint)eptr);

        if (lptr != null)
        {
            SilkMarshal.Free((nint)lptr);
        }
    }

    private unsafe void InitDebugMessenger()
    {
        if (!_isDebug)
        {
            return;
        }

        if (!_vk.TryGetInstanceExtension(_instance, out _dbgUtilities))
        {
            Logger.AppendWarn("VK debug utilities was requested but 'VK_EXT_debug_utils' is not available!");
        }

        _dbgCallback = UtilOnDebugMessage;

        DebugUtilsMessengerCreateInfoEXT dbgInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(_dbgCallback),
        };

        Result result = _dbgUtilities!.CreateDebugUtilsMessenger(_instance, &dbgInfo, null, out _dbgMessenger);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create VK debug messenger! VkResult: {result}");
        }
    }

    private unsafe void InitSurface()
    {
        if (!_vk.TryGetInstanceExtension(_instance, out _surfaceExt))
        {
            throw new InvalidOperationException("VK extension 'VK_KHR_surface' is not available!");
        }

        Hexa.NET.SDL3.VkSurfaceKHR surface;
        bool result = SDL.VulkanCreateSurface(Game.Instance!.Window.Handle, _instance.Handle, null, &surface);

        if (!result)
        {
            throw new InvalidOperationException($"Failed to create VK surface via SDL! VkResult: {result}");
        }

        _surface = new SurfaceKHR((ulong)surface.Handle);
    }

    private unsafe void InitPhysicalDevice()
    {
        uint count;
        _vk.EnumeratePhysicalDevices(_instance, &count, null);

        if (count == 0)
        {
            throw new InvalidOperationException("Failed to find any VK-capable physical devices!");
        }

        PhysicalDevice[] devices = new PhysicalDevice[count];

        fixed (PhysicalDevice* dptr = devices)
        {
            _vk.EnumeratePhysicalDevices(_instance, &count, dptr);
        }

        int bestScore = -1;
        uint bestGraphicsFamily = 0;
        uint bestPresentFamily = 0;
        PhysicalDevice best = default;

        foreach (PhysicalDevice d in devices)
        {
            if (!UtilGetQueueFamilies(d, out uint graphicsFamily, out uint presentFamily))
            {
                continue;
            }

            if (!UtilGetDeviceSupport(d))
            {
                continue;
            }

            uint formatCount, presentCount;

            _surfaceExt.GetPhysicalDeviceSurfaceFormats(d, _surface, &formatCount, null);
            _surfaceExt.GetPhysicalDeviceSurfacePresentModes(d, _surface, &presentCount, null);

            if (formatCount == 0 || presentCount == 0)
            {
                continue;
            }

            _vk.GetPhysicalDeviceProperties(d, out PhysicalDeviceProperties properties);

            int score = properties.DeviceType == PhysicalDeviceType.DiscreteGpu ? 1000 : 1;

            if (score > bestScore)
            {
                bestScore = score;
                best = d;
                bestGraphicsFamily = graphicsFamily;
                bestPresentFamily = presentFamily;
            }
        }

        if (bestScore < 0)
        {
            throw new InvalidOperationException("Failed to find any VK-capable physical devices!");
        }

        _physDevice = best;
        _graphicsQueueFamily = bestGraphicsFamily;
        _presentQueueFamily = bestPresentFamily;
    }

    private unsafe void InitLogicalDevice()
    {
        int queue = 0;
        float priority = 1.0f;
        HashSet<uint> families = new HashSet<uint> { _graphicsQueueFamily, _presentQueueFamily };
        DeviceQueueCreateInfo[] queueInfos = new DeviceQueueCreateInfo[families.Count];

        foreach (uint f in families)
        {
            queueInfos[queue++] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = f,
                QueueCount = 1,
                PQueuePriorities = &priority,
            };
        }

        PhysicalDeviceFeatures features = new PhysicalDeviceFeatures();
        string[] exts = { KhrSwapchain.ExtensionName };
        byte** eptr = (byte**)SilkMarshal.StringArrayToPtr(exts);

        fixed (DeviceQueueCreateInfo* qptr = queueInfos)
        {
            DeviceCreateInfo deviceInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                Flags = 0,
                QueueCreateInfoCount = (uint)queueInfos.Length,
                PQueueCreateInfos = qptr,
                PEnabledFeatures = &features,
                EnabledExtensionCount = (uint)exts.Length,
                PpEnabledExtensionNames = eptr,
            };

            Result result = _vk.CreateDevice(_physDevice, &deviceInfo, null, out _logiDevice);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create VK logical device: VkResult: {result}");
            }

            SilkMarshal.Free((nint)eptr);
        }

        _vk.GetDeviceQueue(_logiDevice, _graphicsQueueFamily, 0, out _graphicsQueue);
        _vk.GetDeviceQueue(_logiDevice, _presentQueueFamily, 0, out _presentQueue);

        if (!_vk.TryGetDeviceExtension(_instance, _logiDevice, out _swapChainExt))
        {
            throw new InvalidOperationException("VK extension 'VK_KHR_swapchain' is not available!");
        }
    }

    private unsafe void InitSwapChain()
    {
        _surfaceExt.GetPhysicalDeviceSurfaceCapabilities(_physDevice, _surface, out SurfaceCapabilitiesKHR capabilities);

        uint formatCount;
        _surfaceExt.GetPhysicalDeviceSurfaceFormats(_physDevice, _surface, &formatCount, null);
        SurfaceFormatKHR[] formats = new SurfaceFormatKHR[formatCount];

        fixed (SurfaceFormatKHR* fptr = formats)
        {
            _surfaceExt.GetPhysicalDeviceSurfaceFormats(_physDevice, _surface, &formatCount, fptr);
        }

        uint presentCount;
        _surfaceExt.GetPhysicalDeviceSurfacePresentModes(_physDevice, _surface, &presentCount, null);
        PresentModeKHR[] presentModes = new PresentModeKHR[presentCount];

        fixed (PresentModeKHR* pptr = presentModes)
        {
            _surfaceExt.GetPhysicalDeviceSurfacePresentModes(_physDevice, _surface, &presentCount, pptr);
        }

        SurfaceFormatKHR format = formats.FirstOrDefault(
            f => f.Format == Silk.NET.Vulkan.Format.R8G8B8A8Unorm && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr,
            formats[0]);

        bool vsync = Game.Instance!.Window.IsVsyncOn;
        PresentModeKHR present = (vsync)
            ? PresentModeKHR.FifoKhr // Should match SDL.GLSetSwapInterval(1)... I think
            : (presentModes.Contains(PresentModeKHR.MailboxKhr) ? PresentModeKHR.MailboxKhr : PresentModeKHR.ImmediateKhr);


        Extent2D extent;

        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            extent = capabilities.CurrentExtent;
        }
        else
        {
            extent = new Extent2D((uint)Game.Instance!.Window.Resolution.W, (uint)Game.Instance!.Window.Resolution.H);
        }

        uint imageCount = BufferCountInternal.Max(capabilities.MinImageCount);

        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
        {
            imageCount = capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR swapInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Flags = SwapchainCreateFlagsKHR.None,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = format.Format,
            ImageColorSpace = format.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.ColorAttachmentBit,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = present,
            Clipped = true,
            OldSwapchain = default,
        };

        uint[] queueFamilies = { _graphicsQueueFamily, _presentQueueFamily };

        if (_graphicsQueueFamily != _presentQueueFamily)
        {
            fixed (uint* qptr = queueFamilies)
            {
                swapInfo.ImageSharingMode = SharingMode.Concurrent;
                swapInfo.QueueFamilyIndexCount = 2;
                swapInfo.PQueueFamilyIndices = qptr;
            }
        }
        else
        {
            swapInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        Result result = _swapChainExt.CreateSwapchain(_logiDevice, &swapInfo, null, out _swapChain);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create VK swapchain: VkResult: {result}");
        }

        _swapFormat = format.Format;
        _swapExtent = extent;

        uint swapCount = 0;
        _swapChainExt.GetSwapchainImages(_logiDevice, _swapChain, &swapCount, null);
        _swapImages = new Image[swapCount];

        fixed (Image* iptr = _swapImages)
        {
            _swapChainExt.GetSwapchainImages(_logiDevice, _swapChain, &swapCount, iptr);
        }

        // Keeping our buffer count the same to what the driver actually gave us
        BufferCountInternal = swapCount;
    }

    private unsafe void InitImageViews()
    {
        _swapImageViews = new ImageView[_swapImages.Length];

        for (int i = 0; i < _swapImages.Length; i++)
        {
            ImageViewCreateInfo viewInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Flags = ImageViewCreateFlags.None,
                Image = _swapImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapFormat,
                Components = new ComponentMapping(ComponentSwizzle.Identity, ComponentSwizzle.Identity, ComponentSwizzle.Identity, ComponentSwizzle.Identity),
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            };

            Result result = _vk.CreateImageView(_logiDevice, &viewInfo, null, out _swapImageViews[i]);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create VK image view: VkResult: {result}");
            }
        }
    }

    private unsafe void InitCommandPool()
    {
        CommandPoolCreateInfo poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = _graphicsQueueFamily,
        };

        Result result = _vk.CreateCommandPool(_logiDevice, &poolInfo, null, out _cmdPool);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create VK command pool: VkResult: {result}");
        }
    }

    private unsafe void InitCommandBuffer()
    {
        _cmdBuffers = new CommandBuffer[BufferCountInternal];

        CommandBufferAllocateInfo allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _cmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = BufferCountInternal,
        };

        fixed (CommandBuffer* bptr = _cmdBuffers)
        {
            Result result = _vk.AllocateCommandBuffers(_logiDevice, &allocInfo, bptr);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create VK command buffer: VkResult: {result}");
            }
        }
    }

    private unsafe void InitSemaphores()
    {
        _imageAvailableSemas = new Semaphore[BufferCountInternal];
        _renderFinishedSemas = new Semaphore[_swapImages.Length];

        SemaphoreCreateInfo semaInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
            Flags = SemaphoreCreateFlags.None
        };

        for (int i = 0; i < BufferCountInternal; i++)
        {
            Result result = _vk.CreateSemaphore(_logiDevice, &semaInfo, null, out _imageAvailableSemas[i]);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create VK pending semaphore: VkResult: {result}");
            }
        }
        for (int i = 0; i < _swapImages.Length; i++)
        {
            Result result = _vk.CreateSemaphore(_logiDevice, &semaInfo, null, out _renderFinishedSemas[i]);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create VK finished semaphore: VkResult: {result}");
            }
        }
    }

    private unsafe void InitFences()
    {
        _inFlightFences = new Fence[BufferCountInternal];

        FenceCreateInfo fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (int i = 0; i < BufferCountInternal; i++)
        {
            Result result = _vk.CreateFence(_logiDevice, &fenceInfo, null, out _inFlightFences[i]);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create VK fence: VkResult: {result}");
            }
        }
    }

    private unsafe void QueryGpuInfo()
    {
        _vk.GetPhysicalDeviceProperties(_physDevice, out PhysicalDeviceProperties properties);

        string gpu = Marshal.PtrToStringAnsi((nint)properties.DeviceName)!;
        uint driver = properties.DriverVersion;

        UtilLogMessage($"Successfully initialized Vulkan!");

        // These specifically pull whatever the vendor set as their vulkan driver identifier.
        // Nvidia usually just uses the actual GeForce driver version, but AMD is kinda weird
        // and has the Adrenalin driver version and the actual version of their Vulkan driver seperated.
        // Example: Adrenalin 26.8.1 may have a Vulkan version of 2.0.395
        if (properties.VendorID == 0x10DE && OperatingSystem.IsWindows()) // Nvidia
        {
            uint major = (driver >> 22) & 0x3FF;
            uint minor = (driver >> 14) & 0x0FF;
            uint subMinor = (driver >> 6) & 0x0FF;
            uint patch = driver & 0x03F;
            UtilLogMessage($"   > GPU: {gpu} (driver: {major}.{minor}.{subMinor}.{patch})");
        }
        else if (properties.VendorID == 0x1002 && OperatingSystem.IsWindows()) // AMD
        {
            uint major = (driver >> 22) & 0x3FF;
            uint minor = 0; // They always set this to 0 for whatever reason
            uint build = driver & 0x3FFFFF;
            UtilLogMessage($"   > GPU: {gpu} (driver: {major}.{minor}.{build})");

        }
        else // Mesa (and Intel apparently?) use standard versioning instead
        {
            uint major = (driver >> 22) & 0x7F;
            uint minor = (driver >> 12) & 0x3FF;
            uint patch = driver & 0xFFF;
            UtilLogMessage($"   > GPU: {gpu} (driver: {major}.{minor}.{patch})");
        }

        // Getting the VRAM stats

        PhysicalDeviceMemoryBudgetPropertiesEXT budgetInfo = new PhysicalDeviceMemoryBudgetPropertiesEXT()
        {
            SType = StructureType.PhysicalDeviceMemoryBudgetPropertiesExt
        };

        PhysicalDeviceMemoryProperties2 memoryInfo = new PhysicalDeviceMemoryProperties2()
        {
            SType = StructureType.PhysicalDeviceMemoryProperties2,
            PNext = &budgetInfo
        };

        _vk.GetPhysicalDeviceMemoryProperties2(_physDevice, &memoryInfo);

        ulong totalVram = 0;
        ulong usableVram = 0;

        for (uint i = 0; i < memoryInfo.MemoryProperties.MemoryHeapCount; ++i)
        {
            MemoryHeapFlags heapFlags = memoryInfo.MemoryProperties.MemoryHeaps[(int)i].Flags;

            if ((heapFlags & MemoryHeapFlags.DeviceLocalBit) != 0)
            {
                totalVram += memoryInfo.MemoryProperties.MemoryHeaps[(int)i].Size;
                usableVram += budgetInfo.HeapBudget[(int)i];
            }
        }

        UtilLogMessage($"   > VRAM: {(totalVram * 1e-6):0.00} MB ({(usableVram * 1e-6):0.00} MB available)");

        // Getting the Vulkan version/feature level

        uint apiMajor = (_vkVersion >> 22);
        uint apiMinor = (_vkVersion >> 12) & 0x3FF;
        uint apiMajorMax = (properties.ApiVersion >> 22);
        uint apiMinorMax = (properties.ApiVersion >> 12) & 0x3FF;
        UtilLogMessage($"   > FL: {apiMajor}.{apiMinor} (Vulkan {apiMajorMax}.{apiMinorMax} available)");
    }

#endregion
}
