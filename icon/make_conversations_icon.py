"""Draws the Conversations icon, two speech bubbles, as a multi-size .ico of PNG images, without third-party packages."""

import struct
import sys
import zlib
from pathlib import Path


class BubblesIcon:
    """Renders the bubbles with supersampling at each icon size and packs the images into one .ico file.

    A coral bubble, the reply, sits behind on the upper right; a blue bubble, the user's message with three
    dots, sits in front on the lower left, cut out from the coral one by a thin transparent gap.
    """

    SIZES: tuple[int, ...] = (16, 24, 32, 48, 64, 128, 256)
    SAMPLES: int = 4
    BACK: tuple[int, int, int] = (217, 119, 87)
    FRONT: tuple[int, int, int] = (47, 128, 237)
    DOTS: tuple[int, int, int] = (255, 255, 255)
    GAP: float = 0.04

    @staticmethod
    def in_rounded_rect(x: float, y: float, left: float, top: float, right: float, bottom: float, radius: float) -> bool:
        """Tells whether a point lies in a rectangle with rounded corners.

        :param x: The point's abscissa.
        :param y: The point's ordinate.
        :param left: The rectangle's left edge.
        :param top: The rectangle's top edge.
        :param right: The rectangle's right edge.
        :param bottom: The rectangle's bottom edge.
        :param radius: The corners' radius.
        :return: True when inside.
        """
        if x < left or x > right or y < top or y > bottom:
            return False
        cx: float = min(max(x, left + radius), right - radius)
        cy: float = min(max(y, top + radius), bottom - radius)
        return (x - cx) ** 2 + (y - cy) ** 2 <= radius * radius

    @staticmethod
    def in_triangle(x: float, y: float, a: tuple[float, float], b: tuple[float, float], c: tuple[float, float]) -> bool:
        """Tells whether a point lies in a triangle.

        :param x: The point's abscissa.
        :param y: The point's ordinate.
        :param a: The first corner.
        :param b: The second corner.
        :param c: The third corner.
        :return: True when inside.
        """
        def side(p: tuple[float, float], q: tuple[float, float]) -> float:
            return (x - q[0]) * (p[1] - q[1]) - (p[0] - q[0]) * (y - q[1])

        d1: float = side(a, b)
        d2: float = side(b, c)
        d3: float = side(c, a)
        return not ((d1 < 0 or d2 < 0 or d3 < 0) and (d1 > 0 or d2 > 0 or d3 > 0))

    @classmethod
    def in_back(cls, x: float, y: float) -> bool:
        """Tells whether a point lies in the coral bubble, its tail on the lower right.

        :param x: The abscissa, from 0 to 1.
        :param y: The ordinate, from 0 to 1.
        :return: True when inside.
        """
        return cls.in_rounded_rect(x, y, 0.34, 0.06, 0.95, 0.52, 0.13) or cls.in_triangle(x, y, (0.70, 0.48), (0.86, 0.48), (0.88, 0.66))

    @classmethod
    def in_front(cls, x: float, y: float, grow: float = 0.0) -> bool:
        """Tells whether a point lies in the blue bubble, its tail on the lower left.

        :param x: The abscissa, from 0 to 1.
        :param y: The ordinate, from 0 to 1.
        :param grow: How much to widen the bubble on every side, for the gap around it.
        :return: True when inside.
        """
        body: bool = cls.in_rounded_rect(x, y, 0.05 - grow, 0.30 - grow, 0.72 + grow, 0.80 + grow, 0.15 + grow)
        tail: bool = cls.in_triangle(x, y, (0.14 - grow, 0.74), (0.34 + grow, 0.74), (0.08 - grow, 0.96 + grow))
        return body or tail

    @classmethod
    def colour_at(cls, x: float, y: float) -> tuple[int, int, int] | None:
        """The colour of the icon at a point.

        :param x: The abscissa, from 0 to 1.
        :param y: The ordinate, from 0 to 1.
        :return: The colour; None where the icon is transparent.
        """
        if cls.in_front(x, y):
            for dot_x in (0.22, 0.385, 0.55):
                if (x - dot_x) ** 2 + (y - 0.55) ** 2 <= 0.052 ** 2:
                    return cls.DOTS
            return cls.FRONT
        if cls.in_front(x, y, cls.GAP):
            return None
        return cls.BACK if cls.in_back(x, y) else None

    @classmethod
    def render(cls, size: int) -> bytes:
        """Renders the icon as RGBA rows.

        :param size: The image's side, in pixels.
        :return: The raw rows, each prefixed by PNG filter byte 0.
        """
        rows: bytearray = bytearray()
        n: int = cls.SAMPLES
        for py in range(size):
            rows.append(0)
            for px in range(size):
                red: int = 0
                green: int = 0
                blue: int = 0
                hits: int = 0
                for sy in range(n):
                    for sx in range(n):
                        colour: tuple[int, int, int] | None = cls.colour_at((px + (sx + 0.5) / n) / size, (py + (sy + 0.5) / n) / size)
                        if colour is not None:
                            red += colour[0]
                            green += colour[1]
                            blue += colour[2]
                            hits += 1
                if hits == 0:
                    rows.extend((0, 0, 0, 0))
                else:
                    rows.extend((red // hits, green // hits, blue // hits, 255 * hits // (n * n)))
        return bytes(rows)

    @staticmethod
    def png(size: int, rows: bytes) -> bytes:
        """Encodes RGBA rows as a PNG file.

        :param size: The image's side, in pixels.
        :param rows: The filtered rows.
        :return: The PNG file.
        """
        def chunk(kind: bytes, data: bytes) -> bytes:
            return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

        header: bytes = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
        return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(rows, 9)) + chunk(b"IEND", b"")

    @classmethod
    def ico(cls) -> bytes:
        """Builds the .ico file, one PNG image per size.

        :return: The file's bytes.
        """
        images: list[bytes] = [cls.png(size, cls.render(size)) for size in cls.SIZES]
        directory: bytearray = bytearray(struct.pack("<HHH", 0, 1, len(images)))
        offset: int = 6 + 16 * len(images)
        for size, image in zip(cls.SIZES, images):
            side: int = 0 if size >= 256 else size
            directory += struct.pack("<BBBBHHII", side, side, 0, 0, 1, 32, len(image), offset)
            offset += len(image)
        return bytes(directory) + b"".join(images)

    @classmethod
    def main(cls, argv: list[str]) -> int:
        """Writes the icon, or one PNG image of it to look at.

        :param argv: The command line: the output path, .ico or .png, and for a .png its side in pixels.
        :return: The exit code.
        """
        if len(argv) not in (2, 3):
            print("usage: make_conversations_icon.py <out.ico> | <out.png> <size>", file=sys.stderr)
            return 2
        out: Path = Path(argv[1])
        if out.suffix == ".png":
            size: int = int(argv[2]) if len(argv) == 3 else 256
            out.write_bytes(cls.png(size, cls.render(size)))
        else:
            out.write_bytes(cls.ico())
        return 0


if __name__ == "__main__":
    sys.exit(BubblesIcon.main(sys.argv))
