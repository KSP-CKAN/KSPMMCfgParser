using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    /// <summary>
    /// The operation to perform on a node or property
    /// </summary>
    public enum MMOperator
    {
        /// <summary>
        /// No operator present, default is to create a new node or property
        /// </summary>
        Insert,
        /// <summary>
        /// @ present to edit existing node or property
        /// </summary>
        Edit,
        /// <summary>
        /// + or $ present to copy existing node or property
        /// </summary>
        Copy,
        /// <summary>
        /// - or ! present to delete existing node or property
        /// </summary>
        Delete,
        /// <summary>
        /// % present to edit existing node or property or create a new one if none found
        /// </summary>
        EditOrCreate,
        /// <summary>
        /// &amp; present to create new node or property
        /// </summary>
        Create,
        /// <summary>
        /// | present to rename a property's parent node
        /// </summary>
        Rename,
        /// <summary>
        /// Normal matching for a paste piece
        /// </summary>
        PathRelative,
        /// <summary>
        /// @ inside of a # node path
        /// </summary>
        PathRoot,
        /// <summary>
        /// .. inside of a # node path
        /// </summary>
        ParentNode,
        /// <summary>
        /// * present for external value access operator
        /// </summary>
        ExternalValueAccess,
    }

    /// <summary>
    /// This is a separate class from KSPConfigParser to ensure that its static
    /// members are initialized before they are used.
    /// </summary>
    public static partial class KSPMMCfgParserPrimitives
    {
        /// <summary>
        /// Some mods have Zero Width No-Break Space randomly pasted in their cfgs,
        /// and Char.IsWhiteSpace doesn't consider them whitespace.
        /// </summary>
        private static readonly Parser<char, char> BOM = Char('\ufeff');
        /// <summary>
        /// Parser matching whitespace other than newlines
        /// </summary>
        public static readonly Parser<char, Unit> SpacesWithinLine = SkipMany(OneOf(" \t") | BOM);

        /// <summary>
        /// Parser matching comment starting with // and ending at end of line
        /// </summary>
        public static readonly Parser<char, string> Comment =
            String("//").Right(Many(NoneOf("\r\n")))
                        .AsString();

        /// <summary>
        /// Parser matching whitespace containing comments
        /// </summary>
        public static readonly Parser<char, Unit> JunkBlock = SkipMany(Comment.Ignore()
                                                                       | WhiteSpace().Ignore()
                                                                       | BOM.Ignore());

        /// <summary>
        /// Parser matching whitespace between tokens with at least one newline,
        /// including optional comments
        /// </summary>
        public static readonly Parser<char, Unit> AtLeastOneEOL = SpacesWithinLine.Left(Optional(Comment))
                                                                                  .Left(EndOfLine())
                                                                                  .Left(JunkBlock);

        /// <summary>
        /// @+$-!%&amp; => enum
        /// </summary>
        public static readonly Parser<char, MMOperator> CommonOperator =
                Char('@').Map(_ => MMOperator.Edit)
            | OneOf("+$").Map(_ => MMOperator.Copy)
            | OneOf("-!").Map(_ => MMOperator.Delete)
            |   Char('%').Map(_ => MMOperator.EditOrCreate)
            |   Char('&').Map(_ => MMOperator.Create);

        /// <summary>
        /// Parser matching an integer, possibly negative
        /// </summary>
        public static readonly Parser<char, int> Integer =
            Optional(Char('-'), '+').Append(Many1(DecDigit()))
                                    .ToInt();
    }
}
