using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

// https://www.youtube.com/watch?v=6Zv-0ElF0fM
// https://github.com/acple/ParsecSharp
// https://www.cs.nott.ac.uk/~pszgmh/monparsing.pdf

// https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook
// https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Syntax
// https://github.com/sarbian/ModuleManager/wiki/Patch-Ordering
// https://forum.kerbalspaceprogram.com/index.php?/topic/50533-*/&do=findComment&comment=2413546

namespace KSPMMCfgParser
{
    using static KSPMMCfgParserPrimitives;

    /// <summary>
    /// Parser output object representing a ConfigNode
    /// </summary>
    public class KSPConfigNode
    {
        /// <summary>
        /// The Module Manager operator for this ConfigNode
        /// </summary>
        public readonly MMOperator          Operator;
        /// <summary>
        /// Name of this ConfigNode
        /// </summary>
        public readonly string              Name;
        /// <summary>
        /// Module Manager Filters for this ConfigNode
        /// </summary>
        public readonly string[]?           Filters;
        /// <summary>
        /// Module Manager :NEEDS[] clause for this ConfigNode
        /// </summary>
        public readonly MMNeedsAnd?         Needs;
        /// <summary>
        /// Module Manager :HAS[] clasue for this ConfigNode
        /// </summary>
        public readonly MMHas?              Has;
        /// <summary>
        /// Module Manager ,index for this ConfigNode
        /// </summary>
        public readonly MMIndex?            Index;
        /// <summary>
        /// Properties contained within this ConfigNode
        /// </summary>
        public readonly KSPConfigProperty[] Properties;
        /// <summary>
        /// ConfigNodes contained within this ConfigNode
        /// </summary>
        public readonly KSPConfigNode[]     Children;
        /// <summary>
        /// Pasted nodes contained in this ConfigNode
        /// </summary>
        public readonly MMPasteNode[]       Pastes;

        /// <summary>
        /// Is the :FIRST suffix set?
        /// </summary>
        public readonly bool    First;
        /// <summary>
        /// :BEFORE[] suffix for this ConfigNode
        /// </summary>
        public readonly string? Before;
        /// <summary>
        /// :FOR[] suffix for this ConfigNode
        /// </summary>
        public readonly string? For;
        /// <summary>
        /// :AFTER[] suffix for this ConfigNode
        /// </summary>
        public readonly string? After;
        /// <summary>
        /// :LAST[] suffix for this ConfigNode
        /// </summary>
        public readonly string? Last;
        /// <summary>
        /// Is the :FINAL suffix set?
        /// </summary>
        public readonly bool    Final;

        /// <summary>
        /// Initialize the ConfigNode object
        /// </summary>
        /// <param name="op">Module Manager operator for this ConfigNode</param>
        /// <param name="name">Name for this ConfigNode</param>
        /// <param name="filters">Filters for this ConfigNode</param>
        /// <param name="needs">:NEEDS[] clause for this ConfigNode</param>
        /// <param name="has">:HAS[] clause for this ConfigNode</param>
        /// <param name="first">true if :FIRST is present, false otherwise</param>
        /// <param name="before">:BEFORE[] clause for this ConfigNode</param>
        /// <param name="forMod">:FOR[] clause for this ConfigNode</param>
        /// <param name="after">:AFTER[] clause for this ConfigNode</param>
        /// <param name="last">:LAST[] clause for this ConfigNode</param>
        /// <param name="finalPass">true if :FINAL is present, false otherwise</param>
        /// <param name="index">Module Manager ,index for this ConfigNode</param>
        /// <param name="props">Properties contained within this ConfigNode</param>
        /// <param name="children">ConfigNodes contained within this ConfigNode</param>
        /// <param name="pastes">Pasted notes contained in this ConfigNode</param>
        public KSPConfigNode(MMOperator                     op,
                             string                         name,
                             IEnumerable<string>?           filters,
                             MMNeedsAnd?                    needs,
                             MMHas?                         has,
                             bool                           first,
                             string?                        before,
                             string?                        forMod,
                             string?                        after,
                             string?                        last,
                             bool                           finalPass,
                             MMIndex?                       index,
                             IEnumerable<KSPConfigProperty> props,
                             IEnumerable<KSPConfigNode>     children,
                             IEnumerable<MMPasteNode>       pastes)
        {
            Operator   = op;
            Name       = name;
            Filters    = filters?.ToArray();
            Needs      = needs;
            Has        = has;
            First      = first;
            Before     = before;
            For        = forMod;
            After      = after;
            Last       = last;
            Final      = finalPass;
            Index      = index;
            Properties = props.ToArray();
            Children   = children.ToArray();
            Pastes     = pastes.ToArray();
        }
    }

