using Microsoft.Extensions.Options;

namespace EPR.SubsidiaryBulkUpload.Application.UnitTests.Support;

public static class OptionsExtensions
{
    public static IOptions<TOptions> CreateOptions<TOptions>(this Fixture fixture)
        where TOptions : class
    {
        var model = fixture.Create<TOptions>();

        var options = new Mock<IOptions<TOptions>>();
        options.Setup(options => options.Value).Returns(model);

        return options.Object;
    }
}
