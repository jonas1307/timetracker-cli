using System.Net;
using RestSharp;
using Timetracker.Options;
using Timetracker.Requests;
using Timetracker.Services;

namespace Timetracker.Tests;

/// <summary>
/// Exercises HttpService against a stub message handler (no network). Each test arranges a
/// canned response via <see cref="Arrange"/>; a temp config supplies the URL/token/user.
/// </summary>
public sealed class HttpServiceTests : IDisposable
{
    private const string BaseUrl = "https://acme.timehub.7pace.com";
    private readonly TempConfigDir _configDir;

    public HttpServiceTests()
    {
        _configDir = new TempConfigDir();
        ConfigService.SaveConfig(
            new ConfigOptions { TimetrackerUrl = BaseUrl, TimetrackerBearerToken = "token-123" },
            userId: "user-1");
    }

    public void Dispose()
    {
        HttpService.ClientFactory = url => new RestClient(url);
        _configDir.Dispose();
    }

    private static StubHttpMessageHandler Arrange(HttpStatusCode status, string body = "")
    {
        var stub = new StubHttpMessageHandler(status, body);
        HttpService.ClientFactory = url => new RestClient(new RestClientOptions(url) { ConfigureMessageHandler = _ => stub });
        return stub;
    }

    // --- GetTimetrackerUser (no config dependency) -------------------------

    [Fact]
    public async Task GetTimetrackerUser_Success_ParsesUser()
    {
        Arrange(HttpStatusCode.OK, """{"data":{"user":{"displayName":"Jane","email":"jane@acme.com","id":"u-1"},"account":{"name":"acme","id":"a-1"}}}""");

        var result = await HttpService.GetTimetrackerUser(BaseUrl, "token-123");

        Assert.Equal("Jane", result.Data.User.DisplayName);
        Assert.Equal("jane@acme.com", result.Data.User.Email);
        Assert.Equal("acme", result.Data.Account.Name);
    }

    [Fact]
    public async Task GetTimetrackerUser_Failure_Throws()
    {
        Arrange(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<Exception>(() => HttpService.GetTimetrackerUser(BaseUrl, "bad-token"));
    }

    // --- ListWorkLogs ------------------------------------------------------

    [Fact]
    public async Task ListWorkLogs_Success_ParsesDataAndSendsDateRange()
    {
        var stub = Arrange(HttpStatusCode.OK, """{"data":[{"id":"w1","length":3600,"workItemId":123}]}""");

        var result = await HttpService.ListWorkLogs(new DateTime(2026, 3, 10), new DateTime(2026, 3, 20), workItemId: 123);

        Assert.Single(result.Data);
        Assert.Equal("w1", result.Data[0].Id);
        Assert.Contains("2026-03-10T00:00:00", stub.DecodedQuery);
        Assert.Contains("2026-03-20T23:59:59", stub.DecodedQuery);
        Assert.Contains("123", stub.DecodedQuery);
    }

    [Fact]
    public async Task ListWorkLogs_OmitsWorkItemFilterWhenNull()
    {
        var stub = Arrange(HttpStatusCode.OK, """{"data":[]}""");

        await HttpService.ListWorkLogs(new DateTime(2026, 3, 10), new DateTime(2026, 3, 20));

        Assert.DoesNotContain("workItemIds", stub.DecodedQuery);
    }

    [Fact]
    public async Task ListWorkLogs_Failure_Throws()
    {
        Arrange(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<Exception>(() => HttpService.ListWorkLogs(DateTime.Today, DateTime.Today));
    }

    // --- GetWorkLog --------------------------------------------------------

    [Fact]
    public async Task GetWorkLog_Success_ReturnsEntry()
    {
        Arrange(HttpStatusCode.OK, """{"data":{"id":"w1","length":1800,"workItemId":42}}""");

        var result = await HttpService.GetWorkLog("w1");

        Assert.Equal("w1", result.Id);
        Assert.Equal(1800, result.Length);
    }

    [Fact]
    public async Task GetWorkLog_Failure_Throws()
    {
        Arrange(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<Exception>(() => HttpService.GetWorkLog("missing"));
    }

    // --- PostWorkLog -------------------------------------------------------

    [Fact]
    public async Task PostWorkLog_Success_ReturnsNewId()
    {
        Arrange(HttpStatusCode.OK, """{"data":{"id":"new-id"}}""");

        var id = await HttpService.PostWorkLog(new TimetrackerWorklogRequest { WorkItemId = 1, Length = 3600 });

        Assert.Equal("new-id", id);
    }

    [Fact]
    public async Task PostWorkLog_Failure_Throws()
    {
        Arrange(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<Exception>(() => HttpService.PostWorkLog(new TimetrackerWorklogRequest()));
    }

    // --- ImportWorkLogs ----------------------------------------------------

    [Fact]
    public async Task ImportWorkLogs_Success_ReturnsList()
    {
        Arrange(HttpStatusCode.OK, """{"data":[{"id":"a"},{"id":"b"}]}""");

        var result = await HttpService.ImportWorkLogs([new TimetrackerWorklogRequest(), new TimetrackerWorklogRequest()]);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportWorkLogs_Failure_Throws()
    {
        Arrange(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<Exception>(() => HttpService.ImportWorkLogs([new TimetrackerWorklogRequest()]));
    }

    // --- RegisterActivity (composition of the worklog body) ----------------

    [Fact]
    public async Task RegisterActivity_ComputesLengthAndPostsWorklog()
    {
        var stub = Arrange(HttpStatusCode.OK, """{"data":{"id":"created"}}""");

        var options = new AddOptions
        {
            ActivityDate = "2026/03/15",
            ActivityStartHour = "09:00",
            ActivityLength = 1.5m,
            WorkItemId = 42,
            ActivityComment = "work",
        };

        var id = await HttpService.RegisterActivity(options, "act-1");

        Assert.Equal("created", id);
        Assert.Contains("\"length\":5400", stub.LastRequestBody);       // 1.5h * 3600
        Assert.Contains("\"workItemId\":42", stub.LastRequestBody);
        Assert.Contains("\"activityTypeId\":\"act-1\"", stub.LastRequestBody);
        Assert.Contains("\"userId\":\"user-1\"", stub.LastRequestBody);  // from config
    }
}
