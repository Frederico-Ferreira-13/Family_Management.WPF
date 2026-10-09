using System.Collections;
using System.ComponentModel;

namespace Family_Management.WPF.Validation;

public abstract class ValidatorBase : INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return _errors.Values.SelectMany(errors => errors);
        }

        return _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Enumerable.Empty<string>();
    }

    protected void AddError(
        string propertyName,
        string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        if (!_errors.TryGetValue(propertyName, out var errors))
        {
            errors = new List<string>();
            _errors[propertyName] = errors;
        }

        if (errors.Contains(error))
        {
            return;
        }

        errors.Add(error);

        OnErrorsChanged(propertyName);
    }

    protected void SetError(
        string propertyName,
        string? error)
    {
        ClearErrors(propertyName);

        if (!string.IsNullOrWhiteSpace(error))
        {
            AddError(propertyName, error);
        }
    }

    protected void ClearErrors(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
        }
    }

    public void ClearAllErrors()
    {
        if (_errors.Count == 0)
        {
            return;
        }

        var properties = _errors.Keys.ToArray();

        _errors.Clear();

        foreach (var propertyName in properties)
        {
            OnErrorsChanged(propertyName);
        }
    }

    protected virtual void OnErrorsChanged(
        string propertyName)
    {
        ErrorsChanged?.Invoke(
            this,
            new DataErrorsChangedEventArgs(propertyName));
    }
}