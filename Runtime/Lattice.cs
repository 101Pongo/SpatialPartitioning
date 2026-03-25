using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	/// A generic 3D grid that partitions a world-space volume into cells, each storing data of type
	/// <typeparamref name="T"/>. Supports both uniform and non-uniform division spacing per axis.
	/// </summary>
	/// <remarks>
	/// Unlike the octree hierarchy, a lattice is a flat grid — useful for building footprints, room zones,
	/// per-cell terrain metadata, or any scenario where you need "a 3D grid of T with world-space bounds."
	/// Position and size are mutable post-creation so the lattice can be repositioned without reallocation.
	/// </remarks>
	/// <typeparam name="T">The type of data stored in each cell.</typeparam>
	public class Lattice<T> : IEquatable<Lattice<T>>, IFormattable, IComparable<Lattice<T>> {
		private readonly T[,,] _data;

		private readonly float[] _xDivisions;
		private readonly float[] _yDivisions;
		private readonly float[] _zDivisions;

		private Vector3Int _position;
		private Vector3Int _size;

		/// <summary>Constructs a lattice with uniform divisions on all axes.</summary>
		/// <param name="position">World-space origin (minimum corner).</param>
		/// <param name="size">World-space dimensions.</param>
		/// <param name="uniformSegments">Number of segments per axis.</param>
		public Lattice(Vector3Int position, Vector3Int size, int uniformSegments) : this(
				position,
				size,
				uniformSegments,
				uniformSegments,
				uniformSegments
		) { }

		/// <summary>Constructs a lattice with per-axis uniform segment counts.</summary>
		/// <param name="position">World-space origin (minimum corner).</param>
		/// <param name="size">World-space dimensions.</param>
		/// <param name="xSegments">Number of segments on the X axis.</param>
		/// <param name="ySegments">Number of segments on the Y axis.</param>
		/// <param name="zSegments">Number of segments on the Z axis.</param>
		public Lattice(Vector3Int position, Vector3Int size, int xSegments, int ySegments, int zSegments) {
			this._position  = position;
			this._size      = size;
			this.XSegments = xSegments;
			this.YSegments = ySegments;
			this.ZSegments = zSegments;

			_xDivisions = CreateUniformDivisions(xSegments);
			_yDivisions = CreateUniformDivisions(ySegments);
			_zDivisions = CreateUniformDivisions(zSegments);

			_data = new T[xSegments, ySegments, zSegments];
		}

		/// <summary>Constructs a lattice with non-uniform division points per axis (normalized 0–1 range).</summary>
		/// <param name="position">World-space origin (minimum corner).</param>
		/// <param name="size">World-space dimensions.</param>
		/// <param name="xDivisions">Normalized division points on the X axis. Length determines segment count (length + 1).</param>
		/// <param name="yDivisions">Normalized division points on the Y axis.</param>
		/// <param name="zDivisions">Normalized division points on the Z axis.</param>
		public Lattice(
				Vector3Int position,
				Vector3Int size,
				float[] xDivisions,
				float[] yDivisions,
				float[] zDivisions
		) {
			this._position = position;
			this._size     = size;

			this._xDivisions = (float[])xDivisions.Clone();
			this._yDivisions = (float[])yDivisions.Clone();
			this._zDivisions = (float[])zDivisions.Clone();

			XSegments = xDivisions.Length + 1;
			YSegments = yDivisions.Length + 1;
			ZSegments = zDivisions.Length + 1;

			_data = new T[XSegments, YSegments, ZSegments];
		}

		/// <summary>World-space origin (minimum corner). Mutable to allow repositioning without reallocation.</summary>
		public Vector3Int Position {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _position = value;
		}

		/// <summary>World-space dimensions. Mutable to allow resizing without reallocation.</summary>
		public Vector3Int Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _size;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _size = value;
		}

		/// <summary>Minimum corner (same as <see cref="Position"/>).</summary>
		public Vector3Int Min {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position;
		}

		/// <summary>Maximum corner.</summary>
		public Vector3Int Max {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position + _size;
		}

		/// <summary>Center point.</summary>
		public Vector3Int Center {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position + _size / 2;
		}

		/// <summary>Number of segments on the X axis.</summary>
		public int XSegments {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Number of segments on the Y axis.</summary>
		public int YSegments {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Number of segments on the Z axis.</summary>
		public int ZSegments {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		/// <summary>Total number of cells (<c>XSegments * YSegments * ZSegments</c>).</summary>
		public int CellCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => XSegments * YSegments * ZSegments;
		}

		/// <summary>Gets or sets data at the specified grid indices.</summary>
		public T this[int x, int y, int z] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _data[x, y, z];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _data[x, y, z] = value;
		}

		/// <summary>Compares by position, then size, then segment counts.</summary>
		public int CompareTo(Lattice<T> other) {
			int posComparison = CompareVector3Int(_position, other._position);

			if (posComparison != 0) {
				return posComparison;
			}

			int sizeComparison = CompareVector3Int(_size, other._size);

			if (sizeComparison != 0) {
				return sizeComparison;
			}

			int xComp = XSegments.CompareTo(other.XSegments);

			if (xComp != 0) {
				return xComp;
			}

			int yComp = YSegments.CompareTo(other.YSegments);

			if (yComp != 0) {
				return yComp;
			}

			return ZSegments.CompareTo(other.ZSegments);
		}

		/// <inheritdoc/>
		public bool Equals(Lattice<T> other) => _position == other._position && _size == other._size &&
				XSegments == other.XSegments && YSegments == other.YSegments && ZSegments == other.ZSegments;

		/// <summary>
		/// Formats this lattice as a string.
		/// Supported formats: "G" (general), "BOUNDS" (min/max corners), "SEGMENTS" (segment counts).
		/// </summary>
		public string ToString(string format, IFormatProvider formatProvider) {
			if (string.IsNullOrEmpty(format)) {
				format = "G";
			}

			return format.ToUpperInvariant() switch {
					"G"        => $"Lattice<{typeof(T).Name}>({_position}, {_size}, {XSegments}x{YSegments}x{ZSegments})",
					"BOUNDS"   => $"Min({_position}), Max({Max})",
					"SEGMENTS" => $"Segments({XSegments}, {YSegments}, {ZSegments})",
					_          => throw new FormatException($"The format '{format}' is not supported.")
			};
		}

		/// <summary>Creates uniform division points for the given number of segments.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float[] CreateUniformDivisions(int segments) {
			if (segments <= 1) {
				return Array.Empty<float>();
			}

			var divisions = new float[segments - 1];

			for (var i = 0; i < divisions.Length; i++) {
				divisions[i] = (float)( i + 1 ) / segments;
			}

			return divisions;
		}

		/// <summary>Returns the world-space minimum corner of the cell at the given indices.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 GetCellMin(int x, int y, int z) => new(
				_position.x + GetAxisPosition(x, _xDivisions, _size.x),
				_position.y + GetAxisPosition(y, _yDivisions, _size.y),
				_position.z + GetAxisPosition(z, _zDivisions, _size.z)
		);

		/// <summary>Returns the world-space maximum corner of the cell at the given indices.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 GetCellMax(int x, int y, int z) => new(
				_position.x + GetAxisPosition(x + 1, _xDivisions, _size.x),
				_position.y + GetAxisPosition(y + 1, _yDivisions, _size.y),
				_position.z + GetAxisPosition(z + 1, _zDivisions, _size.z)
		);

		/// <summary>Returns the world-space center of the cell at the given indices.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 GetCellCenter(int x, int y, int z) => ( GetCellMin(x, y, z) + GetCellMax(x, y, z) ) * 0.5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float GetAxisPosition(int index, float[] divisions, float axisSize) {
			if (index <= 0) {
				return 0f;
			}

			if (index > divisions.Length) {
				return axisSize;
			}

			return divisions[index - 1] * axisSize;
		}

		/// <summary>
		/// Converts a world position to grid indices. Returns false if the position is outside the lattice.
		/// </summary>
		/// <param name="worldPos">World-space position to look up.</param>
		/// <param name="x">Output X index.</param>
		/// <param name="y">Output Y index.</param>
		/// <param name="z">Output Z index.</param>
		/// <returns>True if the position is within the lattice bounds.</returns>
		public bool TryGetIndices(Vector3 worldPos, out int x, out int y, out int z) {
			Vector3 localPos = worldPos - _position;

			if (localPos.x < 0 || localPos.x >= _size.x ||
				localPos.y < 0 || localPos.y >= _size.y ||
				localPos.z < 0 || localPos.z >= _size.z) {
				x = y = z = -1;

				return false;
			}

			x = GetAxisIndex(localPos.x / _size.x, _xDivisions);
			y = GetAxisIndex(localPos.y / _size.y, _yDivisions);
			z = GetAxisIndex(localPos.z / _size.z, _zDivisions);

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetAxisIndex(float normalizedPos, float[] divisions) {
			for (var i = 0; i < divisions.Length; i++) {
				if (normalizedPos < divisions[i]) {
					return i;
				}
			}

			return divisions.Length;
		}

		/// <summary>Returns true if <paramref name="worldPos"/> lies within the lattice bounds.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector3 worldPos) {
			Vector3 localPos = worldPos - _position;

			return localPos.x  >= 0 && localPos.x < _size.x &&
					localPos.y >= 0 && localPos.y < _size.y &&
					localPos.z >= 0 && localPos.z < _size.z;
		}

		/// <summary>Returns true if any part of <paramref name="bounds"/> intersects the lattice.</summary>
		public bool Overlaps(Bounds bounds) {
			Vector3 min = _position;
			Vector3 max = _position + _size;

			return !( bounds.min.x >= max.x || bounds.max.x <= min.x ||
					bounds.min.y   >= max.y || bounds.max.y <= min.y ||
					bounds.min.z   >= max.z || bounds.max.z <= min.z );
		}

		/// <summary>Resets all cells to <c>default(T)</c>.</summary>
		public void Clear() {
			for (var x = 0; x < XSegments; x++) {
				for (var y = 0; y < YSegments; y++) {
					for (var z = 0; z < ZSegments; z++) {
						_data[x, y, z] = default;
					}
				}
			}
		}

		/// <summary>Iterates every cell, providing indices, world-space center, and current value.</summary>
		/// <param name="action">Callback receiving (x, y, z, worldCenter, value).</param>
		public void ForEach(Action<int, int, int, Vector3, T> action) {
			for (var x = 0; x < XSegments; x++) {
				for (var y = 0; y < YSegments; y++) {
					for (var z = 0; z < ZSegments; z++) {
						Vector3 worldPos = GetCellCenter(x, y, z);
						action(x, y, z, worldPos, _data[x, y, z]);
					}
				}
			}
		}

		/// <summary>
		/// Populates every cell by invoking <paramref name="resolver"/> with the cell's indices,
		/// world-space center, and current value. The return value replaces the cell's data.
		/// </summary>
		/// <param name="resolver">Function receiving (x, y, z, worldCenter, currentValue) and returning the new value.</param>
		public void Resolve(Func<int, int, int, Vector3, T, T> resolver) {
			for (var x = 0; x < XSegments; x++) {
				for (var y = 0; y < YSegments; y++) {
					for (var z = 0; z < ZSegments; z++) {
						Vector3 worldPos = GetCellCenter(x, y, z);
						_data[x, y, z] = resolver(x, y, z, worldPos, _data[x, y, z]);
					}
				}
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(_position, _size, XSegments, YSegments, ZSegments);

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is Lattice<T> other && Equals(other);

		/// <inheritdoc/>
		public static bool operator ==(Lattice<T> left, Lattice<T> right) => left.Equals(right);

		/// <inheritdoc/>
		public static bool operator !=(Lattice<T> left, Lattice<T> right) => !left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int CompareVector3Int(Vector3Int a, Vector3Int b) {
			if (a.x != b.x) {
				return a.x.CompareTo(b.x);
			}

			return a.y != b.y ? a.y.CompareTo(b.y) : a.z.CompareTo(b.z);
		}

		/// <inheritdoc/>
		public override string ToString() => ToString("G", null);
	}

}
