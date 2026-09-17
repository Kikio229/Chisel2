using System;

namespace Chisel.Framework;

public enum Input
{
    /* Keys */
    Invalid = 0,
    KeyA,
    KeyB,
    KeyC,
    KeyD,
    KeyE,
    KeyF,
    KeyG,
    KeyH,
    KeyI,
    KeyJ,
    KeyK,
    KeyL,
    KeyM,
    KeyN,
    KeyO,
    KeyP,
    KeyQ,
    KeyR,
    KeyS,
    KeyT,
    KeyU,
    KeyV,
    KeyW,
    KeyX,
    KeyY,
    KeyZ,
    KeyNumber1,
    KeyNumber2,
    KeyNumber3,
    KeyNumber4,
    KeyNumber5,
    KeyNumber6,
    KeyNumber7,
    KeyNumber8,
    KeyNumber9,
    KeyNumber0,
    KeyReturn,
    KeyEscape,
    KeyBackspace,
    KeyTab,
    KeySpace,
    KeyMinus,
    KeyEquals,
    KeyLeftBracket,
    KeyRightBracket,
    KeyBackslash,
    KeyNonUsHash, // Used in ISO keyboards
    KeySemicolon,
    KeyApostrophe,
    KeyGrave, // Also known as Tilde on English keyboards
    KeyComma,
    KeyPeriod,
    KeySlash,
    KeyCapsLock,
    KeyFunction1,
    KeyFunction2,
    KeyFunction3,
    KeyFunction4,
    KeyFunction5,
    KeyFunction6,
    KeyFunction7,
    KeyFunction8,
    KeyFunction9,
    KeyFunction10,
    KeyFunction11,
    KeyFunction12,
    KeyPrintScreen,
    KeyScrollLock,
    KeyPause,
    KeyInsert, // This is also Help on some Mac keyboards
    KeyHome,
    KeyPageUp,
    KeyDelete,
    KeyEnd,
    KeyPageDown,
    KeyRightArrow,
    KeyLeftArrow,
    KeyDownArrow,
    KeyUpArrow,
    KeyNumLock, // This is also the Clear on Mac keyboards
    KeyKeypadDivide,
    KeyKeypadMultiply,
    KeyKeypadMinus,
    KeyKeypadPlus,
    KeyKeypadEnter,
    KeyKeypad1,
    KeyKeypad2,
    KeyKeypad3,
    KeyKeypad4,
    KeyKeypad5,
    KeyKeypad6,
    KeyKeypad7,
    KeyKeypad8,
    KeyKeypad9,
    KeyKeypad0,
    KeyKeypadPeriod,
    KeyNonUSBackslash, // Used in ISO keyboards
    KeyApplication, // Windows key?
    KeyPower, // Some weird USB thing according to SDL
    KeyKeypadEquals,
    KeyFunction13,
    KeyFunction14,
    KeyFunction15,
    KeyFunction16,
    KeyFunction17,
    KeyFunction18,
    KeyFunction19,
    KeyFunction20,
    KeyFunction21,
    KeyFunction22,
    KeyFunction23,
    KeyFunction24,
    KeyLCtrl,
    KeyLShift,
    KeyLAlt,
    KeyLGui,
    KeyRCtrl,
    KeyRShift,
    KeyRAlt,
    KeyRGui,

    /* Mouse */
    MouseLeft,
    MouseMiddle,
    MouseRight,
    MouseMisc1, // Mouse4
    MouseMisc2, // Mouse5

    /* Gamepad */
    GpFaceDown, // Xbox A, Nintendo B, PS Cross, etc.
    GpFaceRight, // Xbox B, Nintendo A, PS Circle, etc.
    GpFaceLeft, // Xbox X, Nintendo Y, PS Square, etc.
    GpFaceUp, // Xbox Y, Nintendo X, PS Triangle, etc.
    GpSelect, // Apparently this is actually called the back button? I'm not calling it that
    GpGuide, // That one button that takes you to like, the home menu on most consoles
    GpStart,
    GpLeftStick,
    GpRightStick,
    GpLeftShoulder,
    GpRightShoulder,
    GpDirPadUp,
    GpDirPadDown,
    GpDirPadLeft,
    GpDirPadRight,
    GpMisc1, // Xbox share, Nintendo capture, PS microphone, etc.
    GpRightPaddle1, // Upper right paddle on the back of the controller
    GpLeftPaddle1, // Upper left paddle on the back of the controller
    GpRightPaddle2, // Lower right paddle on the back of the controller
    GpLeftPaddle2, // Lower left paddle on the back of the controller
    GpTouchpad, // PS touchpad
    GpMisc2,
    GpMisc3,
    GpMisc4,
    GpMisc5,
    GpMisc6,

    /* Gamepad Analog */
    GpAnalogLeftStickX,
    GpAnalogLeftStickY,
    GpAnalogRightStickX,
    GpAnalogRightStickY,
    GpAnalogLeftTrigger,
    GpAnalogRightTrigger,
}
