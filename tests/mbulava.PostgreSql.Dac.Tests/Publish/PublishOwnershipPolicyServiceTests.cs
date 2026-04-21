using FluentAssertions;
using mbulava.PostgreSql.Dac.Models;
using mbulava.PostgreSql.Dac.Publish;

namespace mbulava.PostgreSql.Dac.Tests.Publish;

[TestFixture]
public class PublishOwnershipPolicyServiceTests
{
    [Test]
    public void Apply_WithIgnoreOwnershipMode_DisablesOwnerComparison()
    {
        var options = new PublishOptions
        {
            OwnershipMode = OwnershipMode.Ignore
        };

        var service = new PublishOwnershipPolicyService();

        service.Apply(options);

        options.CompareOptions.CompareOwners.Should().BeFalse();
    }

    [Test]
    public void Apply_WithEnforceOwnershipMode_EnablesOwnerComparison()
    {
        var options = new PublishOptions
        {
            OwnershipMode = OwnershipMode.Enforce
        };

        var service = new PublishOwnershipPolicyService();

        service.Apply(options);

        options.CompareOptions.CompareOwners.Should().BeTrue();
    }

    [Test]
    public void ValidateExplicitOwners_WithMissingRole_ReturnsError()
    {
        var project = new PgProject
        {
            Schemas = new List<PgSchema>
            {
                new()
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new()
                        {
                            Name = "users",
                            Owner = "app_owner"
                        }
                    }
                }
            }
        };

        var service = new PublishOwnershipPolicyService();

        var errors = service.ValidateExplicitOwners(project);

        errors.Should().ContainSingle().Which.Should().Contain("app_owner");
    }
}
