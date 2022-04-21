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
    public class PrimitivesTests
    {

        [Test]
        public void IdentifierParse_Locale_Works()
        {
            NodeIdentifier.Parse("en-us")
                          .WillSucceed(v => Assert.AreEqual("en-us", v));
        }

        [Test]
        public void IdentifierParse_LocalizationToken_Works()
        {
            PropertyIdentifier.Parse("#modname_stringname")
                              .WillSucceed(v => Assert.AreEqual("#modname_stringname", v));
        }

        [Test]
        public void CommentParse_Unpadded_Works()
        {
            Comment.Parse("//test1")
                   .WillSucceed(v => Assert.AreEqual("test1", v));
        }

        [Test]
        public void CommentParse_Padded_Works()
        {
            Comment.Parse("//   test2   ")
                   .WillSucceed(v => Assert.AreEqual("   test2   ", v));
        }

    }
}
