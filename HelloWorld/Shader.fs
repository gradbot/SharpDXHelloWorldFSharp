namespace HelloWorld

open System
open SharpDX
open SharpDX.D3DCompiler
open SharpDX.Direct3D
open SharpDX.Direct3D11
open SharpDX.DXGI
open System.Runtime.InteropServices

type Buffer   = SharpDX.Direct3D11.Buffer
type Device   = SharpDX.Direct3D11.Device
type MapFlags = SharpDX.Direct3D11.MapFlags

#nowarn "9" 
[<StructLayout(LayoutKind.Sequential)>]
type Transforms = 
    {
        world      : Matrix
        view       : Matrix
        projection : Matrix
        wvp        : Matrix
    }

type ShaderConstants<'a> (device : Device) =
    let constBuffer = 
        new Buffer(device, 
             BufferDescription(
                BindFlags      = BindFlags.ConstantBuffer,
                SizeInBytes    = ((sizeof<'a> + 15) / 16) * 16,
                CpuAccessFlags = CpuAccessFlags.Write,
                Usage          = ResourceUsage.Dynamic))

    member this.Update(context : DeviceContext, constants : 'a) =
        let _, stream = context.MapSubresource(constBuffer, MapMode.WriteDiscard, MapFlags.None)
        Marshal.StructureToPtr(constants, stream.DataPointer, false)
        context.UnmapSubresource(constBuffer, 0)

        context.VertexShader.SetConstantBuffers(0, 1, [|constBuffer|])
        context.PixelShader.SetConstantBuffers(0, 1, [|constBuffer|])

    interface IDisposable with
        member this.Dispose() =
            constBuffer.Dispose() 

type ShaderTriangleStrip(device, fileName) =
    let shaderFlags = ShaderFlags.Debug
    //let shaderFlags = ShaderFlags.None

    let vertexShader, inputSignature = 
        use bytecode = ShaderBytecode.CompileFromFile(fileName, "VShader", "vs_4_0", shaderFlags, EffectFlags.None)
        new VertexShader(device, bytecode.Bytecode.Data), ShaderSignature.GetInputSignature(bytecode.Bytecode.Data)

    let pixelShader = 
        use bytecode = ShaderBytecode.CompileFromFile(fileName, "PShader", "ps_4_0", shaderFlags, EffectFlags.None)
        new PixelShader(device, bytecode.Bytecode.Data)

    let inputLayout =
        let elements = [| InputElement("POSITION", 0, Format.R32G32B32_Float, 0 , 0) |]
        new InputLayout(device, inputSignature.Data, elements)

    member this.VertexBuffer(stream : DataStream) =
        stream.Position <- 0L

        new Buffer(device,  stream, 
            BufferDescription(
                BindFlags      = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags    = ResourceOptionFlags.None,
                SizeInBytes    = int stream.Length,
                Usage          = ResourceUsage.Default
            ))

    member this.Draw(context : DeviceContext, buffer : Buffer) =
        this.Bind(context)
        context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffer, 12, 0))
        context.Draw(buffer.Description.SizeInBytes / 12, 0)

    member this.Bind(context : DeviceContext) =
        context.InputAssembler.InputLayout <- inputLayout
        //context.InputAssembler.PrimitiveTopology <- PrimitiveTopology.LineStrip
        context.InputAssembler.PrimitiveTopology <- PrimitiveTopology.TriangleStrip
        context.VertexShader.Set vertexShader
        context.PixelShader.Set pixelShader

    member this.Release(context : DeviceContext) =
        context.VertexShader.Set null
        context.PixelShader.Set null
        context.PixelShader.SetSampler(0, null)
        context.PixelShader.SetShaderResource(0, null)
        context.PixelShader.SetShaderResource(1, null)

    interface IDisposable with
        member this.Dispose() =
            vertexShader.Dispose() 
            pixelShader.Dispose()
            inputLayout.Dispose()
