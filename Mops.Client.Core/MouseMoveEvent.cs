using MessagePack;

namespace Mops.Client
{
    [MessagePackObject]
    public class MouseMoveEvent
    {
        [Key(0)]
        public double WindowHeight { get; set; }
        [Key(1)]
        public double WindowWidth { get; set; }
        [Key(2)]
        public double MouseX { get; set; }
        [Key(3)]
        public double MouseY { get; set; }
    }
}
