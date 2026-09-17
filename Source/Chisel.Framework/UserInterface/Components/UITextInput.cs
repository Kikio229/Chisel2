using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.UI;

public class UITextInput : UIPanel
{
    public string Text = string.Empty;
    public string Placeholder = string.Empty;
    public float FontSize = 20f;
    public int MaxLength = 256;

    public event Action<string> OnSubmit;
    public event Action<string> OnChanged;

    private bool focused;
    private int caretIndex;
    private float caretBlink;

    public UITextInput(UILayoutOptions options) : base(options)
    {
    }

    public override Rectangle PanelRect => focused
        ? new Rectangle(256, 0, 96, 32)
        : new Rectangle(256, 32, 96, 32);

    public override void OnPrimaryClicked()
    {
        focused = true;
        caretIndex = Text.Length;
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        if (!focused)
        {
            return;
        }

        if (InputManager.IsInputPressed(Input.MouseLeft) && !IsHighlighted)
        {
            focused = false;
            return;
        }

        caretBlink += dt;
        if (caretBlink > 1f)
        {
            caretBlink = 0f;
        }

        bool shift = InputManager.IsInputHeld(Input.KeyLShift) || InputManager.IsInputHeld(Input.KeyRShift);

        foreach (Input pressed in InputManager.GetPressedInputs().Concat(InputManager.GetRepeatedInputs()))
        {
            switch (pressed)
            {
                case Input.KeyBackspace:
                    if (caretIndex > 0)
                    {
                        Text = Text.Remove(caretIndex - 1, 1);
                        caretIndex--;
                        OnChanged?.Invoke(Text);
                    }
                    break;

                case Input.KeyDelete:
                    if (caretIndex < Text.Length)
                    {
                        Text = Text.Remove(caretIndex, 1);
                        OnChanged?.Invoke(Text);
                    }
                    break;

                case Input.KeyLeftArrow:
                    if (caretIndex > 0)
                    {
                        caretIndex--;
                    }
                    break;

                case Input.KeyRightArrow:
                    if (caretIndex < Text.Length)
                    {
                        caretIndex++;
                    }
                    break;

                case Input.KeyHome:
                    caretIndex = 0;
                    break;

                case Input.KeyEnd:
                    caretIndex = Text.Length;
                    break;

                case Input.KeyReturn:
                case Input.KeyKeypadEnter:
                    OnSubmit?.Invoke(Text);
                    focused = false;
                    break;

                case Input.KeyEscape:
                    focused = false;
                    break;

                default:
                    char c = KeyToChar(pressed, shift);
                    if (c != '\0' && Text.Length < MaxLength)
                    {
                        Text = Text.Insert(caretIndex, c.ToString());
                        caretIndex++;
                        OnChanged?.Invoke(Text);
                    }
                    break;
            }
        }
    }

    public override void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        base.OnRender(dt, batch, atlasTexture);

        Vector2 topLeft = ContentTopLeft;
        var pos = Position;

        string display = Text.Length > 0 ? Text : Placeholder;
        Color color = Text.Length > 0 ? Color.White : new Color(140, 140, 140);

        Vector2 measured = batch.MeasureText(display, (int)FontSize);
        var textOrigin = new Vector2(topLeft.X, pos.Y - measured.Y / 2f);

        batch.DrawString(display, (int)FontSize, textOrigin, color);

        if (focused && caretBlink < 0.5f)
        {
            Vector2 caretMeasured = batch.MeasureText(Text[..caretIndex], (int)FontSize);
            var caretPos = new Vector2(textOrigin.X + caretMeasured.X, textOrigin.Y);
            batch.DrawString("|", (int)FontSize, caretPos, Color.White);
        }
    }

    private static char KeyToChar(Input input, bool shift)
    {
        if (input >= Input.KeyA && input <= Input.KeyZ)
        {
            char letter = (char)('a' + (input - Input.KeyA));
            return shift ? char.ToUpper(letter) : letter;
        }

        return input switch
        {
            Input.KeyNumber1 => shift ? '!' : '1',
            Input.KeyNumber2 => shift ? '@' : '2',
            Input.KeyNumber3 => shift ? '#' : '3',
            Input.KeyNumber4 => shift ? '$' : '4',
            Input.KeyNumber5 => shift ? '%' : '5',
            Input.KeyNumber6 => shift ? '^' : '6',
            Input.KeyNumber7 => shift ? '&' : '7',
            Input.KeyNumber8 => shift ? '*' : '8',
            Input.KeyNumber9 => shift ? '(' : '9',
            Input.KeyNumber0 => shift ? ')' : '0',
            Input.KeySpace => ' ',
            Input.KeyMinus => shift ? '_' : '-',
            Input.KeyEquals => shift ? '+' : '=',
            Input.KeyComma => shift ? '<' : ',',
            Input.KeyPeriod => shift ? '>' : '.',
            Input.KeySlash => shift ? '?' : '/',
            Input.KeySemicolon => shift ? ':' : ';',
            Input.KeyApostrophe => shift ? '"' : '\'',
            Input.KeyLeftBracket => shift ? '{' : '[',
            Input.KeyRightBracket => shift ? '}' : ']',
            Input.KeyBackslash => shift ? '|' : '\\',
            Input.KeyGrave => shift ? '~' : '`',
            Input.KeyKeypadDivide => '/',
            Input.KeyKeypadMultiply => '*',
            Input.KeyKeypadMinus => '-',
            Input.KeyKeypadPlus => '+',
            Input.KeyKeypadPeriod => '.',
            Input.KeyKeypad0 => '0',
            Input.KeyKeypad1 => '1',
            Input.KeyKeypad2 => '2',
            Input.KeyKeypad3 => '3',
            Input.KeyKeypad4 => '4',
            Input.KeyKeypad5 => '5',
            Input.KeyKeypad6 => '6',
            Input.KeyKeypad7 => '7',
            Input.KeyKeypad8 => '8',
            Input.KeyKeypad9 => '9',
            _ => '\0',
        };
    }
}