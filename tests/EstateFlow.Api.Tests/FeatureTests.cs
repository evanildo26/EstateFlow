using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Api.Tests;

public class LiabilitiesAndChecklistTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public LiabilitiesAndChecklistTests(EstateFlowApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liabilities_are_saved_and_reduce_the_net_value()
    {
        var client = await _factory.CreateControllerClientAsync($"liab-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(client, "EST-LIAB");

        await client.PostAsJsonAsync($"api/estates/{estateId}/financial-claims",
            new CreateFinancialClaimRequest { InstitutionName = "Bank", AccountReference = "123", Value = 50_000m });
        var add = await client.PostAsJsonAsync($"api/estates/{estateId}/liabilities",
            new CreateLiabilityRequest { CreditorName = "Home loan", Amount = 20_000m });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var estate = await client.GetFromJsonAsync<EstateDto>($"api/estates/{estateId}");
        Assert.Single(estate!.Liabilities);
        Assert.Equal(50_000m, estate.TotalAssets);
        Assert.Equal(20_000m, estate.TotalLiabilities);
        Assert.Equal(30_000m, estate.NetValue);

        var delete = await client.DeleteAsync($"api/estates/{estateId}/liabilities/{estate.Liabilities[0].LiabilityId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Completing_stages_updates_progress_and_status()
    {
        var client = await _factory.CreateControllerClientAsync($"stage-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(client, "EST-STAGE");

        var response = await client.PutAsJsonAsync($"api/estates/{estateId}/milestones/ReportedToMaster",
            new CompleteMilestoneRequest { CompletedOn = new DateTime(2026, 1, 20), Notes = "Lodged at Cape Town Master" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var progress = await response.Content.ReadFromJsonAsync<EstateProgressDto>();
        Assert.Equal(1, progress!.CompletedCount);
        Assert.Equal("Letters of Executorship issued", progress.NextMilestoneTitle);

        var estate = await client.GetFromJsonAsync<EstateDto>($"api/estates/{estateId}");
        Assert.Equal("In Progress", estate!.EstateStatus);
        Assert.Equal("Lodged at Cape Town Master", estate.Progress!.Milestones[0].Notes);

        var summaries = await client.GetFromJsonAsync<List<EstateSummaryDto>>("api/estates");
        Assert.Equal(14, summaries!.Single(s => s.EstateId == estateId).ProgressPercent);

        // Future dates and unknown stages are refused.
        var future = await client.PutAsJsonAsync($"api/estates/{estateId}/milestones/PaidOut",
            new CompleteMilestoneRequest { CompletedOn = DateTime.Today.AddDays(5) });
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);

        var unknown = await client.PutAsJsonAsync($"api/estates/{estateId}/milestones/Bogus",
            new CompleteMilestoneRequest { CompletedOn = DateTime.Today });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        var reopen = await client.DeleteAsync($"api/estates/{estateId}/milestones/ReportedToMaster");
        var reopened = await reopen.Content.ReadFromJsonAsync<EstateProgressDto>();
        Assert.Equal(0, reopened!.CompletedCount);
    }

    [Fact]
    public async Task Invalid_south_african_id_numbers_are_rejected()
    {
        var client = await _factory.CreateControllerClientAsync($"id-{Guid.NewGuid():N}@test.local");

        var badDeceased = await client.PostAsJsonAsync("api/estates", new CreateEstateRequest
        {
            FileNumber = "EST-BADID",
            DateOfDeath = new DateTime(2026, 1, 1),
            DeceasedFullName = "Typo Person",
            DeceasedIdNumber = "8001015009088"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badDeceased.StatusCode);

        var estateId = await EstateFlowApiFactory.CreateEstateAsync(client, "EST-GOODID");

        var badExecutor = await client.PostAsJsonAsync($"api/estates/{estateId}/executors", new CreateExecutorRequest
        {
            FullName = "Exec",
            Email = "exec@test.local",
            IdNumber = "123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badExecutor.StatusCode);

        var passportExecutor = await client.PostAsJsonAsync($"api/estates/{estateId}/executors", new CreateExecutorRequest
        {
            FullName = "Foreign Exec",
            Email = "foreign@test.local",
            IdNumber = "P1234567"
        });
        Assert.Equal(HttpStatusCode.OK, passportExecutor.StatusCode);
    }

    [Fact]
    public async Task InApp_diary_entries_keep_their_notification_method()
    {
        var client = await _factory.CreateControllerClientAsync($"inapp-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(client, "EST-INAPP");

        await client.PostAsJsonAsync($"api/diary/estate/{estateId}", new CreateDiaryEntryRequest
        {
            DueDate = DateTime.Today.AddDays(-2),
            Note = "Chase SARS",
            NotifyBy = "InApp"
        });

        var diary = await client.GetFromJsonAsync<List<DiaryEntryWithEstateDto>>("api/diary");
        Assert.Equal("InApp", diary!.Single(d => d.EstateId == estateId).NotifyBy);

        var summary = (await client.GetFromJsonAsync<List<EstateSummaryDto>>("api/estates"))!
            .Single(s => s.EstateId == estateId);
        Assert.Equal(1, summary.OverdueDiaryCount);
    }
}

public class LettersAndCorrespondenceTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public LettersAndCorrespondenceTests(EstateFlowApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Letter_is_a_word_document_filed_on_the_estate_and_logged()
    {
        var client = await _factory.CreateControllerClientAsync($"letter-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(client, "EST-LETTER");

        await client.PostAsJsonAsync($"api/estates/{estateId}/financial-claims", new CreateFinancialClaimRequest
        {
            InstitutionName = "First National Bank",
            InstitutionAddress = "PO Box 1, Cape Town",
            AccountReference = "62000000000",
            Value = 1000m
        });
        var claim = (await client.GetFromJsonAsync<EstateDto>($"api/estates/{estateId}"))!.FinancialClaims.Single();

        var response = await client.PostAsync($"api/estates/{estateId}/letters/institution/{claim.ClaimId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            response.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(".docx", response.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        Assert.Equal("true", response.Headers.GetValues("X-EstateFlow-Letter-Filed").Single());

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal((byte)'P', bytes[0]); // .docx is a zip file
        Assert.Equal((byte)'K', bytes[1]);

        var estate = await client.GetFromJsonAsync<EstateDto>($"api/estates/{estateId}");
        Assert.Contains(estate!.ScannedDocuments, d => d.DocumentType.StartsWith("Letter:"));

        var history = await client.GetFromJsonAsync<List<CorrespondenceDto>>($"api/estates/{estateId}/correspondence");
        var entry = Assert.Single(history!);
        Assert.Equal("Letter", entry.Channel);
        Assert.Equal("First National Bank", entry.RecipientName);
        Assert.NotNull(entry.DocumentId);
    }

    [Fact]
    public async Task Admin_can_edit_letter_wording_but_not_with_unknown_placeholders()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(EstateFlowApiFactory.AdminEmail, EstateFlowApiFactory.AdminPassword);

        var bad = await admin.PutAsJsonAsync("api/admin/letter-templates/VehicleTransfer",
            new UpdateLetterTemplateRequest { Subject = "Transfer", Body = "Hello {NotAThing}" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var good = await admin.PutAsJsonAsync("api/admin/letter-templates/VehicleTransfer",
            new UpdateLetterTemplateRequest { Subject = "Transfer: {DeceasedName}", Body = "Please transfer {RegistrationNumber}." });
        var saved = await good.Content.ReadFromJsonAsync<LetterTemplateDto>();
        Assert.True(saved!.IsCustomised);

        var reset = await admin.DeleteAsync("api/admin/letter-templates/VehicleTransfer");
        var restored = await reset.Content.ReadFromJsonAsync<LetterTemplateDto>();
        Assert.False(restored!.IsCustomised);

        var controller = await _factory.CreateControllerClientAsync($"tpl-{Guid.NewGuid():N}@test.local");
        var forbidden = await controller.GetAsync("api/admin/letter-templates");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}

public class AdminAndAuditTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public AdminAndAuditTests(EstateFlowApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_can_reassign_an_estate_between_controllers()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(EstateFlowApiFactory.AdminEmail, EstateFlowApiFactory.AdminPassword);
        var leavingEmail = $"leaving-{Guid.NewGuid():N}@test.local";
        var takingOverEmail = $"taking-{Guid.NewGuid():N}@test.local";
        var leaving = await _factory.CreateControllerClientAsync(leavingEmail);
        var takingOver = await _factory.CreateControllerClientAsync(takingOverEmail);
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(leaving, "EST-MOVE");

        var staff = await admin.GetFromJsonAsync<List<StaffMemberDto>>("api/admin/staff");
        var target = staff!.Single(s => s.Email == takingOverEmail);

        var move = await admin.PutAsJsonAsync($"api/admin/estates/{estateId}/controller",
            new ReassignEstateRequest { ControllerId = target.ControllerId!.Value });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await takingOver.GetAsync($"api/estates/{estateId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await leaving.GetAsync($"api/estates/{estateId}")).StatusCode);
    }

    [Fact]
    public async Task Deactivated_staff_are_signed_out_immediately()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(EstateFlowApiFactory.AdminEmail, EstateFlowApiFactory.AdminPassword);
        var email = $"gone-{Guid.NewGuid():N}@test.local";
        var leaver = await _factory.CreateControllerClientAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await leaver.GetAsync("api/estates")).StatusCode);

        var staff = await admin.GetFromJsonAsync<List<StaffMemberDto>>("api/admin/staff");
        var userId = staff!.Single(s => s.Email == email).UserId;
        await admin.PostAsync($"api/admin/staff/{userId}/deactivate", null);

        // The token they already hold stops working...
        Assert.Equal(HttpStatusCode.Unauthorized, (await leaver.GetAsync("api/estates")).StatusCode);

        // ...and they cannot sign in again.
        var login = await _factory.CreateClient().PostAsJsonAsync("api/auth/login",
            new LoginRequest { Email = email, Password = "ControllerPassw0rd" });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [Fact]
    public async Task Audit_log_records_views_and_refused_access()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(EstateFlowApiFactory.AdminEmail, EstateFlowApiFactory.AdminPassword);
        var owner = await _factory.CreateControllerClientAsync($"aud-own-{Guid.NewGuid():N}@test.local");
        var snoopEmail = $"aud-snoop-{Guid.NewGuid():N}@test.local";
        var snoop = await _factory.CreateControllerClientAsync(snoopEmail);
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(owner, "EST-AUDIT");

        await owner.GetAsync($"api/estates/{estateId}");
        await snoop.GetAsync($"api/estates/{estateId}");

        var audit = await admin.GetFromJsonAsync<List<AuditEntryDto>>($"api/admin/audit?estate={estateId}");

        Assert.Contains(audit!, a => a.Action == "Create" && a.StatusCode == 201);
        Assert.Contains(audit!, a => a.Action == "View" && a.StatusCode == 200);
        Assert.Contains(audit!, a => a.Action == "View" && a.StatusCode == 404 && a.UserName == snoopEmail);
        Assert.All(audit!, a => Assert.Equal("EST-AUDIT", a.FileNumber));
    }
}

public class PortalTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public PortalTests(EstateFlowApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Invited_heir_can_set_a_password_and_see_only_progress()
    {
        var controller = await _factory.CreateControllerClientAsync($"portal-ctl-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(controller, "EST-PORTAL");
        var heirEmail = $"heir-{Guid.NewGuid():N}@test.local";

        await controller.PostAsJsonAsync($"api/estates/{estateId}/beneficiaries", new CreateBeneficiaryRequest
        {
            FullName = "Thandi Heir",
            Email = heirEmail,
            Relationship = "Daughter",
            HeirType = "Heir"
        });
        await controller.PutAsJsonAsync($"api/estates/{estateId}/milestones/ReportedToMaster",
            new CompleteMilestoneRequest { CompletedOn = new DateTime(2026, 1, 20) });

        var heirId = (await controller.GetFromJsonAsync<EstateDto>($"api/estates/{estateId}"))!.Beneficiaries.Single().BeneficiaryId;

        var invite = await controller.PostAsJsonAsync($"api/estates/{estateId}/portal-invites",
            new InvitePortalUserRequest { PersonType = "Beneficiary", PersonId = heirId });
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);

        // The heir follows the link in the email.
        var email = _factory.Emails.Sent.Last(e => e.To == heirEmail);
        var token = Uri.UnescapeDataString(Regex.Match(email.Body, @"token=([^\s&]+)").Groups[1].Value);

        var anonymous = _factory.CreateClient();
        var set = await anonymous.PostAsJsonAsync("api/auth/portal/set-password",
            new SetPortalPasswordRequest { Email = heirEmail, Token = token, NewPassword = "HeirPassw0rd" });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);

        var heir = await _factory.CreateAuthenticatedClientAsync(heirEmail, "HeirPassw0rd");

        var estates = await heir.GetFromJsonAsync<List<PortalEstateDto>>("api/portal/estates");
        var mine = Assert.Single(estates!);
        Assert.Equal("EST-PORTAL", mine.FileNumber);
        Assert.Equal(14, mine.PercentComplete);
        Assert.Equal("Heir / beneficiary", mine.YourRole);

        // The heir cannot use staff endpoints or see the full estate record.
        Assert.Equal(HttpStatusCode.NotFound, (await heir.GetAsync($"api/estates/{estateId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await heir.PostAsJsonAsync("api/auth/register", new RegisterRequest())).StatusCode);

        // The link only works once.
        var reuse = await anonymous.PostAsJsonAsync("api/auth/portal/set-password",
            new SetPortalPasswordRequest { Email = heirEmail, Token = token, NewPassword = "Another1Passw0rd" });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);

        // Recorded in the correspondence history.
        var history = await controller.GetFromJsonAsync<List<CorrespondenceDto>>($"api/estates/{estateId}/correspondence");
        Assert.Contains(history!, c => c.Kind == "Portal invitation" && c.Status == "Sent");
    }

    [Fact]
    public async Task Staff_passwords_cannot_be_reset_through_the_portal()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("api/auth/portal/set-password",
            new SetPortalPasswordRequest { Email = EstateFlowApiFactory.AdminEmail, Token = "anything", NewPassword = "Hijack1Passw0rd" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Portal_page_is_served_with_a_strict_content_security_policy()
    {
        var response = await _factory.CreateClient().GetAsync("portal/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}

public class ReportSettingsTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public ReportSettingsTests(EstateFlowApiFactory factory) => _factory = factory;

    // Regression: pausing an estate that had never had a report used to fail.
    [Fact]
    public async Task Monthly_reports_can_be_paused_for_a_new_estate()
    {
        var client = await _factory.CreateControllerClientAsync($"pause-{Guid.NewGuid():N}@test.local");
        var estateId = await EstateFlowApiFactory.CreateEstateAsync(client, "EST-PAUSE");

        var pause = await client.PatchAsJsonAsync($"api/reports/settings/{estateId}", new { IsPaused = true });
        Assert.Equal(HttpStatusCode.OK, pause.StatusCode);

        var settings = await client.GetFromJsonAsync<List<ReportSettingDto>>("api/reports/settings");
        Assert.True(settings!.Single(s => s.EstateId == estateId).IsPaused);
    }
}
