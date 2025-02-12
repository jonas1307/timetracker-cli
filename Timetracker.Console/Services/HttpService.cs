using Newtonsoft.Json;
using RestSharp;
using Timetracker.Options;
using Timetracker.Requests;
using Timetracker.Responses;

namespace Timetracker.Services
{
    public static class HttpService
    {
        private const string TIMETRACKER_API_VERSION = "3.2";

        public static async Task RegisterActivity(AddOptions options)
        {
            var activity = ActivityService.GetActivities()
                .First(f => f.Name == options.ActivityType);

            var worklog = new TimetrackerWorklogRequest
            {
                TimeStamp = DateTime.Parse($"{options.ActivityDate} {options.ActivityStartHour}"),
                Length = (int)(options.ActivityLenght * 60 * 60),
                BillableLength = null,
                WorkItemId = options.WorkItemId,
                Comment = options.ActivityComment,
                UserId = ConfigService.LoadSetting("TimetrackerUserId"),
                ActivityTypeId = activity.Id
            };

            var client = new RestClient(ConfigService.LoadSetting("TimetrackerUrl"));

            var request = new RestRequest($"/api/rest/workLogs?api-version={TIMETRACKER_API_VERSION}", Method.Post);
            request.AddHeader("Authorization", $"Bearer {ConfigService.LoadSetting("TimetrackerBearerToken")}");
            request.AddJsonBody(worklog);

            var response = await client.ExecuteAsync(request);

            var responseData = response.Content;
        }

        public static async Task<TimetrackerResponse<ActivityTypeResponse>> ListActivityTypes()
        {
            var client = new RestClient(ConfigService.LoadSetting("TimetrackerUrl"));

            var request = new RestRequest($"/api/rest/activityTypes?api-version={TIMETRACKER_API_VERSION}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {ConfigService.LoadSetting("TimetrackerBearerToken")}");

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TimetrackerResponse<ActivityTypeResponse>>(response.Content);
            }

            throw new Exception("Could not retrieve the list of activities.");
        }

        public static async Task<TimetrackerResponse<TimetrackerUserResponse>> GetTimetrackerUser(string timetrackerUrl, string timetrackerBearerToken)
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
