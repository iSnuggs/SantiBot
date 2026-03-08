using Newtonsoft.Json;

namespace NadekoBot.Voice.Models
{
    public sealed class VoiceSessionDescription
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("secret_key")]
        public byte[] SecretKey { get; set; }

        [JsonProperty("dave_protocol_version")]
        public int DaveProtocolVersion { get; set; }
    }
}