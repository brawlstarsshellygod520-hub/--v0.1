"""Build the approved Fire Rescue residential low-poly prop kit.

Run with Blender 5.1 from the project root:
  blender.exe --background --python ArtSource/Tools/build_household.py
"""
import math
import bpy
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from model_primitives import *


def x_rotate(obj):
    """Turn a Z-axis cylinder so its length points towards -Y."""
    obj.rotation_euler = (math.radians(90), 0, 0)
    return obj


def anchor(name, location):
    obj = bpy.data.objects.new(name, None)
    obj.location = location
    bpy.context.collection.objects.link(obj)
    return obj


def toilet():
    reset_scene()
    # The bowl remains closed; the separately readable, open rectangular tank holds water.
    ellipsoid('Toilet_BowlBase', (0, -.14, .285), (.65, .68, .51), 'White')
    box('Toilet_Foot', (0, -.10, .14), (.40, .44, .28), 'White', .08)
    ellipsoid('Toilet_ClosedSeat', (0, -.19, .555), (.56, .60, .10), 'White')
    ellipsoid('Toilet_ClosedLid', (0, -.20, .603), (.51, .55, .055), 'White')
    box('Tank_Base', (0, .285, .675), (.56, .28, .10), 'White', .035)
    box('Tank_LeftWall', (-.255, .285, .855), (.05, .28, .38), 'White', .025)
    box('Tank_RightWall', (.255, .285, .855), (.05, .28, .38), 'White', .025)
    box('Tank_FrontWall', (0, .165, .855), (.56, .05, .38), 'White', .025)
    box('Tank_BackWall', (0, .405, .855), (.56, .05, .38), 'White', .025)
    box('Tank_Water', (0, .285, 1.015), (.43, .16, .018), 'Water', .008)
    anchor('NozzleConnection', (0, .24, .98))
    export_asset('Toilet')


def salad_oil():
    reset_scene()
    cylinder('Oil_Bottle', (0, 0, .135), .085, .27, 'Oil')
    ellipsoid('Oil_Shoulder', (0, 0, .275), (.17, .17, .09), 'Oil')
    cylinder('Oil_Neck', (0, 0, .33), .042, .05, 'Plastic')
    cylinder('Oil_Cap', (0, 0, .3775), .048, .025, 'Label')
    box('Oil_GreenLabel', (0, -.086, .16), (.112, .008, .115), 'Label', .008)
    export_asset('SaladOil')


def nozzle():
    reset_scene()
    # All geometry extends forward along -Y; the root stays at the hose/grip end.
    x_rotate(cylinder('Nozzle_GripSocket', (0, -.038, .09), .069, .085, 'Metal'))
    x_rotate(cylinder('Nozzle_Grip', (0, -.115, .09), .061, .12, 'Charcoal'))
    x_rotate(cylinder('Nozzle_MetalRing', (0, -.182, .09), .078, .045, 'Metal'))
    x_rotate(cylinder('Nozzle_Barrel', (0, -.245, .09), .063, .11, 'Metal'))
    x_rotate(cylinder('Nozzle_Tip', (0, -.315, .083), .083, .07, 'Charcoal'))
    between('Nozzle_Handle', (-.043, -.12, .075), (-.043, -.12, .19), .018, 'Charcoal')
    between('Nozzle_Trigger', (-.025, -.16, .075), (-.025, -.205, .018), .012, 'Metal')
    anchor('GripOrigin', (0, 0, 0))
    export_asset('Nozzle')


def sofa():
    reset_scene()
    box('Sofa_SeatBase', (0, 0, .34), (1.82, .83, .24), 'Sofa', .08)
    ellipsoid('Sofa_SeatCushion', (0, -.07, .47), (1.55, .64, .22), 'Sofa')
    box('Sofa_Back', (0, .325, .69), (1.84, .20, .52), 'Sofa', .075)
    ellipsoid('Sofa_BackCushion', (0, .205, .69), (1.56, .16, .42), 'Sofa')
    box('Sofa_LeftArm', (-.88, -.02, .57), (.24, .82, .45), 'Sofa', .07)
    box('Sofa_RightArm', (.88, -.02, .57), (.24, .82, .45), 'Sofa', .07)
    for x in (-.72, .72):
        for y in (-.28, .28):
            box('Sofa_Leg', (x, y, .11), (.11, .11, .22), 'Wood', .02)
    export_asset('Sofa')


