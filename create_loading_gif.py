from pathlib import Path
import math
import random

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "SkyrimTogether-loading-vampire-werewolf.png"
OUTPUT = ROOT / "SkyrimTogether-loading.gif"
SIZE = (1024, 576)
FRAME_COUNT = 30
FRAME_MS = 100


def cover_crop(image: Image.Image, size: tuple[int, int], zoom: float) -> Image.Image:
    target_ratio = size[0] / size[1]
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        height = image.height
        width = round(height * target_ratio)
    else:
        width = image.width
        height = round(width / target_ratio)

    width = round(width / zoom)
    height = round(height / zoom)
    left = (image.width - width) // 2
    top = (image.height - height) // 2
    return image.crop((left, top, left + width, top + height)).resize(size, Image.Resampling.LANCZOS)


def add_embers(frame: Image.Image, index: int) -> Image.Image:
    embers = Image.new("RGBA", frame.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(embers)
    rng = random.Random(83470)
    for ember in range(12):
        base_x = rng.randint(40, SIZE[0] - 40)
        speed = rng.uniform(0.7, 1.7)
        phase = rng.random()
        travel = ((index / FRAME_COUNT + phase) % 1.0)
        y = round(SIZE[1] - 12 - travel * 120 * speed)
        x = round(base_x + math.sin((index + ember) * 0.45) * 5)
        radius = rng.choice((1, 1, 2))
        alpha = round(180 * math.sin(math.pi * travel))
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(255, 112, 34, alpha))
    return Image.alpha_composite(frame, embers)


def main() -> None:
    source = Image.open(SOURCE).convert("RGB")
    rgba_frames = []
    for index in range(FRAME_COUNT):
        phase = index / FRAME_COUNT
        zoom = 1.0 + 0.012 * (0.5 - 0.5 * math.cos(phase * math.tau))
        frame = cover_crop(source, SIZE, zoom).convert("RGBA")
        frame = add_embers(frame, index)
        rgba_frames.append(frame.convert("RGB"))

    palette = rgba_frames[0].quantize(colors=192, method=Image.Quantize.MEDIANCUT)
    frames = [frame.quantize(palette=palette, dither=Image.Dither.FLOYDSTEINBERG) for frame in rgba_frames]
    frames[0].save(
        OUTPUT,
        save_all=True,
        append_images=frames[1:],
        duration=FRAME_MS,
        loop=0,
        optimize=True,
        disposal=2,
    )


if __name__ == "__main__":
    main()
