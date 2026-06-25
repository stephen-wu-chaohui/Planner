using System.Reflection;
using HotChocolateAuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;
using Planner.API.GraphQL;

namespace Planner.API.Tests;

public sealed class GraphQLAuthorizationTests {
    [Fact]
    public void All_query_resolvers_require_authorization() {
        var publicResolvers = typeof(Query)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);

        publicResolvers.Should()
            .OnlyContain(method => HasAuthorizeAttribute(method));
    }

    [Fact]
    public void Mutation_type_requires_authorization() {
        typeof(Mutation)
            .GetCustomAttributes(typeof(HotChocolateAuthorizeAttribute), inherit: true)
            .Should()
            .NotBeEmpty();
    }

    private static bool HasAuthorizeAttribute(MethodInfo method) =>
        method.GetCustomAttributes(typeof(HotChocolateAuthorizeAttribute), inherit: true).Any();
}
