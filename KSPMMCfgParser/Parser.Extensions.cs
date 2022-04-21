using ParsecSharp;
using static ParsecSharp.Parser;

namespace KSPMMCfgParser
{
    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// Convert parser output to dynamic type so parsers
        /// with different types can be alternated
        /// </summary>
        public static Parser<TToken, dynamic?> AsDynamic<TToken, T>(this Parser<TToken, T> parser)
            => parser.Map(x => x as dynamic);

        /// <summary>
        /// Return null if an optional part of the syntax isn't there
        /// </summary>
        public static Parser<TToken, T?> AsNullable<TToken, T>(this Parser<TToken, T> parser)
            where T : class => Optional(parser.Map(v => (T?)v), null);
    }
}