    /// <summary>
    /// Parser output object representing a clause after a ConfigNode
    /// of the form :Label[Value]
    /// </summary>
    public class MMNodeSuffix
    {
        /// <summary>
        /// Part of the clause after the colon, like FOR or AFTER
        /// </summary>
        public readonly string Label;
        /// <summary>
        /// Value of the clause in brackets
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="label">Part of the clause after the colon</param>
        /// <param name="value">Part of the clause in brackets</param>
        public MMNodeSuffix(string label, string value = "")
        {
            Label = label;
            Value = value;
        }
    }

    /// <summary>
    /// Parser output object representing one whole paste directive, including its :NEEDS[] if present
    /// </summary>
    public class MMPasteNode
    {
        /// <summary>
        /// Path of the node to copy
        /// </summary>
        public readonly MMNodePathPiece[] Path;
        /// <summary>
        /// Module Manager :NEEDS[] clause for this ConfigNode
        /// </summary>
        public readonly MMNeedsAnd?       Needs;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="path">Path of node this paste copies</param>
        /// <param name="needs">:NEEDS[] for this paste</param>
        public MMPasteNode(IEnumerable<MMNodePathPiece> path,
                           MMNeedsAnd?                  needs)
        {
            Path  = path.ToArray();
            Needs = needs;
        }
    }

    /// <summary>
    /// Our static parser class
    /// </summary>
    public static partial class KSPMMCfgParser
    {
        private static readonly Parser<char, char> OpenBrace  = Char('{').Between(JunkBlock);
        private static readonly Parser<char, char> CloseBrace = JunkBlock.Right(Char('}'));

        /// <summary>
        /// @+$-!%&amp; => enum?
        /// #Paste's complex and very different syntax
        /// is handled in its own separate parser
        /// </summary>
        public static readonly Parser<char, MMOperator> NodeOperator =
            Optional(CommonOperator, MMOperator.Insert);

        /// <summary>
        /// Parser matching a name that may contain wildcards
        /// </summary>
        public static readonly Parser<char, string> NodeIdentifier = Many1(OneOf("-_.+")
                                                                           | LetterOrDigit()).AsString();

        /// <summary>
        /// Parser matching the [name] after a node
        /// </summary>
        public static readonly Parser<char, IEnumerable<string>> Filters =
            Many1(NoneOf("|,]")).AsString()
                                .SeparatedBy(OneOf("|,"))
                                .Between(Char('['),
                                         Char(']'));

        /// <summary>
        /// :BEFORE, :FOR, :AFTER, etc.
        /// </summary>
        /// <param name="label">The text to find after the colon, case insensitive</param>
        public static Parser<char, MMNodeSuffix> SimpleClause(string label)
            => NodeIdentifier.Between(StringIgnoreCase($":{label}["),
                                      Char(']'))
                             .Map(v => new MMNodeSuffix(label, v));

        private static IEnumerable<T> FindByType<T>(IEnumerable<dynamic> suffixes) where T : class
             => suffixes.Where(s => s.GetType() == typeof(T))
                        .Select(s => (T)s);

        private static string? FindSimpleSuffix(IEnumerable<dynamic> suffixes, string label)
            => FindByType<MMNodeSuffix>(suffixes).FirstOrDefault(s => s.Label == label)?.Value;

