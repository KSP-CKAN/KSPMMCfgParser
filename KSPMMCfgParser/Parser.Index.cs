using ParsecSharp;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    using static KSPMMCfgParserPrimitives;

    /// <summary>
    /// Parser output object representing a Module Manager index
    /// of the form ,2 or ,*
    /// </summary>
    public class MMIndex
    {
        /// <summary>
        /// The value to match, if any
        /// </summary>
        public readonly int? Value;
        /// <summary>
        /// A convenient static object representing ,*
        /// </summary>
        public static readonly MMIndex All = new MMIndex(null);

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="value">The index if any, or null for *</param>
        public MMIndex(int? value)
        {
            Value = value;
        }

        /// <summary>
        /// Check whether this index matches a given element of a sequence
        /// </summary>
        /// <param name="index">Which sequence element to consider</param>
        /// <param name="totalCount">Length of the containing sequence (needed because negative indices count back from the end)</param>
        /// <returns>
        /// true if matching, false otherwise
        /// </returns>
        public bool Satisfies(int index, int totalCount)
        {
            // Null matches everything
            return !Value.HasValue
                || (Value.Value >= 0
                    ? index == Value.Value
                    // Negative starts from end
                    : index == Value.Value + totalCount);
        }
    }

    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// ,index
        /// </summary>
        public static readonly Parser<char, MMIndex> Index =
            Char(',').Right(Char('*').Map(_ => MMIndex.All)
                            | Integer.Map(v => new MMIndex(v)));
    }
}
