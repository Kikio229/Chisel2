using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.SDL3;

namespace Chisel.Framework;

public static class InputManager
{
    public static Vector2 MousePosition { get; private set; }
    public static Vector2 MouseDelta { get; private set; }
    public static Vector2 MouseWheelOffset { get; private set; }

    public static Vector2 GamepadLeftStickOffset
    {
        get => new Vector2(GetGamepadAnalog(Input.GpAnalogLeftStickX, 0), GetGamepadAnalog(Input.GpAnalogLeftStickY, 0));
    }

    public static Vector2 GamepadRightStickOffset
    {
        get => new Vector2(GetGamepadAnalog(Input.GpAnalogRightStickX, 0), GetGamepadAnalog(Input.GpAnalogRightStickY, 0));
    }

    public static float GamepadLeftTriggerOffset
    {
        get => (GetGamepadAnalog(Input.GpAnalogLeftTrigger, 0) + 1.0f) * 0.5f;
    }

    public static float GamepadRightTriggerOffset
    { 
        get => (GetGamepadAnalog(Input.GpAnalogRightTrigger, 0) + 1.0f) * 0.5f;
    }

    public static bool IsAnyInputHeld
    { 
        get => _heldInputs.Count > 0;
    }

    public static bool IsAnyInputPressed
    {
        get => _pressedInputs.Count > 0;
    }

    public static bool IsAnyInputReleased 
    { 
        get => _releasedInputs.Count > 0;
    }

    private static HashSet<Input> _heldInputs = new HashSet<Input>();
    private static HashSet<Input> _pressedInputs = new HashSet<Input>();
    private static HashSet<Input> _releasedInputs = new HashSet<Input>();
    private static Dictionary<int, nint> _gamepadList = new Dictionary<int, nint>();
    private static Dictionary<Input, float> _gamepadAnalog = new Dictionary<Input, float>();

    public static Input[] GetHeldInputs()
    {
        return _heldInputs.ToArray();
    }

    public static Input[] GetPressedInputs() 
    {
        return _pressedInputs.ToArray();
    }

    public static Input[] GetReleasedInputs()
    {
        return _releasedInputs.ToArray();
    }

    public static float GetGamepadAnalog(Input button, float deadzone)
    {
        float analog = _gamepadAnalog.GetValueOrDefault(button);
        return (MathF.Abs(analog) < deadzone) ? 0 : analog;
    }

    public static bool IsInputHeld(Input input)
    {
        return _heldInputs.Contains(input);
    }

    public static bool IsInputPressed(Input input)
    {
        return _pressedInputs.Contains(input);
    }

    public static bool IsInputReleased(Input input)
    {
        return _releasedInputs.Contains(input);
    }

    internal static unsafe void Process(SDLEvent ev)
    {
        switch ((SDLEventType)ev.Type)
        {
            case SDLEventType.KeyDown:
                if (ev.Key.Repeat == 0)
                {
                    Input keyDown = SdlKeyToInput(ev.Key.Scancode);
                    if (_heldInputs.Add(keyDown)) _pressedInputs.Add(keyDown);
                }
                break;

            case SDLEventType.KeyUp:
                Input keyUp = SdlKeyToInput(ev.Key.Scancode);
                _heldInputs.Remove(keyUp);
                _releasedInputs.Add(keyUp);
                break;

            case SDLEventType.MouseButtonDown:
                Input mouDown = SdlMouseButtonToInput(ev.Button.Button);
                if (_heldInputs.Add(mouDown)) _pressedInputs.Add(mouDown);
                break;

            case SDLEventType.MouseButtonUp:
                Input mouUp = SdlMouseButtonToInput(ev.Button.Button);
                _heldInputs.Remove(mouUp);
                _releasedInputs.Add(mouUp);
                break;

            case SDLEventType.MouseMotion:
                MousePosition = new Vector2(ev.Motion.X, ev.Motion.Y);
                MouseDelta += new Vector2(ev.Motion.Xrel, ev.Motion.Yrel);
                break;

            case SDLEventType.MouseWheel:
                MouseWheelOffset = new Vector2(ev.Wheel.X, ev.Wheel.Y);
                break;

            case SDLEventType.GamepadAdded:
                int addId = ev.Gdevice.Which;
                SDLGamepad* addPad = SDL.OpenGamepad(addId);
                if (addPad != null) _gamepadList.Add(addId, (nint)addPad);
                break;

            case SDLEventType.GamepadRemoved:
                int removeId = ev.Gdevice.Which;
                if (_gamepadList.Remove(removeId, out var removePad)) SDL.CloseGamepad((SDLGamepad*)removePad);
                break;

            case SDLEventType.GamepadButtonDown:
                Input padDown = SdlGamepadButtonToInput(ev.Gbutton.Button);
                if (_heldInputs.Add(padDown)) _pressedInputs.Add(padDown);
                break;

            case SDLEventType.GamepadButtonUp:
                Input padUp = SdlGamepadButtonToInput(ev.Gbutton.Button);
                _heldInputs.Remove(padUp);
                _releasedInputs.Add(padUp);
                break;

            case SDLEventType.GamepadAxisMotion:
                Input axis = SdlGamepadAxisToInput(ev.Gaxis.Axis);
                float value = ev.Gaxis.Value / 32767.0f;
                value = Math.Clamp(value, -1.0f, 1.0f);
                _gamepadAnalog[axis] = value; // Setting them directly. Clearing them every frame causes issues
                break;
        }
    }

