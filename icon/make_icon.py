"""Draws the claude-sessions icon: two offset Claude Code mascots, as a multi-size .ico."""

import struct
import sys
import zlib
from pathlib import Path


class IconMaker:
    """Renders the pixel-art mascots at each icon size and packs them into one .ico file."""

    MASCOT: tuple[str, ...] = (
        "...#############...",
        "...#############...",
        "...#############...",
        "...##.#######.##...",
        "...##.#######.##...",
        "...##.#######.##...",
        ".#################.",
        ".#################.",
        ".#################.",
        "...#############...",
        "...#############...",
        "...#############...",
        "...#.#.......#.#...",
        "...#.#.......#.#...",
        "...#.#.......#.#...",
    )
    SMALL_MASCOT: tuple[str, ...] = (
        "..###########..",
        "..###########..",
        "..#.#######.#..",
        "..#.#######.#..",
        "###############",
        "###############",
        "..###########..",
        "..###########..",
        "..#.#.....#.#..",
        "..#.#.....#.#..",
    )
    BACK_OFFSET: tuple[int, int] = (7, -9)
    SMALL_BACK_OFFSET: tuple[int, int] = (5, -6)
    GAP: int = 1
    FRONT: tuple[int, int, int, int] = (0xE7, 0x48, 0x56, 0xFF)
    BACK: tuple[int, int, int, int] = (0xE7, 0x48, 0x56, 0x8C)
    CLEAR: tuple[int, int, int, int] = (0, 0, 0, 0)
    SINGLE_SIZES: tuple[int, ...] = (16,)
    SMALL_PAIR_SIZES: tuple[int, ...] = (20, 24)
    PAIR_SIZES: tuple[int, ...] = (32, 40, 48, 64, 96, 256)
    WHOLE_PIXEL_SLACK: float = 0.34

    @staticmethod
    def pair_grid(mascot: tuple[str, ...], offset: tuple[int, int]) -> list[list[tuple[int, int, int, int]]]:
        """Composes two copies of a mascot on one grid, the back one cut away around the front one.

        Args:
            mascot: Rows of the mascot, ``#`` for a painted cell.
            offset: Position of the back copy relative to the front one, in cells.

        Returns:
            Rows of RGBA cells, one cell per mascot pixel.
        """
        dx, dy = offset
        width: int = len(mascot[0]) + abs(dx)
        height: int = len(mascot) + abs(dy)
        front_origin: tuple[int, int] = (max(0, -dx), max(0, -dy))
        back_origin: tuple[int, int] = (max(0, dx), max(0, dy))
        front: set[tuple[int, int]] = IconMaker.cells(mascot, front_origin)
        back: set[tuple[int, int]] = IconMaker.cells(mascot, back_origin)
        halo: set[tuple[int, int]] = IconMaker.grow(IconMaker.filled(mascot, front_origin), IconMaker.GAP)
        grid: list[list[tuple[int, int, int, int]]] = [[IconMaker.CLEAR] * width for _ in range(height)]
        for x, y in back - halo:
            grid[y][x] = IconMaker.BACK
        for x, y in front:
            grid[y][x] = IconMaker.FRONT
        return grid

    @staticmethod
    def single_grid() -> list[list[tuple[int, int, int, int]]]:
        """Draws one mascot for the smallest sizes, where two would not read.

        Returns:
            Rows of RGBA cells.
        """
        return [[IconMaker.FRONT if cell == "#" else IconMaker.CLEAR for cell in row] for row in IconMaker.SMALL_MASCOT]

    @staticmethod
    def cells(shape: tuple[str, ...], origin: tuple[int, int]) -> set[tuple[int, int]]:
        """Lists the painted cells of a shape placed at an origin.

        Args:
            shape: Rows of the shape, ``#`` for a painted cell.
            origin: Grid position of its top-left corner.

        Returns:
            The grid positions of its painted cells.
        """
        return {(origin[0] + x, origin[1] + y) for y, row in enumerate(shape) for x, cell in enumerate(row) if cell == "#"}

    @staticmethod
    def filled(shape: tuple[str, ...], origin: tuple[int, int]) -> set[tuple[int, int]]:
        """Lists the cells a shape covers, its eyes and the gaps between its legs included.

        Args:
            shape: Rows of the shape.
            origin: Grid position of its top-left corner.

        Returns:
            The grid positions between the first and last painted cell of each row.
        """
        covered: set[tuple[int, int]] = set()
        for y, row in enumerate(shape):
            painted: list[int] = [x for x, cell in enumerate(row) if cell == "#"]
            if painted:
                covered.update((origin[0] + x, origin[1] + y) for x in range(painted[0], painted[-1] + 1))
        return covered

    @staticmethod
    def grow(area: set[tuple[int, int]], margin: int) -> set[tuple[int, int]]:
        """Widens an area by a margin in every direction.

        Args:
            area: The cells of the area.
            margin: How many cells to add around it.

        Returns:
            The widened area.
        """
        return {(x + i, y + j) for x, y in area for i in range(-margin, margin + 1) for j in range(-margin, margin + 1)}

    @staticmethod
    def render(grid: list[list[tuple[int, int, int, int]]], size: int) -> list[list[tuple[int, int, int, int]]]:
        """Scales a grid onto a square image, centered; by whole pixels unless that would leave it much smaller.

        Args:
            grid: Rows of RGBA cells.
            size: Side of the image, in pixels.

        Returns:
            Rows of RGBA pixels, top row first.
        """
        columns: int = len(grid[0])
        rows: int = len(grid)
        scale: float = size / max(columns, rows)
        if scale - int(scale) < IconMaker.WHOLE_PIXEL_SLACK or scale < 1:
            scale = max(1, int(scale))
        width: int = round(columns * scale)
        height: int = round(rows * scale)
        left: int = (size - width) // 2
        top: int = (size - height) // 2
        image: list[list[tuple[int, int, int, int]]] = [[IconMaker.CLEAR] * size for _ in range(size)]
        for y in range(height):
            for x in range(width):
                image[top + y][left + x] = grid[min(rows - 1, int(y / scale))][min(columns - 1, int(x / scale))]
        return image

    @staticmethod
    def png(image: list[list[tuple[int, int, int, int]]]) -> bytes:
        """Encodes an RGBA image as PNG.

        Args:
            image: Rows of RGBA pixels, top row first.

        Returns:
            The PNG file content.
        """
        raw: bytes = b"".join(b"\x00" + bytes(channel for pixel in row for channel in pixel) for row in image)
        header: bytes = struct.pack(">IIBBBBB", len(image[0]), len(image), 8, 6, 0, 0, 0)
        return b"\x89PNG\r\n\x1a\n" + IconMaker.chunk(b"IHDR", header) + IconMaker.chunk(b"IDAT", zlib.compress(raw, 9)) + IconMaker.chunk(b"IEND", b"")

    @staticmethod
    def chunk(kind: bytes, data: bytes) -> bytes:
        """Frames one PNG chunk.

        Args:
            kind: Four-letter chunk type.
            data: Chunk payload.

        Returns:
            Length, type, payload and CRC.
        """
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    @staticmethod
    def bitmap(image: list[list[tuple[int, int, int, int]]]) -> bytes:
        """Encodes an RGBA image as a 32-bit icon bitmap with its AND mask.

        Args:
            image: Rows of RGBA pixels, top row first.

        Returns:
            BITMAPINFOHEADER, BGRA rows bottom-up, then the 1-bit mask.
        """
        size: int = len(image)
        header: bytes = struct.pack("<IiiHHIIiiII", 40, size, size * 2, 1, 32, 0, 0, 0, 0, 0, 0)
        colors: bytes = b"".join(bytes((b, g, r, a)) for row in reversed(image) for (r, g, b, a) in row)
        mask_stride: int = ((size + 31) // 32) * 4
        mask: bytes = b"".join(IconMaker.mask_row(row, mask_stride) for row in reversed(image))
        return header + colors + mask

    @staticmethod
    def mask_row(row: list[tuple[int, int, int, int]], stride: int) -> bytes:
        """Encodes one row of the AND mask: a set bit for each transparent pixel.

        Args:
            row: RGBA pixels of the row.
            stride: Bytes per mask row, padded to 32 bits.

        Returns:
            The mask row.
        """
        bits: bytearray = bytearray(stride)
        for x, pixel in enumerate(row):
            if pixel[3] == 0:
                bits[x // 8] |= 0x80 >> (x % 8)
        return bytes(bits)

    @staticmethod
    def ico(images: list[list[list[tuple[int, int, int, int]]]]) -> bytes:
        """Packs square images into an .ico file: bitmaps below 256 pixels, PNG at 256.

        Args:
            images: The images, smallest first.

        Returns:
            The .ico file content.
        """
        frames: list[bytes] = [IconMaker.png(image) if len(image) >= 256 else IconMaker.bitmap(image) for image in images]
        offset: int = 6 + 16 * len(frames)
        directory: bytes = b""
        for image, frame in zip(images, frames):
            side: int = len(image) % 256
            directory += struct.pack("<BBBBHHII", side, side, 0, 0, 1, 32, len(frame), offset)
            offset += len(frame)
        return struct.pack("<HHH", 0, 1, len(frames)) + directory + b"".join(frames)

    @staticmethod
    def preview(image: list[list[tuple[int, int, int, int]]], backgrounds: tuple[tuple[int, int, int], ...]) -> list[list[tuple[int, int, int, int]]]:
        """Lays an image over each background side by side, to judge it on dark and light taskbars.

        Args:
            image: Rows of RGBA pixels.
            backgrounds: RGB colors to lay it over.

        Returns:
            The opaque preview.
        """
        rows: list[list[tuple[int, int, int, int]]] = []
        for row in image:
            line: list[tuple[int, int, int, int]] = []
            for background in backgrounds:
                line.extend(IconMaker.blend(pixel, background) for pixel in row)
            rows.append(line)
        return rows

    @staticmethod
    def blend(pixel: tuple[int, int, int, int], background: tuple[int, int, int]) -> tuple[int, int, int, int]:
        """Blends one RGBA pixel over an opaque color.

        Args:
            pixel: The pixel.
            background: The color under it.

        Returns:
            The opaque result.
        """
        alpha: float = pixel[3] / 255
        r, g, b = (round(pixel[i] * alpha + background[i] * (1 - alpha)) for i in range(3))
        return (r, g, b, 255)

    @staticmethod
    def main(arguments: list[str]) -> None:
        """Writes the .ico, and a preview PNG when a second path is given.

        Args:
            arguments: The .ico path, then optionally the preview PNG path.
        """
        single: list[list[tuple[int, int, int, int]]] = IconMaker.single_grid()
        small_pair: list[list[tuple[int, int, int, int]]] = IconMaker.pair_grid(IconMaker.SMALL_MASCOT, IconMaker.SMALL_BACK_OFFSET)
        pair: list[list[tuple[int, int, int, int]]] = IconMaker.pair_grid(IconMaker.MASCOT, IconMaker.BACK_OFFSET)
        images: list[list[list[tuple[int, int, int, int]]]] = [IconMaker.render(single, size) for size in IconMaker.SINGLE_SIZES]
        images += [IconMaker.render(small_pair, size) for size in IconMaker.SMALL_PAIR_SIZES]
        images += [IconMaker.render(pair, size) for size in IconMaker.PAIR_SIZES]
        Path(arguments[0]).write_bytes(IconMaker.ico(images))
        if len(arguments) > 1:
            Path(arguments[1]).write_bytes(IconMaker.png(IconMaker.preview(images[-1], ((0x20, 0x20, 0x20), (0xF3, 0xF3, 0xF3)))))


if __name__ == "__main__":
    IconMaker.main(sys.argv[1:])
