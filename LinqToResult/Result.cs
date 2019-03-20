using System;
using System.Threading.Tasks;

namespace LinqToResult {
    /// <summary>
    /// Represents the result of an operation which has either succeeded or failed
    /// </summary>
    /// <typeparam name="TOk">The type of the value contained if the operation succeeded</typeparam>
    /// <typeparam name="TError">The type of the value contained if the operation failed</typeparam>
    public readonly struct Result<TOk, TError> {
        /// <summary>
        /// Returns true if the operation succeeded.  Otherwise, false.
        /// </summary>
        public bool IsSuccess { get; }
        private readonly TOk _Ok;
        private readonly TError _Error;
        private Result(TOk ok) {
            IsSuccess = true;
            _Ok = ok;
            _Error = default;
        }
        private Result(TError error) {
            IsSuccess = false;
            _Error = error;
            _Ok = default;
        }

        /// <summary>
        /// Collapses the result into a single value by applying the contained value to an appropriate
        /// conversion function
        /// </summary>
        /// <typeparam name="TResult">
        /// The type to which the contained value will be converted
        /// </typeparam>
        /// <param name="okSelector">
        /// A function which will convert the contained value representing success
        /// </param>
        /// <param name="errorSelector">
        /// A function which will convert the contained value representing failure
        /// </param>
        /// <returns>
        /// The converted value created by the appropriate conversion function
        /// </returns>
        /// <seealso cref="ResultExtensions.Collapse{T}(in Result{T, T})"/>
        public TResult Either<TResult>(Func<TOk, TResult> okSelector, Func<TError, TResult> errorSelector) =>
            IsSuccess ? okSelector(_Ok) : errorSelector(_Error)
        ;

        /// <summary>
        /// Performs an action on the contained value if it represents success
        /// </summary>
        /// <param name="okAction">
        /// An action to perform on the contained value if it represents success
        /// </param>
        /// <returns>The current result as is</returns>
        public Result<TOk, TError> Do(Action<TOk> okAction) {
            if (IsSuccess) okAction(_Ok);
            return this;
        }

        /// <summary>
        /// Performs an action on the contained value
        /// </summary>
        /// <param name="okAction">
        /// The action to perform if the contained value represents success
        /// </param>
        /// <param name="errorAction">
        /// The action to perform if the contained value represents failure
        /// </param>
        public void Do(Action<TOk> okAction, Action<TError> errorAction) {
            if (IsSuccess) okAction(_Ok);
            else errorAction(_Error);
        }

        /// <summary>
        /// Creates a new successful <see cref="Result{TOk, TError}"/>
        /// </summary>
        /// <param name="ok">The value representing success</param>
        /// <returns>A new successful <see cref="Result{TOk, TError}"/></returns>
        /// <seealso cref="Error(TError)"/>
        public static Result<TOk, TError> Ok(TOk ok) => new Result<TOk, TError>(ok);

        /// <summary>
        /// Creats a new failed <see cref="Result{TOk, TError}"/>
        /// </summary>
        /// <param name="error">The value representing failure</param>
        /// <returns>A new failed <see cref="Result{TOk, TError}"/></returns>
        /// <seealso cref="Ok(TOk)"/>
        public static Result<TOk, TError> Error(TError error) => new Result<TOk, TError>(error);

        #region LINQ

        /// <summary>
        /// Creates a new <see cref="Result{TNewOk, TError}"/>, transforming the value if it
        /// represents success
        /// </summary>
        /// <typeparam name="TNewOk">
        /// The type representing success of the new <see cref="Result{TNewOk, TError}"/>
        /// </typeparam>
        /// <param name="selector">A conversion function for the value if it represents success</param>
        /// <returns>
        /// A new <see cref="Result{TNewOk, TError}"/> with the value converted if it represents
        /// success, or the unmodified value if it represents failure
        /// </returns>
        /// <remarks>
        /// This method is used to support the <c>let</c> and <c>select</c> keywords when using
        /// LINQ syntax.
        /// </remarks>
        /// <seealso cref="SelectError{TNewError}(Func{TError, TNewError})"/>
        public Result<TNewOk, TError> Select<TNewOk>(Func<TOk, TNewOk> selector) => IsSuccess ? Result<TNewOk, TError>.Ok(selector(_Ok)) : Result<TNewOk, TError>.Error(_Error);

        /// <summary>
        /// Creates a new <see cref="Result{TOk, TNewError}"/>, transforming the value if it
        /// represents failure
        /// </summary>
        /// <typeparam name="TNewError">
        /// The type representing failure of the new <see cref="Result{TOk, TNewError}"/>
        /// </typeparam>
        /// <param name="selector">A conversion function for the value if it represents failure</param>
        /// <returns>
        /// A new <see cref="Result{TOk, TNewError}"/> with the value converted if it represents
        /// failure, or the unmodified value if it represents success
        /// </returns>
        /// <seealso cref="Select{TNewOk}(Func{TOk, TNewOk})"/>
        public Result<TOk, TNewError> SelectError<TNewError>(Func<TError, TNewError> selector) => IsSuccess ? Result<TOk, TNewError>.Ok(_Ok) : Result<TOk, TNewError>.Error(selector(_Error));

        /// <summary>
        /// The purpose of the <see cref="Combiner{TNewOk, TResult}"/> type is to manually create
        /// a closure, as closures are not allowed on value types.  It has been made readonly for
        /// performance reasons to prevent creation of defensive copies when it is used.
        /// </summary>
        private readonly struct Combiner<TNewOk, TResult> {
            readonly TOk _Ok;
            readonly Func<TOk, TNewOk, TResult> _Combine;
            public Combiner(TOk ok, Func<TOk, TNewOk, TResult> combine) { _Ok = ok; _Combine = combine; }
            public TResult Combine(TNewOk newOk) => _Combine(_Ok, newOk);
        }

        /// <summary>
        /// Monadically composes the <see cref="Result{TOk, TError}"/> with the next operation
        /// by either processing the successful value with the continuation, or immediately
        /// returning the failed value without process.
        /// </summary>
        /// <typeparam name="TOut">The type of the success value of the continuation</typeparam>
        /// <typeparam name="TResult">
        /// The success value after combining the initial value with the value of the continuation
        /// </typeparam>
        /// <param name="selector">
        /// The function which processes the successful value and returns a new
        /// <see cref="Result{TOut, TError}"/>
        /// </param>
        /// <param name="combine">
        /// A function which combines the current successful value with the new successful value 
        /// of the continuation and combines them into a <typeparamref name="TResult"/>
        /// </param>
        /// <remarks>
        /// This method is used to support multiple uses of the <c>from</c> keywords when using
        /// LINQ syntax.  The <paramref name="combine"/> parameter is used by the LINQ syntax
        /// to allow multiple uses of the from clause while avoiding nested lambdas.
        /// </remarks>
        public Result<TResult, TError> SelectMany<TOut, TResult>(ResultSelector<TOk, TOut, TError> selector, Func<TOk, TOut, TResult> combine) =>
            IsSuccess
            ? selector(_Ok).Select(new Combiner<TOut, TResult>(_Ok, combine).Combine)
            : Result<TResult, TError>.Error(_Error)
        ;

        /// <summary>
        /// Monadically composes the <see cref="Result{TOk, TError}"/> with the next asynchronous
        /// operation by either processing the successful value with the continuation, or 
        /// immediately returning the failed value without process.
        /// </summary>
        /// <typeparam name="TOut">The type of the success value of the continuation</typeparam>
        /// <typeparam name="TResult">
        /// The success value after combining the initial value with the value of the continuation
        /// </typeparam>
        /// <param name="selector">
        /// The function which asynchronously processes the successful value and returns a new
        /// <see cref="Task{Result{TOut, TError}}"/>
        /// </param>
        /// <param name="combine">
        /// A function which combines the current successful value with the new successful value 
        /// of the continuation and combines them into a <typeparamref name="TResult"/>
        /// </param>
        /// <remarks>
        /// This method is used to support multiple uses of the <c>from</c> keywords when using
        /// LINQ syntax.  The <paramref name="combine"/> parameter is used by the LINQ syntax
        /// to allow multiple uses of the from clause while avoiding nested lambdas.
        /// </remarks>
        public async Task<Result<TResult, TError>> SelectMany<TOut, TResult>(AsyncResultSelector<TOk, TOut, TError> selector, Func<TOk, TOut, TResult> combine) =>
            IsSuccess
            ? (await selector(_Ok)).Select(new Combiner<TOut, TResult>(_Ok, combine).Combine)
            : Result<TResult, TError>.Error(_Error)
        ;


        #endregion

    }

