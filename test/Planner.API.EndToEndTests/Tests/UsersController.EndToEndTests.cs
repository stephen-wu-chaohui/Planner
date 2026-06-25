using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Planner.API.Controllers;
using Planner.API.EndToEndTests.Fixtures;
using Planner.Application;
using Planner.Application.Features.Users;
using Planner.Domain;
using Planner.Infrastructure.Persistence;
using Planner.Testing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Planner.API.EndToEndTests.Tests;

public sealed class UsersControllerEndToEndTests {
    [Fact]
    public void UsersController_requires_authorization() {
        typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task GetUsers_returns_only_current_tenant_users() {
        using var factory = new TestApiFactory();
        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();

        db.Users.AddRange(
            new User {
                TenantId = tenant.TenantId,
                Email = "current@example.com",
                Role = "Admin"
            },
            new User {
                TenantId = Guid.NewGuid(),
                Email = "other@example.com",
                Role = "Admin"
            });
        await db.SaveChangesAsync();

        var controller = new UsersController(
            factory.Get<IMediator>(),
            factory.Get<ILogger<UsersController>>());
        controller.MockUserContext();

        var result = await controller.GetUsers(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ok.Value!
            .GetType()
            .GetProperty("Data")!
            .GetValue(ok.Value)
            .Should()
            .BeAssignableTo<List<UserSummary>>()
            .Subject;

        data.Should().ContainSingle();
        data[0].Email.Should().Be("current@example.com");
    }
}
