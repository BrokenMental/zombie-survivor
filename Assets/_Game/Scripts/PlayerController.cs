using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieSurvivor
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed = 6f;
        private InputAction move;
        private Vector2 origin;
        private int touchId = -1;

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        }

        private void OnEnable() => move.Enable();
        private void OnDisable() { move.Disable(); touchId = -1; }
        private void OnDestroy() => move.Dispose();

        private void Update()
        {
            Vector2 input = move.ReadValue<Vector2>();
            var screen = Touchscreen.current;
            if (screen != null)
            {
                bool found = false;
                foreach (var touch in screen.touches)
                {
                    if (!touch.press.isPressed) continue;
                    Vector2 position = touch.position.ReadValue();
                    Vector2 pressedAt = touch.startPosition.ReadValue();
                    int id = touch.touchId.ReadValue();
                    if (touchId < 0 && touch.press.wasPressedThisFrame && pressedAt.x < Screen.width * .5f)
                    { touchId = id; origin = pressedAt; }
                    if (id != touchId) continue;
                    found = true;
                    input = Vector2.ClampMagnitude((position - origin) / (Screen.dpi > 0 ? Screen.dpi * .4f : 100f), 1f);
                }
                if (!found) touchId = -1;
            }
            Vector3 direction = new Vector3(input.x, 0, input.y);
            transform.position += Vector3.ClampMagnitude(direction, 1f) * (speed * Time.deltaTime);
            Vector3 p = transform.position;
            transform.position = new Vector3(Mathf.Clamp(p.x, -24, 24), p.y, Mathf.Clamp(p.z, -24, 24));
            if (direction.sqrMagnitude > .01f) transform.forward = direction;
        }

        private void OnGUI()
        {
            using (new PrototypeGUI(true))
            {
                GUI.Label(new Rect(16, 16, 448, 32), "Move: WASD / arrows | Touch: drag left");
                if (touchId >= 0)
                {
                    Vector2 point = PrototypeGUI.FromScreen(origin);
                    point.x = Mathf.Clamp(point.x, 35, PrototypeGUI.Width - 35);
                    point.y = Mathf.Clamp(point.y, 35, PrototypeGUI.Height - 35);
                    GUI.Box(new Rect(point.x - 35, point.y - 35, 70, 70), "Move");
                }
            }
        }
    }
}
