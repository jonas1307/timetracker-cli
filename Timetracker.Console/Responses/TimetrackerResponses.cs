using Newtonsoft.Json;

namespace Timetracker.Responses
{
    public class TimetrackerResponse<T> where T : class
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class ActivityTypeResponse
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("systemDefaultActivityTypeId")]
        public string DefaultActivityTypeId { get; set; }

        [JsonProperty("activityTypes")]
        public List<ActivityType> ActivityTypes { get; set; }
    }

    public class ActivityType
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class TimetrackerUserResponse
    {
        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("account")]
        public Account Account { get; set; }

        [JsonProperty("defaultActivityType")]
        public object DefaultActivityType { get; set; }

        [JsonProperty("timeZone")]
        public int TimeZone { get; set; }
    }

    public class User
    {
        [JsonProperty("uniqueName")]
        public string UniqueName { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("vstsId")]
        public string VstsId { get; set; }

        [JsonProperty("vstsCollectionId")]
        public string VstsCollectionId { get; set; }

        [JsonProperty("vstsCollectionId2")]
        public string VstsCollectionId2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Account
    {
        [JsonProperty("vstsCollectionId")]
        public string VstsCollectionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
