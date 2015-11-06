﻿// Prototyping extended expression trees for C#.
//
// bartde - October 2015

using Microsoft.CSharp.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;
using System.Reflection;
using static Tests.TestHelpers;

namespace Tests
{
    [TestClass]
    public class ConditionalMemberTests
    {
        [TestMethod]
        public void ConditionalMember_Factory_ArgumentChecking()
        {
            var expr = Expression.Default(typeof(Bar));
            var other = Expression.Default(typeof(string));
            var propName = "P";
            var propInfo = typeof(Bar).GetProperty(propName);
            var getInfo = propInfo.GetGetMethod(true);
            var fieldName = "F";
            var fieldInfo = typeof(Bar).GetField(fieldName);

            // null
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalProperty(default(Expression), propName));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalProperty(expr, default(string)));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalProperty(default(Expression), propInfo));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalProperty(expr, default(PropertyInfo)));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalProperty(default(Expression), getInfo));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalProperty(expr, default(MethodInfo)));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalField(default(Expression), fieldName));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalField(expr, default(string)));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalField(default(Expression), fieldInfo));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.ConditionalField(expr, default(FieldInfo)));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.MakeConditionalMemberAccess(default(Expression), propInfo));
            AssertEx.Throws<ArgumentNullException>(() => CSharpExpression.MakeConditionalMemberAccess(expr, default(MemberInfo)));

            // not exist
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, "X"));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalField(expr, "X"));

            // static
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, expr.Type.GetProperty("SP")));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalField(expr, expr.Type.GetField("SF")));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, "SP"));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalField(expr, "SF"));

            // set-only
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, expr.Type.GetProperty("XP")));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, "XP"));

            // indexer
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, expr.Type.GetProperty("Item")));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(expr, "Item"));

            // wrong declaring type
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(other, expr.Type.GetProperty("P")));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalProperty(other, "P"));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalField(other, expr.Type.GetField("F")));
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.ConditionalField(other, "F"));

            // not field or property
            AssertEx.Throws<ArgumentException>(() => CSharpExpression.MakeConditionalMemberAccess(expr, expr.Type.GetConstructors()[0]));
        }

        [TestMethod]
        public void ConditionalMember_Properties()
        {
            var expr = Expression.Default(typeof(Bar));
            var propName = "P";
            var propInfo = typeof(Bar).GetProperty(propName);
            var getInfo = propInfo.GetGetMethod(true);
            var fieldName = "F";
            var fieldInfo = typeof(Bar).GetField(fieldName);

            foreach (var e in new[]
            {
                CSharpExpression.ConditionalField(expr, fieldInfo),
                CSharpExpression.ConditionalField(expr, fieldName),
                CSharpExpression.MakeConditionalMemberAccess(expr, fieldInfo),
            })
            {
                Assert.AreSame(expr, e.Expression);
                Assert.AreEqual(fieldInfo, e.Member);
                Assert.AreEqual(typeof(int?), e.Type);
                Assert.AreEqual(CSharpExpressionType.ConditionalMemberAccess, e.CSharpNodeType);
            }

            foreach (var e in new[]
            {
                CSharpExpression.ConditionalProperty(expr, propInfo),
                CSharpExpression.ConditionalProperty(expr, getInfo),
                CSharpExpression.ConditionalProperty(expr, propName),
                CSharpExpression.MakeConditionalMemberAccess(expr, propInfo),
            })
            {
                Assert.AreSame(expr, e.Expression);
                Assert.AreEqual(propInfo, e.Member);
                Assert.AreEqual(typeof(int?), e.Type);
                Assert.AreEqual(CSharpExpressionType.ConditionalMemberAccess, e.CSharpNodeType);
            }
        }

        [TestMethod]
        public void ConditionalMember_Update()
        {
            var expr1 = Expression.Default(typeof(Bar));
            var expr2 = Expression.Default(typeof(Bar));
            var propName = "P";
            var propInfo = typeof(Bar).GetProperty(propName);
            var fieldName = "F";
            var fieldInfo = typeof(Bar).GetField(fieldName);

            var res1 = CSharpExpression.ConditionalProperty(expr1, propInfo);
            var res2 = CSharpExpression.ConditionalField(expr1, fieldInfo);

            Assert.AreSame(res1, res1.Update(res1.Expression));
            Assert.AreSame(res2, res2.Update(res2.Expression));

            var upd1 = res1.Update(expr2);
            var upd2 = res2.Update(expr2);

            Assert.AreSame(expr2, upd1.Expression);
            Assert.AreSame(expr2, upd2.Expression);
        }

        [TestMethod]
        public void ConditionalMember_Compile()
        {
        }

        [TestMethod]
        public void ConditionalMember_Visitor()
        {
            var expr = Expression.Default(typeof(Bar));
            var prop = expr.Type.GetProperty("P");
            var res = CSharpExpression.ConditionalProperty(expr, prop);

            var v = new V();
            Assert.AreSame(res, v.Visit(res));
            Assert.IsTrue(v.Visited);
        }

        class V : CSharpExpressionVisitor
        {
            public bool Visited = false;

            protected internal override Expression VisitConditionalMember(ConditionalMemberCSharpExpression node)
            {
                Visited = true;

                return base.VisitConditionalMember(node);
            }
        }

        class Bar
        {
            public bool this[int x] { get { return false; } }
            public int F;
            public int P { get; set; }
            public string XP { set { } }

            public static int SF;
            public static string SP { get; set; }
        }
    }
}
