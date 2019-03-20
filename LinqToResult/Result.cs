using System;
using System.Threading.Tasks;

namespace LinqToResult {
    public readonly struct Result<TOk, TError> {
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

        public TResult Either<TResult>(Func<TOk, TResult> okSelector, Func<TError, TResult> errorSelector) =>
            IsSuccess ? okSelector(_Ok) : errorSelector(_Error)
        ;

        public Result<TOk, TError> Do(Action<TOk> okAction) {
            if (IsSuccess) okAction(_Ok);
            return this;
        }

        public void Do(Action<TOk> okAction, Action<TError> errorAction) {
            if (IsSuccess) okAction(_Ok);
            else errorAction(_Error);
        }


        public static Result<TOk, TError> Ok(TOk ok) => new Result<TOk, TError>(ok);
        public static Result<TOk, TError> Error(TError error) => new Result<TOk, TError>(error);

        #region LINQ

        public Result<TNewOk, TError> Select<TNewOk>(Func<TOk, TNewOk> selector) => IsSuccess ? Result<TNewOk, TError>.Ok(selector(_Ok)) : Result<TNewOk, TError>.Error(_Error);
        public Result<TOk, TNewError> SelectError<TNewError>(Func<TError, TNewError> selector) => IsSuccess ? Result<TOk, TNewError>.Ok(_Ok) : Result<TOk, TNewError>.Error(selector(_Error));

        private readonly struct Combiner<TNewOk, TResult> {
            readonly TOk _Ok;
            readonly Func<TOk, TNewOk, TResult> _Combine;
            public Combiner(TOk ok, Func<TOk, TNewOk, TResult> combine) { _Ok = ok; _Combine = combine; }
            public TResult Combine(TNewOk newOk) => _Combine(_Ok, newOk);
        }

        public Result<TResult, TError> SelectMany<TOut, TResult>(ResultSelector<TOk, TOut, TError> selector, Func<TOk, TOut, TResult> combine) =>
            IsSuccess
            ? selector(_Ok).Select(new Combiner<TOut, TResult>(_Ok, combine).Combine)
            : Result<TResult, TError>.Error(_Error)
        ;

        public async Task<Result<TResult, TError>> SelectMany<TOut, TResult>(AsyncResultSelector<TOk, TOut, TError> selector, Func<TOk, TOut, TResult> combine) =>
            IsSuccess
            ? (await selector(_Ok)).Select(new Combiner<TOut, TResult>(_Ok, combine).Combine)
            : Result<TResult, TError>.Error(_Error)
        ;


        #endregion

    }

    public delegate Result<TOut, TError> ResultSelector<in TIn, TOut, TError>(TIn @in);
    public delegate Task<Result<TOut, TError>> AsyncResultSelector<in TIn, TOut, TError>(TIn @in);

    public static class ResultExtensions {
        public static T Collapse<T>(this in Result<T, T> result) => result.Either(a => a, a => a);

        public static async Task<Result<TOut, TError>> Select<TIn, TOut, TError>(this Task<Result<TIn, TError>> source, Func<TIn, TOut> selector) =>
            (await source.ConfigureAwait(false)).Select(selector)
        ;

        public static async Task<Result<TResult, TError>> SelectMany<TIn, TOut, TResult, TError>(this Task<Result<TIn, TError>> source, ResultSelector<TIn, TOut, TError> selector, Func<TIn, TOut, TResult> combine) =>
            (await source.ConfigureAwait(false)).SelectMany(selector, combine)
        ;

        public static async Task<Result<TResult, TError>> SelectMany<TIn, TOut, TResult, TError>(this Task<Result<TIn, TError>> source, AsyncResultSelector<TIn, TOut, TError> selector, Func<TIn, TOut, TResult> combine) =>
            await (await source.ConfigureAwait(false)).SelectMany(selector, combine).ConfigureAwait(false)
        ;
        public static async Task<Result<TOk, TError>> Do<TOk, TError>(this Task<Result<TOk, TError>> source, Action<TOk> okAction) =>
            (await source).Do(okAction)
        ;

        public static async Task<Result<TOk, TError>> AsResult<TOk, TError>(this Task<TOk> source) =>
            Result<TOk, TError>.Ok(await source)
        ;
    }
}
