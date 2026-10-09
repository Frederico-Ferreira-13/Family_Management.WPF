using System.Windows;
using System.Windows.Controls;

namespace Family_Management.WPF.Utilities;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordPropertyChanged));

    public static readonly DependencyProperty AttachProperty =
        DependencyProperty.RegisterAttached(
            "Attach",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(
                false,
                OnAttachPropertyChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static string GetPassword(DependencyObject obj)
    {
        return (string)obj.GetValue(PasswordProperty);
    }

    public static void SetPassword(
        DependencyObject obj,
        string value)
    {
        obj.SetValue(
            PasswordProperty,
            value ?? string.Empty);
    }

    public static bool GetAttach(DependencyObject obj)
    {
        return (bool)obj.GetValue(AttachProperty);
    }

    public static void SetAttach(
        DependencyObject obj,
        bool value)
    {
        obj.SetValue(
            AttachProperty,
            value);
    }

    private static bool GetIsUpdating(
        DependencyObject obj)
    {
        return (bool)obj.GetValue(IsUpdatingProperty);
    }

    private static void SetIsUpdating(
        DependencyObject obj,
        bool value)
    {
        obj.SetValue(
            IsUpdatingProperty,
            value);
    }

    private static void OnAttachPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        if (e.OldValue is true)
        {
            passwordBox.PasswordChanged -= OnPasswordChanged;
        }

        if (e.NewValue is true)
        {
            passwordBox.PasswordChanged += OnPasswordChanged;
        }
    }

    private static void OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        SetIsUpdating(
            passwordBox,
            true);

        try
        {
            SetPassword(
                passwordBox,
                passwordBox.Password);
        }
        finally
        {
            SetIsUpdating(
                passwordBox,
                false);
        }
    }

    private static void OnPasswordPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox ||
            GetIsUpdating(passwordBox))
        {
            return;
        }

        var newPassword =
            e.NewValue as string ?? string.Empty;

        if (passwordBox.Password == newPassword)
        {
            return;
        }

        passwordBox.Password = newPassword;
    }
}