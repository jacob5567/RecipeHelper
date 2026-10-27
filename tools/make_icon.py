#!/usr/bin/env python3
"""Renders the RecipeHelper icon (256x256): the Wandering CHEF's crafting, with a hint of the recipe list.

Usage: make_icon.py [--option 1|2|3|4|4a|4b|4c|4d|4e] [out.png]
The option defaults to 4e, the one Jacob chose. With no output path it overwrites thunderstore/icon.png.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFilter

SIZE = 256
SS = 4  # supersampling factor
W = SIZE * SS

BACKGROUND = (8, 12, 22, 255)
ACCENT = (143, 163, 199)  # the sidebar's muted blue, #8fa3c7
ACCENT_LIGHT = (195, 210, 238)
TILE = (23, 27, 36)
WHITE_TIER = (233, 234, 238)
GREEN_TIER = (140, 224, 78)
RED_TIER = (240, 96, 74)
HAT = (242, 244, 250)
HAT_SHADE = (178, 188, 214)
PAN = (62, 70, 88)
PAN_LIGHT = (104, 114, 138)
CURSOR = (255, 255, 255)
CURSOR_EDGE = (8, 12, 22)

DEFAULT_OPTION = "4e"
DEFAULT_OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "thunderstore", "icon.png")


def s(v):
    return int(round(v * SS))


def blank_mask():
    return Image.new("L", (W, W), 0)


def solid(color, alpha):
    return Image.merge("RGBA", (*[Image.new("L", (W, W), c) for c in color], alpha))


def paint(art, mask, color, strength=1.0):
    alpha = mask if strength == 1.0 else mask.point(lambda v: int(v * strength))
    art.alpha_composite(solid(color, alpha))


def glow(art, mask, color, blur, strength=1.0):
    paint(art, mask.filter(ImageFilter.GaussianBlur(s(blur))), color, strength)


def grow(mask, width):
    return mask.filter(ImageFilter.MaxFilter(s(width) * 2 | 1))


def subtract(a, b):
    from PIL import ImageChops
    return ImageChops.subtract(a, b)


def union(*masks):
    from PIL import ImageChops
    out = masks[0]
    for m in masks[1:]:
        out = ImageChops.lighter(out, m)
    return out


def rounded(x0, y0, x1, y1, r):
    mask = blank_mask()
    ImageDraw.Draw(mask).rounded_rectangle([s(x0), s(y0), s(x1), s(y1)], radius=s(r), fill=255)
    return mask


def disc(cx, cy, r):
    mask = blank_mask()
    ImageDraw.Draw(mask).ellipse([s(cx - r), s(cy - r), s(cx + r), s(cy + r)], fill=255)
    return mask


def polygon(points):
    mask = blank_mask()
    ImageDraw.Draw(mask).polygon([(s(x), s(y)) for x, y in points], fill=255)
    return mask


def stroke(points, width):
    mask = blank_mask()
    draw = ImageDraw.Draw(mask)
    pts = [(s(x), s(y)) for x, y in points]
    draw.line(pts, fill=255, width=s(width), joint="curve")
    for x, y in pts[:1] + pts[-1:]:
        draw.ellipse([x - s(width) / 2, y - s(width) / 2, x + s(width) / 2, y + s(width) / 2], fill=255)
    return mask


def plus(cx, cy, size, width):
    h = size / 2
    return union(stroke([(cx - h, cy), (cx + h, cy)], width), stroke([(cx, cy - h), (cx, cy + h)], width))


def arrow(x0, y, x1, width, head):
    shaft = stroke([(x0, y), (x1 - head * 0.6, y)], width)
    tip = polygon([(x1, y), (x1 - head, y - head * 0.75), (x1 - head, y + head * 0.75)])
    return union(shaft, tip)


def tile(art, x, y, size, color, inner=None):
    """An item tile: a frame in its tier colour around a dark square, with a simple gem inside."""
    frame = rounded(x, y, x + size, y + size, size * 0.12)
    inset = size * 0.09
    body = rounded(x + inset, y + inset, x + size - inset, y + size - inset, size * 0.06)
    paint(art, frame, color)
    paint(art, body, TILE)
    cx, cy, r = x + size / 2, y + size / 2, size * 0.24
    gem = polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)])
    paint(art, gem, inner or color)


def toque(cx, top, width):
    """A chef's hat: a puffed crown over a band. Returns (crown mask, band mask)."""
    r = width * 0.26
    crown = union(
        disc(cx - width * 0.25, top + r * 1.25, r),
        disc(cx, top + r, r * 1.08),
        disc(cx + width * 0.25, top + r * 1.25, r),
        rounded(cx - width * 0.36, top + r * 1.2, cx + width * 0.36, top + width * 0.78, width * 0.04),
    )
    band = rounded(cx - width * 0.38, top + width * 0.74, cx + width * 0.38, top + width * 0.98, width * 0.05)
    return crown, band