    internal static void Reset()
    {
        // Clearing everything from the last frame
        MouseWheelOffset = Vector2.Zero;
        MouseDelta = Vector2.Zero;
        _pressedInputs.Clear();
        _releasedInputs.Clear();
    }

    private static Input SdlKeyToInput(SDLScancode scancode)
    {
        return scancode switch
        {
            SDLScancode.A => Input.KeyA,
            SDLScancode.B => Input.KeyB,
            SDLScancode.C => Input.KeyC,
            SDLScancode.D => Input.KeyD,
            SDLScancode.E => Input.KeyE,
            SDLScancode.F => Input.KeyF,
            SDLScancode.G => Input.KeyG,
            SDLScancode.H => Input.KeyH,
            SDLScancode.I => Input.KeyI,
            SDLScancode.J => Input.KeyJ,
            SDLScancode.K => Input.KeyK,
            SDLScancode.L => Input.KeyL,
            SDLScancode.M => Input.KeyM,
            SDLScancode.N => Input.KeyN,
            SDLScancode.O => Input.KeyO,
            SDLScancode.P => Input.KeyP,
            SDLScancode.Q => Input.KeyQ,
            SDLScancode.R => Input.KeyR,
            SDLScancode.S => Input.KeyS,
            SDLScancode.T => Input.KeyT,
            SDLScancode.U => Input.KeyU,
            SDLScancode.V => Input.KeyV,
            SDLScancode.W => Input.KeyW,
            SDLScancode.X => Input.KeyX,
            SDLScancode.Y => Input.KeyY,
            SDLScancode.Z => Input.KeyZ,
            SDLScancode.Scancode1 => Input.KeyNumber1,
            SDLScancode.Scancode2 => Input.KeyNumber2,
            SDLScancode.Scancode3 => Input.KeyNumber3,
            SDLScancode.Scancode4 => Input.KeyNumber4,
            SDLScancode.Scancode5 => Input.KeyNumber5,
            SDLScancode.Scancode6 => Input.KeyNumber6,
            SDLScancode.Scancode7 => Input.KeyNumber7,
            SDLScancode.Scancode8 => Input.KeyNumber8,
            SDLScancode.Scancode9 => Input.KeyNumber9,
            SDLScancode.Scancode0 => Input.KeyNumber0,
            SDLScancode.Return => Input.KeyReturn,
            SDLScancode.Escape => Input.KeyEscape,
            SDLScancode.Backspace => Input.KeyBackspace,
            SDLScancode.Tab => Input.KeyTab,
            SDLScancode.Space => Input.KeySpace,
            SDLScancode.Minus => Input.KeyMinus,
            SDLScancode.Equals => Input.KeyEquals,
            SDLScancode.Leftbracket => Input.KeyLeftBracket,
            SDLScancode.Rightbracket => Input.KeyRightBracket,
            SDLScancode.Backslash => Input.KeyBackslash,
            SDLScancode.Nonushash => Input.KeyNonUsHash,
            SDLScancode.Semicolon => Input.KeySemicolon,
            SDLScancode.Apostrophe => Input.KeyApostrophe,
            SDLScancode.Grave => Input.KeyGrave,
            SDLScancode.Comma => Input.KeyComma,
            SDLScancode.Period => Input.KeyPeriod,
            SDLScancode.Slash => Input.KeySlash,
            SDLScancode.Capslock => Input.KeyCapsLock,
            SDLScancode.F1 => Input.KeyFunction1,
            SDLScancode.F2 => Input.KeyFunction2,
            SDLScancode.F3 => Input.KeyFunction3,
            SDLScancode.F4 => Input.KeyFunction4,
            SDLScancode.F5 => Input.KeyFunction5,
            SDLScancode.F6 => Input.KeyFunction6,
            SDLScancode.F7 => Input.KeyFunction7,
            SDLScancode.F8 => Input.KeyFunction8,
            SDLScancode.F9 => Input.KeyFunction9,
            SDLScancode.F10 => Input.KeyFunction10,
            SDLScancode.F11 => Input.KeyFunction11,
            SDLScancode.F12 => Input.KeyFunction12,
            SDLScancode.Printscreen => Input.KeyPrintScreen,
            SDLScancode.Scrolllock => Input.KeyScrollLock,
            SDLScancode.Pause => Input.KeyPause,
            SDLScancode.Insert => Input.KeyInsert,
            SDLScancode.Home => Input.KeyHome,
            SDLScancode.Pageup => Input.KeyPageUp,
            SDLScancode.Delete => Input.KeyDelete,
            SDLScancode.End => Input.KeyEnd,
            SDLScancode.Pagedown => Input.KeyPageDown,
            SDLScancode.Right => Input.KeyRightArrow,
            SDLScancode.Left => Input.KeyLeftArrow,
            SDLScancode.Down => Input.KeyDownArrow,
            SDLScancode.Up => Input.KeyUpArrow,
            SDLScancode.Numlockclear => Input.KeyNumLock,
            SDLScancode.KpDivide => Input.KeyKeypadDivide,
            SDLScancode.KpMultiply => Input.KeyKeypadMultiply,
            SDLScancode.KpMinus => Input.KeyKeypadMinus,
            SDLScancode.KpPlus => Input.KeyKeypadPlus,
            SDLScancode.KpEnter => Input.KeyKeypadEnter,
            SDLScancode.Kp1 => Input.KeyKeypad1,
            SDLScancode.Kp2 => Input.KeyKeypad2,
            SDLScancode.Kp3 => Input.KeyKeypad3,
            SDLScancode.Kp4 => Input.KeyKeypad4,
            SDLScancode.Kp5 => Input.KeyKeypad5,
            SDLScancode.Kp6 => Input.KeyKeypad6,
            SDLScancode.Kp7 => Input.KeyKeypad7,
            SDLScancode.Kp8 => Input.KeyKeypad8,
            SDLScancode.Kp9 => Input.KeyKeypad9,
            SDLScancode.Kp0 => Input.KeyKeypad0,
            SDLScancode.KpPeriod => Input.KeyKeypadPeriod,
            SDLScancode.Nonusbackslash => Input.KeyNonUSBackslash,
            SDLScancode.Application => Input.KeyApplication,
            SDLScancode.Power => Input.KeyPower,
            SDLScancode.KpEquals => Input.KeyKeypadEquals,
            SDLScancode.F13 => Input.KeyFunction13,
            SDLScancode.F14 => Input.KeyFunction14,
            SDLScancode.F15 => Input.KeyFunction15,
            SDLScancode.F16 => Input.KeyFunction16,
            SDLScancode.F17 => Input.KeyFunction17,
            SDLScancode.F18 => Input.KeyFunction18,
            SDLScancode.F19 => Input.KeyFunction19,
            SDLScancode.F20 => Input.KeyFunction20,
            SDLScancode.F21 => Input.KeyFunction21,
            SDLScancode.F22 => Input.KeyFunction22,
            SDLScancode.F23 => Input.KeyFunction23,
            SDLScancode.F24 => Input.KeyFunction24,
            SDLScancode.Lctrl => Input.KeyLCtrl,
            SDLScancode.Lshift => Input.KeyLShift,
            SDLScancode.Lalt => Input.KeyLAlt,
            SDLScancode.Lgui => Input.KeyLGui,
            SDLScancode.Rctrl => Input.KeyRCtrl,
            SDLScancode.Rshift => Input.KeyRShift,
            SDLScancode.Ralt => Input.KeyRAlt,
            SDLScancode.Rgui => Input.KeyRGui,
            _ => Input.Invalid,
        };
    }

