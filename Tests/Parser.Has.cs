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
    public class HasTests
    {

        [Test]
        public void HasClauseParse_Operators_Works()
        {
            HasClause.Parse(":HAS[@NODE,!NONODE,#PROP,~NOPROP,#model[a/b/c/d]]")
                     .WillSucceed(v =>
                     {
                         Assert.AreEqual(5,                    v.Pieces.Length);
                         Assert.AreEqual(MMHasType.Node,       v.Pieces[0].HasType);
                         Assert.AreEqual(MMHasType.NoNode,     v.Pieces[1].HasType);
                         Assert.AreEqual(MMHasType.Property,   v.Pieces[2].HasType);
                         Assert.AreEqual(MMHasType.NoProperty, v.Pieces[3].HasType);
                         Assert.AreEqual(MMHasType.Property,   v.Pieces[4].HasType);
                     });
        }

        [Test]
        public void HasClauseParse_Nested_Works()
        {
            HasClause.Parse(":HAS[@MODULE[ModuleEngines]:HAS[@PROPELLANT[XenonGas],@PROPELLANT[ElectricCharge]]]")
                     .WillSucceed(v =>
                     {
                         Assert.AreEqual(1,                v.Pieces.Length);
                         Assert.AreEqual("MODULE",         v.Pieces[0].Key);
                         Assert.AreEqual("ModuleEngines",  v.Pieces[0].Value);
                         Assert.AreEqual("PROPELLANT",     v.Pieces[0].HasClause.Pieces[0].Key);
                         Assert.AreEqual("XenonGas",       v.Pieces[0].HasClause.Pieces[0].Value);
                         Assert.AreEqual("PROPELLANT",     v.Pieces[0].HasClause.Pieces[1].Key);
                         Assert.AreEqual("ElectricCharge", v.Pieces[0].HasClause.Pieces[1].Value);
                     });
        }

    }
}
