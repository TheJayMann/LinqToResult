using BenchmarkDotNet.Attributes;
using LinqToResult;

namespace Benchmark {
    [CoreJob, ClrJob]
    [MemoryDiagnoser]
    public class Benchmark {

        private Result<int, string> GetNumber() => Result<int, string>.Ok(42);
        private Result<int, string> GetNumberError() => Result<int, string>.Error("Error 42");

        private Result<string, string> GetStringFromNumber(int number) => Result<string, string>.Ok("Forty-two");
        private Result<string, string> GetStringFromNumberError(int number) => Result<string, string>.Error("Error Forty-two");

        private Result<string, string> GetMessage(int number, string words) => Result<string, string>.Error("42 Forty-two");
        private Result<string, string> GetMessageError(int number, string words) => Result<string, string>.Error("Error Message");

        [Benchmark]
        public string WithoutError() =>
            (
                from num in GetNumber()
                from str in GetStringFromNumber(num)
                from msg in GetMessage(num, str)
                select msg
            )
            .Collapse()
        ;
        [Benchmark]
        public string WithErrorAtEnd() =>
            (
                from num in GetNumber()
                from str in GetStringFromNumber(num)
                from msg in GetMessageError(num, str)
                select msg
            )
            .Collapse()
        ;
        [Benchmark]
        public string WithErrorInMiddle() =>
            (
                from num in GetNumber()
                from str in GetStringFromNumberError(num)
                from msg in GetMessage(num, str)
                select msg
            )
            .Collapse()
        ;
        [Benchmark]
        public string WithErrorAtBegining() =>
            (
                from num in GetNumberError()
                from str in GetStringFromNumber(num)
                from msg in GetMessage(num, str)
                select msg
            )
            .Collapse()
        ;
        [Benchmark]
        public string WithAllError() =>
            (
                from num in GetNumberError()
                from str in GetStringFromNumberError(num)
                from msg in GetMessageError(num, str)
                select msg
            )
            .Collapse()
        ;
    }
}
