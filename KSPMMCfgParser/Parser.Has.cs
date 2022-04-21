using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    /// <summary>
    /// Whether the piece starts with @, !, #, or ~
    /// </summary>
    public enum MMHasType
    {
        /// <summary>
        /// For pieces starting with @
        /// </summary>
        Node,
        /// <summary>
        /// For pieces starting with !
        /// </summary>
        NoNode,
        /// <summary>
        /// For pieces starting with #
        /// </summary>
        Property,
        /// <summary>
        /// For pieces starting with ~
        /// </summary>
        NoProperty,
    }

    /// <summary>
    /// Parser output object representing the lowest level of a :HAS[] clause
    /// </summary>
    public class MMHasPiece
    {
        /// <summary>
        /// Whether to match a node, absence of node, property, or absence of property
        /// </summary>
        public readonly MMHasType HasType;
        /// <summary>
        /// The name to match
        /// </summary>
        public readonly string    Key;
        /// <summary>
        /// The value to match, if any
        /// </summary>
        public readonly string?   Value;
        /// <summary>
        /// Recursive :HAS[] clause applied to matching nodes
        /// </summary>
        public readonly MMHas     HasClause;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="hasType">What kind of object to match</param>
        /// <param name="key">Name of object to match</param>
        /// <param name="value">Value to match</param>
        /// <param name="hasClause">Recursive :HAS[] clause to apply</param>
        public MMHasPiece(MMHasType hasType, string key, string value, MMHas hasClause)
        {
            HasType   = hasType;
            Key       = key;
            Value     = value;
            HasClause = hasClause;
        }
    }

    /// <summary>
    /// Parser output object representing a complete :HAS[] clause
    /// containing multiple pieces separated by , or &amp;
    /// </summary>
    public class MMHas
    {
        /// <summary>
        /// The pieces to match
        /// </summary>
        public readonly MMHasPiece[] Pieces;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="pieces">Pieces contained in this :HAS[] clause</param>
        public MMHas(IEnumerable<MMHasPiece> pieces)
        {
            Pieces = pieces.ToArray();
        }
    }

    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// Parser matching a name that may contain wildcards
        /// </summary>
        public static readonly Parser<char, string> HasIdentifier = Many1(Char('_')
                                                                          | LetterOrDigit()).AsString();

        /// <summary>
        /// :HAS[@MODULE[ModuleName],!MODULE[BadModule]:HAS[@BADPROP[BadVal],#PROP[PropPal]]
        /// </summary>
        public static readonly Parser<char, MMHas> HasClause =
            Fix<char, MMHas>(hasClause =>
            {
                // @MODULE[ModuleName]:HAS[etc...]
                var HasPiece =
                    from hasType  in   Char('@').Map(_ => MMHasType.Node)
                                     | Char('!').Map(_ => MMHasType.NoNode)
                                     | Char('#').Map(_ => MMHasType.Property)
                                     | Char('~').Map(_ => MMHasType.NoProperty)
                    from hasKey   in HasIdentifier
                    from hasValue in Many(NoneOf("]")).Between(Char('['),
                                                               Char(']'))
                                                      .AsString()
                                                      .AsNullable()
                    from subHas   in hasClause.AsNullable()
                    select new MMHasPiece(hasType, hasKey, hasValue, subHas);

                return from _open  in String(":HAS[")
                       from pieces in HasPiece.SeparatedBy(OneOf("&,"))
                       from _close in Char(']')
                       select new MMHas(pieces);
            });
    }
}
