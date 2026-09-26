using System.Net;
using System.Text;
using DevTrack.Core.AI;
using DevTrack.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace DevTrack.Tests;

public sealed class OllamaTaskPlanningServiceTests
{
    [Fact]
    public async Task SuggestSubtasksAsync_WhenOllamaReturnsValidPlan_MapsStructuredResult()
    {
        const string response = """
            {
              "message": {
                "content": "{\"summary\":\"Add password reset\",\"assumptions\":[\"Email delivery is configured\"],\"subtasks\":[{\"title\":\"Create token\",\"description\":\"Configure tokens\",\"estimatedHours\":2},{\"title\":\"Send email\",\"description\":\"Add reset email\",\"estimatedHours\":3},{\"title\":\"Test flow\",\"description\":\"Test the reset flow\",\"estimatedHours\":2}]}"
              }
            }
            """;
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(response, Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") };
        var service = new OllamaTaskPlanningService(
            client,
            Options.Create(new OllamaOptions { Model = "test-model" }));

        var result = await service.SuggestSubtasksAsync(
            new TaskPlanningRequest("Password reset", "Let users reset passwords.", DateTime.Today, DateTime.Today.AddDays(14)));

        Assert.Equal("Add password reset", result.Summary);
        Assert.Single(result.Assumptions);
        var subtask = Assert.Single(result.Subtasks, item => item.Title == "Send email");
        Assert.Equal(3, subtask.EstimatedHours);
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/chat", request.RequestUri?.AbsolutePath);
            return Task.FromResult(response);
        }
    }
}
