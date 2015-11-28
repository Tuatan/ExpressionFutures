﻿// Prototyping extended expression trees for C#.
//
// bartde - October 2015

using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.ExpressionStubs;
using LinqError = System.Linq.Expressions.Error;

namespace Microsoft.CSharp.Expressions
{
    /// <summary>
    /// Represents conditional (null-propagating) access to a member.
    /// </summary>
    public sealed partial class ConditionalMemberCSharpExpression : ConditionalAccessCSharpExpression<MemberExpression>
    {
        internal ConditionalMemberCSharpExpression(Expression expression, MemberInfo member)
            : this(expression, MakeReceiver(expression), member)
        {
        }

        private ConditionalMemberCSharpExpression(Expression expression, ConditionalReceiver receiver, MemberInfo member)
            : base(expression, receiver, MakeAccess(receiver, member))
        {
        }

        private static MemberExpression MakeAccess(ConditionalReceiver receiver, MemberInfo member)
        {
            return Expression.MakeMemberAccess(receiver, member); // TODO: call ctor directly
        }

        internal static ConditionalMemberCSharpExpression Make(Expression expression, MemberInfo member)
        {
            return new ConditionalMemberCSharpExpression(expression, member); // TODO: remove layer of indirection if not needed
        }

        /// <summary>
        /// Gets the <see cref="Expression" /> that represents the instance whose member is accessed.
        /// </summary>
        public Expression Expression => Receiver;

        /// <summary>
        /// Gets the field or property to be accessed.
        /// </summary>
        public MemberInfo Member => WhenNotNull.Member;

        /// <summary>
        /// Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.
        /// </summary>
        /// <param name="expression">The <see cref="OldConditionalAccessCSharpExpression.Expression" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public ConditionalMemberCSharpExpression Update(Expression expression)
        {
            if (expression == Expression)
            {
                return this;
            }

            return CSharpExpression.MakeConditionalMemberAccess(expression, Member);
        }

        // TODO: Rewrite virtual
    }

    partial class CSharpExpression
    {
        /// <summary>
        /// Creates a <see cref="ConditionalMemberCSharpExpression" /> that represents a conditional (null-propagating) member lookup.
        /// </summary>
        /// <param name="expression">An <see cref="Expression" /> that specifies the instance to access the member of.</param>
        /// <param name="member">The <see cref="MemberInfo" /> representing the member to access conditionally.</param>
        /// <returns>A <see cref="ConditionalMemberCSharpExpression" /> that has the <see cref="CSharpNodeType" /> property equal to <see cref="CSharpExpressionType.ConditionalMemberAccess" /> and the <see cref="OldConditionalAccessCSharpExpression.Expression" /> and <see cref="Microsoft.CSharp.Expressions.ConditionalMemberCSharpExpression.Member" /> properties set to the specified values.</returns>
        public static ConditionalMemberCSharpExpression MakeConditionalMemberAccess(Expression expression, MemberInfo member)
        {
            ContractUtils.RequiresNotNull(member, nameof(member));

            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
            {
                return ConditionalField(expression, fieldInfo);
            }

            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
            {
                return ConditionalProperty(expression, propertyInfo);
            }

            // NB: LINQ doesn't allow a MethodInfo for a property getter here either; should we change this?

            throw LinqError.MemberNotFieldOrProperty(member);
        }

        /// <summary>
        /// Creates a <see cref="ConditionalMemberCSharpExpression" /> that represents a conditional (null-propagating) field lookup.
        /// </summary>
        /// <param name="expression">An <see cref="Expression" /> that specifies the instance to access the member of.</param>
        /// <param name="field">The <see cref="FieldInfo" /> representing the field to access conditionally.</param>
        /// <returns>A <see cref="ConditionalMemberCSharpExpression" /> that has the <see cref="CSharpNodeType" /> property equal to <see cref="CSharpExpressionType.ConditionalMemberAccess" /> and the <see cref="OldConditionalAccessCSharpExpression.Expression" /> and <see cref="ConditionalMemberCSharpExpression.Member" /> properties set to the specified values.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Done by helper method.")]
        public static ConditionalMemberCSharpExpression ConditionalField(Expression expression, FieldInfo field)
        {
            RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(field, nameof(field));

            if (field.IsStatic)
            {
                throw Error.ConditionalAccessRequiresNonStaticMember();
            }

            var type = expression.Type.GetNonNullReceiverType();

            if (!TypeUtils.AreReferenceAssignable(field.DeclaringType, type))
            {
                throw LinqError.FieldInfoNotDefinedForType(field.DeclaringType, field.Name, type);
            }

            return ConditionalMemberCSharpExpression.Make(expression, field);
        }

