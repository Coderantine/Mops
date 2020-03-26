using System;

namespace Mops.Client
{
    public static class MouseController
    {
        private static Action<double, double> _move;

        public static void RegisterCallback(Action<double, double> move)
        {
            _move = move;
        }

        public static void Move(MouseMoveEvent @event)
        {
            var ratioX = @event.MouseX / @event.WindowWidth;
            var ratioY = @event.MouseY / @event.WindowHeight;
            _move(WindowSettings.WindowWidth * ratioX, WindowSettings.WindowHeight * ratioY);
        }
    }
}
