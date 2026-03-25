# Spatial Partitioning

A lightweight, deterministic spatial description system for Unity, built for procedural content generation.

## Why This Exists

I needed a way to describe 3D space that was cheap enough to run every frame, deterministic across sessions, and simple enough to drive downstream systems without becoming one itself. Most spatial partitioning libraries are built around runtime insertion and querying — you put objects in, you ask what's nearby. That's great for physics broadphase, but it's the wrong abstraction for procedural generation where the space itself is the data and the content doesn't exist yet.

This library doesn't store objects. It describes space: where regions are, how they subdivide, and what changed since last frame. Downstream systems subscribe to those changes and decide what to do — spawn terrain, generate buildings, place vegetation, whatever. The partitioning layer stays dumb and fast.

## Architecture

The hierarchy is three levels: **Domain → Region → Octant**.

**Region** is the fundamental unit — a fixed-size cubic cell on an integer grid, defined by its index. The default size is 1024 units (a power of two, chosen so octant subdivision always lands on integer boundaries). Regions are value types, hashable, comparable, and free to create — they're just three ints and some arithmetic.

**Domain** is a bounding box expressed as a range of regions. Given a min and max world position, it snaps to region boundaries and lets you iterate over every region inside. It's the "what part of the world are we looking at" query.

**Octant** is a node in an octree rooted at a region. Each region can subdivide recursively into octants, halving in size at each level. Octants are also value types — they're defined entirely by their min corner and size, so you can reconstruct any octant from those four ints without traversing a tree. Parent, child, sibling, and root lookups are all arithmetic, no pointer chasing.

**AdaptiveOctree** is the orchestrator. It takes a target position (typically the camera or player), builds a set of regions around it, and subdivides each region's octree based on distance thresholds — closer to the target means deeper subdivision. Then it diffs the result against the previous frame and exposes added/removed sets for both regions and octants. Downstream systems read those sets, react, and call `ClearDirty()` when they're done.

The adaptive octree is intentionally static. This was designed for a single-focus-point scenario (one camera, one player) where the entire system is a global service. If you needed multiple independent octrees, you'd refactor to instance-based — but for the procgen use case that motivated this, a static API kept the call sites clean.

## Design Decisions

**Integer coordinates everywhere.** Regions, octants, and domains all use `Vector3Int`. This eliminates floating-point drift, makes hashing exact, and means two systems evaluating the same position will always agree on which cell it belongs to. Determinism was non-negotiable — procedural generation that produces different results on different machines is a bug.

**Power-of-two sizing.** Region size is 1024, octant subdivision halves each level. This guarantees every boundary lands on an integer. It also means depth can be computed from size with a single log2, and child/parent relationships are just bit shifts conceptually.

**Value-type spatial primitives.** Region and Octant are `readonly struct` with proper `IEquatable<T>` and `GetHashCode`. They live in `HashSet<T>` for the diff tracking, so avoiding boxing and getting correct equality was critical. You can create millions of these per frame without GC pressure.

**Dirty tracking, not events.** Instead of firing callbacks when regions change, the system accumulates added/removed sets that consumers poll. This avoids allocation from delegates, gives consumers control over when they process changes, and makes the update order explicit. It's a pull model — the spatial system doesn't know or care what's listening.

**Distance-threshold subdivision.** The `Thresholds` array defines how far from the target each depth level extends. This gives you LOD-like behavior: dense subdivision near the camera, coarse far away. The thresholds are expressed in world units as multiples of `Region.Factor`, so they scale with your world size.

## The Lattice

`Lattice<T>` is a separate tool in the same package — a generic 3D grid with configurable non-uniform divisions. Where the octree describes space hierarchically, the lattice describes it as a flat grid of cells, each storing arbitrary data.

The use case is different: you might use a lattice to define a building footprint, divide a room into zones, or store per-cell metadata for a chunk of terrain. It supports both uniform and non-uniform division spacing, world-space position lookups, and a `Resolve` method that lets you populate every cell based on its position.

It's here because I kept needing "a 3D grid of T with world-space bounds" alongside the octree, and the two share the same spatial conventions.

## What This Is For

- **Procedural world generation** — drive chunk loading, terrain generation, or structure placement from spatial changes
- **LOD management** — use octant depth as a natural LOD signal
- **Deterministic spatial queries** — given the same position and thresholds, the output is always identical
- **Lightweight runtime budgets** — no allocations during steady-state updates, integer math only for spatial lookups

## Structure

```
Runtime/
  Public/
    AdaptiveOctree.cs   — Static orchestrator, distance-based subdivision, dirty tracking
    Region.cs           — Fixed-size grid cell, integer coordinates
    Octant.cs           — Octree node, value type, arithmetic hierarchy traversal
    Domain.cs           — Bounding box as a range of regions
    Lattice.cs          — Generic 3D grid with non-uniform divisions
    PowerOfTwo.cs       — Named constants for power-of-two values
    Vector4Int.cs       — 4-component integer vector
```

## Dependencies

- Unity (uses `Vector3`, `Vector3Int`, `Bounds`, `Mathf`, `Gizmos`)
- No third-party packages
