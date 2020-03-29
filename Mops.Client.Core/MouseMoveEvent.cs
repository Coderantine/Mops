using Newtonsoft.Json;

namespace Mops.Client
{
    public class MouseMoveEvent
    {
        [JsonProperty("a")]
        public double WindowHeight { get; set; }

        [JsonProperty("b")]
        public double WindowWidth { get; set; }

        [JsonProperty("c")]
        public double MouseX { get; set; }

        [JsonProperty("d")]
        public double MouseY { get; set; }
    }
}