def draw_toque(art, cx, top, width):
    crown, band = toque(cx, top, width)
    paint(art, crown, HAT)
    # Two pleats down the crown.
    for dx in (-0.12, 0.12):
        pleat = stroke([(cx + width * dx, top + width * 0.42), (cx + width * dx, top + width * 0.72)], width * 0.03)
        paint(art, pleat, HAT_SHADE)
    paint(art, band, HAT_SHADE)


def option_1(art):
    """The sidebar's equation: white + green, then an arrow to a red result."""
    t = 70
    tile(art, 17, 46, t, WHITE_TIER)
    tile(art, 17, 140, t, GREEN_TIER)
    paint(art, plus(52, 128, 18, 6), ACCENT_LIGHT)
    paint(art, arrow(103, 128, 141, 8, 20), ACCENT_LIGHT)
    big = 92
    glow(art, rounded(147, 82, 147 + big, 82 + big, 12), RED_TIER, 10, 0.55)
    tile(art, 147, 82, big, RED_TIER)


def option_2(art):
    """A chef's hat over a recipe card listing results."""
    card = rounded(84, 76, 236, 230, 14)
    glow(art, card, ACCENT, 10, 0.35)
    paint(art, card, (30, 38, 56))
    rows = [(100, WHITE_TIER), (144, GREEN_TIER), (188, RED_TIER)]
    for y, color in rows:
        tile(art, 124, y, 30, color)
        paint(art, rounded(164, y + 9, 220, y + 21, 6), ACCENT)
    draw_toque(art, 66, 30, 96)


def option_3(art):
    """A wok with two items dropping in and a sparkle of the result rising."""
    bowl = polygon([(28, 150), (204, 150), (186, 196), (150, 214), (82, 214), (46, 196)])
    rim = rounded(20, 142, 212, 158, 8)
    handle = rounded(204, 146, 246, 158, 6)
    paint(art, handle, PAN_LIGHT)
    paint(art, bowl, PAN)
    paint(art, rim, PAN_LIGHT)
    tile(art, 50, 62, 56, WHITE_TIER)
    tile(art, 124, 42, 56, GREEN_TIER)
    for x0, y0, x1, y1 in ((78, 124, 84, 136), (152, 104, 146, 136)):
        paint(art, stroke([(x0, y0), (x1, y1)], 5), ACCENT)
    cx, cy, r = 206, 108, 22
    star = polygon([(cx, cy - r), (cx + r * 0.25, cy - r * 0.25), (cx + r, cy), (cx + r * 0.25, cy + r * 0.25),
                    (cx, cy + r), (cx - r * 0.25, cy + r * 0.25), (cx - r, cy), (cx - r * 0.25, cy - r * 0.25)])
    glow(art, star, RED_TIER, 8, 0.8)
    paint(art, star, RED_TIER)


def option_4(art):
    """Two of the CHEF's slots being filled by a click on a list row."""
    for x, color in ((42, WHITE_TIER), (142, GREEN_TIER)):
        slot = rounded(x, 40, x + 72, 112, 8)
        paint(art, slot, (36, 44, 62))
        tile(art, x + 8, 48, 56, color)
    paint(art, plus(128, 76, 16, 6), ACCENT_LIGHT)
    row = rounded(26, 146, 230, 210, 10)
    paint(art, row, (30, 40, 60))
    tile(art, 36, 154, 48, RED_TIER)
    paint(art, rounded(96, 164, 210, 176, 6), ACCENT_LIGHT)
    paint(art, rounded(96, 186, 176, 196, 5), ACCENT)
    cursor = polygon([(150, 150), (150, 222), (166, 206), (178, 232), (190, 226), (178, 200), (200, 200)])
    paint(art, grow(cursor, 3), CURSOR_EDGE)
    paint(art, cursor, CURSOR)


def list_row(art, x0, y0, x1, y1, color):
    """A sidebar row: a result tile and two lines of text."""
    h = y1 - y0
    paint(art, rounded(x0, y0, x1, y1, h * 0.16), (30, 40, 60))
    t = h * 0.72
    ty = y0 + (h - t) / 2
    tile(art, x0 + ty - y0, ty, t, color)
    lx = x0 + (ty - y0) * 2 + t
    paint(art, rounded(lx, y0 + h * 0.28, x1 - h * 0.25, y0 + h * 0.46, h * 0.09), ACCENT_LIGHT)
    paint(art, rounded(lx, y0 + h * 0.58, lx + (x1 - lx) * 0.6, y0 + h * 0.74, h * 0.08), ACCENT)


def cursor(art, x, y, scale=1.0):
    """The mouse pointer, its tip at (x, y)."""
    points = [(0, 0), (0, 72), (16, 56), (28, 82), (40, 76), (28, 50), (50, 50)]
    shape = polygon([(x + px * scale, y + py * scale) for px, py in points])
    paint(art, grow(shape, 3), CURSOR_EDGE)
    paint(art, shape, CURSOR)


