using Newtonsoft.Json;
using RestSharp;
using Timetracker.Options;
using Timetracker.Requests;
using Timetracker.Responses;
using Timetracker.Utils;

namespace Timetracker.Services
{
    public static class HttpService
    {
        private const string TIMETRACKER_API_VERSION = "3.2";

        public static async Task<string> RegisterActivity(AddOptions options, string activityId, CancellationToken cancellationToken = default)
        {
            var config = ConfigService.LoadConfig();

            var worklog = new TimetrackerWorklogRequest
            {
                TimeStamp = ValidationUtils.ResolveDate(options.ActivityDate).Add(TimeSpan.Parse(options.ActivityStartHour)),
                Length = (int)Math.Round(options.ActivityLength * 3600),
                BillableLength = null,
                WorkItemId = options.WorkItemId,
                Comment = options.ActivityComment,
                UserId = config.TimetrackerUserId,
                ActivityTypeId = activityId
            };

            using var client = new RestClient(config.TimetrackerUrl);

            var request = new RestRequest($"/api/rest/workLogs?api-version={TIMETRACKER_API_VERSION}", Method.Post);
            request.AddHeader("Authorization", $"Bearer {config.TimetrackerBearerToken}");
            request.AddJsonBody(worklog);

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to register activity. Response: {response.Content}.");
            }

            return JsonConvert.DeserializeObject<TimetrackerResponse<WorkLog>>(response.Content).Data.Id;
        }

        public static async Task<TimetrackerResponse<ActivityTypeResponse>> ListActivityTypes(CancellationToken cancellationToken = default)
        {
            var config = ConfigService.LoadConfig();
            using var client = new RestClient(config.TimetrackerUrl);

            var request = new RestRequest($"/api/rest/activityTypes?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {config.TimetrackerBearerToken}");

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TimetrackerResponse<ActivityTypeResponse>>(response.Content);
            }

            throw new Exception("Could not retrieve the list of activities.");
        }

        public static async Task<TimetrackerResponse<List<WorkLog>>> ListWorkLogs(DateTime from, DateTime to, int? workItemId = null, CancellationToken cancellationToken = default)
        {
            var config = ConfigService.LoadConfig();
            using var client = new RestClient(config.TimetrackerUrl);
            var request = new RestRequest($"/api/rest/workLogs?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {config.TimetrackerBearerToken}");
            request.AddQueryParameter("$fromTimestamp", from.ToString("yyyy-MM-ddT00:00:00"));
            request.AddQueryParameter("$toTimestamp", to.ToString("yyyy-MM-ddT23:59:59"));

            if (workItemId.HasValue)
                request.AddQueryParameter("$workItemIds", workItemId.Value.ToString());

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TimetrackerResponse<List<WorkLog>>>(response.Content);
            }

            throw new Exception("Could not retrieve the list of work logs.");
        }

        public static async Task<WorkLog> GetWorkLog(string workLogId, CancellationToken cancellationToken = default)
        {
            var config = ConfigService.LoadConfig();
            using var client = new RestClient(config.TimetrackerUrl);

            var request = new RestRequest($"/api/rest/workLogs/{workLogId}?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {config.TimetrackerBearerToken}");

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<TimetrackerResponse<WorkLog>>(response.Content).Data;

            throw new Exception($"Could not retrieve time entry '{workLogId}'. Status: {response.StatusCode}.");
        }

        public static async Task UpdateWorkLog(string workLogId, TimetrackerWorklogRequest worklog, CancellationToken cancellationToken = default)
        {
            var config = ConfigService.LoadConfig();
            using var client = new RestClient(config.TimetrackerUrl);

            var request = new RestRequest($"/api/rest/workLogs/{workLogId}?api-version={TIMETRACKER_API_VERSION}", Method.Put);
            request.AddHeader("Authorization", $"Bearer {config.TimetrackerBearerToken}");
            request.AddJsonBody(worklog);

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to update time entry. Status: {response.StatusCode}. Response: {response.Content}.");
        }

        public static async Task DeleteWorkLog(string workLogId, CancellationToken cancellationToken = default)
        {
            var config = ConfigService.LoadConfig();
            using var client = new RestClient(config.TimetrackerUrl);

            var request = new RestRequest($"/api/rest/workLogs/{workLogId}?api-version={TIMETRACKER_API_VERSION}", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {config.TimetrackerBearerToken}");

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to delete time entry. Status: {response.StatusCode}. Response: {response.Content}.");
            }
        }

        public static async Task<TimetrackerResponse<TimetrackerUserResponse>> GetTimetrackerUser(string timetrackerUrl, string timetrackerBearerToken, CancellationToken cancellationToken = default)
        {
            using var client = new RestClient(timetrackerUrl);

            var request = new RestRequest($"/api/rest/me?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {timetrackerBearerToken}");

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TimetrackerResponse<TimetrackerUserResponse>>(response.Content);
            }

            throw new Exception("Could not retrieve the user profile. Check if the URL and Bearer Token provided are valid.");
        }
    }
}
