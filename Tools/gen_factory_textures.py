import struct, zlib, os, math

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art", "Factory", "Textures")


def write_png(path, w, h, rgba_fn):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        for x in range(w):
            r, g, b, a = rgba_fn(x, y, w, h)
            raw += bytes((r & 255, g & 255, b & 255, a & 255))

    def chunk(tag, data):
        return (
            struct.pack(">I", len(data))
            + tag
            + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        )

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    data = (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
        + chunk(b"IEND", b"")
    )
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(data)
    print("wrote", path, len(data))


def noise(x, y, seed=0):
    n = math.sin(x * 12.9898 + y * 78.233 + seed * 45.164) * 43758.5453
    return n - math.floor(n)


def floor_px(x, y, w, h):
    tile = 64
    gx, gy = x % tile, y % tile
    seam = 2
    base = 118 + int(noise(x * 0.7, y * 0.7, 1) * 18)
    if gx < seam or gy < seam or gx >= tile - seam or gy >= tile - seam:
        base = int(base * 0.72)
    s = noise(x * 0.05, y * 0.05, 3)
    if s > 0.82:
        base = int(base * 0.85)
    r = g = b = max(40, min(200, base))
    tx, ty = x // tile, y // tile
    if (tx + ty) % 8 == 0 and (gx < 6 or gy < 6):
        r = min(255, int(r * 1.15 + 20))
        g = min(255, int(g * 1.05 + 10))
        b = int(b * 0.7)
    return r, g, b, 255


def wall_px(x, y, w, h):
    panel_w, panel_h = 96, 128
    px, py = x % panel_w, y % panel_h
    base = 70 + int(noise(x * 0.3, y * 0.3, 7) * 12)
    if px < 3 or py < 3 or px >= panel_w - 3 or py >= panel_h - 3:
        base = int(base * 0.55)
    wave = abs(((x % 16) - 8)) / 8.0
    base = int(base * (0.88 + 0.12 * wave))
    if (px in (10, panel_w - 11) or py in (10, panel_h - 11)) and (x // 16) % 2 == 0 and (y // 16) % 2 == 0:
        if (x % 16) < 3 and (y % 16) < 3:
            base = min(180, base + 40)
    r = int(base * 0.85)
    g = int(base * 0.9)
    b = int(base * 0.95)
    return r, g, b, 255


def ceiling_px(x, y, w, h):
    base = 55 + int(noise(x * 0.4, y * 0.4, 11) * 10)
    cell = 128
    cx, cy = x % cell, y % cell
    if 20 < cx < cell - 20 and 20 < cy < cell - 20:
        base = int(base * 0.75)
        if 40 < cx < cell - 40 and 40 < cy < cell - 40:
            base = min(200, int(base * 1.8 + 30))
            return min(255, base + 20), min(255, base + 15), min(255, int(base * 0.85)), 255
    if cx < 4 or cy < 4:
        base = int(base * 0.5)
    return base, base, int(base * 0.95), 255


def main():
    root = os.path.normpath(ROOT)
    write_png(os.path.join(root, "ConcreteFloor.png"), 512, 512, floor_px)
    write_png(os.path.join(root, "MetalWall.png"), 512, 512, wall_px)
    write_png(os.path.join(root, "IndustrialCeiling.png"), 512, 512, ceiling_px)


if __name__ == "__main__":
    main()
