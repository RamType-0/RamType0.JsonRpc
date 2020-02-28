using NUnit;
using NUnit.Framework;
using RamType0.CodeAnalysis.HLSL.PreProcessor.Directives;
using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.CodeAnalysis.HLSL.PreProcessor.Directives.Tests
{
    using String = LanguageServer.String;
    public class PreProcessorDirectiveParserTests
    {

        [Test]
        public void ParseMacroObjectTest()
        {
            var directive = PreProcessorDirectiveParser.Parse(new String("#define ELEVEN 11"), default);
            if(directive is DefineDirective defineDirective)
            {
                if(defineDirective.DefiningMacro is Macro.UserDefinedMacroObject macroObj)
                {
                    Assert.AreEqual(macroObj.Identifier, new String("ELEVEN"));
                    Assert.AreEqual(macroObj.Value, new String("11"));
                }
                else
                {
                    Assert.Fail();
                }
            }
            else
            {
                Assert.Fail();
            }
            
            
        }


        [Test]
        public void ParseMacroFunctionTest()
        {
            var directive = PreProcessorDirectiveParser.Parse(new String("#define vec2(x,y) float2(x,y)"), default);
            if (directive is DefineDirective defineDirective)
            {
                if (defineDirective.DefiningMacro is Macro.UserDefinedMacroFunction macroFunction)
                {
                    Assert.AreEqual(macroFunction.Identifier, new String("vec2"));
                    Assert.IsTrue(macroFunction.Parameters.Span.SequenceEqual(new[] { new String("x"), new String("y") }));
                    Assert.AreEqual(macroFunction.Value, new String("float2(x,y)"));
                }
                else
                {
                    Assert.Fail();
                }
            }
            else
            {
                Assert.Fail();
            }
        }
        [Test]
        public void ParseInconsistentSplicedMacroFunctionTest()
        {
            var directive = PreProcessorDirectiveParser.Parse(new String("#  \\\n\\\n\\\n  def\\\r\n\\\rine   vec\\\n2(x,y)   float2(x,y)"), default);
            if (directive is DefineDirective defineDirective)
            {
                if (defineDirective.DefiningMacro is Macro.UserDefinedMacroFunction macroFunction)
                {
                    Assert.AreEqual(macroFunction.Identifier, new String("vec2"));
                    Assert.IsTrue(macroFunction.Parameters.Span.SequenceEqual(new[] { new String("x"), new String("y") }));
                    Assert.AreEqual(macroFunction.Value, new String("float2(x,y)"));
                }
                else
                {
                    Assert.Fail();
                }
            }
            else
            {
                Assert.Fail();
            }
        }
    }
}