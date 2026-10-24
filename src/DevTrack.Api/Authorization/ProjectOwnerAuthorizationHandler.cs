using System.Security.Claims;
using DevTrack.Core.Entities;
using Microsoft.AspNetCore.Authorization;

namespace DevTrack.Api.Authorization;

public sealed class ProjectOwnerRequirement : IAuthorizationRequirement;

public sealed class ProjectOwnerAuthorizationHandler : AuthorizationHandler<ProjectOwnerRequirement, ProjectEntity>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectOwnerRequirement requirement,
        ProjectEntity project)
    {
        if (context.User.IsInRole("Admin") ||
            project.OwnerId == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
