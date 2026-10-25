"""Regenerates thunderstore/icon.png (256x256 pause symbol). Standard library only."""
import math
import pathlib
import struct
import zlib

SIZE = 256
SS = 4  # supersampling factor per axis
BG_INNER = (34, 38, 44)
BG_OUTER = (14, 16, 19)
AMBER = (232, 163, 61)


def coverage(x, y):
    """Fraction (0..1) of the amber shape covering point (x, y) in 256-space; None for background."""
    cx = cy = SIZE / 2
    r = math.hypot(x - cx, y - cy)
    if 96 <= r <= 108:  # ring
        return True
    for left in (cx - 44, cx + 14):  # two bars, 30 wide, 100 tall, 8 px corner radius
        right, top, bottom, rad = left + 30, cy - 50, cy + 50, 8
        if left <= x <= right and top <= y <= bottom:
            nx = min(max(x, left + rad), right - rad)
            ny = min(max(y, top + rad), bottom - rad)
            if math.hypot(x - nx, y - ny) <= rad:
                return True
    return False


def pixel(px, py):
    cx = cy = SIZE / 2
    t = min(math.hypot(px + 0.5 - cx, py + 0.5 - cy) / (SIZE * 0.7), 1.0)
    bg = [BG_INNER[i] + (BG_OUTER[i] - BG_INNER[i]) * t for i in range(3)]
    hits = sum(
        coverage(px + (sx + 0.5) / SS, py + (sy + 0.5) / SS)
        for sx in range(SS)
        for sy in range(SS)
    )
    a = hits / (SS * SS)
    return bytes(round(bg[i] * (1 - a) + AMBER[i] * a) for i in range(3))


def write_png(path, rows):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    raw = b"".join(b"\x00" + row for row in rows)
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    path.write_bytes(png)


if __name__ == "__main__":
    out = pathlib.Path(__file__).resolve().parent.parent / "thunderstore" / "icon.png"
    write_png(out, [b"".join(pixel(x, y) for x in range(SIZE)) for y in range(SIZE)])
    print(f"wrote {out}")
