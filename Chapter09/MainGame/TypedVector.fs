namespace PracticalFSharp

open Microsoft.Xna.Framework

/// A two-dimensional vector with a unit of measure. Built on top of Xna's Vector2.
type TypedVector2<[<Measure>] 'M> =
    struct
        val v : Vector2
        new(x : float32<'M>, y : float32<'M>) =
            { v = Vector2(float32 x, float32 y) }
        new(V) = { v = V }

        member this.X : float32<'M> = LanguagePrimitives.Float32WithMeasure this.v.X
        member this.Y : float32<'M> = LanguagePrimitives.Float32WithMeasure this.v.Y
    end

[<RequireQualifiedAccessAttribute>]
module TypedVector =
    let add2 (U : TypedVector2<'M>, V : TypedVector2<'M>) =
        TypedVector2(U.v + V.v)

    let sub2 (U : TypedVector2<'M>, V : TypedVector2<'M>) =
        TypedVector2(U.v - V.v)

    let dot2 (U : TypedVector2<'M>, V : TypedVector2<'N>) =
        Vector2.Dot(U.v, V.v)
        |> LanguagePrimitives.Float32WithMeasure<'M 'N>

    let len2 (U : TypedVector2<'M>) =
        LanguagePrimitives.Float32WithMeasure<'M> (U.v.Length())

    let scale2 (k : float32<'K>, U : TypedVector2<'M>) : TypedVector2<'K 'M> =
        let conv = LanguagePrimitives.Float32WithMeasure<'K 'M>
        let v = Vector2.Multiply(U.v, float32 k)
        TypedVector2(v)

    let normalize2 (U : TypedVector2<'M>) : TypedVector2<1> =
        let normalized = Vector2.Normalize(U.v)
        TypedVector2(normalized)

    let tryNormalize2 (U : TypedVector2<'M>) =
        let len = len2 U
        if len > LanguagePrimitives.Float32WithMeasure<'M>(1e-3f) then
            Some <| scale2 ((1.0f/ len), U)
        else
            None


type TypedVector2<[<Measure>] 'M>
with
    static member public (*) (k, U) = TypedVector.scale2 (k, U)
    static member public (+) (U, V) = TypedVector.add2 (U, V)
    static member public (-) (U, V) = TypedVector.sub2 (U, V)
    member public this.Length = this |> TypedVector.len2
    static member public Zero = TypedVector2<'M>()