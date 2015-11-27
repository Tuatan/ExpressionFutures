﻿// Prototyping extended expression trees for C#.
//
// bartde - November 2015

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp.Expressions;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CSharpDynamic = Microsoft.CSharp.RuntimeBinder;

namespace Tests.Microsoft.CodeAnalysis.CSharp
{
    public static class TestUtilities
    {
        // NB: This domain is used to load custom-built Roslyn assemblies when invoking TestUtilities from the T4
        //     text template when generating tests. The problem is that the T4 engine is loaded in VS, with the Roslyn
        //     binaries on the AppDomain's probing path. If we don't tweak this, it will pick up the v1 RTM binaries
        //     from "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\PrivateAssemblies".
        private static AppDomain s_roslyn;

        public static void InitializeDomain(string path)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = path,
                ShadowCopyFiles = "true",
            };

            s_roslyn = AppDomain.CreateDomain("RoslynHost", null, setup);
        }

        public static void UnloadDomain()
        {
            if (s_roslyn != null)
            {
                AppDomain.Unload(s_roslyn);
                // NB: not setting to null, so subsequent invocations fail DoCallBack upon misuse
            }
        }

        public static string GetDebugView(string expr)
        {
            if (s_roslyn != null)
            {
                return GetDebugViewMarshal(expr);
            }

            return GetDebugViewCore(expr);
        }

        private static string GetDebugViewMarshal(string expr)
        {
            s_roslyn.SetData("expr", expr);

            s_roslyn.DoCallBack(GetDebugViewCallback);

            var res = (string)s_roslyn.GetData("debugView");
            return res;
        }

        private static void GetDebugViewCallback()
        {
            var expr = (string)AppDomain.CurrentDomain.GetData("expr");
            var res = GetDebugViewCore(expr);
            AppDomain.CurrentDomain.SetData("debugView", res);
        }

        private static string GetDebugViewCore(string expr)
        {
            // TODO: Investigate using the scripting APIs here instead.

            var typeName = "Expressions";
            var propName = "Expression";

            var exprProp = $"    public static Expression {propName} => {expr};";

            var tree = CSharpSyntaxTree.ParseText($@"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

public static class {typeName}
{{
{exprProp}
}}
");
            var csc = CSharpCompilation
                // A class library `Expressions` which will be emitted in memory
                .Create("Expressions")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: 0))

                // BCL assemblies
                .AddReferences(MetadataReference.CreateFromFile(typeof(int).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location))

                // Our custom assembly
                .AddReferences(MetadataReference.CreateFromFile(typeof(CSharpExpression).Assembly.Location))

                // Support for dynamic
                .AddReferences(MetadataReference.CreateFromFile(typeof(CSharpDynamic.Binder).Assembly.Location))

                // Generated test code based on `expr`
                .AddSyntaxTrees(tree);

            var asm = default(Assembly);

            using (var ms = new MemoryStream())
            {
                var res = default(EmitResult);

                try
                {
                    // NB: This can fail because we're testing custom builds of the compiler.
                    res = csc.Emit(ms);
                }
                catch (Exception ex)
                {
                    throw new Exception("Fatal compiler error!", ex); // TODO: add the program text
                }

                if (!res.Success)
                {
                    var diag = string.Join("\r\n", res.Diagnostics.Select(d => d.ToString()));
                    throw new InvalidProgramException(diag);
                }

                ms.Position = 0;

                asm = Assembly.Load(ms.ToArray());
            }

            var typ = asm.GetType(typeName);
            var prp = typ.GetProperty(propName);
            var exp = (Expression)prp.GetValue(null);
            return exp.DebugView().ToString();
        }
    }
}