def wok(art, x0, x1, rim_y, depth):
    """A wok seen from the side, with its handle to the right. Returns the bowl mask, for drawing in front of things."""
    w = x1 - x0
    bowl = polygon([(x0 + w * 0.04, rim_y), (x1 - w * 0.04, rim_y), (x1 - w * 0.14, rim_y + depth * 0.7),
                    (x1 - w * 0.32, rim_y + depth), (x0 + w * 0.32, rim_y + depth), (x0 + w * 0.14, rim_y + depth * 0.7)])
    rim = rounded(x0, rim_y - 8, x1, rim_y + 8, 8)
    handle = rounded(x1 - 4, rim_y - 6, x1 + 40, rim_y + 6, 6)
    paint(art, handle, PAN_LIGHT)
    paint(art, bowl, PAN)
    paint(art, rim, PAN_LIGHT)


def option_4a(art):
    """Option 4 under the CHEF's hat: the hat, the two filled slots, and the clicked row."""
    draw_toque(art, 128, 12, 78)
    for x, color in ((52, WHITE_TIER), (148, GREEN_TIER)):
        paint(art, rounded(x, 96, x + 56, 152, 7), (36, 44, 62))
        tile(art, x + 6, 102, 44, color)
    paint(art, plus(128, 124, 14, 5), ACCENT_LIGHT)
    list_row(art, 22, 166, 234, 226, RED_TIER)
    cursor(art, 168, 176, 0.85)


def option_4b(art):
    """Clicking a row drops its items into the wok."""
    list_row(art, 22, 20, 234, 82, RED_TIER)
    cursor(art, 170, 32, 0.8)
    tile(art, 62, 112, 54, WHITE_TIER)
    tile(art, 128, 104, 54, GREEN_TIER)
    wok(art, 16, 206, 172, 58)


def option_4e(art):
    """Option 4b with option 3's falling lines: the row's items drop into the wok."""
    list_row(art, 22, 20, 234, 82, RED_TIER)
    cursor(art, 170, 32, 0.8)
    tile(art, 52, 100, 48, WHITE_TIER)
    tile(art, 118, 92, 48, GREEN_TIER)
    for x0, y0, x1, y1 in ((76, 154, 81, 166), (142, 146, 137, 166)):
        paint(art, stroke([(x0, y0), (x1, y1)], 5), ACCENT)
    wok(art, 16, 206, 180, 54)


def option_4c(art):
    """A big chef's hat whose band is the clicked row."""
    width = 212
    top = 16
    crown, band = toque(128, top, width)
    paint(art, crown, HAT)
    for dx in (-0.14, 0.0, 0.14):
        pleat = stroke([(128 + width * dx, top + width * 0.42), (128 + width * dx, top + width * 0.66)], width * 0.025)
        paint(art, pleat, HAT_SHADE)
    paint(art, band, HAT_SHADE)
    y0, y1 = top + width * 0.74, top + width * 0.98
    h = y1 - y0
    t = h * 0.7
    tile(art, 128 - width * 0.38 + 10, y0 + (h - t) / 2, t, RED_TIER)
    lx = 128 - width * 0.38 + 20 + t
    paint(art, rounded(lx, y0 + h * 0.28, 128 + width * 0.38 - 14, y0 + h * 0.46, h * 0.09), (62, 74, 104))
    paint(art, rounded(lx, y0 + h * 0.58, lx + 60, y0 + h * 0.74, h * 0.08), (96, 110, 142))
    cursor(art, 176, y0 + 10, 0.8)


def option_4d(art):
    """The wok as the CHEF's slots: two filled tiles in it, with the pointer that put them there."""
    tile(art, 52, 98, 64, WHITE_TIER)
    tile(art, 126, 98, 64, GREEN_TIER)
    wok(art, 14, 214, 158, 64)
    cursor(art, 176, 30, 0.9)
    for x0, y0, x1, y1 in ((166, 30, 152, 22), (172, 20, 168, 6), (184, 26, 194, 14)):
        paint(art, stroke([(x0, y0), (x1, y1)], 5), ACCENT_LIGHT)


OPTIONS = {"1": option_1, "2": option_2, "3": option_3, "4": option_4,
           "4a": option_4a, "4b": option_4b, "4c": option_4c, "4d": option_4d, "4e": option_4e}


def render(option):
    art = Image.new("RGBA", (W, W), BACKGROUND)
    OPTIONS[option](art)
    return art.resize((SIZE, SIZE), Image.LANCZOS)


def main(argv):
    option = DEFAULT_OPTION
    if len(argv) >= 2 and argv[0] == "--option":
        option = argv[1]
        argv = argv[2:]
    out = argv[0] if argv else DEFAULT_OUT
    render(option).save(out)


if __name__ == "__main__":
    main(sys.argv[1:])
