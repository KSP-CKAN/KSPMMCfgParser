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

    }
}
