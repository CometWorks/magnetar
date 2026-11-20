"""Run with Blender --background PlayerTeleporter.blend --python-exit-code 1 --python export.py."""
from importlib import import_module
from pathlib import Path
from shutil import copyfile
from struct import pack
from tempfile import mkdtemp

import bpy

source = Path(bpy.data.filepath).parent
root = source.parents[1]
build = root / "obj" / "teleporter"
build.mkdir(parents=True, exist_ok=True)
stage = Path(mkdtemp(prefix="export-", dir=build))
models = Path("Models/Cubes/large")
output = stage / models
output.mkdir(parents=True)
scene = bpy.context.scene
name = "MagnetarMod_PlayerTeleporter"
assert bpy.app.background and scene.seut.subtypeId == name
scene.seut.mod_path = str(stage)
scene.seut.export_exportPath = str(output)
scene.seut.export_deleteLooseFiles = False
scene.seut.export_sbc_type = "none"  # The gameplay definition is maintained in src.
for collection in bpy.data.collections:
    collection.hide_viewport = False
# SEUT reports through a popup in the UI. Preserve its messages in batch builds.
import_module("space-engineers-utilities.seut_errors").show_popup_report = lambda *args: print(*args)
area = next(a for a in bpy.context.screen.areas if a.type == "VIEW_3D")
region = next(r for r in area.regions if r.type == "WINDOW")
exporter = import_module("space-engineers-utilities.export.seut_ot_export")


class Reporter:
    def report(self, level, message):
        print(level, message, flush=True)


reporter = Reporter()
with bpy.context.temp_override(area=area, region=region):
    # Stage editable render geometry, weld geometry and the authored Havok hull.
    for export in (exporter.export_main, exporter.export_bs, exporter.export_hkt):
        assert export(reporter, bpy.context) != {"CANCELLED"}, stage
    # MWMBuilder expands SEUT's wildcard to every FBX. Keep the Havok input
    # out of that wildcard after Havok has consumed it.
    (output / (name + ".hkt.fbx")).rename(stage / (name + ".hkt.fbx"))
    exporter.export_mwm(reporter, bpy.context)

native_log = (output / (name + ".mwm.log")).read_text(errors="replace")
assert ": ERROR:" not in native_log, stage
hull = (output / (name + ".hkt")).read_bytes()
assert hull and "0 Errors, 0 Warnings" in (output / (name + ".hkt.filter.log")).read_text(), stage
tag = b"\x16HavokCollisionGeometry" + pack("<I", len(hull)) + hull
for stem in (name, name + "_BS1"):
    assert tag in (output / (stem + ".mwm")).read_bytes(), f"Missing embedded collision in {stem}"
    assert (output / (stem + ".fbx")).stat().st_size > 0
destination = root / "src" / models
destination.mkdir(parents=True, exist_ok=True)
for stem in (name, name + "_BS1"):
    copyfile(output / (stem + ".mwm"), destination / (stem + ".mwm"))
    for ext in (".fbx", ".xml"):
        copyfile(output / (stem + ext), source / (stem + ext))
copyfile(output / (name + ".hkt"), source / (name + ".hkt"))
print("Verified build and native tool logs:", stage)
