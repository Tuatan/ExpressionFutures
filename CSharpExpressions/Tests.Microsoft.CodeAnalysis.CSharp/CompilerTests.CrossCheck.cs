﻿// Prototyping extended expression trees for C#.
//
// bartde - December 2015

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Tests.Microsoft.CodeAnalysis.CSharp
{
    partial class CompilerTests
    {
        [TestMethod]
        public void CrossCheck_Arithmetic()
        {
            var f = Compile<Func<int>>("() => Return(1) + Return(2)");
            f();
        }

        [TestMethod]
        public void CrossCheck_NullConditional()
        {
            var f = Compile<Func<string, int?>>("s => s?.Length");
            f("bar");
            f(null);
        }

        [TestMethod]
        public void CrossCheck_NamedParameters()
        {
            var f = Compile<Func<int>>(@"() =>
{
    var b = new StrongBox<int>(1);
    Log(b.Value);
    return System.Threading.Interlocked.Exchange(value: Return(42), location1: ref b.Value);
}");
            f();
        }

        [TestMethod]
        public void CrossCheck_ForEach()
        {
            var f = Compile<Action>(@"() =>
{
    Log(""Before"");

    for (var i = Return(0); Return(i < 10); Return(i++))
    {
        if (i == 2)
        {
            Log(""continue"");
            continue;
        }

        if (i == 5)
        {
            Log(""break"");
            break;
        }

        Log($""body({i})"");
    }

    Log(""After"");
}");
            f();
        }

        [TestMethod]
        public void CrossCheck_CompoundAssignment()
        {
            var f = Compile<Func<int, int>>(@"i =>
{
    var b = new StrongBox<int>(i);
    Log(b.Value);
    var res = b.Value += Return(1);
    Log(res);
    return b.Value;
}");
            f(0);
            f(41);
        }

        [TestMethod]
        [Ignore] // See https://github.com/dotnet/corefx/issues/4984; we may have to fix this with C#-specific nodes
        public void CrossCheck_CompoundAssignment_Issue()
        {
            var f = Compile<Func<int, int>>(@"i =>
{
    var b = new WeakBox<int>();
    Log(b.Value);
    var res = b.Value += Return(1);
    Log(res);
    return b.Value;
}");
            f(0);
            f(41);
        }

        private TDelegate Compile<TDelegate>(string code)
        {
            var res = TestUtilities.FuncEval<TDelegate>(code);

            var exp = res.Expression.Compile();
            var fnc = res.Function;
            var log = res.Log;

            var invoke = typeof(TDelegate).GetMethod("Invoke");
            var returnType = invoke.ReturnType;
            var resultType = returnType == typeof(void) ? typeof(object) : returnType;
            var parameters = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

            var evalExp = CreateInvoke<TDelegate>(exp, returnType, parameters, log);
            var evalFnc = CreateInvoke<TDelegate>(fnc, returnType, parameters, log);

            var evalExpVar = Expression.Parameter(evalExp.Type);
            var evalFncVar = Expression.Parameter(evalFnc.Type);

            var evalExpAsg = Expression.Assign(evalExpVar, evalExp);
            var evalFncAsg = Expression.Assign(evalFncVar, evalFnc);

            var assertMethod = typeof(CompilerTests).GetMethod(nameof(CheckResults), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(resultType);
            var assert = Expression.Call(assertMethod, evalExpVar, evalFncVar);

            var returnMethod = evalFnc.Type.GetMethod("Return");
            var body = Expression.Block(new[] { evalExpVar, evalFncVar }, evalExpAsg, evalFncAsg, assert, Expression.Call(evalFncVar, returnMethod));

            var f = Expression.Lambda<TDelegate>(body, parameters);
            return f.Compile();
        }

        private static void CheckResults<TResult>(LogAndResult<TResult> expression, LogAndResult<TResult> function)
        {
            if (!expression.Equals(function))
            {
                throw new InvalidOperationException("Results don't match.");
            }
        }

        private static Expression CreateInvoke<TDelegate>(TDelegate f, Type returnType, ParameterExpression[] parameters, List<string> log)
        {
            var inv = Expression.Invoke(Expression.Constant(f, typeof(TDelegate)), parameters);

            var resultType = returnType == typeof(void) ? typeof(object) : returnType;
            var result = Expression.Parameter(resultType);

            var eval = (Expression)inv;
            if (returnType != typeof(void))
            {
                eval = Expression.Block(typeof(void), Expression.Assign(result, eval));
            }

            var err = Expression.Parameter(typeof(Exception));
            var ex = Expression.Parameter(typeof(Exception));
            eval = Expression.TryCatch(eval, Expression.Catch(ex, Expression.Block(typeof(void), Expression.Assign(err, ex))));

            var logAndResultType = typeof(LogAndResult<>).MakeGenericType(resultType);
            var logAndResultCtor = logAndResultType.GetConstructors().Single(c => c.GetParameters().Length == 3);

            var logValue = Expression.Constant(log);
            var copyLog = Expression.New(typeof(List<string>).GetConstructor(new[] { typeof(IEnumerable<string>) }), logValue);
            var clearLog = Expression.Call(logValue, typeof(List<string>).GetMethod("Clear", Array.Empty<Type>()));

            var loggedResult = Expression.Parameter(logAndResultType);
            var logResult = Expression.Assign(loggedResult, Expression.New(logAndResultCtor, copyLog, result, err));

            var res = Expression.Block(new[] { result, err, loggedResult }, eval, logResult, clearLog, loggedResult);
            return res;
        }
    }
}
