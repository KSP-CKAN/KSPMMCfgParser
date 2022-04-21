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
    public class PropertyTests
    {

        [Test]
        public void PropertyParse_Unpadded_Works()
        {
            Property.Parse("a=b")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual("a", v.Name);
                        Assert.AreEqual("b", v.Value);
                    });
            Property.Parse("1=2.0")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual("1", v.Name);
                        Assert.AreEqual("2.0", v.Value);
                    });
        }

        [Test]
        public void PropertyParse_Padded_Works()
        {
            Property.Parse("a          =               b     x      ")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual("a",       v.Name);
                        Assert.AreEqual("b     x", v.Value);
                    });
        }

        [Test]
        public void PropertyParse_NeedsClause_works()
        {
            Property.Parse("key:NEEDS[Astrogator|PlanningNode&SmartTank] = value")
                    .WillSucceed(v =>
                    {
                        Assert.IsTrue(v.Needs!.Satisfies("Astrogator", "SmartTank"));
                        Assert.IsTrue(v.Needs!.Satisfies("PlanningNode", "SmartTank"));
                        Assert.IsFalse(v.Needs!.Satisfies("Astrogator"));
                        Assert.IsFalse(v.Needs!.Satisfies("PlanningNode"));
                        Assert.IsFalse(v.Needs!.Satisfies("SmartTank"));
                    });
        }

        [Test]
        public void MultiPropertyParse_ThreeLines_Works()
        {
            Property.SeparatedBy(AtLeastOneEOL).ToArray()
                    .Parse("k1=v1\n k2 = v2\n  k3  =  v3")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual(3,    v.Length);
                        Assert.AreEqual("k1", v[0].Name);
                        Assert.AreEqual("v1", v[0].Value);
                        Assert.AreEqual("k2", v[1].Name);
                        Assert.AreEqual("v2", v[1].Value);
                        Assert.AreEqual("k3", v[2].Name);
                        Assert.AreEqual("v3", v[2].Value);
                    });
        }

        [Test]
        public void ConfigNodeParse_AssignmentOperator_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    @example  = 0
                    @example += 1
                    @example -= 2
                    @example *= 3
                    @example /= 4
                    @example != 5
                    @example ^= 6
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(MMAssignmentOperator.Assign,       v.Properties[0].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Add,          v.Properties[1].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Subtract,     v.Properties[2].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Multiply,     v.Properties[3].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Divide,       v.Properties[4].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Power,        v.Properties[5].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.RegexReplace, v.Properties[6].AssignmentOperator);
                });
        }

        [Test]
        public void ExternalValueAccessOperator_TypicalInput_Works()
        {
            ConfigFile.ToArray().Parse(@"
                // Destroy RO-M55 if RT20 from VSR exists
                @PART[RT20]:BEFORE[RealismOverhaul] { *@PART[RO-M55]/deleteMe = true }
                !PART[RO-M55]:HAS[#deleteMe[true]]:BEFORE[RealismOverhaul] {}
            ").WillSucceed(v =>
            {
                Assert.AreEqual(2,                              v.Length);
                Assert.AreEqual(MMOperator.ExternalValueAccess, v[0].Properties[0].Operator);
                Assert.AreEqual(2,                              v[0].Properties[0].Path!.Length);
                Assert.AreEqual(MMOperator.PathRoot,            v[0].Properties[0].Path![0].Operator);
                Assert.AreEqual("PART",                         v[0].Properties[0].Path![0].Name);
                Assert.AreEqual("RO-M55",                       v[0].Properties[0].Path![0].Filters![0]);
                Assert.AreEqual("deleteMe",                     v[0].Properties[0].Path![1].Name);
            });
        }

    }
}
