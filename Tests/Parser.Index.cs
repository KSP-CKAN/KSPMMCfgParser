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
    public class IndexTests
    {

        [Test]
        public void ConfigNodeParse_PropertyIndex_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    @example    = 0
                    @example,0  = 1
                    @example,1  = 2
                    @example,-1 = 3
                    @example,*  = 4
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(null, v.Properties[0].Index);
                    Assert.AreEqual(0,    v.Properties[1].Index!.Value);
                    Assert.IsTrue(v.Properties[1].Index!.Satisfies(0, 5));
                    Assert.AreEqual(1,    v.Properties[2].Index!.Value);
                    Assert.IsTrue(v.Properties[2].Index!.Satisfies(1, 5));
                    Assert.AreEqual(-1,   v.Properties[3].Index!.Value);
                    Assert.IsTrue(v.Properties[3].Index!.Satisfies(4, 5));
                    Assert.AreEqual(null, v.Properties[4].Index!.Value);
                    Assert.IsTrue(v.Properties[4].Index!.Satisfies(0, 5));
                    Assert.IsTrue(v.Properties[4].Index!.Satisfies(1, 5));
                    Assert.IsTrue(v.Properties[4].Index!.Satisfies(2, 5));
                });
        }

    }
}
