using ErrorOr;
using FluentAssertions;

namespace TestCommon.ErrorHandling;

/// <summary>
/// Extension methods for <see cref="ErrorOr{T}"/> to make assertions easier.
/// </summary>
public static class ErrorOrFluentAssertionExtensions
{
    /// <summary>
    /// Asserts that the <see cref="ErrorOr{TResult}"/> is not an error and returns the value.
    /// </summary>
    /// <param name="errorOr">The <see cref="ErrorOr{TResult}"/> to assert.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>The value of the <see cref="ErrorOr{TResult}"/>.</returns>
    public static TResult GetResultIfNoErrorsExist<TResult>(this ErrorOr<TResult> errorOr)
    {
        errorOr.IsError.Should().BeFalse();
        return errorOr.Value;
    }

    /// <summary>
    /// Asserts that the <see cref="ErrorOr.Error"/> is equivalent to the expected <see cref="ErrorOr.Error"/>.
    /// </summary>
    /// <param name="error">The <see cref="ErrorOr.Error"/> to assert.</param>
    /// <param name="expectedError">The expected <see cref="ErrorOr.Error"/>.</param>
    public static void ShouldBeEquivalentTo(this Error error, Error expectedError)
    {
        error.Should().BeEquivalentTo(expectedError, options => options.ComparingByMembers<Error>());
    }

    /// <summary>
    /// Asserts that the <see cref="IEnumerable{Error}"/> is equivalent to the expected <see cref="IEnumerable{Error}"/>.
    /// </summary>
    /// <param name="errors">The <see cref="IEnumerable{Error}"/> to assert.</param>
    /// <param name="expectedErrors">The expected <see cref="IEnumerable{Error}"/>.</param>
    public static void ShouldBeEquivalentTo(this IEnumerable<Error> errors, IEnumerable<Error> expectedErrors)
    {
        errors.Should().BeEquivalentTo(expectedErrors, options => options.ComparingByMembers<Error>());
    }

    /// <summary>
    /// Asserts that the <see cref="IEnumerable{Error}"/> contains a single error which is equivalent to the expected <see cref="ErrorOr.Error"/>.
    /// </summary>
    /// <param name="errors">The <see cref="IEnumerable{Error}"/> to assert.</param>
    /// <param name="expectedError">The expected <see cref="ErrorOr.Error"/>.</param>
    public static void ShouldContainSingleEquivalentTo(this IEnumerable<Error> errors, Error expectedError)
    {
        errors.Should().ContainSingle().Which.ShouldBeEquivalentTo(expectedError);
    }

    /// <summary>
    /// Asserts that the <see cref="ErrorOr{TResult}"/> contains a single error which is equivalent to the expected <see cref="ErrorOr.Error"/>.
    /// </summary>
    /// <param name="errorOr">The <see cref="ErrorOr{TResult}"/> to assert.</param>
    /// <param name="expectedError">The expected <see cref="ErrorOr.Error"/>.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public static void ShouldContainSingleEquivalentTo<TResult>(this ErrorOr<TResult> errorOr, Error expectedError)
    {
        errorOr.IsError.Should().BeTrue();
        errorOr.Errors.ShouldContainSingleEquivalentTo(expectedError);
    }
}
