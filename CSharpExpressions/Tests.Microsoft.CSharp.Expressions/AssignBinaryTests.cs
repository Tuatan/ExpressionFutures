﻿// Prototyping extended expression trees for C#.
//
// bartde - November 2015

using Microsoft.CSharp.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using static Tests.ReflectionUtils;

namespace Tests
{
    [TestClass]
    public partial class AssignBinaryTests
    {
        [TestMethod]
        public void AssignBinary_Factory_ArgumentChecking()
        {
            var l = Expression.Parameter(typeof(int));
            var r = Expression.Constant(2);

            AssertEx.Throws<ArgumentException>(() => CSharpExpression.MakeBinaryAssign(CSharpExpressionType.Await, l, r, null, null));
        }

        [TestMethod]
        public void AssignBinary_Factory_MakeBinaryAssign()
        {
            foreach (var l in GetLhs())
            {
                var r = Expression.Constant(2);
                var m = MethodInfoOf(() => Op(0, 0));
                var x = Expression.Parameter(typeof(int));
                var c = Expression.Lambda(x, x);

                foreach (var n in new[]
                {
                    CSharpExpressionType.Assign,
                    CSharpExpressionType.AddAssign,
                    CSharpExpressionType.AndAssign,
                    CSharpExpressionType.DivideAssign,
                    CSharpExpressionType.ExclusiveOrAssign,
                    CSharpExpressionType.LeftShiftAssign,
                    CSharpExpressionType.ModuloAssign,
                    CSharpExpressionType.MultiplyAssign,
                    CSharpExpressionType.OrAssign,
                    CSharpExpressionType.RightShiftAssign,
                    CSharpExpressionType.SubtractAssign,
                    CSharpExpressionType.AddAssignChecked,
                    CSharpExpressionType.MultiplyAssignChecked,
                    CSharpExpressionType.SubtractAssignChecked,
                })
                {
                    var a1 = CSharpExpression.MakeBinaryAssign(n, l, r, m, c);
                    Assert.AreEqual(n, a1.CSharpNodeType);
                    Assert.AreSame(l, a1.Left);
                    Assert.AreSame(r, a1.Right);
                    Assert.AreEqual(typeof(int), a1.Type);
                    Assert.IsFalse(a1.IsLiftedToNull);
                    Assert.IsFalse(a1.IsLifted);

                    if (n == CSharpExpressionType.Assign)
                    {
                        Assert.IsNull(a1.Method);
                        Assert.IsNull(a1.Conversion);
                    }
                    else
                    {
                        Assert.AreSame(m, a1.Method);
                        Assert.AreSame(c, a1.Conversion);
                    }

                    var a2 = CSharpExpression.MakeBinaryAssign(n, l, r, m, null);
                    Assert.AreEqual(n, a2.CSharpNodeType);
                    Assert.AreSame(l, a2.Left);
                    Assert.AreSame(r, a2.Right);
                    Assert.AreEqual(typeof(int), a2.Type);
                    Assert.IsFalse(a2.IsLiftedToNull);
                    Assert.IsFalse(a2.IsLifted);
                    Assert.IsNull(a2.Conversion);

                    if (n == CSharpExpressionType.Assign)
                    {
                        Assert.IsNull(a2.Method);
                    }
                    else
                    {
                        Assert.AreSame(m, a2.Method);
                    }

                    var a3 = CSharpExpression.MakeBinaryAssign(n, l, r, null, null);
                    Assert.AreEqual(n, a3.CSharpNodeType);
                    Assert.AreSame(l, a3.Left);
                    Assert.AreSame(r, a3.Right);
                    Assert.AreEqual(typeof(int), a3.Type);
                    Assert.IsFalse(a3.IsLiftedToNull);
                    Assert.IsFalse(a3.IsLifted);
                    Assert.IsNull(a3.Method);
                    Assert.IsNull(a3.Conversion);
                }
            }
        }

        private static int Op(int x, int y)
        {
            throw new NotImplementedException();
        }
        
        private static IEnumerable<Expression> GetLhs()
        {
            yield return Expression.Parameter(typeof(int));
            yield return Expression.Field(Expression.Parameter(typeof(StrongBox<int>)), "Value");
            yield return Expression.MakeIndex(Expression.Parameter(typeof(List<int>)), typeof(List<int>).GetProperty("Item"), new[] { Expression.Constant(0) });
            yield return Expression.ArrayAccess(Expression.Parameter(typeof(int[])), Expression.Constant(0));
            yield return CSharpExpression.Index(Expression.Parameter(typeof(List<int>)), typeof(List<int>).GetProperty("Item"), CSharpExpression.Bind(typeof(List<int>).GetProperty("Item").GetIndexParameters()[0], Expression.Constant(0)));
        }
    }
}