    /// <summary>
    /// Defines a continuation function for processing a <see cref="Result{TIn, TError}"/>
    /// with a <see cref="Result{TOut, TError}"/>
    /// </summary>
    public delegate Result<TOut, TError> ResultSelector<in TIn, TOut, TError>(TIn @in);
    /// <summary>
    /// Defines an asynchronous continuation function for processing a 
    /// <see cref="Result{TIn, TError}"/> with a <see cref="Result{TOut, TError}"/>
    /// </summary>
    public delegate Task<Result<TOut, TError>> AsyncResultSelector<in TIn, TOut, TError>(TIn @in);

    public static class ResultExtensions {

        /// <summary>
        /// Obtains the contained value regardless of whether it represents success or failure
        /// </summary>
        /// <typeparam name="T">The type of the contained value</typeparam>
        /// <returns>The contained value</returns>
        /// <remarks>
        /// This method is defined as <c>Either(okValue => okValue, errorValue => errorValue)</c>
        /// </remarks>
        /// <seealso cref="Result{TOk, TError}.Either{TResult}(Func{TOk, TResult}, Func{TError, TResult})"/>
        public static T Collapse<T>(this in Result<T, T> result) => result.Either(a => a, a => a);

        /// <summary>
        /// Creates a new <see cref="Result{TOut, TError}"/>, transforming the value if it
        /// represents success
        /// </summary>
        /// <typeparam name="TOut">
        /// The type representing success of the new <see cref="Result{TOut, TError}"/>
        /// </typeparam>
        /// <param name="selector">A conversion function for the value if it represents success</param>
        /// <returns>
        /// A task eventually containing a new <see cref="Result{TNewOk, TError}"/> with the value
        /// converted if it represents  success, or the unmodified value if it represents failure
        /// </returns>
        /// <see cref="Result{TOk, TError}.Select{TNewOk}(Func{TOk, TNewOk})"/>
        /// <seealso cref="SelectError{TOk, TIn, TOut}(Task{Result{TOk, TIn}}, Func{TIn, TOut})"/>
        public static async Task<Result<TOut, TError>> Select<TIn, TOut, TError>(this Task<Result<TIn, TError>> source, Func<TIn, TOut> selector) =>
            (await source.ConfigureAwait(false)).Select(selector)
        ;

