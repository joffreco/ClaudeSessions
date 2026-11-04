"""Draws the Consignes icon, a bell, as a multi-size .ico of PNG images, without third-party packages."""

import struct
import sys
import zlib
from pathlib import Path


class BellIcon:
    """Renders the bell with supersampling at each icon size and packs the images into one .ico file."""

    SIZES: tuple[int, ...] = (16, 24, 32, 48, 64, 128, 256)
    SAMPLES: int = 4
    BODY: tuple[int, int, int] = (242, 179, 61)
    SHADE: tuple[int, int, int] = (214, 146, 36)
    CLAPPER: tuple[int, int, int] = (176, 112, 22)

    @staticmethod
    def in_circle(x: float, y: float, cx: float, cy: float, r: float) -> bool:
        """Tells whether a point lies in a circle.

        :param x: The point's abscissa.
        :param y: The point's ordinate.
        :param cx: The centre's abscissa.
        :param cy: The centre's ordinate.
        :param r: The radius.
        :return: True when inside.
        """
        return (x - cx) ** 2 + (y - cy) ** 2 <= r * r

    @staticmethod
    def body_half_width(y: float) -> float:
        """The bell's half width at a height, from the shoulders down to the rim.

        :param y: The height, 0 at the top and 1 at the bottom.
        :return: The half width; 0 above the shoulders.
        """
        top: float = 0.34
        bottom: float = 0.75
        if y < top or y > bottom:
            return 0.0
        t: float = (y - top) / (bottom - top)
        return 0.18 + 0.19 * t ** 2.2

    @classmethod
    def colour_at(cls, x: float, y: float) -> tuple[int, int, int] | None:
        """The colour of the bell at a point.

        :param x: The abscissa, from 0 to 1.
        :param y: The ordinate, from 0 to 1.
        :return: The colour; None outside the bell.
        """
        if cls.in_circle(x, y, 0.5, 0.86, 0.085):
            return cls.CLAPPER
        rim: bool = 0.72 <= y <= 0.81 and 0.11 <= x <= 0.89
        dome: bool = cls.in_circle(x, y, 0.5, 0.36, 0.18)
        body: bool = abs(x - 0.5) <= cls.body_half_width(y)
        knob: bool = cls.in_circle(x, y, 0.5, 0.15, 0.065)
        if not (rim or dome or body or knob):
            return None
        if rim:
            return cls.SHADE
        return cls.SHADE if x > 0.5 + 0.55 * max(cls.body_half_width(y), 0.18 if dome else 0.0) else cls.BODY

    @classmethod
    def render(cls, size: int) -> bytes:
        """Renders the bell as RGBA rows.

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
        """Writes the icon.

        :param argv: The command line; the output path is its only argument.
        :return: The exit code.
        """
        if len(argv) != 2:
            print("usage: make_consignes_icon.py <out.ico>", file=sys.stderr)
            return 2
        Path(argv[1]).write_bytes(cls.ico())
        return 0


if __name__ == "__main__":
    sys.exit(BellIcon.main(sys.argv))
