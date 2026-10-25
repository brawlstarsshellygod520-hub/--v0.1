"""Pack actual Unity-rendered animation frames; never generate replacement artwork."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
frames = ROOT / "Logs" / "CharacterFrames" / "v0.3"
output = ROOT / "Docs" / "Art" / "Models" / "v0.3"
# GIF timings are multiples of 10ms; distribute rounding to retain .8s / 2s loops.
for action, timing in (("run", (30, 30, 40)), ("run-side", (30, 30, 40)), ("idle", (70, 60, 70))):
    paths = sorted(frames.glob(action + "-[0-9][0-9].png"))
    if not paths:
        raise RuntimeError("No Unity frames: " + action)
    images = [Image.open(path).convert("RGB") for path in paths]
    images[0].save(output / (action + ".gif"), save_all=True,
                   append_images=images[1:], duration=[timing[i % 3] for i in range(len(images))], loop=0,
                   optimize=True, disposal=2)
    print("UNITY_ANIMATION_PREVIEW", action, len(images), output / (action + ".gif"))
