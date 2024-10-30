using Newtonsoft.Json;
using RestSharp;
using Timetracker.Options;
using Timetracker.Responses;

namespace Timetracker.Services
{
    public record WorklogRequest
    {
        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("billableLength")]
        public object BillableLength { get; set; }

        [JsonProperty("workItemId")]
        public int WorkItemId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("activityTypeId")]
        public string ActivityTypeId { get; set; }
    }

    public static class HttpService
    {
        private const string TIMETRACKER_API_VERSION = "3.2";

        public static async Task RegisterActivity(TrackOptions options)
        {
            var activity = ActivityService.GetActivities()
                .First(f => f.Name == options.ActivityType);

            var worklog = new WorklogRequest
            {
                TimeStamp = DateTime.Parse($"{options.ActivityDate} 09:00:00"),
                Length = (int)(options.ActivityLenght * 60 * 60),
                BillableLength = null,
                WorkItemId = options.WorkItemId,
                Comment = options.ActivityComment,
                UserId = FileService.LoadSetting("TimetrackerUserId"),
                ActivityTypeId = activity.Id
            };

            var client = new RestClient(FileService.LoadSetting("TimetrackerUrl"));

            var request = new RestRequest($"/api/rest/workLogs?api-version={TIMETRACKER_API_VERSION}", Method.Post);
            request.AddHeader("Authorization", $"Bearer {FileService.LoadSetting("TimetrackerBearerToken")}");
            request.AddJsonBody(worklog);

            var response = await client.ExecuteAsync(request);

            var responseData = response.Content;
        }

        public static async Task<TimetrackerResponse<ActivityTypeResponse>> ListActivityTypes()
        {
            var client = new RestClient(FileService.LoadSetting("TimetrackerUrl"));

            var request = new RestRequest($"/api/rest/activityTypes?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {FileService.LoadSetting("TimetrackerBearerToken")}");

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TimetrackerResponse<ActivityTypeResponse>>(response.Content);
            }

            throw new Exception("Could not retrieve the list of activities.");
        }

        internal static async Task<TimetrackerResponse<TimetrackerUserResponse>> GetTimetrackerUser(string timetrackerUrl, string timetrackerBearerToken)
        {
            var client = new RestClient(timetrackerUrl);

            var request = new RestRequest($"/api/rest/me?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {timetrackerBearerToken}");

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TimetrackerResponse<TimetrackerUserResponse>>(response.Content);
            }

            throw new Exception("Could not retrieve the user profile. Check if the URL and Bearer Token provided are valid.");
        }
    }
}
