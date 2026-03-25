# Spatial Partitioning

A lightweight spatial partitioning system for Unity, built for procedural content generation.

## Why I Built This

Most spatial partitioning libraries are designed around storing objects and querying them — you insert things, then ask "what's near this point?" That's useful for physics or AI, but it wasn't what I needed.

I was working on procedural generation and needed something that could describe space itself. Not "what objects are here," but "what chunks of space exist around the player, and which ones just appeared or disappeared?" The idea was that downstream systems — terrain, buildings, vegetation, whatever — could listen for those changes and spawn content in response. The partitioning layer just figures out the spatial layout and stays out of the way.

I also needed it to be deterministic. Given the same position and settings, it should always produce the same result, on any machine. That ruled out floating-point coordinates for the spatial structure, so everything is integer-based.

## How It Works

There are three main pieces: **Regions**, **Octants**, and the **AdaptiveOctree** that ties them together.

**Regions** are the base grid. The world is divided into fixed-size cubic cells (1024 units by default), and each one is identified by an integer index — like tile coordinates, but in 3D. They're cheap to create and compare since they're just three ints under the hood.

**Octants** subdivide a region. Each region can be split into eight smaller cubes, and those can split again, and so on. An octant is defined by its corner position and size — four ints total — so you can figure out its parent, children, or siblings with just math. There's no tree of linked nodes to walk through.

**AdaptiveOctree** is the thing you actually call each frame. You give it a position (usually the player or camera), and it figures out which regions are nearby and how deeply to subdivide them. Regions close to the target get subdivided more, regions far away stay coarse. Then it compares the result to last frame and tells you what changed — which regions and octants were added or removed. Your other systems read those changes, do their thing, and call `ClearDirty()` when they're done.

The octree is static (one global instance) because I only needed one focus point. If you needed multiple cameras or independent octrees, you'd refactor it to be instance-based, but for a single-player procgen scenario this kept things simple.

## Why It's Designed This Way

**Integers everywhere.** Floating-point math can give slightly different results on different hardware. For procedural generation that's a problem — if two systems disagree about which cell a position belongs to, you get mismatched content. Integer coordinates eliminate that entirely.

**Power-of-two sizes.** Regions are 1024 units, and octants halve each level. This means every subdivision boundary is always a whole number. It also makes the math clean — depth is just a log2 of the size ratio.

**Value types.** Regions and Octants are structs, not classes. They go in HashSets for the change tracking, so they need to be fast to hash and compare, and they shouldn't create garbage collection pressure. You can create a lot of these per frame without worrying about it.

**Pull-based change tracking.** Instead of firing events when things change, the system just builds up sets of what was added and removed. Consumers check those sets when they're ready. This avoids allocations from delegates and lets each system decide when to process updates.

## The Lattice

There's also a `Lattice<T>` in here — a simple 3D grid where each cell stores whatever data type you want. It supports both uniform and non-uniform spacing between cells.

I kept building this alongside the octree because I kept needing "a box divided into a grid, where each cell holds some data." Things like defining zones within a building footprint, or storing per-cell metadata for a terrain chunk. It shares the same spatial conventions as the rest of the package.

## What It's For

- Driving procedural generation from spatial changes (chunks loading/unloading, terrain, structures)
- LOD-like behavior using octant depth as a distance signal
- Any scenario where you need deterministic, repeatable spatial queries
- Lightweight enough to run every frame without allocations during steady state

## Structure

```
Runtime/
  Public/
    AdaptiveOctree.cs   — The main system. Subdivides space around a target, tracks changes.
    Region.cs           — A fixed-size grid cell on an integer grid.
    Octant.cs           — A node in the octree. Just a corner + size, all math, no pointers.
    Domain.cs           — A bounding box expressed as a range of regions.
    Lattice.cs          — A generic 3D grid with configurable divisions.
    PowerOfTwo.cs       — Named constants for power-of-two values.
    Vector4Int.cs       — 4-component integer vector.
```

## Dependencies

Unity only — no third-party packages.
