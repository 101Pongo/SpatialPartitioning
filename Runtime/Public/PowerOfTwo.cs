namespace Atlas.SpatialPartitioning.Runtime.Public {

	/// <summary>
	///     Common powers of two, useful for texture sizes, buffer allocations, and bit operations.
	/// </summary>
	public static class PowerOfTwo {
		// Fractions
		/// <summary>1/8 (2⁻³)</summary>
		public const float P_EIGHTH = 0.125f;

		/// <summary>1/4 (2⁻²)</summary>
		public const float P_QUARTER = 0.25f;

		/// <summary>1/2 (2⁻¹)</summary>
		public const float P_HALF = 0.5f;


		// Whole-number powers of two

		/// <summary>2⁰</summary>
		public const int P_1 = 1;

		/// <summary>2¹</summary>
		public const int P_2 = 2;

		/// <summary>2²</summary>
		public const int P_4 = 4;

		/// <summary>2³</summary>
		public const int P_8 = 8;

		/// <summary>2⁴</summary>
		public const int P_16 = 16;

		/// <summary>2⁵</summary>
		public const int P_32 = 32;

		/// <summary>2⁶</summary>
		public const int P_64 = 64;

		/// <summary>2⁷</summary>
		public const int P_128 = 128;

		/// <summary>2⁸</summary>
		public const int P_256 = 256;

		/// <summary>2⁹</summary>
		public const int P_512 = 512;

		/// <summary>2¹⁰</summary>
		public const int P_1024 = 1024;

		/// <summary>2¹¹</summary>
		public const int P_2048 = 2048;

		/// <summary>2¹²</summary>
		public const int P_4096 = 4096;

		/// <summary>2¹³</summary>
		public const int P_8192 = 8192;

		/// <summary>2¹⁴</summary>
		public const int P_16384 = 16384;

		/// <summary>2¹⁵</summary>
		public const int P_32768 = 32768;

		/// <summary>2¹⁶</summary>
		public const int P_65536 = 65536;

		/// <summary>2¹⁷</summary>
		public const int P_131072 = 131072;

		/// <summary>2¹⁸</summary>
		public const int P_262144 = 262144;

		/// <summary>2¹⁹</summary>
		public const int P_524288 = 524288;

		/// <summary>2²⁰</summary>
		public const int P_1048576 = 1048576;

		/// <summary>2²¹</summary>
		public const int P_2097152 = 2097152;
	}

}