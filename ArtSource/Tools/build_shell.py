"""One simple beveled modular block for the deliberately minimal residential shell."""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent))
from model_primitives import *

reset_scene()
box('Block', (0, 0, .5), (1, 1, 1), 'White', .025)
export_asset('Block')
