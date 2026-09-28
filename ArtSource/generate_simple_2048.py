"""Generate flat-color 2048 sprites for Unity's UI Image component."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets" / "Art" / "2048Simple"
ASSETS.mkdir(parents=True, exist_ok=True)

COLORS = {
    "page": "#FAF8EF",
    "board": "#BBADA0",
    "empty": "#CDC1B4",
    "dark_text": "#776E65",
    "light_text": "#F9F6F2",
    2: "#EEE4DA",
    4: "#EDE0C8",
    8: "#F2B179",
    16: "#F59563",
    32: "#F67C5F",
    64: "#F65E3B",
    128: "#EDCF72",
    256: "#EDCC61",
    512: "#EDC850",
    1024: "#EDC53F",
    2048: "#EDC22E",
}

SCALE = 4
TILE = 216
BOARD = 1024
GAP = 32
RADIUS = 15


def rounded_sprite(size, color, radius):
    image = Image.new("RGBA", (size * SCALE, size * SCALE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(
        (0, 0, size * SCALE - 1, size * SCALE - 1),
        radius=radius * SCALE,
        fill=color,
    )
    return image.resize((size, size), Image.Resampling.LANCZOS)


def font(size):
    for path in (
        "C:/Windows/Fonts/segoeuib.ttf",
        "C:/Windows/Fonts/arialbd.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def numbered_tile(value):
    image = rounded_sprite(TILE, COLORS[value], RADIUS).resize(
        (TILE * SCALE, TILE * SCALE), Image.Resampling.NEAREST
    )
    draw = ImageDraw.Draw(image)
    digits = str(value)
    text_size = {1: 116, 2: 106, 3: 84, 4: 66}[len(digits)] * SCALE
    draw.text(
        (TILE * SCALE / 2, TILE * SCALE / 2 - 3 * SCALE),
        digits,
        font=font(text_size),
        fill=COLORS["dark_text"] if value <= 4 else COLORS["light_text"],
        anchor="mm",
    )
    return image.resize((TILE, TILE), Image.Resampling.LANCZOS)


rounded_sprite(BOARD, COLORS["board"], 24).save(ASSETS / "board_panel.png")
rounded_sprite(TILE, COLORS["empty"], RADIUS).save(ASSETS / "cell_empty.png")
rounded_sprite(216, COLORS["board"], RADIUS).save(ASSETS / "score_panel.png")
rounded_sprite(256, "#8F7A66", RADIUS).save(ASSETS / "button_primary.png")

for value in [2 ** power for power in range(1, 12)]:
    rounded_sprite(TILE, COLORS[value], RADIUS).save(ASSETS / f"tile_bg_{value}.png")
    numbered_tile(value).save(ASSETS / f"tile_{value}.png")

# A board with the 16 empty cells already drawn is useful when tiles are
# positioned directly on top of one static Image rather than a GridLayoutGroup.
board_with_slots = rounded_sprite(BOARD, COLORS["board"], 24)
empty = Image.open(ASSETS / "cell_empty.png")
for row in range(4):
    for col in range(4):
        board_with_slots.alpha_composite(empty, (GAP + col * (TILE + GAP), GAP + row * (TILE + GAP)))
board_with_slots.save(ASSETS / "board_with_slots.png")

# A separate preview keeps the Unity Assets folder free of demo-only images.
preview = Image.new("RGB", (1120, 1430), COLORS["page"])
draw = ImageDraw.Draw(preview)
draw.text((48, 25), "2048", font=font(115), fill=COLORS["dark_text"])
draw.text((52, 158), "Join the numbers to reach 2048", font=font(27), fill=COLORS["dark_text"])

score = Image.open(ASSETS / "score_panel.png")
preview.paste(score, (850, 48), score)
draw.text((958, 96), "SCORE", font=font(24), fill=COLORS["light_text"], anchor="mm")
draw.text((958, 142), "1248", font=font(37), fill=COLORS["light_text"], anchor="mm")

board_x, board_y = 48, 270
board_image = Image.open(ASSETS / "board_with_slots.png")
preview.paste(board_image, (board_x, board_y), board_image)
layout = [
    [2, 4, 8, 16],
    [32, 64, 128, 256],
    [512, 1024, 2048, 0],
    [2, 4, 0, 0],
]
for row, values in enumerate(layout):
    for col, value in enumerate(values):
        if value:
            tile_image = Image.open(ASSETS / f"tile_{value}.png")
            x = board_x + GAP + col * (TILE + GAP)
            y = board_y + GAP + row * (TILE + GAP)
            preview.paste(tile_image, (x, y), tile_image)

draw.text((50, 1342), "Simple warm-color UI sprites  |  4 x 4 board", font=font(26), fill=COLORS["dark_text"])
preview.save(ROOT / "ArtSource" / "2048_preview.png")

print(f"Generated sprites in {ASSETS}")
