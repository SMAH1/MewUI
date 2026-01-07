#:sdk Microsoft.NET.Sdk
#:property OutputType=WinExe
#:property TargetFramework=net10.0-windows
#:property PublishAot=true
#:package Aprillz.MewUI@0.1.0-preview.1

using System.Globalization;
using System.Text;

using Aprillz.MewUI.Binding;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Core;
using Aprillz.MewUI.Input;
using Aprillz.MewUI.Markup;
using Aprillz.MewUI.Panels;
using Aprillz.MewUI.Primitives;
using Aprillz.MewUI.Rendering;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

var useGdi = args.Any(a => a.Equals("--gdi", StringComparison.OrdinalIgnoreCase));
Application.DefaultGraphicsBackend = useGdi ? GraphicsBackend.Gdi : GraphicsBackend.Direct2D;

var goldAccent = Color.FromRgb(214, 176, 82);
Theme.Current = Theme.Dark.WithAccent(goldAccent);

var expression = new ObservableValue<string>(string.Empty);
var result = new ObservableValue<string>("0");
var error = new ObservableValue<string>(string.Empty);

var keySink = new KeySink
{
    IsHitTestVisible = false,
    KeyDown = OnKeySyncKeyDown,
    TextInput = e =>
    {
        if (TryAppendFromText(e.Text))
            e.Handled = true;
    }
};


UniformGrid Keypad() => new UniformGrid()
    .Rows(5)
    .Columns(4)
    .Children(
        Key("C", Clear),
        Key("(", () => Append("(")),
        Key(")", () => Append(")")),
        Key("⌫", Backspace),

        Key("7", () => Append("7")),
        Key("8", () => Append("8")),
        Key("9", () => Append("9")),
        Key("/", () => Append("/")),

        Key("4", () => Append("4")),
        Key("5", () => Append("5")),
        Key("6", () => Append("6")),
        Key("*", () => Append("*")),

        Key("1", () => Append("1")),
        Key("2", () => Append("2")),
        Key("3", () => Append("3")),
        Key("-", () => Append("-")),

        Key("0", () => Append("0")),
        Key(".", () => Append(".")),
        Key("=", CommitEquals, isPrimary: true),
        Key("+", () => Append("+"))
    );

var window = new Window() 
    .Title("MewUI FBA Calculator")
    .Size(360, 520)
    .Content(
        new DockPanel()
            .Margin(8)
            .Children(
                keySink,
                new StackPanel()
                    .DockTop()
                    .Spacing(6)
                    .Children(
                        new Label()
                            .BindText(expression, s => string.IsNullOrEmpty(s) ? " " : s)
                            .TextWrapping(TextWrapping.Wrap)
                            .FontSize(16),

                        new Label()
                            .BindText(result)
                            .FontSize(28)
                            .Bold()
                            .TextAlignment(TextAlignment.Right),

                        new Label()
                            .BindText(error, s => string.IsNullOrEmpty(s) ? " " : s)
                            .Foreground(Theme.Current.DisabledText)
                            .TextWrapping(TextWrapping.Wrap)
                    ),

                Keypad()
            )
    );

Recompute();
window.Loaded = () => keySink.Focus();

Application.Run(window);


void Recompute()
{
    if (string.IsNullOrWhiteSpace(expression.Value))
    {
        result.Value = "0";
        error.Value = string.Empty;
        return;
    }

    try
    {
        var value = ExpressionEvaluator.Evaluate(expression.Value);
        result.Value = value.ToString("G15", CultureInfo.InvariantCulture);
        error.Value = string.Empty;
    }
    catch (Exception ex)
    {
        result.Value = "—";
        error.Value = ex.Message;
    }
}

void Append(string token)
{
    if (string.IsNullOrEmpty(token))
        return;

    expression.Value += token;
    Recompute();
}

void Clear()
{
    expression.Value = string.Empty;
    result.Value = "0";
    error.Value = string.Empty;
}

void Backspace()
{
    if (string.IsNullOrEmpty(expression.Value))
        return;

    expression.Value = expression.Value[..^1];
    Recompute();
}

void CommitEquals()
{
    if (!string.IsNullOrEmpty(error.Value))
        return;

    expression.Value = result.Value;
    Recompute();
}

bool TryAppendFromText(string text)
{
    if (string.IsNullOrEmpty(text))
        return false;

    bool appended = false;
    for (int i = 0; i < text.Length; i++)
    {
        char c = text[i];
        if (c is >= '0' and <= '9' or '.' or '+' or '-' or '*' or '/' or '(' or ')')
        {
            Append(c.ToString());
            appended = true;
        }
    }

    return appended;
}

void OnKeySyncKeyDown(KeyEventArgs e)
{
    const int VK_BACK = 0x08;
    const int VK_ESCAPE = 0x1B;
    const int VK_RETURN = 0x0D;

    if (e.Key == VK_BACK)
    {
        Backspace();
        e.Handled = true;
        return;
    }

    if (e.Key == VK_ESCAPE)
    {
        Clear();
        e.Handled = true;
        return;
    }

    if (e.Key == VK_RETURN)
    {
        CommitEquals();
        e.Handled = true;
        return;
    }
}


