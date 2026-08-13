"""Regenerate the bundled CJK subset font from the system Noto Sans SC variable font.

Run after editing any Chinese/visible text in the app:
    python subset_font.py

Strategy (avoids the known varLib.instancer glyph-corruption pitfall):
  - subset the variable font with pyftsubset (keeps fvar/gvar, full glyphs),
  - pin the wght axis default to 400 so Skia renders the Regular instance.
"""
import glob
import os

from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = os.path.dirname(os.path.abspath(__file__))
SRC_DIR = os.path.join(ROOT, "CiBi")
SRC_FONT = r"C:\Windows\Fonts\NotoSansSC-VF.ttf"
OUT_FONT = os.path.join(SRC_DIR, "Assets", "Fonts", "NotoSansSC.ttf")
TEXT_FILE = os.path.join(ROOT, "subset_chars.txt")

# 1) collect every character that must render
chars = set()
for path in glob.glob(os.path.join(SRC_DIR, "**", "*"), recursive=True):
    norm = path.replace("\\", "/")
    if "/obj/" in norm or "/bin/" in norm:
        continue
    if os.path.isfile(path) and norm.lower().endswith((".cs", ".axaml")):
        with open(path, encoding="utf-8-sig") as f:
            chars.update(f.read())

# full ASCII + currency/math glyphs that fall outside ASCII & CJK ranges
for cp in range(0x20, 0x7F):
    chars.add(chr(cp))
for cp in (0x00A5, 0xFFE5, 0x00D7, 0x00F7, 0x00B7, 0x2014, 0x2022, 0x2192, 0x00A0):
    chars.add(chr(cp))

text = "".join(sorted(c for c in chars if ord(c) >= 0x20))
with open(TEXT_FILE, "w", encoding="utf-8") as f:
    f.write(text)

# 2) subset the variable font (keep variation tables)
subset.main([
    SRC_FONT,
    "--text-file=" + TEXT_FILE,
    "--output-file=" + OUT_FONT,
    "--recalc-bounds",
])

# 3) pin wght so Skia always renders Regular (400), keep max=900 for Bold/SemiBold
f = TTFont(OUT_FONT)
if "fvar" in f:
    for a in f["fvar"].axes:
        if a.axisTag == "wght":
            a.minValue = 400.0
            a.defaultValue = 400.0
f.save(OUT_FONT)

# 4) report
f2 = TTFont(OUT_FONT)
cmap = f2.getBestCmap()
needed = [c for c in chars if ord(c) >= 0x20]
missing = [c for c in needed if ord(c) not in cmap]
print("OUT   :", OUT_FONT)
print("SIZE  :", os.path.getsize(OUT_FONT), "bytes")
print("family:", f2["name"].getDebugName(1))
print("axes  :", [(a.axisTag, a.minValue, a.defaultValue, a.maxValue) for a in f2["fvar"].axes] if "fvar" in f2 else "static")
print("glyphs:", len(cmap))
print("missing:", missing)
