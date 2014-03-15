open System
open System.Drawing
open System.Windows.Forms

open SharpDX
open SharpDX.DXGI
open SharpDX.Direct3D11
open SharpDX.Direct3D
open SharpDX.Windows

open HelloWorld

let main() =
    use form = 
        new Form(
            Text            = "Hello World",
            ClientSize      = new Size(800, 600),
            FormBorderStyle = FormBorderStyle.Sizable
        )

    let videoCardMemory, videoCardDescription =
        use factory = new Factory()
        let adapterDescription = factory.GetAdapter(0).Description
        adapterDescription.DedicatedVideoMemory, adapterDescription.Description

    use swapChain = new SwapChain(form)
    let device = swapChain.Device
    let context = swapChain.Context

    // Create the rasterizer state from the description we just filled out.
    use rasterizerState = 
        new RasterizerState(
            device, 
            RasterizerStateDescription(
                IsAntialiasedLineEnabled = Bool(true),
                CullMode                 = CullMode.None,
                DepthBias                = 0,
                DepthBiasClamp           = 0.0f,
                IsDepthClipEnabled       = Bool(false),
                FillMode                 = FillMode.Solid,
                IsFrontCounterClockwise  = Bool(false),
                IsMultisampleEnabled     = Bool(false),
                IsScissorEnabled         = Bool(false),
                SlopeScaledDepthBias     = 0.0f
            )
        )

    context.Rasterizer.State <- rasterizerState
    context.Rasterizer.SetViewport(0.0f, 0.0f, float32 form.Width, float32 form.Height, 0.0f, 1.0f)

    use triangles = new ShaderTriangleStrip(device, "shader.fx")

    use vertexBuffer = 
        use stream = new DataStream(48, true, true)
        let s, z = 0.5f, 10.5f
        stream.Write(Vector3(-s,  s, z));
        stream.Write(Vector3( s,  s, z));
        stream.Write(Vector3(-s, -s, z));
        stream.Write(Vector3( s, -s, z));

        triangles.VertexBuffer stream
    
    use constants = new ShaderConstants<Transforms>(device)

    let isFormClosed = ref false

    form.KeyDown.Add(fun key -> if key.KeyCode = Keys.Escape then isFormClosed := true)
    form.Closed.Add(fun _ -> isFormClosed := true)
    form.MouseEnter.Add(fun _ -> Cursor.Hide())
    form.MouseLeave.Add(fun _ -> Cursor.Show())
    form.Resize.Add(fun _ -> swapChain.Resize())
    form.Show()

    let projection = Matrix.PerspectiveFovLH(float32(Math.PI / 4.0), float32 form.Width / float32 form.Height, 0.1f, 1000.0f)
    let world      = Matrix.Identity
    let view       = Matrix.OrthoLH(float32 form.Width, float32 form.Height, 0.1f, 1000.0f)

    let transforms = {
        world      = world;
        view       = view;
        projection = projection;
        wvp        = world * view * projection;
        }

    RenderLoop.Run(form, fun () -> 
        if !isFormClosed then form.Close()

        context.ClearDepthStencilView(swapChain.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0uy)
        context.ClearRenderTargetView(swapChain.RenderTargetView, new Color4(0.10f, 0.0f, 0.0f, 1.0f))

        constants.Update(context, transforms)
        triangles.Draw(context, vertexBuffer)
    
        swapChain.Present(1, PresentFlags.None)
        )

main()