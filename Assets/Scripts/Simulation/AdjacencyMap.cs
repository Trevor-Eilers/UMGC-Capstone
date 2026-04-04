// Author: Malcolm Bramble

/// <summary>
/// District adjacency model for the four-quadrant city layout.
/// Quadrants: 0=NW, 1=NE, 2=SW, 3=SE.
/// Direct neighbors (shared border) = weight 1.0.
/// Diagonal neighbors (shared corner only) = weight 0.5.
/// </summary>
public static class AdjacencyMap
{
    public struct DistrictPair
    {
        public int indexA;
        public int indexB;
        public float weight;

        public DistrictPair(int a, int b, float w)
        {
            indexA = a;
            indexB = b;
            weight = w;
        }
    }

    // All six pairs in the four-quadrant grid
    public static readonly DistrictPair[] AllPairs = new DistrictPair[]
    {
        // Direct neighbors (share a full border): weight 1.0
        new DistrictPair(0, 1, 1.0f),   // NW ↔ NE (north border)
        new DistrictPair(0, 2, 1.0f),   // NW ↔ SW (west border)
        new DistrictPair(1, 3, 1.0f),   // NE ↔ SE (east border)
        new DistrictPair(2, 3, 1.0f),   // SW ↔ SE (south border)

        // Diagonal neighbors (share downtown corner only): weight 0.5
        new DistrictPair(0, 3, 0.5f),   // NW ↔ SE (diagonal)
        new DistrictPair(1, 2, 0.5f),   // NE ↔ SW (diagonal)
    };

    /// <summary>
    /// Returns the adjacency weight between two districts.
    /// Returns 0 if the pair is not adjacent (should not happen in 4-quadrant model).
    /// </summary>
    public static float GetWeight(int a, int b)
    {
        for (int i = 0; i < AllPairs.Length; i++)
        {
            var p = AllPairs[i];
            if ((p.indexA == a && p.indexB == b) || (p.indexA == b && p.indexB == a))
                return p.weight;
        }
        return 0f;
    }

    /// <summary>
    /// Returns indices of all neighbors for a given district.
    /// In the 4-quadrant model, every district neighbors every other district.
    /// </summary>
    public static int[] GetNeighbors(int districtIndex)
    {
        switch (districtIndex)
        {
            case 0: return new[] { 1, 2, 3 };
            case 1: return new[] { 0, 2, 3 };
            case 2: return new[] { 0, 1, 3 };
            case 3: return new[] { 0, 1, 2 };
            default: return new int[0];
        }
    }
}
