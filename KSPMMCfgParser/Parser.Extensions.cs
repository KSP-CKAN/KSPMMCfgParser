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

        /// <summary>
        /// Borrowed from internal ParsecSharp.ClosureOptimization.
        /// I don't understand what it does, but it's part of the
        /// implementation of AbortIfEntered.
        /// </summary>
        /// <param name="value">A value</param>
        /// <param name="_">Some parser state to be ignored</param>
        /// <returns>
        /// Parser output object representing the value
        /// </returns>
        public static T Const<T, TIgnore>(this T value, TIgnore _)
            where T : class
            => value;

        /// <summary>
        /// A clone of AbortIfEntered that doesn't add junk to its output.
        /// No user benefits from seeing "At AbortIfEntered -&gt;".
        /// </summary>
        /// <param name="parser">A parser that might fail</param>
        /// <returns>
        /// Parser output object with error message if parsing fails
        /// </returns>
        public static Parser<TToken, T> AbortQuietlyIfEntered<TToken, T>(this Parser<TToken, T> parser)
            => parser.Alternative(failure =>
                GetPosition<TToken>().Bind(position =>
                    position.Equals(failure.State.Position)
                        ? Fail<TToken, T>(failure.Message)
                        : Abort<TToken, T>(failure.Message.Const)));
    }
}
