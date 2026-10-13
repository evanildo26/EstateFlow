using System.Net;
using System.Net.Http.Json;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Api.Tests;

// A logged-in Estate Controller must never be able to see or change an estate
// assigned to someone else, through any endpoint.
public class EstateOwnershipTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public EstateOwnershipTests(EstateFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Owner_can_read_and_update_their_estate()
    {
        var owner = await _factory.CreateControllerClientAsync($"owner-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(owner, "EST-OWN-1");

        var get = await owner.GetAsync($"api/estates/{estateId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var addDiary = await owner.PostAsJsonAsync($"api/diary/estate/{estateId}", new CreateDiaryEntryRequest
        {
            DueDate = DateTime.Today.AddDays(3),
            Note = "Follow up with bank",
            NotifyBy = "Email"
        });
        Assert.Equal(HttpStatusCode.OK, addDiary.StatusCode);
    }

    [Fact]
    public async Task Other_controller_gets_404_on_every_estate_endpoint()
    {
        var owner = await _factory.CreateControllerClientAsync($"owner-{Guid.NewGuid():N}@test.local");
        var intruder = await _factory.CreateControllerClientAsync($"intruder-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(owner, "EST-OWN-2");
        var anyId = Guid.NewGuid();

        var attempts = new List<(string Name, Func<Task<HttpResponseMessage>> Call)>
        {
            ("get estate", () => intruder.GetAsync($"api/estates/{estateId}")),
            ("update estate", () => intruder.PutAsJsonAsync($"api/estates/{estateId}", new UpdateEstateRequest
            {
                FileNumber = "HIJACKED",
                DateOfDeath = DateTime.Today,
                EstateStatus = "Open",
                DeceasedFullName = "x",
                DeceasedIdNumber = "x"
            })),
            ("delete estate", () => intruder.DeleteAsync($"api/estates/{estateId}")),
            ("add executor", () => intruder.PostAsJsonAsync($"api/estates/{estateId}/executors", new { FullName = "x" })),
            ("add diary", () => intruder.PostAsJsonAsync($"api/diary/estate/{estateId}", new CreateDiaryEntryRequest
            {
                DueDate = DateTime.Today,
                Note = "x"
            })),
            ("send diary email", () => intruder.PostAsJsonAsync($"api/diary/estate/{estateId}/{anyId}/send-email",
                new { RecipientEmail = "attacker@example.com" })),
            ("list documents download", () => intruder.GetAsync($"api/estates/{estateId}/documents/{anyId}/download")),
            ("vehicle letter", () => intruder.PostAsync($"api/estates/{estateId}/letters/vehicle/{anyId}", null)),
            ("pause reports", () => intruder.PatchAsJsonAsync($"api/reports/settings/{estateId}", new { IsPaused = true }))
        };

        foreach (var (name, call) in attempts)
        {
            var response = await call();
            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound,
                $"'{name}' returned {(int)response.StatusCode} instead of 404.");
        }

        // The owner's estate is untouched.
        var estate = await owner.GetFromJsonAsync<EstateDto>($"api/estates/{estateId}");
        Assert.Equal("EST-OWN-2", estate!.FileNumber);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("api/estates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
