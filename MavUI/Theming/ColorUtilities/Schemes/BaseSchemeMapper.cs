using Mav.ColorUtilities.Palettes;

namespace Mav.ColorUtilities.Schemes
{
    /// <summary>
    /// Base class for mappers.
    /// </summary>
    internal abstract class BaseSchemeMapper<TCorePalette, TScheme> : ISchemeMapper<TCorePalette, TScheme>
        where TCorePalette : CorePalette
        where TScheme : Scheme<uint>, new()
    {
        /// <summary>
        /// Maps the core palette to a new scheme.
        /// </summary>
        public TScheme Map(TCorePalette corePalette) => Map(corePalette, new());

        /// <summary>
        /// Maps the core palette onto the given scheme and returns it.
        /// </summary>
        public TScheme Map(TCorePalette corePalette, TScheme scheme)
        {
            MapCore(corePalette, scheme);
            return scheme;
        }

        /// <summary>
        /// Does the actual mapping of the core palette to the scheme.
        /// Override to add more mapping statements.
        /// </summary>
        protected abstract void MapCore(TCorePalette corePalette, TScheme scheme);
    }
}
