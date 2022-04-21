using System;

using NUnit.Framework;
using ParsecSharp;
using static ParsecSharp.Text;

using KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParserPrimitives;

namespace Tests
{

    public static class ParsecSharpTestExtensions
    {
        /// <summary>
        /// Test helper from ParsecSharpTestExtensions.
        /// I don't know why their copy isn't public.
        /// </summary>
        public static void WillSucceed<TToken, T>(this Result<TToken, T> result, Action<T> assert)
            => result.CaseOf(failure => Assert.Fail(failure.ToString()),
                             success => assert(success.Value));
    }

}
