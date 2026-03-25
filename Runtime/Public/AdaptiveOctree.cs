using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	/// Static orchestrator that subdivides space into <see cref="Region"/>s and <see cref="Octant"/>s
	/// around a target position, using distance-based thresholds for adaptive LOD. Tracks frame-to-frame
	/// changes via added/removed sets so downstream systems can react without polling.
	/// </summary>
	/// <remarks>
	/// The static design is intentional — this serves a single-focus-point scenario (one camera or player).
	/// Call <see cref="UpdateTarget"/> each frame, read the dirty sets, then call <see cref="ClearDirty"/>.
	/// </remarks>
	public static class AdaptiveOctree {
		/// <summary>
		/// Distance thresholds that control subdivision depth. Each entry defines how far from the target
		/// the corresponding octree depth extends. Expressed as multiples of <see cref="Region.Factor"/>.
		/// </summary>
		public static readonly int[] Thresholds = {
				Region.Factor * 12,
				Region.Factor * 4,
				Region.Factor * 2,
				Region.Factor,
				Region.HalfFactor
		};

		private static readonly HashSet<Region> _constructionRegions = new();
		private static readonly HashSet<Octant> _constructionNodes = new();

		private static Domain _domain;

		private static readonly HashSet<Region> _regions = new();
		private static readonly HashSet<Octant> _octants = new();

		private static readonly HashSet<Region> _addedRegions = new();
		private static readonly HashSet<Region> _removedRegions = new();
		private static readonly HashSet<Octant> _addedNodes = new();
		private static readonly HashSet<Octant> _removedNodes = new();

		private static int _leafOctantCount;

		/// <summary>Total number of leaf (non-subdivided) octants in the current tree.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeafOctantCount() => _leafOctantCount;

		/// <summary>Total number of active regions.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int RegionCount() => _regions.Count;

		/// <summary>Total number of active octants (including non-leaf).</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int OctantCount() => _octants.Count;

		/// <summary>Returns true if any regions or octants were added or removed since the last <see cref="ClearDirty"/> call.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AnyDirty()
			=> _addedRegions.Any() || _removedRegions.Any() || _addedNodes.Any() || _removedNodes.Any();

		/// <summary>Returns true if <paramref name="octant"/> was added or removed since the last clear.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsOctantDirty(Octant octant)
			=> _addedNodes.Contains(octant) || _removedNodes.Contains(octant);

		/// <summary>Returns true if <paramref name="region"/> was added or removed since the last clear.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRegionDirty(Region region)
			=> _addedRegions.Contains(region) || _removedRegions.Contains(region);

		/// <summary>Returns true if <paramref name="octant"/> exists in the current tree.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsOctant(Octant octant) => _octants.Contains(octant);

		/// <summary>Returns true if <paramref name="region"/> exists in the current tree.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsRegion(Region region) => _regions.Contains(region);

		/// <summary>Returns true if <paramref name="octant"/> has no children in the current tree.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLeafOctant(Octant octant) => !_octants.Contains(octant.GetFirstChild());

		/// <summary>Regions added during the last <see cref="UpdateTarget"/> call.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<Region> GetAddedRegions() => _addedRegions;

		/// <summary>Regions removed during the last <see cref="UpdateTarget"/> call.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<Region> GetRemovedRegions() => _removedRegions;

		/// <summary>Octants added during the last <see cref="UpdateTarget"/> call.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<Octant> GetAddedOctants() => _addedNodes;

		/// <summary>Octants removed during the last <see cref="UpdateTarget"/> call.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IEnumerable<Octant> GetRemovedOctants() => _removedNodes;

		/// <summary>
		/// Clears all dirty state. Call this after downstream systems have processed the added/removed sets.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearDirty() {
			_constructionRegions.Clear();
			_constructionNodes.Clear();
			_addedRegions.Clear();
			_removedRegions.Clear();
			_addedNodes.Clear();
			_removedNodes.Clear();
		}

		/// <summary>
		/// Rebuilds the octree around <paramref name="target"/>, subdividing up to <paramref name="divisions"/>
		/// levels deep based on distance thresholds. Populates the added/removed dirty sets.
		/// </summary>
		/// <param name="target">World-space focus point (typically camera or player position).</param>
		/// <param name="divisions">Maximum subdivision depth (clamped by <see cref="Thresholds"/> length).</param>
		public static void UpdateTarget(Vector3 target, int divisions = 4) {
			_leafOctantCount = 0;
			_constructionRegions.Clear();
			_constructionNodes.Clear();

			float   threshold = Thresholds[0];
			Vector3 targetMin = target - new Vector3(threshold, threshold, threshold);
			Vector3 targetMax = target + new Vector3(threshold, threshold, threshold);

			_domain = new Domain(targetMin, targetMax);

			foreach (Region region in _domain.GetAllRegions()) {
				_constructionRegions.Add(region);
				Octant rootOctant = region.GetRootNode();
				SubdivideRecursively(rootOctant, _constructionNodes, target, 0, divisions);
			}

			ApplyChanges(_constructionRegions, _constructionNodes);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SubdivideRecursively(
				Octant octant,
				HashSet<Octant> newNodes,
				Vector3 target,
				int currentDepth,
				int maxDepth
		) {
			newNodes.Add(octant);

			if (!ShouldSubdivide(octant.Center, target, currentDepth) || currentDepth >= maxDepth) {
				_leafOctantCount++;

				return;
			}

			foreach (Octant child in octant.GetChildren()) {
				SubdivideRecursively(child, newNodes, target, currentDepth + 1, maxDepth);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ShouldSubdivide(Vector3 nodeCenter, Vector3 target, int depth) => depth < Thresholds.Length
				&& Vector3.Distance(nodeCenter, target)                                             < Thresholds[depth];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ApplyChanges(HashSet<Region> newRegions, HashSet<Octant> newOctants) {
			RecordChanges(_regions, newRegions, _addedRegions, _removedRegions);
			RecordChanges(_octants, newOctants, _addedNodes, _removedNodes);

			_regions.Clear();
			_regions.UnionWith(newRegions);

			_octants.Clear();
			_octants.UnionWith(newOctants);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void RecordChanges<T>(
				HashSet<T> oldSet,
				HashSet<T> newSet,
				HashSet<T> addedSet,
				HashSet<T> removedSet
		) {
			foreach (T item in newSet.Except(oldSet)) {
				addedSet.Add(item);
			}
			foreach (T item in oldSet.Except(newSet)) {
				removedSet.Add(item);
			}
		}

		/// <summary>Draws gizmos for the current domain and all leaf octants. Editor only.</summary>
		public static void DrawGizmos() {
			_domain.DrawGizmo();

			foreach (Octant octant in _octants) {
				if (IsLeafOctant(octant)) {
					octant.DrawGizmo(0, 5);
				}
			}
		}
	}

}
