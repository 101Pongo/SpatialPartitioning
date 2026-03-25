using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	/// A bounding box expressed as a range of <see cref="Region"/> indices. Provides containment checks,
	/// overlap detection, and iteration over all encompassed regions.
	/// </summary>
	/// <remarks>
	/// Constructed from world-space positions or bounds, snapping to region boundaries automatically.
	/// Useful for defining the "active area" around a target before octree subdivision.
	/// </remarks>
	public readonly struct Domain : IEquatable<Domain>, IFormattable, IComparable<Domain> {
		private const int _MAX_REGIONS_TO_ITERATE = 100000;

		/// <summary>Minimum region index (inclusive).</summary>
		public readonly Vector3Int MinIndex;

		/// <summary>Maximum region index (inclusive).</summary>
		public readonly Vector3Int MaxIndex;

		/// <summary>World-space size of the domain.</summary>
		public readonly Vector3Int Size;

		/// <summary>World-space center of the domain.</summary>
		public readonly Vector3Int Center;

		/// <summary>World-space minimum corner.</summary>
		public readonly Vector3Int MinPosition;

		/// <summary>World-space maximum corner.</summary>
		public readonly Vector3Int MaxPosition;

		/// <summary>The domain as a Unity <see cref="UnityEngine.Bounds"/>.</summary>
		public Bounds Bounds {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(Center, Size);
		}

		/// <summary>Constructs a domain spanning from <paramref name="regionMin"/> to <paramref name="regionMax"/>.</summary>
		public Domain(Region regionMin, Region regionMax) {
			MinIndex    = regionMin.Index;
			MaxIndex    = regionMax.Index;
			MinPosition = regionMin.Min;
			MaxPosition = regionMax.Max;
			Size        = MaxPosition - MinPosition;
			Center      = MinPosition + Size / 2;
		}

		/// <summary>Constructs a domain from a bounding box, snapping to region boundaries.</summary>
		public Domain(Bounds bounds) : this(Region.FromPosition(bounds.min), Region.FromPosition(bounds.max)) { }

		/// <summary>Constructs a domain from world-space min/max positions, snapping to region boundaries.</summary>
		public Domain(Vector3 minPosition, Vector3 maxPosition) : this(
				Region.FromPosition(minPosition),
				Region.FromPosition(maxPosition)
		) { }

		/// <summary>Returns the total number of regions in this domain.</summary>
		public int RegionCount() {
			int regionCountX = MaxIndex.x - MinIndex.x + 1;
			int regionCountY = MaxIndex.y - MinIndex.y + 1;
			int regionCountZ = MaxIndex.z - MinIndex.z + 1;

			return regionCountX * regionCountY * regionCountZ;
		}

		/// <summary>Returns true if <paramref name="point"/> lies within this domain's world-space bounds.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ContainsPoint(Vector3 point) {
			if (point.x < MinPosition.x || point.x > MaxPosition.x) {
				return false;
			}

			if (point.y < MinPosition.y || point.y > MaxPosition.y) {
				return false;
			}

			if (point.z < MinPosition.z || point.z > MaxPosition.z) {
				return false;
			}

			return true;
		}

		/// <summary>Returns true if <paramref name="region"/> lies within this domain's index bounds.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ContainsRegion(Region region) {
			if (region.IndexX < MinIndex.x || region.IndexX > MaxIndex.x) {
				return false;
			}

			if (region.IndexY < MinIndex.y || region.IndexY > MaxIndex.y) {
				return false;
			}

			if (region.IndexZ < MinIndex.z || region.IndexZ > MaxIndex.z) {
				return false;
			}

			return true;
		}

		/// <summary>Returns true if any part of <paramref name="bounds"/> intersects this domain.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool OverlapsBounds(Bounds bounds) {
			if (bounds.min.x > MaxPosition.x || bounds.max.x < MinPosition.x) {
				return false;
			}

			if (bounds.min.y > MaxPosition.y || bounds.max.y < MinPosition.y) {
				return false;
			}

			if (bounds.min.z > MaxPosition.z || bounds.max.z < MinPosition.z) {
				return false;
			}

			return true;
		}

		/// <summary>Returns true if any part of <paramref name="other"/> intersects this domain.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool OverlapsTerritory(Domain other) {
			if (other.MaxIndex.x < MinIndex.x || other.MinIndex.x > MaxIndex.x) {
				return false;
			}

			if (other.MaxIndex.y < MinIndex.y || other.MinIndex.y > MaxIndex.y) {
				return false;
			}

			if (other.MaxIndex.z < MinIndex.z || other.MinIndex.z > MaxIndex.z) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Iterates through all regions encompassed by this domain.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the region count exceeds <c>100,000</c> as a safety limit.
		/// </exception>
		public IEnumerable<Region> GetAllRegions() {
			int regionCountX = MaxIndex.x - MinIndex.x + 1;
			int regionCountY = MaxIndex.y - MinIndex.y + 1;
			int regionCountZ = MaxIndex.z - MinIndex.z + 1;

			int totalRegions = regionCountX * regionCountY * regionCountZ;

			if (totalRegions > _MAX_REGIONS_TO_ITERATE) {
				throw new InvalidOperationException(
						$"The number of regions ({totalRegions}) is too large. Maximum allowed is {_MAX_REGIONS_TO_ITERATE}."
				);
			}

			for (int x = MinIndex.x; x <= MaxIndex.x; x++) {
				for (int y = MinIndex.y; y <= MaxIndex.y; y++) {
					for (int z = MinIndex.z; z <= MaxIndex.z; z++) {
						yield return new Region(x, y, z);
					}
				}
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(MinIndex, MaxIndex);

		/// <inheritdoc/>
		public bool Equals(Domain other) => MinIndex == other.MinIndex && MaxIndex == other.MaxIndex;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is Domain other && Equals(other);

		/// <inheritdoc/>
		public static bool operator ==(Domain left, Domain right) => left.Equals(right);

		/// <inheritdoc/>
		public static bool operator !=(Domain left, Domain right) => !( left == right );

		/// <summary>Compares by min index first, then max index.</summary>
		public int CompareTo(Domain other) {
			int minComparison = CompareVector3Int(MinIndex, other.MinIndex);

			if (minComparison != 0) {
				return minComparison;
			}

			return CompareVector3Int(MaxIndex, other.MaxIndex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int CompareVector3Int(Vector3Int a, Vector3Int b) {
			if (a.x != b.x) {
				return a.x.CompareTo(b.x);
			}

			return a.y != b.y ? a.y.CompareTo(b.y) : a.z.CompareTo(b.z);
		}

		/// <inheritdoc/>
		public override string ToString() => ToString("G", null);

		/// <summary>
		/// Formats this domain as a string.
		/// Supported formats: "G" (general), "MIN", "MAX", "MINMAX" (region index ranges).
		/// </summary>
		public string ToString(string format, IFormatProvider formatProvider) {
			if (string.IsNullOrEmpty(format)) {
				format = "G";
			}

			return format.ToUpperInvariant() switch {
					"G"   => $"Domain(Min: {MinIndex}, Max: {MaxIndex})",
					"MIN" => $"regionMin({MinIndex.x}, {MinIndex.y}, {MinIndex.z})",
					"MAX" => $"regionMax({MaxIndex.x}, {MaxIndex.y}, {MaxIndex.z})",
					"MINMAX" => $"regionMin({MinIndex.x}, {MinIndex.y}, {MinIndex.z}), "
							+ $"regionMax({MaxIndex.x}, {MaxIndex.y}, {MaxIndex.z})",
					_ => throw new FormatException($"The format '{format}' is not supported.")
			};
		}

		/// <summary>Draws a wireframe cube gizmo for editor visualization.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DrawGizmo() {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(Center, Size);
		}
	}

}