        private static T? FindSuffix<T>(IEnumerable<dynamic> suffixes) where T : class
            => FindByType<T>(suffixes).FirstOrDefault();

        /// <summary>
        /// Parser for a paste directive including its contents (ignored)
        /// </summary>
        /// <param name="configNode">Parser to match a config node, needed for mutual recursion</param>
        public static Parser<char, MMPasteNode> PasteNode(Parser<char, KSPConfigNode> configNode)
            => from path  in Char('#').Right(NodePath)
               from needs in NeedsClause!.AsNullable()
               from _     in (Property!.Ignore()
                              | configNode!.Ignore()
                              | Comment!.Ignore())
                             .SeparatedBy(AtLeastOneEOL)
                             .Between(OpenBrace,
                                      CloseBrace)
               select new MMPasteNode(path, needs);

        /// <summary>
        /// Parser for a full ConfigNode
        /// </summary>
        public static readonly Parser<char, KSPConfigNode> ConfigNode =
            Fix<char, KSPConfigNode>(configNode =>
                from op        in NodeOperator
                from name      in NodeIdentifier
                from filters   in Filters.AbortQuietlyIfEntered()
                                         .AsNullable()
                from suffixes  in Many(HasClause!.AbortQuietlyIfEntered()
                                                 .AsDynamic()
                                       | NeedsClause!.AbortQuietlyIfEntered()
                                                     .AsDynamic()
                                       | Index!.AbortQuietlyIfEntered()
                                               .AsDynamic()
                                       | SimpleClause("FOR").AbortQuietlyIfEntered()
                                                            .AsDynamic()
                                       | SimpleClause("BEFORE").AbortQuietlyIfEntered()
                                                               .AsDynamic()
                                       | SimpleClause("AFTER").AbortQuietlyIfEntered()
                                                              .AsDynamic()
                                       | SimpleClause("LAST").AbortQuietlyIfEntered()
                                                             .AsDynamic()
                                       | StringIgnoreCase(":FIRST").Map(_ => new MMNodeSuffix("FIRST"))
                                                                   .AsDynamic()
                                       | StringIgnoreCase(":FINAL").Map(_ => new MMNodeSuffix("FINAL"))
                                                                   .AsDynamic())
                from contents  in (Property!.AbortQuietlyIfEntered()
                                            .AsDynamic()
                                   | configNode!.AbortQuietlyIfEntered()
                                                .AsDynamic()
                                   | PasteNode(configNode).AbortQuietlyIfEntered()
                                                          .AsDynamic()
                                   | Comment!.AsDynamic())
                                  .SeparatedBy(AtLeastOneEOL)
                                  .Between(OpenBrace,
                                           CloseBrace)
                select new KSPConfigNode(
                    op, name, filters,

                    FindSuffix<MMNeedsAnd>(suffixes),
                    FindSuffix<MMHas>(suffixes),
                    FindSimpleSuffix(suffixes, "FIRST") != null,
                    FindSimpleSuffix(suffixes, "BEFORE"),
                    FindSimpleSuffix(suffixes, "FOR"),
                    FindSimpleSuffix(suffixes, "AFTER"),
                    FindSimpleSuffix(suffixes, "LAST"),
                    FindSimpleSuffix(suffixes, "FINAL") != null,
                    FindSuffix<MMIndex>(suffixes),

                    FindByType<KSPConfigProperty>(contents),
                    FindByType<KSPConfigNode>(contents),
                    FindByType<MMPasteNode>(contents)));

        /// <summary>
        /// Parser for a whole file (multiple config nodes)
        /// </summary>
        public static readonly Parser<char, IEnumerable<KSPConfigNode>> ConfigFile =
            ConfigNode.AbortQuietlyIfEntered()
                      .SeparatedBy(AtLeastOneEOL)
                      .Between(JunkBlock)
                      .End();
    }
}
