[![Build status](https://dev.azure.com/themann/LinqToResult/_apis/build/status/LinqToResult-CI)](https://dev.azure.com/themann/LinqToResult/_build/latest?definitionId=-1)
[![Release status](https://vsrm.dev.azure.com/themann/_apis/public/Release/badge/d3d5d22b-da2a-4356-82f5-afa1a1243750/1/1)](https://dev.azure.com/themann/LinqToResult/_release/latest?definitionId=-1)
[![Nuget](https://img.shields.io/nuget/v/LinqToResult.svg)](https://www.nuget.org/packages/LinqToResult/)
# LinqToResult - Railway Oriented Programming using LINQ

This library attempts to use the ROP design by using LINQ.  In most functional
programming languages, ROP is implemented by making use of the `Result` or 
`Either` monad.  In C#, one of the two methods for having language integrated
monad support is via LINQ.

This library implements the `Result` monad as a value type implementing the
`Select` and `SelectMany` methods.  This allows multiple `from` and `let` clauses,
the `select` clause, and the `select ... into` clause.  Most other clauses tend
to be collection specific and are not currently implemented.

## Creating a `Result<TOk,TError>`

Two static methods exist for creating a result, `Ok` and `Error`.  This will
create the result, set the success status, and set the value to the value
passed in.

## Using the `Result<TOk,TError>` type

The main focus of this project is the `Result<TOk, TError>` type.  This type
can hold one of two values, either a `TOk` to indicate that the process was
successful, or a `TError` to indicate the process failed.  Direct access to
the value is not allowed; instead, callbacks are used to process the value
to prevent attempting to access a value which is invalid for the given context.
There are five methods which allow access to the internal value.

### `Either`

The `Either` method ends the ROP flow by converting the final result to a
single value.  Two callbacks are required, one of which will be called with
the `TOk` value if the result indicates success, and one of which will be
called with the `TError` value if the result indicates failure.  Both of
these callbacks are expected to return the same type.  This method returns
the value of whichever callback was called.

### `Do`

There are two overloads of the `Do` method.  They are used to perform actions
based on the value of the result.

#### `Do(Action<TOk> okAction)`

The first overload is used to perform inline operations during the ROP
flow process.  Assuming the result indicates success, the callback will
be called with the current `TOk` value.  If the result indicates failure,
the callback is ignored.  The original result is returned to allow continued
use in the ROP flow.

#### `Do(Action<TOk> okAction, Action<TError> errorAction)`

The second overload ends the ROP flow by performing an action on the value
of the result.  Two callbacks are required. `okAction` is called if the
result was successful, and `errorAction` is called if the result failed.

### `Select`

The `Select` method is used to change the `TOk` value to a new value. This
results in a new `Result` with the new `TOk` value if the result indicates
success.  A callback is required which will convert the `TOk` to a new `TOk`.

### `SelectError`

The `SelectError` method is similar to the `Select` method, except that it
converts the `TError` value.

### `Collapse`

The `Collapse` method is available for `Result` only in the specific case that
`TOk` and `TError` are the same type.  The `Collapse` method returns the value.
It is the same as calling the `Either` method as `Either(_ => _, _ => _)`

## Using LinqToResult

Typical use of LinqToResult is to create individual steps of an application as
methods returning the `Result` type if the method has a failure case, and
returning a regular value if it does not have a failure case.

```csharp
from value1 in MethodReturningResult1()
from value2 in MethodReturningResult2(value1)
let value3 = NoFailMethod(value1, value2)
select (value2, value3)
```

Each `from` clause will assign the `TOk` value from the result if the result
indicates success, and will continue executing the rest of the query.  In the
case the result indicates failure, the result is immediately returned without
executing the rest of the query.  In this way, it is similar to exception
handling, but without having to allocate an exception object and without
creating a stack trace.  For expected errors, this has both a performance
benefit as well as requiring that errors cases are handled without having
to handle error cases at every invocation.

## Asynchronous LinqToResult

Given the proliferation of the async/await pattern in C#, a many methods
which would return a result will also be asynchronous.  In order to make
asynchronouos results easier to work with, most operations defined for
`Result<TOk,TError>` have been defined as extensions for 
`Task<Result<TOk,TError>`.  Specifically, `SelectMany` is defined on
`Result<TOk,TError>` accepting both `Result<TOk,TError>` and
`Task<Result<TOk,TError>>`, and `Task<Result<TOk,TError>>` is extended
to support accepting both `Result<TOk,TError>` and
`Task<Result<TOk,TError>>`. 

Given how LINQ queries make use of the `Select` method, it is not possible
to make use of `Task<T>` values within a LINQ query where `T` is not
`Result<TOk,TError>`.  To allow such operations, `Task<T>` has been
extended to support `AsResult` which will convert a `Task<T>` into
a `Task<Result<T,TError>`.