        /// <summary>
        /// Asynchronously creates a new <see cref="Result{TOk, TOut}"/>, transforming the
        /// value if it represents failure
        /// </summary>
        /// <typeparam name="TOut">
        /// The type representing failure of the new <see cref="Result{TOk, TOut}"/>
        /// </typeparam>
        /// <param name="selector">A conversion function for the value if it represents failure</param>
        /// <returns>
        /// A task eventually containing a new <see cref="Result{TOk, TOut}"/> with the value 
        /// converted if it represents  failure, or the unmodified value if it represents success
        /// </returns>
        /// <see cref="Result{TOk, TError}.SelectError{TNewError}(Func{TError, TNewError})"/>
        /// <seealso cref="Select{TIn, TOut, TError}(Task{Result{TIn, TError}}, Func{TIn, TOut})"/>
        public static async Task<Result<TOk, TOut>> SelectError<TOk, TIn, TOut>(this Task<Result<TOk, TIn>> source, Func<TIn, TOut> selector) =>
            (await source.ConfigureAwait(false)).SelectError(selector)
        ;

        /// <summary>
        /// Monadically and asynchronously composes the <see cref="Result{TOk, TError}"/> with
        /// the next operation by either processing the successful value with the continuation,
        /// or immediately returning the failed value without process.
        /// </summary>
        /// <typeparam name="TIn">The type of the success value of the source</typeparam>
        /// <typeparam name="TOut">The type of the success value of the continuation</typeparam>
        /// <typeparam name="TResult">
        /// The success value after combining the initial value with the value of the continuation
        /// </typeparam>
        /// <typeparam name="TError">The type of the error value</typeparam>
        /// <param name="selector">
        /// The function which processes the successful value and returns a new
        /// <see cref="Result{TOut, TError}"/>
        /// </param>
        /// <param name="combine">
        /// A function which combines the current successful value with the new successful value 
        /// of the continuation and combines them into a <typeparamref name="TResult"/>
        /// </param>
        /// <remarks>
        /// This method is used to support multiple uses of the <c>from</c> keywords when using
        /// LINQ syntax.  The <paramref name="combine"/> parameter is used by the LINQ syntax
        /// to allow multiple uses of the from clause while avoiding nested lambdas.
        /// </remarks>
        /// <see cref="Result{TOk, TError}.SelectMany{TOut, TResult}(ResultSelector{TOk, TOut, TError}, Func{TOk, TOut, TResult})"/>
        public static async Task<Result<TResult, TError>> SelectMany<TIn, TOut, TResult, TError>(this Task<Result<TIn, TError>> source, ResultSelector<TIn, TOut, TError> selector, Func<TIn, TOut, TResult> combine) =>
            (await source.ConfigureAwait(false)).SelectMany(selector, combine)
        ;

