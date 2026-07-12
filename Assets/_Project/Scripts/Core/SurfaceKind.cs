namespace Nordo.Core
{
    /// <summary>
    /// The material category a footstep lands on. Shared by the audio layer (which picks the
    /// right footstep bank) and, from Milestone 5, the noise system (different surfaces carry
    /// sound differently — snow muffles, metal rings). Keeping this in Core lets both sides
    /// agree on a vocabulary without depending on each other.
    /// </summary>
    public enum SurfaceKind
    {
        /// <summary>Fallback used when nothing more specific is identified.</summary>
        Generic = 0,
        Wood = 1,
        Concrete = 2,
        Carpet = 3,
        Metal = 4,
        Dirt = 5,
        Tile = 6,
        Snow = 7,
        Gravel = 8,
        Water = 9
    }
}