def coffee_table():
    reset_scene()
    box('CoffeeTable_Top', (0, 0, .37), (1.10, .65, .10), 'Wood', .05)
    for x in (-.43, .43):
        for y in (-.24, .24):
            box('CoffeeTable_Leg', (x, y, .175), (.10, .10, .35), 'Wood', .025)
    export_asset('CoffeeTable')


def bed():
    reset_scene()
    box('Bed_Frame', (0, 0, .125), (1.40, 2.10, .25), 'Wood', .04)
    box('Bed_Mattress', (0, -.06, .34), (1.32, 1.92, .18), 'Cream', .055)
    box('Bed_Headboard', (0, .93, .45), (1.40, .10, .50), 'Wood', .04)
    ellipsoid('Bed_PillowLeft', (-.33, .54, .465), (.50, .42, .14), 'Pillow')
    ellipsoid('Bed_PillowRight', (.33, .54, .465), (.50, .42, .14), 'Pillow')
    box('Bed_Blanket', (0, -.34, .475), (1.26, 1.10, .09), 'Blanket', .04)
    export_asset('Bed')


def kitchen_counter():
    reset_scene()
    box('Counter_Base', (0, 0, .40), (2.30, .70, .80), 'Cream', .055)
    box('Counter_Top', (0, 0, .84), (2.30, .70, .12), 'Wood', .035)
    box('Counter_SinkInset', (-.62, 0, .891), (.58, .46, .022), 'Charcoal', .014)
    box('Counter_SinkWater', (-.62, 0, .901), (.43, .32, .009), 'Water', .004)
    box('Counter_Stove', (.54, -.01, .894), (.61, .47, .016), 'Charcoal', .012)
    for x in (.39, .69):
        for y in (-.13, .13):
            cylinder('Counter_Burner', (x, y, .904), .072, .012, 'Metal', 12)
    anchor('SaladOilPlacement', (.98, .03, .92))
    export_asset('KitchenCounter')


def sink():
    reset_scene()
    box('Sink_Platform', (0, .03, .012), (.38, .33, .024), 'Cream', 0)
    box('Sink_Pedestal', (0, .03, .31), (.36, .31, .58), 'Cream', .045)
    box('Sink_Basin', (0, 0, .64), (.60, .45, .14), 'White', .05)
    box('Sink_Bowl', (0, -.025, .715), (.44, .29, .03), 'Charcoal', .018)
    box('Sink_Water', (0, -.025, .733), (.30, .18, .010), 'Water', .006)
    between('Sink_Faucet', (0, .16, .71), (0, .16, .79), .020, 'Metal')
    between('Sink_Spout', (0, .16, .79), (0, .045, .79), .020, 'Metal')
    cylinder('Sink_KnobLeft', (-.14, .16, .745), .030, .025, 'Metal', 12)
    cylinder('Sink_KnobRight', (.14, .16, .745), .030, .025, 'Metal', 12)
    export_asset('Sink')


def shower():
    reset_scene()
    box('Shower_LowPan', (0, 0, .045), (.90, .90, .09), 'Cream', .045)
    box('Shower_Drain', (0, -.08, .095), (.13, .13, .018), 'Metal', .012)
    between('Shower_Column', (0, .34, .09), (0, .34, 1.66), .028, 'Metal')
    between('Shower_Arm', (0, .34, 1.66), (0, .16, 1.66), .028, 'Metal')
    x_rotate(cylinder('Shower_Head', (0, .09, 1.64), .075, .11, 'Plastic'))
    cylinder('Shower_Mixer', (0, .31, .92), .07, .05, 'Metal', 12)
    between('Shower_Handset', (.18, .34, .82), (.18, .34, 1.08), .018, 'Metal')
    ellipsoid('Shower_HandsetHead', (.18, .34, 1.11), (.08, .08, .13), 'Plastic')
    export_asset('Shower')


for build in (toilet, salad_oil, nozzle, sofa, coffee_table, bed, kitchen_counter, sink, shower):
    build()
