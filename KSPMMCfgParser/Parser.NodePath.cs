using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{

    /// <summary>
    /// Parser output object representing one /-delimited piece of a node path
    /// </summary>
    public class MMNodePathPiece
    {
        /// <summary>
        /// The Module Manager operator for this piece
        /// </summary>
        public readonly MMOperator Operator;
        /// <summary>
        /// Name of this piece
        /// </summary>
        public readonly string     Name;
        /// <summary>
        /// Module Manager Filters for this piece
        /// </summary>
        public readonly string[]?  Filters;
        /// <summary>
        /// Module Manager :HAS[] clasue for this piece
        /// </summary>
        public readonly MMHas?     Has;
        /// <summary>
        /// Module Manager ,index for this piece
        /// </summary>
        public readonly MMIndex?   Index;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="op">Operator used in this piece</param>
        /// <param name="name">ConfigNode name matched by this piece</param>
        /// <param name="filters">Filters for this piece</param>
        /// <param name="has">:HAS[] clause for this piece</param>
        /// <param name="index">Module Manager ,index for this piece</param>
        public MMNodePathPiece(MMOperator           op,
                               string               name,
                               IEnumerable<string>? filters,
                               MMHas?               has,
                               MMIndex?             index)
        {
            Operator = op;
            Name     = name;
            Filters  = filters?.ToArray();
            Has      = has;
            Index    = index;
        }

        /// <summary>
        /// Representation of a ".." piece
        /// </summary>
        public static readonly MMNodePathPiece DotDot = new MMNodePathPiece(
            MMOperator.ParentNode, "", null, null, null);
    }

    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// Parser for a node part of a # paste operator string
        /// </summary>
        public static readonly Parser<char, MMNodePathPiece> NodePathPieceNode =
            from op        in Optional(Char('@').Map(_ => MMOperator.PathRoot),
                                       MMOperator.PathRelative)
            from name      in NodeIdentifier
            from filters   in Filters.AsNullable()
            from suffixes  in Many(HasClause!.AsDynamic()
                                   | Index!.AsDynamic())
            select new MMNodePathPiece(op, name, filters,
                                       FindSuffix<MMHas>(suffixes),
                                       FindSuffix<MMIndex>(suffixes));

        /// <summary>
        /// Parser for a piece of a # paste operator string, either
        /// a node or .. for parent node context
        /// </summary>
        public static readonly Parser<char, MMNodePathPiece> NodePathPiece =
            String("..").Map(_ => MMNodePathPiece.DotDot) | NodePathPieceNode;

        /// <summary>
        /// Argument of the paste operator
        /// </summary>
        public static readonly Parser<char, IEnumerable<MMNodePathPiece?>> NodePath =
            // Inject a null to signify the missing piece from an absolute path
            Optional(Char('/').Map(_ => Enumerable.Repeat((MMNodePathPiece?)null, 1)),
                     Enumerable.Empty<MMNodePathPiece?>())
            .Append(NodePathPiece.Map(v => (MMNodePathPiece?)v)
                                 .SeparatedBy(Char('/')));
    }
}
