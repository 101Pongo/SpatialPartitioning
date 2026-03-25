using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	/// Represents an octant in a 3D octree structure — a cubic partition of space defined entirely
	/// by its minimum corner and edge length. Octants are value types with arithmetic hierarchy
	/// traversal: parent, child, sibling, and root lookups require no pointer chasing.
	/// </summary>
	/// <remarks>
	/// Implements <see cref="IEnumerable{T}"/> over child octants for convenient iteration.
	/// All spatial coordinates are integer-based to guarantee deterministic hashing and set membership.
	/// </remarks>
	public readonly struct Octant : IEquatable<Octant>, IFormattable, IComparable<Octant>, IEnumerable<Octant> {
		/// <summary>The size of the root octant, corresponding to the size of a <see cref="Region"/>.</summary>
		public static int RootSize => Region.Factor;

		/// <summary>Depth-indexed gizmo colors for editor visualization.</summary>
		public static readonly Color[] OctantGizmoColors = {
				WithAlpha(Color.black, 0.1f),
				WithAlpha(Color.gray, 0.1f),
				WithAlpha(Color.white, 0.2f),
				WithAlpha(Color.green, 0.2f),
				WithAlpha(Color.yellow, 0.3f),
				WithAlpha(Color.red, 0.5f),
				Color.magenta
		};

		/// <summary>Returns a copy of <paramref name="color"/> with the specified alpha.</summary>
		public static Color WithAlpha(Color color, float alpha) {
			color.a = alpha;

			return color;
		}

		/// <summary>Constructs an octant from a minimum point and edge length.</summary>
		/// <param name="min">Minimum corner of the octant in world coordinates.</param>
		/// <param name="size">Edge length of the cubic octant.</param>
		public Octant(Vector3Int min, int size) : this(min.x, min.y, min.z, size) { }

		/// <summary>Constructs an octant from minimum x, y, z coordinates and edge length.</summary>
		/// <param name="minX">Minimum X coordinate.</param>
		/// <param name="minY">Minimum Y coordinate.</param>
		/// <param name="minZ">Minimum Z coordinate.</param>
		/// <param name="size">Edge length of the cubic octant.</param>
		public Octant(int minX, int minY, int minZ, int size) {
			MinX      = minX;
			MinY      = minY;
			MinZ      = minZ;
			this.Size = size;
		}

		/// <summary>Minimum corner of the octant.</summary>
		public Vector3Int Min {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(MinX, MinY, MinZ);
		}

		/// <summary>Center point of the octant.</summary>
		public Vector3Int Center {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(CenterX, CenterY, CenterZ);
		}

		/// <summary>Maximum corner of the octant (exclusive).</summary>
		public Vector3Int Max {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(MaxX, MaxY, MaxZ);
		}

		/// <summary>Edge length of this octant.</summary>
		public int Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Half the edge length of this octant.</summary>
		public int HalfSize {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Size / 2;
		}

		/// <summary>Size as a <see cref="Vector3Int"/> for use with Unity APIs.</summary>
		public Vector3Int SizeVector {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(Size, Size, Size);
		}

		/// <summary>Half-size as a <see cref="Vector3Int"/>.</summary>
		public Vector3Int HalfSizeVector {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				int halfSize = HalfSize;

				return new Vector3Int(halfSize, halfSize, halfSize);
			}
		}

		/// <summary>Minimum X coordinate.</summary>
		public int MinX { get; }

		/// <summary>Minimum Y coordinate.</summary>
		public int MinY { get; }

		/// <summary>Minimum Z coordinate.</summary>
		public int MinZ { get; }

		/// <summary>Center X coordinate.</summary>
		public int CenterX => MinX + HalfSize;

		/// <summary>Center Y coordinate.</summary>
		public int CenterY => MinY + HalfSize;

		/// <summary>Center Z coordinate.</summary>
		public int CenterZ => MinZ + HalfSize;

		/// <summary>Maximum X coordinate (exclusive).</summary>
		public int MaxX => MinX + Size;

		/// <summary>Maximum Y coordinate (exclusive).</summary>
		public int MaxY => MinY + Size;

		/// <summary>Maximum Z coordinate (exclusive).</summary>
		public int MaxZ => MinZ + Size;

		/// <summary>Depth of this octant in the hierarchy, where 0 is root. Computed from <c>log2(RootSize / Size)</c>.</summary>
		public int Depth {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Mathf.FloorToInt(Mathf.Log(RootSize / (float)Size, 2));
		}

		/// <summary>True if this octant is at the root level (size equals <see cref="RootSize"/>).</summary>
		public bool IsRoot {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Size == RootSize;
		}

		/// <summary>Returns the first child octant (minimum corner, half size).</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Octant GetFirstChild() => new(Min, HalfSize);

		/// <summary>Returns all eight child octants produced by subdividing this octant.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Octant[] GetChildren() {
			int half = HalfSize;

			return new[] {
					new Octant(MinX, MinY, MinZ, half),
					new Octant(MinX             + half, MinY, MinZ, half),
					new Octant(MinX, MinY       + half, MinZ, half),
					new Octant(MinX             + half, MinY + half, MinZ, half),
					new Octant(MinX, MinY, MinZ + half, half),
					new Octant(MinX             + half, MinY, MinZ + half, half),
					new Octant(MinX, MinY       + half, MinZ       + half, half),
					new Octant(MinX             + half, MinY       + half, MinZ + half, half)
			};
		}

		/// <summary>Returns the parent octant that contains this one. Returns self if already root.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Octant GetParent() {
			if (IsRoot) {
				return this;
			}

			int parentSize = Size                                       * 2;
			int parentX    = Mathf.FloorToInt((float)MinX / parentSize) * parentSize;
			int parentY    = Mathf.FloorToInt((float)MinY / parentSize) * parentSize;
			int parentZ    = Mathf.FloorToInt((float)MinZ / parentSize) * parentSize;

			return new Octant(parentX, parentY, parentZ, parentSize);
		}

		/// <summary>Returns all sibling octants (the eight children of this octant's parent).</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Octant[] GetSiblings() => GetParent().GetChildren();

		/// <summary>Returns the root-level octant that contains this one. Returns self if already root.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Octant GetRoot() {
			if (IsRoot) {
				return this;
			}

			int rootX = Mathf.FloorToInt((float)MinX / RootSize) * RootSize;
			int rootY = Mathf.FloorToInt((float)MinY / RootSize) * RootSize;
			int rootZ = Mathf.FloorToInt((float)MinZ / RootSize) * RootSize;

			return new Octant(rootX, rootY, rootZ, RootSize);
		}

		/// <summary>Returns the <see cref="Region"/> that contains this octant's minimum corner.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Region GetRegion() => Region.FromPosition(MinX, MinY, MinZ);

		/// <summary>Returns true if <paramref name="other"/> is entirely contained within this octant.</summary>
		public bool IsAncestorOf(Octant other) => other.MinX >= MinX && other.MaxX <= MaxX &&
				other.MinY                                   >= MinY && other.MaxY <= MaxY &&
				other.MinZ                                   >= MinZ && other.MaxZ <= MaxZ;

		/// <summary>Returns true if this octant is entirely contained within <paramref name="other"/>.</summary>
		public bool IsDescendantOf(Octant other) => other.IsAncestorOf(this);

		/// <summary>Returns true if <paramref name="position"/> lies within this octant (min-inclusive, max-exclusive).</summary>
		public bool Contains(Vector3 position) => position.x >= MinX && position.x < MaxX &&
				position.y                                   >= MinY && position.y < MaxY &&
				position.z                                   >= MinZ && position.z < MaxZ;

		/// <summary>Returns true if any part of <paramref name="bounds"/> intersects this octant.</summary>
		public bool Overlaps(Bounds bounds) => !( bounds.min.x >= MaxX || bounds.max.x <= MinX ||
				bounds.min.y                                   >= MaxY || bounds.max.y <= MinY ||
				bounds.min.z                                   >= MaxZ || bounds.max.z <= MinZ );

		/// <summary>Enumerates the eight child octants.</summary>
		public IEnumerator<Octant> GetEnumerator() {
			foreach (Octant child in GetChildren()) {
				yield return child;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(MinX, MinY, MinZ, Size);

		/// <inheritdoc/>
		public bool Equals(Octant other)
			=> MinX == other.MinX && MinY == other.MinY && MinZ == other.MinZ && Size == other.Size;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is Octant other && Equals(other);

		/// <inheritdoc/>
		public static bool operator ==(Octant a, Octant b)
			=> a.MinX == b.MinX && a.MinY == b.MinY && a.MinZ == b.MinZ && a.Size == b.Size;

		/// <inheritdoc/>
		public static bool operator !=(Octant a, Octant b) => !( a == b );

		/// <summary>Compares by size first, then by X, Y, Z coordinates.</summary>
		public int CompareTo(Octant other) {
			int sizeComparison = Size.CompareTo(other.Size);

			if (sizeComparison != 0) {
				return sizeComparison;
			}

			int xComparison = MinX.CompareTo(other.MinX);

			if (xComparison != 0) {
				return xComparison;
			}

			int yComparison = MinY.CompareTo(other.MinY);

			if (yComparison != 0) {
				return yComparison;
			}

			return MinZ.CompareTo(other.MinZ);
		}

		/// <inheritdoc/>
		public override string ToString() => ToString("G", null);

		/// <summary>
		/// Formats this octant as a string. Supported formats: "G" (general), "MINMAX" (min/max corners).
		/// </summary>
		public string ToString(string format, IFormatProvider formatProvider) {
			if (string.IsNullOrEmpty(format)) {
				format = "G";
			}

			return format.ToUpperInvariant() switch {
					"G" or "F" => $"Octant(Min: ({MinX}, {MinY}, {MinZ}), Size: {Size})",
					"MINMAX"   => $"Min({MinX}, {MinY}, {MinZ}), Max({MaxX}, {MaxY}, {MaxZ})",
					_          => throw new FormatException($"The format '{format}' is not supported.")
			};
		}

		/// <summary>Draws a depth-colored wireframe cube gizmo for editor visualization.</summary>
		/// <param name="minDepth">Minimum depth to draw (inclusive).</param>
		/// <param name="maxDepth">Maximum depth to draw (inclusive).</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawGizmo(int minDepth = 0, int maxDepth = 100) {
			int depth = Depth;

			if (depth < minDepth || depth > maxDepth) {
				return;
			}

			Gizmos.color = depth switch {
					0 => OctantGizmoColors[0],
					1 => OctantGizmoColors[1],
					2 => OctantGizmoColors[2],
					3 => OctantGizmoColors[3],
					4 => OctantGizmoColors[4],
					5 => OctantGizmoColors[5],
					6 => OctantGizmoColors[6],
					_ => OctantGizmoColors[0]
			};

			Gizmos.DrawWireCube(Center, SizeVector);
		}
	}

}
