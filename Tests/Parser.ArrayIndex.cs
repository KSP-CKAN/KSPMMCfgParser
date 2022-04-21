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
    public class ArrayIndexTests
    {

        [Test]
        public void ConfigNodeParse_ArrayIndex_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    @example      = 0
                    @example[1]   = 1
                    @example[2, ] = 2
                    @example[3,_] = 3
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(null, v.Properties[0].ArrayIndex);
                    Assert.AreEqual(',',  v.Properties[1].ArrayIndex!.Separator);
                    Assert.AreEqual(1,    v.Properties[1].ArrayIndex!.Value);
                    Assert.AreEqual(' ',  v.Properties[2].ArrayIndex!.Separator);
                    Assert.AreEqual(2,    v.Properties[2].ArrayIndex!.Value);
                    Assert.AreEqual('_',  v.Properties[3].ArrayIndex!.Separator);
                    Assert.AreEqual(3,    v.Properties[3].ArrayIndex!.Value);
                });
        }

    }
}