        /// <summary>
        /// Monadically and asynchronously composes the <see cref="Result{TOk, TError}"/> with
        /// the next asynchronous operation by either processing the successful value with the
        /// continuation, or immediately returning the failed value without process.
        /// </summary>
        /// <typeparam name="TIn">The type of the success value of the source</typeparam>
        /// <typeparam name="TOut">The type of the success value of the continuation</typeparam>
        /// <typeparam name="TResult">
        /// The success value after combining the initial value with the value of the continuation
        /// </typeparam>
        /// <typeparam name="TError">The type of the error value</typeparam>
        /// <param name="selector">
        /// The function which asynchronously processes the successful value and returns a new
        /// <see cref="Task{Result{TOut, TError}}"/>
        /// </param>
        /// <param name="combine">
        /// A function which combines the current successful value with the new successful value 
        /// of the continuation and combines them into a <typeparamref name="TResult"/>
        /// </param>
        /// <remarks>
        /// This method is used to support multiple uses of the <c>from</c> keywords when using
        /// LINQ syntax.  The <paramref name="combine"/> parameter is used by the LINQ syntax
        /// to allow multiple uses of the from clause while avoiding nested lambdas.
        /// </remarks>
        /// <see cref="Result{TOk, TError}.SelectMany{TOut, TResult}(AsyncResultSelector{TOk, TOut, TError}, Func{TOk, TOut, TResult})"/>
        public static async Task<Result<TResult, TError>> SelectMany<TIn, TOut, TResult, TError>(this Task<Result<TIn, TError>> source, AsyncResultSelector<TIn, TOut, TError> selector, Func<TIn, TOut, TResult> combine) =>
            await (await source.ConfigureAwait(false)).SelectMany(selector, combine).ConfigureAwait(false)
        ;

        /// <summary>
        /// Asynchronosly performs an action on the contained value if it represents success
        /// </summary>
        /// <param name="okAction">
        /// An action to perform on the contained value if it represents success
        /// </param>
        /// <returns>The current result as is</returns>
        public static async Task<Result<TOk, TError>> Do<TOk, TError>(this Task<Result<TOk, TError>> source, Action<TOk> okAction) =>
            (await source).Do(okAction)
        ;

        /// <summary>
        /// Converts a task value into an asynchronous <see cref="Result{TOk, TError}"/> so that
        /// it can be properly composed in a LINQ query
        /// </summary>
        public static async Task<Result<TOk, TError>> AsResult<TOk, TError>(this Task<TOk> source) =>
            Result<TOk, TError>.Ok(await source)
        ;
    }
}
