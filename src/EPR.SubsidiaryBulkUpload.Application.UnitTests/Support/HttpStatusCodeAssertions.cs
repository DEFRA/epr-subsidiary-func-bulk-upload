using System.Net;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;

public class HttpStatusCodeAssertions(HttpStatusCode statusCode) : ReferenceTypeAssertions<HttpStatusCode, HttpStatusCodeAssertions>(statusCode)
{
    protected override string Identifier => "HttpStatusCode";

    public AndConstraint<HttpStatusCodeAssertions> BeSuccessful(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(((int)Subject >= 200) && ((int)Subject < 300))
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:HttpStatusCode} to be a successful status code (200-299){reason}, but found {0}.", Subject);

        return new AndConstraint<HttpStatusCodeAssertions>(this);
    }

    public AndConstraint<HttpStatusCodeAssertions> Be(HttpStatusCode expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .ForCondition(Subject == expected)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:HttpStatusCode} to be {0}{reason}, but found {1}.", expected, Subject);

        return new AndConstraint<HttpStatusCodeAssertions>(this);
    }
}