        /// <summary>
        /// Creates a <see cref="ConditionalMemberCSharpExpression" /> that represents a conditional (null-propagating) field lookup.
        /// </summary>
        /// <param name="expression">An <see cref="Expression" /> that specifies the instance to access the member of.</param>
        /// <param name="fieldName">The name of the field to access conditionally.</param>
        /// <returns>A <see cref="ConditionalMemberCSharpExpression" /> that has the <see cref="CSharpNodeType" /> property equal to <see cref="CSharpExpressionType.ConditionalMemberAccess" />, the <see cref="OldConditionalAccessCSharpExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="ConditionalMemberCSharpExpression.Member" /> property set to the <see cref="FieldInfo" /> that represents the field denoted by <paramref name="fieldName" />.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Done by helper method.")]
        public static ConditionalMemberCSharpExpression ConditionalField(Expression expression, string fieldName)
        {
            RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(fieldName, nameof(fieldName));

            var type = expression.Type.GetNonNullReceiverType();

            var field = type.GetField(fieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (field == null)
            {
                field = type.GetField(fieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            }

            if (field == null)
            {
                throw LinqError.InstanceFieldNotDefinedForType(fieldName, type);
            }

            return ConditionalField(expression, field);
        }

        /// <summary>
        /// Creates a <see cref="ConditionalMemberCSharpExpression" /> that represents a conditional (null-propagating) property lookup.
        /// </summary>
        /// <param name="expression">An <see cref="Expression" /> that specifies the instance to access the member of.</param>
        /// <param name="property">The <see cref="PropertyInfo" /> representing the property to access conditionally.</param>
        /// <returns>A <see cref="ConditionalMemberCSharpExpression" /> that has the <see cref="CSharpNodeType" /> property equal to <see cref="CSharpExpressionType.ConditionalMemberAccess" /> and the <see cref="OldConditionalAccessCSharpExpression.Expression" /> and <see cref="Microsoft.CSharp.Expressions.ConditionalMemberCSharpExpression.Member" /> properties set to the specified values.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Done by helper method.")]
        public static ConditionalMemberCSharpExpression ConditionalProperty(Expression expression, PropertyInfo property)
        {
            RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(property, nameof(property));

            if (!property.CanRead)
            {
                throw Error.ConditionalAccessRequiresReadableProperty();
            }

            if (property.GetIndexParameters().Length != 0)
            {
                throw Error.ConditionalAccessRequiresReadableProperty();
            }

            if (property.GetGetMethod(true).IsStatic)
            {
                throw Error.ConditionalAccessRequiresNonStaticMember();
            }

            var type = expression.Type.GetNonNullReceiverType();

            if (!TypeUtils.IsValidInstanceType(property, type))
            {
                throw LinqError.PropertyNotDefinedForType(property, type);
            }

            return ConditionalMemberCSharpExpression.Make(expression, property);
        }

        /// <summary>
        /// Creates a <see cref= "ConditionalMemberCSharpExpression" /> that represents a conditional (null-propagating) property lookup.
        /// </summary>
        /// <param name="expression">An <see cref="Expression" /> whose <see cref="Expression.Type" /> contains a property named <paramref name="propertyName" />.</param>
        /// <param name="propertyName">The name of a property to be accessed.</param>
        /// <returns>A <see cref="ConditionalMemberCSharpExpression" /> that has the <see cref="CSharpNodeType" /> property equal to <see cref="CSharpExpressionType.ConditionalMemberAccess" />, the <see cref="OldConditionalAccessCSharpExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="ConditionalMemberCSharpExpression.Member" /> property set to the <see cref="PropertyInfo" /> that represents the property denoted by <paramref name="propertyName" />.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Done by helper method.")]
        public static ConditionalMemberCSharpExpression ConditionalProperty(Expression expression, string propertyName)
        {
            RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(propertyName, nameof(propertyName));

            var type = expression.Type.GetNonNullReceiverType();

            var property = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (property == null)
            {
                property = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            }

            if (property == null)
            {
                throw LinqError.InstancePropertyNotDefinedForType(propertyName, type);
            }

            return ConditionalProperty(expression, property);
        }

        /// <summary>
        /// Creates a <see cref= "ConditionalMemberCSharpExpression" /> that represents a conditional (null-propagating) property lookup by using a property accessor method.
        /// </summary>
        /// <param name="expression">An <see cref="Expression" /> to set the <see cref="Expression" /> property equal to. This can be null for static properties.</param>
        /// <param name="propertyAccessor">The <see cref="MethodInfo" /> that represents a property accessor method.</param>
        /// <returns>A <see cref="ConditionalMemberCSharpExpression" /> that has the <see cref="CSharpNodeType" /> property equal to <see cref="CSharpExpressionType.ConditionalMemberAccess" />, the <see cref="OldConditionalAccessCSharpExpression.Expression" /> property set to <paramref name="expression" /> and the <see cref="ConditionalMemberCSharpExpression.Member" /> property set to the <see cref="PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />.</returns>
        public static ConditionalMemberCSharpExpression ConditionalProperty(Expression expression, MethodInfo propertyAccessor)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));

            ValidateMethodInfo(propertyAccessor);

            return ConditionalProperty(expression, GetProperty(propertyAccessor));
        }
        
        // TODO: Add PropertyOrField equivalent?
    }
}
