using System.Globalization;
using ErrorOr;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using Xunit.Abstractions;

namespace TestCommon.Logging;

/// <summary>
/// Provides a set of methods to assert that certain log messages were created.
/// Also provides a factory method to create a logger that logs to the test correlator.
/// </summary>
public static class TestLogger
{
    /// <summary>
    /// Creates a logger that logs to the test correlator and the test output.
    /// </summary>
    /// <param name="outputHelper">The test output helper.</param>
    /// <returns>A logger that logs to the test correlator and the test output.</returns>
    public static ILogger CreateWithNewContext(ITestOutputHelper outputHelper)
    {
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.TestCorrelator()
            .WriteTo.TestOutput(outputHelper)
            .MinimumLevel.Debug()
            .CreateLogger();

        TestCorrelator.CreateContext();

        return logger;
    }

    /// <summary>
    /// Asserts that a certain amount of log messages has been created.
    /// </summary>
    /// <param name="amount">The expected amount of log messages.</param>
    public static void HasLogEventCountEqualTo(int amount)
    {
        TestCorrelator
            .GetLogEventsFromCurrentContext()
            .Should()
            .HaveCount(amount);
    }

    /// <summary>
    /// Asserts that at least one log event with a property with the specified <paramref name="name"/>
    /// and <paramref name="expectedValue"/> was created.
    /// </summary>
    /// <param name="name">The name of the property</param>
    /// <param name="expectedValue">The expected value of the property</param>
    /// <param name="amount">The expected amount of log messages with the property</param>
    public static void HasLogEventWithPropertyScalarValueEqualTo(string name, object expectedValue, int? amount = null)
    {
        ValidateAmount(amount, TestCorrelator
            .GetLogEventsFromCurrentContext()
            .Where(logEvent =>
                logEvent.Properties.ContainsKey(name)
                && new ScalarValue(expectedValue).Equals(logEvent.Properties[name])));
    }

    /// <summary>
    /// Asserts that at least one log event with a property with the specified <paramref name="name"/>
    /// and <paramref name="expectedErrors"/> was created.
    /// </summary>
    /// <param name="name">The name of the property</param>
    /// <param name="expectedErrors">The expected values of the property</param>
    /// <param name="amount">The expected amount of log messages with the property</param>
    public static void HasLogEventWithPropertyWithListOfErrorsEqualTo(string name, IEnumerable<Error> expectedErrors, int? amount = null)
    {
        var expectedSequenceValue = expectedErrors.ToSequenceValue();

        ValidateAmount(amount, TestCorrelator
            .GetLogEventsFromCurrentContext()
            .Where(logEvent =>
                logEvent.Properties.ContainsKey(name)
                && logEvent.Properties[name] is SequenceValue sequenceValue
                && sequenceValue.Elements.Count == expectedSequenceValue.Elements.Count)
            .Select(logEvent => ((SequenceValue)logEvent.Properties[name])
                .Should().BeEquivalentTo(expectedSequenceValue)));
    }

    /// <summary>
    /// Asserts that at least one log event with <see cref="LogEventLevel.Error"/> was created.
    /// </summary>
    /// <param name="messageFilter">A filter expression for the log message.</param>
    /// <param name="amount">The expected amount of log messages.</param>
    public static void HasLogEventWithError(Func<string, bool>? messageFilter = null, int? amount = null)
        => HasLogEventWith(LogEventLevel.Error, messageFilter, amount);

    /// <summary>
    /// Asserts that at least one log event with <see cref="LogEventLevel.Error"/> was created.
    /// </summary>
    /// <param name="messageFilter">A filter expression for the log message.</param>
    /// <param name="amount">The expected amount of log messages.</param>
    public static void HasLogEventWithInformation(Func<string, bool>? messageFilter = null, int? amount = null)
        => HasLogEventWith(LogEventLevel.Information, messageFilter, amount);

    /// <summary>
    /// Asserts that at least one of the created log messages has a
    /// message template that is equal to <paramref name="expectedMessageTemplate"/>.
    /// </summary>
    /// <param name="expectedMessageTemplate">The expected messageTemplate.</param>
    /// <param name="amount">The expected amount of log messages.</param>
    public static void HasLogEventWithTemplateEqualTo(string expectedMessageTemplate, int? amount = null)
    {
        ValidateAmount(amount, TestCorrelator
            .GetLogEventsFromCurrentContext()
            .Where(logEvent => logEvent.MessageTemplate.Text
                .Equals(expectedMessageTemplate, StringComparison.InvariantCulture)));
    }

    /// <summary>
    /// Asserts that at least one of the created log messages has a
    /// message template that contains an equivalent to <paramref name="expectedEquivalent"/>.
    /// </summary>
    /// <param name="expectedEquivalent">The expected equivalent.</param>
    /// <param name="amount">The expected amount of log messages.</param>
    public static void HasLogEventWithTemplateEquivalentTo(string expectedEquivalent, int? amount = null)
    {
        ValidateAmount(amount, TestCorrelator
            .GetLogEventsFromCurrentContext()
            .Where(logEvent => logEvent.MessageTemplate.Text
                .Contains(expectedEquivalent, StringComparison.CurrentCultureIgnoreCase)));
    }

    /// <summary>
    /// Asserts that at least one log event with <paramref name="level"/> was created.
    /// </summary>
    /// <param name="level">The expected log level.</param>
    /// <param name="messageFilter">A filter expression for the log message.</param>
    /// <param name="amount">The expected amount of log messages.</param>
    private static void HasLogEventWith(LogEventLevel level, Func<string, bool>? messageFilter = null, int? amount = null)
    {
        var logEvents = TestCorrelator
            .GetLogEventsFromCurrentContext()
            .Where(logEvent => logEvent.Level == level);

        if (messageFilter is not null)
        {
            logEvents = logEvents.Where(logEvent =>
                messageFilter.Invoke(logEvent.RenderMessage(CultureInfo.InvariantCulture)));
        }

        ValidateAmount(amount, logEvents);
    }

    private static void ValidateAmount<TValue>(int? amount, IEnumerable<TValue> logEvents)
    {
        if (amount is not null)
        {
            logEvents.Should().HaveCount(amount.Value);
        }
        else
        {
            logEvents.Should().NotBeEmpty();
        }
    }

    private static SequenceValue ToSequenceValue(this IEnumerable<Error> errors)
    {
        return new SequenceValue(errors.Select(e => new StructureValue(new List<LogEventProperty>
        {
            new(nameof(e.Code), new ScalarValue(e.Code)),
            new(nameof(e.Description), new ScalarValue(e.Description)),
            new(nameof(e.Type), new ScalarValue(e.Type.ToString())),
            new(nameof(e.NumericType), new ScalarValue(e.NumericType)),
            new(nameof(e.Metadata), new ScalarValue(e.Metadata))
        })));
    }
}
