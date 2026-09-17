using System;

namespace Chisel.Framework;

// Two independent filtering axes, matching how GL and D3D actually implement this: how texels
// within a single mip level blend (Nearest vs Bilinear), and separately, how sampling behaves
// once a texture has more than one mip level (no mip flag = mips ignored entirely, sample level 0
// only, regardless of how many levels exist; MipmapNearest = jump to the single nearest level;
// MipmapBilinear = blend linearly between the two nearest levels
//
// Bilinear | MipmapBilinear == old "Trilinear" (removed as its own value - it's just this
// combination now). Bilinear alone == old "Linear"/"Bilinear" (also removed as separate values;
// they were synonyms) - note this now means mip 0 only, even on a texture with more levels. If
// you want bilinear filtering that still looks past mip 0, use Bilinear | MipmapBilinear or
// Bilinear | MipmapNearest.
[Flags]
public enum SamplerFilterMode
{
    Nearest = 0,

    Bilinear = 1 << 0,

    // Mip selection. Meaningless (and ignored) on a texture with only 1 mip level. Leave both
    // unset to deliberately clamp sampling to mip 0 even on a texture that has more.
    MipmapNearest = 1 << 1,
    MipmapBilinear = 1 << 2,

    // Collapses to Bilinear|MipmapBilinear (real trilinear) on both backends today - true
    // anisotropic filtering isn't wired up yet (GL_TEXTURE_MAX_ANISOTROPY / D3D12 MaxAnisotropy
    // both need real work - see status-and-known-gaps.md). Kept as distinct values so existing
    // call sites don't need to change, and so wiring up real anisotropy later is a
    // translation-function-only change, not another API break.
    Anisotropic4x = 1 << 3,
    Anisotropic8x = 1 << 4,
    Anisotropic16x = 1 << 5,
}