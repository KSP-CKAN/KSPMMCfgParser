using System.IO;
using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    using static KSPMMCfgParserPrimitives;

    /// <summary>
    /// The kind of assignment operator present on the line
    /// </summary>
    public enum MMAssignmentOperator
    {
        /// <summary>
        /// Just a regular =
        /// </summary>
        Assign,
        /// <summary>
        /// Addition assignment with +=
        /// </summary>
        Add,
        /// <summary>
        /// Subtraction assignment with -=
        /// </summary>
        Subtract,
        /// <summary>
        /// Multiplication assignment with *=
        /// </summary>
        Multiply,
        /// <summary>
        /// Division assignment with /=
        /// </summary>
        Divide,
        /// <summary>
        /// Exponentiation assignment with !=
        /// </summary>
        Power,
        /// <summary>
        /// Regex replacement assignment with ^=
        /// </summary>
        RegexReplace,
    }

    /// <summary>
    /// Parser output object representing one line that sets a key to a value
    /// </summary>
    public class KSPConfigProperty
    {
        /// <summary>
        /// Operator for this property
        /// </summary>
        public readonly MMOperator           Operator;
        /// <summary>
        /// Name being assigned to for this property
        /// </summary>
        public readonly string?              Name;
        /// <summary>
        /// The path being assigned to for this property
        /// </summary>
        public readonly MMNodePathPiece[]?   Path;
        /// <summary>
        /// :NEEDS[] clause of this property
        /// </summary>
        public readonly MMNeedsAnd?          Needs;
        /// <summary>
        /// ,index of this property
        /// </summary>
        public readonly MMIndex?             Index;
        /// <summary>
        /// [index] of this property
        /// </summary>
        public readonly MMArrayIndex?        ArrayIndex;
        /// <summary>
        /// The assignment operator used for this property
        /// </summary>
        public readonly MMAssignmentOperator AssignmentOperator;
        /// <summary>
        /// Value being assigned for this property
        /// </summary>
        public readonly string               Value;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="op">Operator for the property</param>
        /// <param name="name">Name of variable being assigned</param>
        /// <param name="needs">:NEEDS[] clause for this property</param>
        /// <param name="index">,index for this property</param>
        /// <param name="arrayIndex">[index] for this property</param>
        /// <param name="assignOp">Assignment operator used for this property</param>
        /// <param name="value">Value being assigned for this property</param>
        public KSPConfigProperty(MMOperator           op,
                                 string               name,
                                 MMNeedsAnd?          needs,
                                 MMIndex?             index,
                                 MMArrayIndex?        arrayIndex,
                                 MMAssignmentOperator assignOp,
                                 string               value)
        {
            Operator           = op;
            Name               = name;
            Needs              = needs;
            Index              = index;
            ArrayIndex         = arrayIndex;
            AssignmentOperator = assignOp;
            Value              = value;
        }
        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="op">Operator for the property</param>
        /// <param name="path">Path of external value being assigned</param>
        /// <param name="needs">:NEEDS[] clause for this property</param>
        /// <param name="assignOp">Assignment operator used for this property</param>
        /// <param name="value">Value being assigned for this property</param>
        public KSPConfigProperty(MMOperator                   op,
                                 IEnumerable<MMNodePathPiece> path,
                                 MMNeedsAnd?                  needs,
                                 MMAssignmentOperator         assignOp,
                                 string                       value)
        {
            Operator           = op;
            Path               = path.ToArray();
            Needs              = needs;
            AssignmentOperator = assignOp;
            Value              = value;
        }
    }

    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// @+$-!%&amp;*| => enum?
        /// </summary>
        public static readonly Parser<char, MMOperator> PropertyOperator =
            Optional(CommonOperator | Char('|').Map(_ => MMOperator.Rename),
                     MMOperator.Insert);

        /// <summary>
        /// The non-space characters that are allowed in a property identifier
        /// </summary>
        private static readonly Parser<char, char> PropertyWordChar =
            LetterOrDigit() | OneOf("#_.?")
                            // Only allow these if they're not part of += -= *= /=
                            | OneOf("+-*/").Left(LookAhead(Not(Char('='))));

        /// <summary>
        /// Section of a property identifier containing spaces, always ends in a non-space
        /// </summary>
        private static readonly Parser<char, IEnumerable<char>> PropertyPiece =
            Many(Char(' ')).Append(PropertyWordChar);

        /// <summary>
        /// Full property identifier, starting and ending with non-space characters,
        /// with spaces allowed in between
        /// </summary>
        public static readonly Parser<char, string> PropertyIdentifier =
            PropertyWordChar.Append(Many(PropertyPiece).Flatten())
                            .AsString();

        private static readonly Parser<char, MMAssignmentOperator> ArithmeticAssignmentOperator =
                 Char('=').Map(_ => MMAssignmentOperator.Assign)
            | String("+=").Map(_ => MMAssignmentOperator.Add)
            | String("-=").Map(_ => MMAssignmentOperator.Subtract)
            | String("*=").Map(_ => MMAssignmentOperator.Multiply)
            | String("/=").Map(_ => MMAssignmentOperator.Divide)
            | String("!=").Map(_ => MMAssignmentOperator.Power);

        private static readonly Parser<char, MMAssignmentOperator> AssignmentOperator =
            ArithmeticAssignmentOperator
            | String("^=").Map(_ => MMAssignmentOperator.RegexReplace);

        /// <summary>
        /// Capture characters after '=' until end of line, end of node, or comment
        /// </summary>
        public static readonly Parser<char, string> PropertyValue =
            Many(NoneOf("\r\n}/")
                 // Terminate if we find // for a comment, allow single /
                 | Char('/').Left(LookAhead(Not(Char('/'))))).AsString()
                                                             .Map(v => v.Trim());

        /// <summary>
        /// Parser for a line setting a value in a ConfigNode, of the form
        /// name = value
        /// </summary>
        public static readonly Parser<char, KSPConfigProperty> NormalProperty =
            from op    in PropertyOperator
            from name  in PropertyIdentifier
            from needs in NeedsClause.AsNullable()
            from index in Index.AsNullable()
            from arIdx in ArrayIndex.AsNullable()
            from asOp  in AssignmentOperator.Between(SpacesWithinLine)
            from value in PropertyValue
            select new KSPConfigProperty(op, name, needs, index, arIdx, asOp, value);

        /// <summary>
        /// Parser for the external value access operator, starts with * followed by a node path
        /// and only uses arithmetic assignment operators
        /// </summary>
        public static readonly Parser<char, KSPConfigProperty> ExternalValueAccessProperty =
            from path  in Char('*').Right(NodePath)
            from needs in NeedsClause.AsNullable()
            from asOp  in ArithmeticAssignmentOperator.Between(SpacesWithinLine)
            from value in PropertyValue
            select new KSPConfigProperty(MMOperator.ExternalValueAccess, path, needs, asOp, value);

        /// <summary>
        /// Parser for all properties, normal or external value access
        /// </summary>
        public static readonly Parser<char, KSPConfigProperty> Property =
            ExternalValueAccessProperty | NormalProperty;
    }
}
