using Newtonsoft.Json;

namespace RecordWhisperClient.Models
{
    public class WhisperResponse
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("segments")]
        public WhisperSegment[] Segments { get; set; }
    }

    public class WhisperSegment
    {
        [JsonProperty("start")]
        public float Start { get; set; }

        [JsonProperty("end")]
        public float End { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}