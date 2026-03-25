namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	/// A 4-component integer vector. Useful for packing spatial coordinates with an additional
	/// channel (e.g., depth, LOD level, or material index).
	/// </summary>
	public struct Vector4Int {
		/// <summary>X component.</summary>
		public int X;

		/// <summary>Y component.</summary>
		public int Y;

		/// <summary>Z component.</summary>
		public int Z;

		/// <summary>W component.</summary>
		public int W;

		/// <summary>Constructs a <see cref="Vector4Int"/> from four components.</summary>
		public Vector4Int(int x, int y, int z, int w) {
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
	}

}
