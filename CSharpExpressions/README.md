# C# Expression API

This project contains C#-specific extensions to the `System.Linq.Expressions` API to support C# language constructs that were added after C# 3.0. It only contains the runtime library; the C# compiler changes required to support assignment of lambda expressions containing those language constructs to an `Expression<TDelegate>` is maintained separately.

## API Basics

The top-level namespace of the API is `Microsoft.CSharp.Expressions`. Unless specified otherwise, all references to types in the description below will assume this namespace.

### CSharpExpression

All C#-specific expression tree node types derive from `CSharpExpression` which in turn derives from `System.Linq.Expressions.Expression`. This type also provides factory methods for the various C#-specific expression tree nodes. Users familiar with the LINQ expression tree API should feel familiar with this design immediately.

In order to distinguish different node kinds, the `CSharpExpression` type exposes an instance property `CSharpNodeType` of enumeration type `CSharpExpressionType`. The `NodeType` property derived from `System.Linq.Expressions.Expression` always returns `System.Linq.Expressions.ExpressionType.Extension`.

### Visitors

Analogous to the LINQ expression tree API, an expression visitor is provided using an abstract base class called `CSharpExpressionVisitor`, which is derived from `System.Linq.Expressions.ExpressionVisitor` and overrides `VisitExtension` to dispatch into protected virtual `Visit` methods for each C# node type. It uses the typical dispatch pattern whereby the `CSharpExpression` types expose an `Accept` method.

### Compilation

Support for runtime compilation through the `Compile` methods on `System.Linq.Expressions.LambdaExpression` and `System.Linq.Expressions.Expression<TDelegate>` is provided by making the C#-specific expression tree nodes reducible. The `Reduce` method returns a more primitive LINQ expression tree which the compiler and interpreter in the LINQ API can deal with. By using this implementation strategy, the LINQ APIs don't have to change.

Note that compilation of asynchronous lambda expressions is supported through `Compile` methods on `AsyncLambdaCSharpExpression` and `AsyncCSharpExpression<TDelegate>`. This works around the limitation of LINQ's lambda expression nodes being sealed types. For more information, read on.

Also note that some language constructs such as indexer initializers introduced in C# 6.0 can't be provided as reducible extensions to the LINQ APIs. For more information, read on.

## Supported Language Features

In the following sections, we'll describe the C# language features that are supported in the `Microsoft.CSharp.Expressions` API, excluding those that are already supported by the LINQ expression APIs.

### C# 3.0

#### Multi-dimensional array initializers

One omission in the LINQ expression API is support for multi-dimensional array initializers. An example of this restriction is shown below:

```csharp
Expression<Func<int[,]>> f = () => new int[2, 2] { { 1, 2 }, { 3, 4 } };
```

This will fail to compile with:

```
error CS0838: An expression tree may not contain a multidimensional array initializer
```

Note that the bounds of the array have to be compile-time constants. Therefore, the number of expected elements is known statically.

In order to lift this restriction, we introduced `NewMultidimensionalArrayInitCSharpExpression` which contains a list of expressions denoting the array elements listed in row-major order. The node is parameterized on an array of `Int32` bounds for the array's dimensions. An example of creating an instance of this node typeis shown below:

```csharp
CSharpExpression.NewMultidimensionalArrayInit(
  typeof(int), new[] { 2, 2 },
  Expression.Constant(1), Expression.Constant(2),
  Expression.Constant(3), Expression.Constant(4)
);
```

The `GetExpression(int[])` method can be used to retrieve an expression representing an element in the array. This is useful when analyzing an instance of the node type.

Reduction of the node produces a `Block` expression containing a `NewArrayBounds` expression to instantiate an empty multi-dimensional array, followed by a series of `Assign` binary expressions that assign the elements of the array via `ArrayAccess` index expressions. The expressions representing the elements are evaluated in left-to-right row-major order as the language prescribes.