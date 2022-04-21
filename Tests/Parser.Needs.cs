using System;

using NUnit.Framework;
using ParsecSharp;
using static ParsecSharp.Text;

using KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParserPrimitives;

namespace Tests
{
    [TestFixture]
    public class NeedsTests
    {

        [Test]
        public void NeedsClauseParse_Complex_Satisfied()
        {
            NeedsClause.Parse(":NEEDS[RealFuels|ModularFuelSystem]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Satisfies("RealFuels", "Anything"));
                           Assert.IsTrue(v.Satisfies("ModularFuelSystem"));
                           Assert.IsTrue(v.Satisfies("RealFuels", "ModularFuelSystem"));
                           Assert.IsFalse(v.Satisfies("SomethingElse"));
                       });
            NeedsClause.Parse(":NEEDS[RealFuels&!ModularFuelSystem]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Satisfies("RealFuels"));
                           Assert.IsFalse(v.Satisfies("RealFuels", "ModularFuelSystem"));
                           Assert.IsFalse(v.Satisfies("ModularFuelSystem"));
                       });
            NeedsClause.Parse(":NEEDS[Mod1|Mod2,!Mod3|Mod4|Mod_5]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Satisfies("Mod1", "Mod3", "Mod4"));
                           Assert.IsTrue(v.Satisfies("Mod2"));
                           Assert.IsFalse(v.Satisfies("Mod1", "Mod3"));
                       });
        }

        [Test]
        public void ConfigNodeParse_NeedsClause_Works()
        {
            ConfigNode.Between(Spaces()).Parse(
                "%NEWNODE:NEEDS[ModularFuelSystem&!RealFuels] { a=b }"
            ).WillSucceed(v =>
            {
                Assert.IsNotNull(v.Needs);
                Assert.IsTrue(v.Needs!.Satisfies("ModularFuelSystem", "Whatever"));
                Assert.IsFalse(v.Needs!.Satisfies("ModularFuelSystem", "RealFuels"));
                Assert.IsFalse(v.Needs!.Satisfies("RealFuels"));
            });
        }

    }
}
