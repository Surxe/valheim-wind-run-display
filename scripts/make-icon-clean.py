"""Cut the Wind run HUD element (title, cloak icon, speed label) out of an in-game
screenshot onto a transparent 256x256 canvas, with a dark outline so it reads on
light and dark backgrounds.

Masks are found per element from colour, not hardcoded positions: the white title
and orange label by their text rows, the cloak as the largest olive/bone blob between
them (holes filled so its dark markings stay). Tuned on docs/icon-source.png.

Needs Pillow, numpy and scipy, e.g. in a venv (.venv/ is gitignored):
    python3 -m venv .venv && .venv/bin/pip install pillow numpy scipy
    .venv/bin/python scripts/make-icon-clean.py docs/icon-source.png icon-clean.png

usage: make-icon-clean.py <source.png> <out.png> [debug_dir]
  debug_dir: also writes the per-element masks there (4x, nearest) for tuning.
"""
import sys
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SIZE = 256
MARGIN = 10          # px of transparent border in the output
OUTLINE_PX = 4       # outline thickness at output scale


def ramp(x, lo, hi):
    """0 at lo, 1 at hi (works for lo > hi as a falling ramp)."""
    return np.clip((x - lo) / float(hi - lo), 0.0, 1.0)


def drop_specks(mask, min_px=5):
    """Zero out isolated blobs smaller than min_px (stray bright background pixels);
    glyphs are larger or touch neighbours through their anti-aliased edges."""
    lab, n = ndimage.label(mask > 0.02)
    if not n:
        return mask
    idx = range(1, n + 1)
    sizes = ndimage.sum(np.ones_like(mask), lab, idx)
    peaks = ndimage.maximum(mask, lab, idx)
    # Faint blobs are background leaking through the ramps; real glyph parts (even
    # small ones like the dot of an i) reach full opacity somewhere.
    drop = np.isin(lab, 1 + np.where((sizes < min_px) | (peaks < 0.5))[0])
    return np.where(drop, 0.0, mask)


def masks(src):
    rgb = np.asarray(src.convert("RGB")).astype(np.float32)
    hsv = np.asarray(src.convert("RGB").convert("HSV")).astype(np.float32)
    h, s, v = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    H, W = h.shape
    rows = np.arange(H)[:, None] * np.ones((1, W))

    # Find the bands from the data instead of hardcoding rows.
    white = (s < 40) & (v > 200)
    orange = (h >= 10) & (h <= 32) & (s > 110) & (v > 150)
    wrows = np.where(white.sum(1) >= 5)[0]
    orows = np.where(orange.sum(1) >= 5)[0]
    t0, t1 = wrows.min() - 2, wrows.max() + 2
    l0, l1 = orows.min() - 2, orows.max() + 2

    # Title: bright + unsaturated (the pinkish backdrop is S 40-90, text S ~0).
    title = ramp(v, 130, 210) * ramp(s, 55, 25)
    title *= (rows >= t0) & (rows <= t1)

    # Label: saturated orange.
    hue_ok = ramp(h, 6, 12) * ramp(h, 38, 32)
    label = hue_ok * ramp(s, 80, 120) * ramp(v, 100, 150)
    label *= (rows >= l0) & (rows <= l1)

    title = drop_specks(title)
    label = drop_specks(label)

    # Cloak: olive/bone hues between the two text bands; keep the largest blob
    # and fill its holes so dark scale markings stay opaque.
    band = (rows > t1) & (rows < l0)
    cloak_bin = (h >= 22) & (h <= 80) & (v > 40) & band
    lab, n = ndimage.label(cloak_bin)
    if n:
        sizes = ndimage.sum(cloak_bin, lab, range(1, n + 1))
        cloak_bin = lab == (1 + int(np.argmax(sizes)))
    cloak_bin = ndimage.binary_closing(cloak_bin, iterations=1)
    cloak_bin = ndimage.binary_fill_holes(cloak_bin)
    cloak = ndimage.gaussian_filter(cloak_bin.astype(np.float32), 0.5)
    cloak = np.where(cloak_bin, np.maximum(cloak, 0.85), cloak)

    alpha = np.clip(np.maximum.reduce([title, label, cloak]), 0, 1)
    return rgb, alpha, {"title": title, "label": label, "cloak": cloak}


def compose(rgb, alpha):
    ys, xs = np.where(alpha > 0.05)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgba = np.dstack([rgb, alpha * 255]).astype(np.uint8)[y0:y1, x0:x1]
    cut = Image.fromarray(rgba, "RGBA")

    inner = SIZE - 2 * (MARGIN + OUTLINE_PX)
    scale = inner / max(cut.width, cut.height)
    size = (max(1, round(cut.width * scale)), max(1, round(cut.height * scale)))
    # Resize premultiplied so transparent pixels don't bleed dark fringes.
    big = cut.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")

    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ox, oy = (SIZE - big.width) // 2, (SIZE - big.height) // 2
    canvas.paste(big, (ox, oy))

    # Dark outline: dilate the alpha, soften slightly, put black under the art.
    a = canvas.getchannel("A")
    halo = a.filter(ImageFilter.MaxFilter(2 * OUTLINE_PX + 1)).filter(ImageFilter.GaussianBlur(1.2))
    halo = halo.point(lambda p: int(p * 0.9))
    out = Image.new("RGBA", (SIZE, SIZE), (12, 10, 8, 0))
    out.putalpha(halo)
    out.alpha_composite(canvas)
    return out


def main():
    src = Image.open(sys.argv[1])
    rgb, alpha, parts = masks(src)
    compose(rgb, alpha).save(sys.argv[2])
    if len(sys.argv) > 3:
        for name, m in parts.items():
            Image.fromarray((m * 255).astype(np.uint8)).resize(
                (src.width * 4, src.height * 4), Image.NEAREST).save(f"{sys.argv[3]}/mask-{name}.png")


if __name__ == "__main__":
    main()
