"""Canonical character rebuild entry point: current rounded, rigged v0.3 assets."""
from pathlib import Path
import runpy

if __name__ == "__main__":
    runpy.run_path(str(Path(__file__).with_name("build_rounded_characters.py")), run_name="__main__")
