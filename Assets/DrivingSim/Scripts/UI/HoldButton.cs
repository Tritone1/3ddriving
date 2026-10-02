using UnityEngine;
using UnityEngine.EventSystems;

namespace DrivingSim.UI
{
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Action { Throttle, Reverse, Brake, Handbrake, Left, Right, Refuel }
        [SerializeField] private MobileInputState input;
        [SerializeField] private Action action;

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);
        public void OnPointerExit(PointerEventData eventData) => Set(false);

        private void OnDisable() => Set(false);

        private void Set(bool pressed)
        {
            if (input == null) return;
            switch (action)
            {
                case Action.Throttle: input.SetThrottle(pressed); break;
                case Action.Reverse: input.SetReverse(pressed); break;
                case Action.Brake: input.SetBrake(pressed); break;
                case Action.Handbrake: input.SetHandbrake(pressed); break;
                case Action.Left: input.SetLeft(pressed); break;
                case Action.Right: input.SetRight(pressed); break;
                case Action.Refuel: input.SetRefuel(pressed); break;
            }
        }
    }
}
