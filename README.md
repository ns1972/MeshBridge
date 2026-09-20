[README.md](https://github.com/user-attachments/files/32440584/README.md)
# MeshBridge

A companion mod for **Procedural Objects** in Cities: Skylines 1.

Bring your own 3D models into the game, and stop PO losing objects when an asset goes missing.

**Requires [Procedural Objects](https://steamcommunity.com/sharedfiles/filedetails/?id=1094334744).** MeshBridge does nothing without it.

---

## ⚠ Read this first

This is an early release. It has been tested thoroughly, but on **one machine only** — mine. Different hardware, different mod lists, and cities I did not build are all unknown territory.

**Back up your city before using it.** Not the autosave. A real copy you can go back to.

---

## What it does

### 1. Your models, in your city

Drop an `.obj` or `.fbx` in a folder, press Refresh, and place it. It arrives as a normal Procedural Object — movable, rotatable, vertex-editable, saved with your city like anything else.

No Asset Editor. No `.crp`. No restarting the game to see it.

- Multi-material models are split into parts and grouped automatically
- Textures come from your MTL, or from a file named with a `_d` suffix
- Transparent glass works — set Alpha below 1 in Blender and that is all
- Models keep the position, scale and rotation you gave them in Blender

### 2. Your city stops losing things

This half matters whether or not you ever import a model.

Procedural Objects finds its base assets by name. If an asset you converted to a PO is ever unsubscribed, disabled, or breaks, those objects fail to load — and the next save quietly deletes them. Autosave counts. Most people find out long after it happened.

MeshBridge:

- Writes a record into your save of exactly which assets your procedural objects need
- Checks it on load, **about eight seconds before PO restores anything**, while nothing has been lost yet
- Tells you which asset is missing and how many objects depend on it
- Can **stand in** for the missing asset entirely, using geometry and textures captured earlier, so PO carries on as though it were still installed

Press **Protect city** once and MeshBridge copies the geometry, textures and shaders behind every base your POs use into its own store. Verified against a real Workshop building: asset disabled in Content Manager, and the building still rendered complete.

### 3. Move creations between cities

PO's own export saves a selection's layout and edits, but not the geometry it needs, so it arrives hollow somewhere else. MeshBridge packages the export together with the geometry and textures, so it rebuilds properly in another city.

---

## Installing

Copy the `MeshBridge` folder into:

```
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\
```

Enable it in Content Manager → Mods, then load a city. It creates `Import\`, `Store\`, `Logs\`, `PO Assets\` and `Diagnostics\` beside itself on first run.

---

## Using it

`Ctrl+Shift+B` opens the window. There is also a small draggable status button:

| | |
|---|---|
| **OK** | nothing wrong |
| **STAND-IN** | something is being substituted, or something you captured is no longer backed up |
| **MISSING** | a dependency is absent — **do not save this city** |

The window only opens by itself on MISSING, because that is the one state where you have seconds rather than minutes.

Text size is adjustable in the window and it remembers. Useful on a 4K monitor.

---

## Authoring models

MeshBridge gives you exactly what you exported, so anything wrong in Blender arrives wrong in the game.

| | |
|---|---|
| **Scale** | real-world size, in metres. Nothing is rescaled. |
| **Position** | at the origin. Nothing is recentred. |
| **Rotation** | upright, facing forward, and **apply your transforms**. An unapplied rotation arrives baked in. |
| **Materials** | one per intended part. |
| **Glass** | Alpha below 1. `0.3` gives 30% transparent. |
| **Textures** | a diffuse map with a `_d` suffix is picked up automatically. |
| **Export** | OBJ with materials, Forward −Z, Up Y. |
| **Size** | 65,534 vertices is the hard engine limit per part. Far less is wiser — PO's vertex editor draws one handle per vertex. |

If a model looks wrong in game, open it in Blender first. Most "import bugs" turn out to be the source file.

---

## What it will not do

- It cannot recover objects PO has already dropped from a save. The warning exists so that never happens; there is no cure afterwards.
- It cannot capture an asset that was already gone before you pressed Protect city.
- Stand-ins cover geometry, textures, shader and colour — not sub-buildings, props, paths, LODs or simulation behaviour.
- Captured Workshop geometry is for **your own recovery**. It is someone else's work. Please do not redistribute it.

---

## Reporting a problem

Press **Diagnostics** → **Create report** in the mod. It writes a zip containing the logs, your store and package state, and a short form to fill in. Your Windows account name is stripped from the file paths.

**Do it in the same session the problem happened** — the game overwrites its own log every launch, so quitting first destroys the evidence.

Then [open an issue](https://github.com/ns1972/MeshBridge/issues) and attach the zip.

---

## Compatibility

MeshBridge is a companion mod, not a fork. It calls only Procedural Objects' public API — no Harmony, no patching, and none of PO's source is copied. PO is left exactly as it is, which is the point: this mod's reliability depends on PO not changing underneath it.

Built and tested against PO 1.7.8 and Cities: Skylines 1.21.1-f9 (Unity 5.6.7f1).

### A note on which Procedural Objects you have

Developed against Workshop item `3071492847`, which reports itself as version 1.7.8.

```
SHA-256  17ea6f4986f295d2c95eb42a2963c84eaf00b57702ebee80c32fe05b7fc51eb2
MVID     954c4fee-271f-42e3-929e-b2f3ccc7b644
```

That copy is a fork, and it differs from upstream in one way that matters here: its `PopupStart` class has no `OnGUI` and no `DrawUpdateUI`, so the keep-or-discard prompt PO normally shows after a failed restore never appears. `RegisterFailure` still runs and `keep` still defaults to `false`, which means on that build any save after a failed load silently discards the affected objects, with nothing on screen to tell you.

That gap is why MeshBridge raises its own warning before PO's restore runs.

If your PO hashes differently from the above, please say so in an issue.

---

## Licence and credit

MeshBridge is by **Night Hare**. Copyright © 2026 Night Hare. Free to use.

Procedural Objects is by **simon56modder** and is CC-BY-NC. MeshBridge calls its public API only and contains none of its source.
