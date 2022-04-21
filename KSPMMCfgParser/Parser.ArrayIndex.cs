using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    using static KSPMMCfgParserPrimitives;

    /// <summary>
    /// Parser output object representing a Module Manager
    /// array index such as [2] or [*] or [3, ]
    /// </summary>
    public class MMArrayIndex
    {
        /// <summary>
        /// The character to use for splitting the target value into pieces
        /// </summary>
        public readonly char Separator;
        /// <summary>
        /// The piece to target, or null for all
        /// </summary>
        public readonly int? Value;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="sep">Separator to use</param>
        /// <param name="val">Index to use</param>
        public MMArrayIndex(char sep, int? val)
        {
            Separator = sep;
            Value     = val;
        }
    }

    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// Parser for Module Manager array index of the form
        /// [2] or [1, ]
        /// </summary>
        public static readonly Parser<char, MMArrayIndex> ArrayIndex =
            from _open     in Char('[')
            from index     in Integer.Map(v => (int?)v)
                              | Char('*').Map(_ => (int?)null)
            from separator in Optional(Char(',').Right(Any()),
                                       ',')
            from _close    in Char(']')
            select new MMArrayIndex(separator, index);
    }
}
