using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Family_Management.WPF.Controls;

public sealed class MoneyTextBox : TextBox
{
    private static readonly CultureInfo PortugueseCulture =
        CultureInfo.GetCultureInfo("pt-PT");

    private static readonly Regex AmountPattern = new(
        @"^\d*(?:[.,]\d{0,2})?$",
        RegexOptions.CultureInvariant);

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(decimal),
            typeof(MoneyTextBox),
            new FrameworkPropertyMetadata(
                0m,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    private bool _synchronizing;

    public decimal Value
    {
        get => (decimal)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        ShowFormattedValue(Value);
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        if (_synchronizing)
            return;

        // Um texto inválido limpa o valor utilizado pelo ViewModel.
        // Assim, Guardar/Adicionar não reutiliza um montante anterior.
        var amount = TryReadAmount(Text, out var parsedAmount)
            ? parsedAmount
            : 0m;

        _synchronizing = true;

        try
        {
            // Preserva o binding existente na propriedade Value.
            SetCurrentValue(ValueProperty, amount);
        }
        finally
        {
            _synchronizing = false;
        }
    }

    protected override void OnLostKeyboardFocus(
        KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);

        // Mantém texto inválido visível para permitir a sua correção.
        if (TryReadAmount(Text, out var amount))
            ShowFormattedValue(amount);
    }

    private static void OnValueChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (MoneyTextBox)dependencyObject;

        if (control._synchronizing)
            return;

        // Atualizações vindas do ViewModel, por exemplo ao selecionar
        // outro movimento para editar, atualizam o texto apresentado.
        control.ShowFormattedValue((decimal)e.NewValue);
    }

    private void ShowFormattedValue(decimal amount)
    {
        _synchronizing = true;

        try
        {
            SetCurrentValue(
                TextProperty,
                amount.ToString("F2", PortugueseCulture));
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private static bool TryReadAmount(
        string? text,
        out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim();

        // Aceita até duas casas decimais.
        // Não aceita separadores de milhares para evitar ambiguidades.
        if (!AmountPattern.IsMatch(normalized))
            return false;

        normalized = normalized.Replace(',', '.');

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out amount);
    }
}