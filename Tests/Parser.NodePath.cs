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
    public class NodePathTests
    {
        [Test]
        public void NodePath_TypicalInput_Works()
        {
            NodePath.ToArray().Parse(@"@AJE_TPR_CURVE_DEFAULTS/FixedCone/TPRCurve")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual(3, v.Length);
                    });
        }

        [Test]
        public void NodePath_NamedPieces_Work()
        {
            NodePath.ToArray().Parse("@PART[x]/MODULE[m]/k")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual(3, v.Length);
                        CollectionAssert.AreEqual(new string[] { "x" }, v[0]!.Filters);
                        CollectionAssert.AreEqual(new string[] { "m" }, v[1]!.Filters);
                        Assert.AreEqual("k", v[2]!.Name);
                    });
        }

        [Test]
        public void ConfigNodeParse_EmptyNameAlternativeInPath_Fails()
        {
            // ModuleManager reads the name in a path piece as a single pattern.
            // An empty alternative there finds nothing, so it stays an error.
            foreach (var member in new string[]
                     {
                         "*@PART[|x]/mass = 3",
                         "*@PART[x|]/mass = 3",
                         "*@PART[x]/MODULE[|m]/k = 8",
                         "#@PART[|x]/MODULE[m] { }",
                         "#@PART[x]/MODULE[|m] { }",
                     })
            {
                Assert.IsFalse(ConfigNode.Parse("@TARGET[t]\n{\n\t" + member + "\n}\n")
                                         .CaseOf(failure => false, success => true),
                               member + " should not parse");
            }
            // The same pieces without the empty alternative do
            foreach (var member in new string[]
                     {
                         "*@PART[x]/mass = 3",
                         "*@PART[x]/MODULE[m]/k = 8",
                         "#@PART[x]/MODULE[m] { }",
                     })
            {
                Assert.IsTrue(ConfigNode.Parse("@TARGET[t]\n{\n\t" + member + "\n}\n")
                                        .CaseOf(failure => false, success => true),
                              member + " should parse");
            }
        }

    }
}
