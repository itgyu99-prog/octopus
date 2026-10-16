using UnityEngine;
using UnityEngine.InputSystem;

namespace OctopusSurvivors
{
    public sealed class PlayerInputReader
    {
        readonly InputAction _move;
        readonly InputAction _dash;
        readonly InputAction _interact;
        readonly InputAction _combine;
        readonly InputAction _transform;
        readonly InputAction _ultimate;
        readonly InputAction _back;
        readonly InputAction[] _picks;

        public PlayerInputReader()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _dash = new InputAction("Dash", InputActionType.Button);
            _dash.AddBinding("<Keyboard>/leftShift");
            _dash.AddBinding("<Keyboard>/rightShift");


            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");

            _combine = new InputAction("Combine", InputActionType.Button);
            _combine.AddBinding("<Keyboard>/f");

            _transform = new InputAction("Transform", InputActionType.Button);
            _transform.AddBinding("<Keyboard>/space");

            _ultimate = new InputAction("Ultimate", InputActionType.Button);
            _ultimate.AddBinding("<Keyboard>/q");

            _back = new InputAction("Back", InputActionType.Button);
            _back.AddBinding("<Keyboard>/escape");

            _picks = new InputAction[GameConfig.UpgradeChoices];
            for (int i = 0; i < _picks.Length; i++)
            {
                var action = new InputAction("Pick" + (i + 1), InputActionType.Button);
                action.AddBinding($"<Keyboard>/{i + 1}");
                action.AddBinding($"<Keyboard>/numpad{i + 1}");
                _picks[i] = action;
            }
        }

        public void Enable()
        {
            _move.Enable();
            _dash.Enable();
            _interact.Enable();
            _combine.Enable();
            _transform.Enable();
            _ultimate.Enable();
            _back.Enable();
            for (int i = 0; i < _picks.Length; i++)
                _picks[i].Enable();
        }

        public void Disable()
        {
            _move.Disable();
            _dash.Disable();
            _interact.Disable();
            _combine.Disable();
            _transform.Disable();
            _ultimate.Disable();
            _back.Disable();
            for (int i = 0; i < _picks.Length; i++)
                _picks[i].Disable();
        }

        public int PickPressed()
        {
            for (int i = 0; i < _picks.Length; i++)
            {
                if (_picks[i].WasPressedThisFrame())
                    return i;
            }

            return -1;
        }

        public Vector2 Move => _move.ReadValue<Vector2>();
        public bool DashPressed => _dash.WasPressedThisFrame();
        public bool InteractPressed => _interact.WasPressedThisFrame();
        public bool CombinePressed => _combine.WasPressedThisFrame();
        public bool TransformPressed => _transform.WasPressedThisFrame();
        public bool UltimatePressed => _ultimate.WasPressedThisFrame();
        public bool BackPressed => _back.WasPressedThisFrame();
    }
}
