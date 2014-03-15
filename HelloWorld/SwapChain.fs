namespace HelloWorld

open System
open System.Drawing
open System.Windows.Forms

open SharpDX
open SharpDX.DXGI
open SharpDX.Direct3D11
open SharpDX.Direct3D
open SharpDX.Windows

type SwapChain(form : Form) =
    let swapChainDesc = 
        SwapChainDescription(
            BufferCount       = 1,
            ModeDescription   = ModeDescription(form.Width, form.Height, Rational(60, 1), Format.R8G8B8A8_UNorm),
            Usage             = Usage.RenderTargetOutput,
            OutputHandle      = form.Handle,
            SampleDescription = SampleDescription(1, 0),
            IsWindowed        = Bool(true),
            Flags             = SwapChainFlags.None,
            SwapEffect        = SwapEffect.Discard
        )

    let device, swapChain = Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, [|FeatureLevel.Level_11_0|], swapChainDesc)
    let context = device.ImmediateContext

    // Initialize and set up the depth stencil view.
    let depthStencilViewDesc = 
        DepthStencilViewDescription(
            Format    = Format.D24_UNorm_S8_UInt,
            Dimension = DepthStencilViewDimension.Texture2D,
            Texture2D = DepthStencilViewDescription.Texture2DResource(MipSlice = 0)
        )

    // Create the depth stencil state.
    let depthStencilState = 
        // Initialize and set up the description of the stencil state.
        new DepthStencilState(
            device, 
            DepthStencilStateDescription(
                IsDepthEnabled   = Bool(false),
                DepthWriteMask   = DepthWriteMask.All,
                DepthComparison  = Comparison.Less,
                IsStencilEnabled = Bool(true),
                StencilReadMask  = 0xFFuy,
                StencilWriteMask = 0xFFuy,
                FrontFace = 
                    DepthStencilOperationDescription(
                        FailOperation      = StencilOperation.Keep,
                        DepthFailOperation = StencilOperation.Increment,
                        PassOperation      = StencilOperation.Keep,
                        Comparison         = Comparison.Always
                    ),
                BackFace = 
                    DepthStencilOperationDescription(
                        FailOperation      = StencilOperation.Keep,
                        DepthFailOperation = StencilOperation.Decrement,
                        PassOperation      = StencilOperation.Keep,
                        Comparison         = Comparison.Always
                    )
                )
            )

    let mutable renderTargetView = null
    let mutable depthStencilView = null

    let setup() = 
        use backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0)
        renderTargetView <- new RenderTargetView(device, backBuffer)

        // Create the texture for the depth buffer using the filled out description.
        use depthStencilBuffer = 
            new Texture2D(
                device, 
                Texture2DDescription(
                    Width             = form.Width,
                    Height            = form.Height,
                    MipLevels         = 1,
                    ArraySize         = 1,
                    Format            = Format.D24_UNorm_S8_UInt,
                    SampleDescription = SampleDescription(1, 0),
                    Usage             = ResourceUsage.Default,
                    BindFlags         = BindFlags.DepthStencil,
                    CpuAccessFlags    = CpuAccessFlags.None,
                    OptionFlags       = ResourceOptionFlags.None
                    )
                )

        // Create the depth stencil view.
        depthStencilView <- new DepthStencilView(device, depthStencilBuffer, depthStencilViewDesc)
        // Set the depth stencil state.
        context.OutputMerger.SetDepthStencilState(depthStencilState, 0)
        // Bind the render target view and depth stencil buffer to the output render pipeline.
        context.OutputMerger.SetTargets(depthStencilView, renderTargetView)

    do setup()

    member this.RenderTargetView with get() = renderTargetView
    member this.DepthStencilView with get() = depthStencilView

    member this.Context = context
    member this.Device  = device
    member this.Present = swapChain.Present

    member this.Resize() =
        this.Context.OutputMerger.SetTargets([||])
        renderTargetView.Dispose()
        depthStencilView.Dispose()
        swapChain.ResizeBuffers(1, form.Width, form.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None)
        setup()

    interface IDisposable with
        member this.Dispose() =
            renderTargetView.Dispose()
            depthStencilView.Dispose()
            depthStencilState.Dispose()
