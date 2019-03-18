namespace libbmp

type d1d = double array
type d2d = d1d array

type picture(r, g, b) =
    member this.r = r
    member this.g = g
    member this.b = b

module imageprocessor =
    open System.Drawing.Imaging
    open System.Drawing
    open System.Collections.Generic
    open System.Threading.Tasks
    open System.Linq

    let inline private forloop<'t> (init: 't) (until: 't -> bool) (step: 't -> 't) (func : 't -> _) =
        let mutable i = init
        while until i do
            func i
            i <- step i
            done

    let private count<'t> (a: IList<'t>) = a.Count |> int32

    let inline private arrayloop<'t> (array: IList<'t>) (func : int32 -> _) =
        forloop
            0
            (fun i -> i < count array)
            (fun i -> i + 1)
            func
            
    let inline private array2dloop<'t> (array: IList<'t array>) (func: int32 -> int32 -> _) =
        arrayloop array (fun a -> arrayloop array.[a] (fun b -> func a b))

    let private azaza (array: d2d) size =
        let list = array :> IList<d1d>
        
        arrayloop list (fun i -> list.Item i <- Array.zeroCreate size)
        
    let private wtf = Task.Delay 6000 |> Task.WaitAll
    
    let private extractchannel (bmp: Bitmap) (selector: Color -> byte) =
        let a = Array.zeroCreate<d1d> bmp.Height
        azaza a bmp.Width
        
        array2dloop
            a
            (fun y x ->
                let pixel = bmp.GetPixel(x, y) |> selector
                Array.set a.[y] x (double(pixel))
            )
        a

    let private bmpToPicture (bmp : Bitmap) =
        let r = extractchannel bmp (fun c -> c.R)
        let g = extractchannel bmp (fun c -> c.G)
        let b = extractchannel bmp (fun c -> c.B)
        picture (r, g, b)
        
    let load file =
        Image.FromFile file :?> Bitmap |> bmpToPicture
        
    let grayscale (pic: picture) =
        let r, g, b = pic.r, pic.g, pic.b
        let a = Array.zeroCreate<d1d> (count r)
        azaza a (count r.[0])
        wtf
        
        for color in [r; g; b] do
            array2dloop
                a
                (fun y x ->
                    let value = a.[y].[x] + (color.[y].[x] / 3.0)
                    Array.set a.[y] x value
                )
                
        picture(a, a, a)
        
    let private toBmp (pic: picture) : Bitmap =
        let r, g, b = pic.r, pic.g, pic.b
        let bmp = new Bitmap(count r.[0], count r)
        array2dloop
            r
            (fun y x ->
                let color = Color.FromArgb(0, int32(r.[y].[x]), int32(g.[y].[x]), int32(b.[y].[x]))
                bmp.SetPixel(x, y, color)
            )
        bmp
        
    let save (pic: picture, name: string) =
        let bmp = pic |> toBmp
        bmp.Save(name, ImageFormat.Bmp)