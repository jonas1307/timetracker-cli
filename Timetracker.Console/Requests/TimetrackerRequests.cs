using Newtonsoft.Json;

namespace Timetracker.Requests
{
    public record TimetrackerWorklogRequest
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
}