Button Key(string text, Action onClick, bool isPrimary = false)
{
    void WrappedClick()
    {
        onClick();
        keySink.Focus();
    }

    var b = new Button()
        .Content(text)
        .Margin(2)
        .FontSize(20)
        .OnClick(WrappedClick)
        .MinWidth(56)
        .MinHeight(44);

    if (isPrimary)
        b.BorderBrush(Theme.Current.Accent);

    return b;
}

sealed class KeySink : Control
{
    public override bool Focusable => true;
    protected override Size MeasureOverride(Size availableSize) => new(0, 0);
    protected override Size ArrangeOverride(Size finalSize) => new(0, 0);
}

static class ExpressionEvaluator
{
    public static double Evaluate(string input)
    {
        var tokens = Tokenize(input);
        var rpn = ToRpn(tokens);
        return EvalRpn(rpn);
    }

    private enum TokenKind
    {
        Number,
        Operator,
        LParen,
        RParen,
    }

    private readonly record struct Token(TokenKind Kind, double Number, char Op)
    {
        public static Token Num(double value) => new(TokenKind.Number, value, '\0');
        public static Token Oper(char op) => new(TokenKind.Operator, 0, op);
        public static Token LParen() => new(TokenKind.LParen, 0, '\0');
        public static Token RParen() => new(TokenKind.RParen, 0, '\0');
    }

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var number = new StringBuilder();

        bool lastWasValue = false; // number or ')'
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c))
                continue;

            if (char.IsDigit(c) || c == '.')
            {
                number.Append(c);
                continue;
            }

            FlushNumber();

            if (c == '(')
            {
                tokens.Add(Token.LParen());
                lastWasValue = false;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(Token.RParen());
                lastWasValue = true;
                continue;
            }

            if (IsOperator(c))
            {
                // Unary minus: treat "-x" as "0 - x".
                if (c == '-' && !lastWasValue)
                    tokens.Add(Token.Num(0));

                tokens.Add(Token.Oper(c));
                lastWasValue = false;
                continue;
            }

            throw new FormatException($"Invalid character '{c}'.");
        }

        FlushNumber();
        return tokens;

        void FlushNumber()
        {
            if (number.Length == 0)
                return;

            if (!double.TryParse(number.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"Invalid number '{number}'.");

            tokens.Add(Token.Num(value));
            number.Clear();
            lastWasValue = true;
        }
    }

    private static bool IsOperator(char c) => c is '+' or '-' or '*' or '/';

    private static int Precedence(char op) => op is '*' or '/' ? 2 : 1;

    private static List<Token> ToRpn(List<Token> tokens)
    {
        var output = new List<Token>(tokens.Count);
        var stack = new Stack<Token>();

        foreach (var t in tokens)
        {
            switch (t.Kind)
            {
                case TokenKind.Number:
                    output.Add(t);
                    break;

                case TokenKind.Operator:
                    while (stack.Count > 0 && stack.Peek().Kind == TokenKind.Operator)
                    {
                        var top = stack.Peek();
                        if (Precedence(top.Op) < Precedence(t.Op))
                            break;
                        output.Add(stack.Pop());
                    }
                    stack.Push(t);
                    break;

                case TokenKind.LParen:
                    stack.Push(t);
                    break;

                case TokenKind.RParen:
                    while (stack.Count > 0 && stack.Peek().Kind != TokenKind.LParen)
                        output.Add(stack.Pop());

                    if (stack.Count == 0 || stack.Peek().Kind != TokenKind.LParen)
                        throw new FormatException("Mismatched parentheses.");

                    stack.Pop(); // '('
                    break;
            }
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t.Kind is TokenKind.LParen or TokenKind.RParen)
                throw new FormatException("Mismatched parentheses.");
            output.Add(t);
        }

        return output;
    }

    private static double EvalRpn(List<Token> tokens)
    {
        var values = new Stack<double>();

        foreach (var t in tokens)
        {
            if (t.Kind == TokenKind.Number)
            {
                values.Push(t.Number);
                continue;
            }

            if (t.Kind != TokenKind.Operator)
                throw new FormatException("Invalid expression.");

            if (values.Count < 2)
                throw new FormatException("Invalid expression.");

            var b = values.Pop();
            var a = values.Pop();
            values.Push(t.Op switch
            {
                '+' => a + b,
                '-' => a - b,
                '*' => a * b,
                '/' => b == 0 ? throw new DivideByZeroException("Division by zero.") : a / b,
                _ => throw new FormatException($"Unknown operator '{t.Op}'."),
            });
        }

        if (values.Count != 1)
            throw new FormatException("Invalid expression.");

        return values.Pop();
    }
}
