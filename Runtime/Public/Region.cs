using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	/// A fixed-size cubic cell on an integer grid, identified by its (X, Y, Z) index.
	/// Regions are the fundamental spatial unit of the partitioning system — all positions snap
	/// to region boundaries, and each region serves as the root of an <see cref="Octant"/> hierarchy.
	/// </summary>
	/// <remarks>
	/// Region size defaults to 1024 units (a power of two) so that octant subdivision always
	/// lands on integer boundaries. All coordinates are integer-based for deterministic hashing.
	/// </remarks>
	public readonly struct Region : IEquatable<Region>, IFormattable, IComparable<Region> {
		/// <summary>Edge length of every region. Must be a power of two.</summary>
		public const int Factor = 1024;

		/// <summary>Half of <see cref="Factor"/>.</summary>
		public static int HalfFactor => Factor / 2;

		/// <summary>Size as a <see cref="Vector3Int"/>.</summary>
		public static Vector3Int SizeVector => new(Factor, Factor, Factor);

		/// <summary>Half-size as a <see cref="Vector3Int"/>.</summary>
		public static Vector3Int HalfSizeVector => new(HalfFactor, HalfFactor, HalfFactor);

		/// <summary>Constructs a region from a grid index.</summary>
		/// <param name="index">Grid index (X, Y, Z).</param>
		public Region(Vector3Int index) : this(index.x, index.y, index.z) { }

		/// <summary>Constructs a region from individual grid indices.</summary>
		/// <param name="indexX">Grid index on the X axis.</param>
		/// <param name="indexY">Grid index on the Y axis.</param>
		/// <param name="indexZ">Grid index on the Z axis.</param>
		public Region(int indexX, int indexY, int indexZ) {
			this.IndexX = indexX;
			this.IndexY = indexY;
			this.IndexZ = indexZ;
		}

		/// <summary>Grid index as a <see cref="Vector3Int"/>.</summary>
		public Vector3Int Index {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(IndexX, IndexY, IndexZ);
		}

		/// <summary>Grid index on the X axis.</summary>
		public int IndexX {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Grid index on the Y axis.</summary>
		public int IndexY {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Grid index on the Z axis.</summary>
		public int IndexZ {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Minimum corner in world coordinates.</summary>
		public Vector3Int Min {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(MinX, MinY, MinZ);
		}

		/// <summary>Center point in world coordinates.</summary>
		public Vector3Int Center {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(CenterX, CenterY, CenterZ);
		}

		/// <summary>Size of this region (always equal to <see cref="SizeVector"/>).</summary>
		public Vector3Int Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => SizeVector;
		}

		/// <summary>Maximum corner in world coordinates (exclusive).</summary>
		public Vector3Int Max {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(MaxX, MaxY, MaxZ);
		}

		/// <summary>Minimum X in world coordinates.</summary>
		public int MinX => IndexX * Factor;

		/// <summary>Minimum Y in world coordinates.</summary>
		public int MinY => IndexY * Factor;

		/// <summary>Minimum Z in world coordinates.</summary>
		public int MinZ => IndexZ * Factor;

		/// <summary>Center X in world coordinates.</summary>
		public int CenterX => IndexX * Factor + HalfFactor;

		/// <summary>Center Y in world coordinates.</summary>
		public int CenterY => IndexY * Factor + HalfFactor;

		/// <summary>Center Z in world coordinates.</summary>
		public int CenterZ => IndexZ * Factor + HalfFactor;

		/// <summary>Maximum X in world coordinates (exclusive).</summary>
		public int MaxX => IndexX * Factor + Factor;

		/// <summary>Maximum Y in world coordinates (exclusive).</summary>
		public int MaxY => IndexY * Factor + Factor;

		/// <summary>Maximum Z in world coordinates (exclusive).</summary>
		public int MaxZ => IndexZ * Factor + Factor;

		/// <summary>Creates a region from a world position by flooring to the containing grid cell.</summary>
		/// <param name="x">World X coordinate.</param>
		/// <param name="y">World Y coordinate.</param>
		/// <param name="z">World Z coordinate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Region FromPosition(float x, float y, float z) => new(
				Mathf.FloorToInt(x / Factor),
				Mathf.FloorToInt(y / Factor),
				Mathf.FloorToInt(z / Factor)
		);

		/// <summary>Creates a region from a world position by flooring to the containing grid cell.</summary>
		/// <param name="position">World position.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Region FromPosition(Vector3 position) => new(
				Mathf.FloorToInt(position.x / Factor),
				Mathf.FloorToInt(position.y / Factor),
				Mathf.FloorToInt(position.z / Factor)
		);

		/// <summary>Returns the root <see cref="Octant"/> for this region's octree hierarchy.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Octant GetRootNode() => new(MinX, MinY, MinZ, Octant.RootSize);

		/// <summary>Returns true if <paramref name="position"/> lies within this region (min-inclusive, max-exclusive).</summary>
		public bool Contains(Vector3 position) => position.x >= MinX && position.x < MaxX &&
				position.y                                   >= MinY && position.y < MaxY &&
				position.z                                   >= MinZ && position.z < MaxZ;

		/// <summary>Returns true if any part of <paramref name="bounds"/> intersects this region.</summary>
		public bool Overlaps(Bounds bounds) => !( bounds.min.x >= MaxX || bounds.max.x <= MinX ||
				bounds.min.y                                   >= MaxY || bounds.max.y <= MinY ||
				bounds.min.z                                   >= MaxZ || bounds.max.z <= MinZ );

		/// <summary>Returns true if any part of <paramref name="region"/> intersects this region.</summary>
		public bool Overlaps(Region region) => !( region.MinX >= MaxX || region.MaxX <= MinX ||
				region.MinY                                   >= MaxY || region.MaxY <= MinY ||
				region.MinZ                                   >= MaxZ || region.MaxZ <= MinZ );

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(IndexX, IndexY, IndexZ);

		/// <inheritdoc/>
		public bool Equals(Region other) => IndexX == other.IndexX && IndexY == other.IndexY && IndexZ == other.IndexZ;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is Region other && Equals(other);

		/// <inheritdoc/>
		public static bool operator ==(Region a, Region b)
			=> a.IndexX == b.IndexX && a.IndexY == b.IndexY && a.IndexZ == b.IndexZ;

		/// <inheritdoc/>
		public static bool operator !=(Region a, Region b) => !( a == b );

		/// <summary>Compares by grid index: X, then Y, then Z.</summary>
		public int CompareTo(Region other) {
			int xComparison = IndexX.CompareTo(other.IndexX);

			if (xComparison != 0) {
				return xComparison;
			}

			int yComparison = IndexY.CompareTo(other.IndexY);

			if (yComparison != 0) {
				return yComparison;
			}

			return IndexZ.CompareTo(other.IndexZ);
		}

		/// <inheritdoc/>
		public override string ToString() => ToString("G", null);

		/// <summary>
		/// Formats this region as a string. Supported formats: "G" (general), "MINMAX" (world-space corners).
		/// </summary>
		public string ToString(string format, IFormatProvider formatProvider) {
			if (string.IsNullOrEmpty(format)) {
				format = "G";
			}

			return format.ToUpperInvariant() switch {
					"G" or "F" => $"Region({IndexX}, {IndexY}, {IndexZ})",
					"MINMAX"   => $"Min({MinX}, {MinY}, {MinZ}), Max({MaxX}, {MaxY}, {MaxZ})",
					_          => throw new FormatException($"The format '{format}' is not supported.")
			};
		}

		/// <summary>Draws a wireframe cube gizmo for editor visualization.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawGizmo() {
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(Center, SizeVector);
		}
	}

}