    private static Input SdlMouseButtonToInput(byte button)
    {
        return button switch
        {
            1 => Input.MouseLeft,
            2 => Input.MouseMiddle,
            3 => Input.MouseRight,
            4 => Input.MouseMisc1,
            5 => Input.MouseMisc2,
            _ => Input.Invalid
        };
    }

    private static Input SdlGamepadButtonToInput(byte button)
    {
        return button switch
        {
            0 => Input.GpFaceDown,
            1 => Input.GpFaceRight,
            2 => Input.GpFaceLeft,
            3 => Input.GpFaceUp,
            4 => Input.GpSelect,
            5 => Input.GpGuide,
            6 => Input.GpStart,
            7 => Input.GpLeftStick,
            8 => Input.GpRightStick,
            9 => Input.GpLeftShoulder,
            10 => Input.GpRightShoulder,
            11 => Input.GpDirPadUp,
            12 => Input.GpDirPadDown,
            13 => Input.GpDirPadLeft,
            14 => Input.GpDirPadRight,
            15 => Input.GpMisc1,
            16 => Input.GpRightPaddle1, // Might be 17, i'm not sure?
            17 => Input.GpLeftPaddle1,
            18 => Input.GpRightPaddle2,
            19 => Input.GpLeftPaddle2,
            20 => Input.GpTouchpad,
            21 => Input.GpMisc2,
            22 => Input.GpMisc3,
            23 => Input.GpMisc4,
            24 => Input.GpMisc5,
            25 => Input.GpMisc6,
            _ => Input.Invalid
        };
    }

    private static Input SdlGamepadAxisToInput(byte axis)
    {
        return axis switch
        {
            0 => Input.GpAnalogLeftStickX,
            1 => Input.GpAnalogLeftStickY,
            2 => Input.GpAnalogRightStickX,
            3 => Input.GpAnalogRightStickY,
            4 => Input.GpAnalogLeftTrigger,
            5 => Input.GpAnalogRightTrigger,
            _ => Input.Invalid
        };
    }
}
