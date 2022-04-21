using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    /// <summary>
    /// Parser output object representing a piece of a :NEEDS[] clause
    /// </summary>
    public class MMNeedsMod
    {
        /// <summary>
        /// Name of mod to look for
        /// </summary>
        public readonly string ModName;
        /// <summary>
        /// true if we want the mod to be absent,
        /// false if it should be present
        /// </summary>
        public readonly bool   Negated;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="modname">Name of mod to consider</param>
        /// <param name="negated">true to match if mod is absent, false to match if it's present</param>
        public MMNeedsMod(string modname, bool negated = false)
        {
            ModName = modname;
            Negated = negated;
        }

        /// <summary>
        /// Check whether this piece matches the installed mods
        /// </summary>
        /// <param name="InstalledMods">List of names of mods that are installed</param>
        /// <returns>
        /// true if the mod list satisifies our requirement, false otherwise
        /// </returns>
        public bool Satisfies(List<string> InstalledMods)
            => Negated ^ InstalledMods.Contains(ModName);
    }

    /// <summary>
    /// Parser output object representing one or more :NEEDS[] pieces
    /// separated by | any of which can match to satisfy the overall
    /// requirement
    /// </summary>
    public class MMNeedsOr
    {
        /// <summary>
        /// The parts of this expression
        /// </summary>
        public readonly MMNeedsMod[] Arguments;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="args">Pieces in this expression</param>
        public MMNeedsOr(IEnumerable<MMNeedsMod> args)
        {
            Arguments = args.ToArray();
        }

        /// <summary>
        /// Check whether this piece matches the installed mods
        /// </summary>
        /// <param name="InstalledMods">List of names of mods that are installed</param>
        /// <returns>
        /// true if the mod list satisifies our requirement, false otherwise
        /// </returns>
        public bool Satisfies(List<string> InstalledMods)
            => Arguments.Any(arg => arg.Satisfies(InstalledMods));
    }

    /// <summary>
    /// Parser output object representing a complete :NEEDS[] clause,
    /// containing pieces separated by | or &amp; or ,
    /// </summary>
    public class MMNeedsAnd
    {
        /// <summary>
        /// The parts of this expression
        /// </summary>
        public readonly MMNeedsOr[] Arguments;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="args">Pieces in this expression</param>
        public MMNeedsAnd(IEnumerable<MMNeedsOr> args)
        {
            Arguments = args.ToArray();
        }

        /// <summary>
        /// Check whether this piece matches the installed mods
        /// </summary>
        /// <param name="InstalledMods">List of names of mods that are installed</param>
        /// <returns>
        /// true if the mod list satisifies our requirement, false otherwise
        /// </returns>
        public bool Satisfies(List<string> InstalledMods)
            => Arguments.All(arg => arg.Satisfies(InstalledMods));

        /// <summary>
        /// Check whether this piece matches the installed mods
        /// </summary>
        /// <param name="InstalledMods">List of names of mods that are installed</param>
        /// <returns>
        /// true if the mod list satisifies our requirement, false otherwise
        /// </returns>
        public bool Satisfies(params string[] InstalledMods)
            => Satisfies(InstalledMods.ToList());
    }

    public static partial class KSPMMCfgParser
    {
        private static readonly Parser<char, MMNeedsMod> NeedsMod =
            from negated in Optional(Char('!'))
            from name    in Many1(LetterOrDigit() | OneOf("/_-?")).AsString()
            select new MMNeedsMod(name, negated);

        private static readonly Parser<char, MMNeedsOr> NeedsOr =
            NeedsMod.SeparatedBy(Char('|'))
                    .Map(v => new MMNeedsOr(v));

        private static readonly Parser<char, MMNeedsAnd> NeedsAnd =
            NeedsOr.SeparatedBy(OneOf("&,"))
                   .Map(v => new MMNeedsAnd(v));

        /// <summary>
        /// :NEEDS[blah|blah&amp;blah]
        /// </summary>
        public static readonly Parser<char, MMNeedsAnd> NeedsClause =
            NeedsAnd.Between(String(":NEEDS["), Char(']'));
    }
}
