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

        // Every printable ASCII character that ModuleManager 4.2.3 accepts in a
        // mod name on root nodes, nested nodes, and values alike
        private const string ModNameChars =
            "!\"#$%'*+-./0123456789:;<>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ\\^_`abcdefghijklmnopqrstuvwxyz~";

        [Test]
        public void NeedsClauseParse_ModNameCharacters_Work()
        {
            foreach (var c in ModNameChars)
            {
                NeedsClause.Parse($":NEEDS[A{c}B]")
                           .WillSucceed(v =>
                           {
                               Assert.AreEqual(1, v.Arguments.Length, $"'{c}' in the middle");
                               Assert.AreEqual(1, v.Arguments[0].Arguments.Length, $"'{c}' in the middle");
                               Assert.AreEqual($"A{c}B", v.Arguments[0].Arguments[0].ModName);
                           });
                NeedsClause.Parse($":NEEDS[AB{c}]")
                           .WillSucceed(v => Assert.AreEqual($"AB{c}", v.Arguments[0].Arguments[0].ModName));
                if (c != '!')
                {
                    NeedsClause.Parse($":NEEDS[{c}AB]")
                               .WillSucceed(v =>
                               {
                                   Assert.AreEqual($"{c}AB", v.Arguments[0].Arguments[0].ModName);
                                   Assert.IsFalse(v.Arguments[0].Arguments[0].Negated);
                               });
                }
            }
        }

        [Test]
        public void NeedsClauseParse_ModNameNonAscii_Works()
        {
            foreach (var name in new string[] { "A\u00e9B", "A\u65e5B", "A\u00a0B", "A\u0130B", "A\U0001F680B" })
            {
                NeedsClause.Parse($":NEEDS[{name}]")
                           .WillSucceed(v => Assert.AreEqual(name, v.Arguments[0].Arguments[0].ModName));
            }
        }

        [Test]
        public void NeedsClauseParse_LeadingBang_Negates()
        {
            NeedsClause.Parse(":NEEDS[!A!B]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Arguments[0].Arguments[0].Negated);
                           Assert.AreEqual("A!B", v.Arguments[0].Arguments[0].ModName);
                       });
        }

        [Test]
        public void NeedsClauseParse_Separators_Split()
        {
            NeedsClause.Parse(":NEEDS[A.B&C.D,E.F|G.H]")
                       .WillSucceed(v =>
                       {
                           Assert.AreEqual(3,     v.Arguments.Length);
                           Assert.AreEqual("A.B", v.Arguments[0].Arguments[0].ModName);
                           Assert.AreEqual("C.D", v.Arguments[1].Arguments[0].ModName);
                           Assert.AreEqual(2,     v.Arguments[2].Arguments.Length);
                           Assert.AreEqual("E.F", v.Arguments[2].Arguments[0].ModName);
                           Assert.AreEqual("G.H", v.Arguments[2].Arguments[1].ModName);
                       });
        }

        [Test]
        public void NeedsClauseParse_ModNameTerminators_Fail()
        {
            foreach (var name in new string[] { "A B", "A\tB", "A(B", "A)B", "A{B", "A}B",
                                                "A=B", "A[B", "A]B", "A[1]B", "A//B" })
            {
                Assert.IsFalse(NeedsClause.End().Parse($":NEEDS[{name}]")
                                          .CaseOf(failure => false, success => true),
                               $"\"{name}\" should not parse");
            }
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
