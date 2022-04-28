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
    public class ConfigNodeTests
    {

        [Test]
        public void NodeMemberParse_PropertyAndNode_Works()
        {
            var parser = Property.AsDynamic() | ConfigNode.AsDynamic();
            parser.Parse("x=y")
                  .WillSucceed(v =>
                  {
                      KSPConfigProperty value = v!;
                      Assert.AreEqual("x", value!.Name);
                      Assert.AreEqual("y", value!.Value);
                  });
            parser.Parse("NODENAME{k=v}")
                  .WillSucceed(v =>
                  {
                      KSPConfigNode value = v!;
                      Assert.AreEqual("NODENAME", value.Name);
                      Assert.AreEqual("k",        value.Properties[0].Name);
                      Assert.AreEqual("v",        value.Properties[0].Value);
                  });
        }

        [Test]
        public void MultiNodeMemberParse_BothTogether_Works()
        {
            (Property.AsDynamic() | ConfigNode.AsDynamic())
                .SeparatedBy(AtLeastOneEOL)
                .ToArray()
                .Parse("a=b\nNODE1{c=d}\n\ne=f\n\n\nNODE2{g=h}")
                .WillSucceed(v =>
                {
                    Assert.AreEqual(4, v.Length);
                    KSPConfigProperty v1 = v[0]!;
                    Assert.AreEqual("a", v1.Name);
                    Assert.AreEqual("b", v1.Value);
                    KSPConfigNode v2 = v[1]!;
                    Assert.AreEqual("NODE1", v2.Name);
                    Assert.AreEqual("c",     v2.Properties[0].Name);
                    Assert.AreEqual("d",     v2.Properties[0].Value);
                    KSPConfigProperty v3 = v[2]!;
                    Assert.AreEqual("e", v3.Name);
                    Assert.AreEqual("f", v3.Value);
                    KSPConfigNode v4 = v[3]!;
                    Assert.AreEqual("NODE2", v4.Name);
                    Assert.AreEqual("g",     v4.Properties[0].Name);
                    Assert.AreEqual("h",     v4.Properties[0].Value);
                });
        }

        [Test]
        public void SimpleClauseParse_Simple_Works()
        {
            SimpleClause("FOR").Parse(":FOR[Astrogator]")
                               .WillSucceed(v =>
                               {
                                   //
                               });
        }

        [Test]
        public void ConfigNodeParse_PropertyOperators_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    insert = 1
                    @edit = 2
                    +copy1 = 3
                    $copy2 = 4
                    -delete1 = 5
                    !delete2 = 6
                    %editorcreate = 7
                    &create = 8
                    |rename = NEWNAME
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(MMOperator.Insert,       v.Properties[0].Operator);
                    Assert.AreEqual(MMOperator.Edit,         v.Properties[1].Operator);
                    Assert.AreEqual(MMOperator.Copy,         v.Properties[2].Operator);
                    Assert.AreEqual(MMOperator.Copy,         v.Properties[3].Operator);
                    Assert.AreEqual(MMOperator.Delete,       v.Properties[4].Operator);
                    Assert.AreEqual(MMOperator.Delete,       v.Properties[5].Operator);
                    Assert.AreEqual(MMOperator.EditOrCreate, v.Properties[6].Operator);
                    Assert.AreEqual(MMOperator.Create,       v.Properties[7].Operator);
                    Assert.AreEqual(MMOperator.Rename,       v.Properties[8].Operator);
                });
        }

        [Test]
        public void ConfigNodeParse_PatchOrdering_Works()
        {
            ConfigNode.SeparatedBy(Spaces()).Between(Spaces()).ToArray()
                      .Parse(@"
                NODE1:FIRST { }
                NODE2:BEFORE[AnotherMod] { }
                NODE3:FOR[ThisMod] { }
                NODE4:FOR[000_ThisMod] { }
                NODE5:AFTER[AnotherMod] { }
                NODE6:LAST[AnotherMod] { }
                NODE7:FINAL { }
            ").WillSucceed(v =>
            {
                Assert.IsTrue(v[0].First, "First node is FIRST");
                Assert.AreEqual("AnotherMod",  v[1].Before);
                Assert.AreEqual("ThisMod",     v[2].For);
                Assert.AreEqual("000_ThisMod", v[3].For);
                Assert.AreEqual("AnotherMod",  v[4].After);
                Assert.AreEqual("AnotherMod",  v[5].Last);
                Assert.IsTrue(v[6].Final, "Final node is FINAL");
            });
        }

        [Test]
        public void ConfigNodeParse_MultipleNames_Works()
        {
            ConfigNode.Parse(
@"@PART[KA_Engine_125_02|KA_Engine_250_02|KA_Engine_625_02]:NEEDS[UmbraSpaceIndustries/KarbonitePlus]
{
    @MODULE[ModuleEngines*]
    {
        @atmosphereCurve
        {
            !key,* = nope
            key = 0 10000 -17578.79 -17578.79
            key = 1 1500 -1210.658 -1210.658
            key = 4 0.001 0 0
        }
    }
}"
            ).WillSucceed(v =>
            {
                Assert.AreEqual(1, v.Children.Length);
                Assert.AreEqual(3, v.Filters!.Length);
            });
        }

        [Test]
        public void ConfigFileParse_Empty_Works()
        {
            ConfigFile.ToArray().Parse("")
                      .WillSucceed(v => Assert.AreEqual(0, v.Length,
                                                        "Top level node count"));
        }

        [Test]
        public void ConfigFileParse_UnpaddedNode_Works()
        {
            ConfigFile.ToArray().Parse(@"NODENAME{propname=value}")
                .WillSucceed(v =>
                {
                    Assert.AreEqual(1,          v.Length,                 "Top level node count");
                    Assert.AreEqual("NODENAME", v[0].Name,                "First node name");
                    Assert.AreEqual("propname", v[0].Properties[0].Name,  "First node property name");
                    Assert.AreEqual("value",    v[0].Properties[0].Value, "First node property value");
                    Assert.AreEqual(0,          v[0].Children.Length,     "First node child count");
                });
        }

        [Test]
        public void ConfigFileParse_PaddedNode_Works()
        {
            ConfigFile.ToArray().Parse(@"

            NODENAME
            {

                propname    =    value

            }

            ").WillSucceed(v =>
            {
                Assert.AreEqual(1,          v.Length,                 "Top level node count");
                Assert.AreEqual("NODENAME", v[0].Name,                "First node name");
                Assert.AreEqual("propname", v[0].Properties[0].Name,  "First node property");
                Assert.AreEqual("value",    v[0].Properties[0].Value, "First node property");
                Assert.AreEqual(0,          v[0].Children.Length,     "First node child count");
            });
        }

        [Test]
        public void PasteNode_TypicalInput_Works()
        {
            PasteNode(ConfigNode).Parse(@"#@AJE_TPR_CURVE_DEFAULTS/FixedCone/TPRCurve {}")
                                 .WillSucceed(v =>
                                 {
                                     Assert.AreEqual(3, v.Path.Length);
                                 });
        }

        [Test]
        public void ConfigFileParse_NodeOperators_Works()
        {
            ConfigFile.ToArray().Parse(@"
                INSERT { k = v }
                @EDIT { k = v }
                +COPY1 { k = v }
                $COPY2 { k = v }
                -DELETE1 { }
                !DELETE2 { }
                %EDITORCREATE { k = v }
                PARENT {
                    #@AJE_TPR_CURVE_DEFAULTS/FixedCone/TPRCurve {}
                    #/NODEFROMROOT/CHILD {}
                    #../NODEFROMPARENT/CHILD {}
                    #PASTEWITHNEEDS:NEEDS[Something] {}
                }
            ").WillSucceed(v =>
            {
                Assert.AreEqual(MMOperator.Insert,        v[0].Operator);
                Assert.AreEqual(MMOperator.Edit,          v[1].Operator);
                Assert.AreEqual(MMOperator.Copy,          v[2].Operator);
                Assert.AreEqual(MMOperator.Copy,          v[3].Operator);
                Assert.AreEqual(MMOperator.Delete,        v[4].Operator);
                Assert.AreEqual(MMOperator.Delete,        v[5].Operator);
                Assert.AreEqual(MMOperator.EditOrCreate,  v[6].Operator);
                Assert.AreEqual(4,                        v[7].Pastes.Length);
                Assert.AreEqual(3,                        v[7].Pastes[0].Path.Length, "First path length");
                Assert.AreEqual(MMOperator.PathRoot,      v[7].Pastes[0].Path[0].Operator);
                Assert.AreEqual("AJE_TPR_CURVE_DEFAULTS", v[7].Pastes[0].Path[0].Name);
                Assert.AreEqual(MMOperator.PathRelative,  v[7].Pastes[0].Path[1].Operator);
                Assert.AreEqual("FixedCone",              v[7].Pastes[0].Path[1].Name);
                Assert.AreEqual(MMOperator.PathRelative,  v[7].Pastes[0].Path[2].Operator);
                Assert.AreEqual("TPRCurve",               v[7].Pastes[0].Path[2].Name);
                Assert.AreEqual(3,                        v[7].Pastes[1].Path.Length, "Second path length");
                Assert.AreEqual(null,                     v[7].Pastes[1].Path[0]);
                Assert.AreEqual("NODEFROMROOT",           v[7].Pastes[1].Path[1].Name);
                Assert.AreEqual("CHILD",                  v[7].Pastes[1].Path[2].Name);
                Assert.AreEqual(3,                        v[7].Pastes[2].Path.Length, "Third path length");
                Assert.AreEqual(MMNodePathPiece.DotDot,   v[7].Pastes[2].Path[0]);
                Assert.AreEqual("NODEFROMPARENT",         v[7].Pastes[2].Path[1].Name);
                Assert.AreEqual("CHILD",                  v[7].Pastes[2].Path[2].Name);
                Assert.IsFalse(v[7].Pastes[3].Needs!.Satisfies("Nothing"));
                Assert.IsTrue(v[7].Pastes[3].Needs!.Satisfies("Something"));
            });
        }

        [Test]
        public void ConfigFileParse_MultipleNodes_Works()
        {
            ConfigFile.ToArray().Parse(@"NODENAME1
            {
                propname1    =    value1
            }

            // Comment between nodes

            Gradient
            {
                0.0 = 0.38,0.40,0.44,1
                0.2 = 0.08,0.08,0.08,1
                0.4 = 0.01,0.01,0.01,1
                1.0 = 0,0,0,1
            }").WillSucceed(v =>
            {
                Assert.AreEqual(2,           v.Length,                 "Top level node count");
                Assert.AreEqual("NODENAME1", v[0].Name,                "First node name");
                Assert.AreEqual("propname1", v[0].Properties[0].Name,  "First node property name");
                Assert.AreEqual("value1",    v[0].Properties[0].Value, "First node property value");
                Assert.AreEqual(0,           v[0].Children.Length,     "First node child count");
            });
        }

        [Test]
        public void ConfigFileParse_NestedNode_Works()
        {
            ConfigFile.ToArray().Parse(@"

            // A top level comment

            NODENAME1
            {

                // A comment in a node

                propname1    =    value1

                %NODENAME2
                {

                    // A comment in a subnode

                    propname2    =    value2

                }

                propname3   =  value3

                // Comment at the end of a node
            }

            @PART[*]:HAS[#engineType[EXAMPLE]]:FOR[RealismOverhaulEngines]:NEEDS[DONOTRUNME]
            {
                !MODULE[ModuleEngineConfigs],*{}    // Comment between a one line node and another node

                //If the original engine doesn't have a gimbal, you must set up a module gimbal for it first
                @MODULE[ModuleGimbal]
                {
                }
            }

            // Another top level comment

            ").WillSucceed(v =>
            {
                Assert.AreEqual("NODENAME1", v[0].Name,                "First node name");
                Assert.AreEqual("propname1", v[0].Properties[0].Name,  "First node property name");
                Assert.AreEqual("value1",    v[0].Properties[0].Value, "First node property value");
                Assert.AreEqual(1,           v[0].Children.Length,     "First node child count");
            });
        }

        [Test]
        public void ConfigFileParse_PathsAndComments_Work()
        {
            ConfigFile.ToArray().Parse(@"
            PART //Comment after node name
            {
            }
            PARENT {
                #PART/ThisIsAPath
                {
                }
            }
            PART//NotAPath
            {
            }
            PART//Comment confused with path
            {
            }
            PART
            {
                k = v // } property followed by comment with close brace and stuff after
            }
            ").WillSucceed(v =>
            {
                Assert.AreEqual("PART", v[0].Name, "First node name");
            });
        }

    }
}
