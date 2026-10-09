using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Models;
using FamilyManagement.Application.UseCases.Currencies.Commands.ActivateCurrency;
using FamilyManagement.Application.UseCases.Currencies.Commands.CreateCurrency;
using FamilyManagement.Application.UseCases.Currencies.Commands.DeactivateCurrency;
using FamilyManagement.Application.UseCases.Currencies.Commands.SetDefaultCurrency;
using FamilyManagement.Application.UseCases.Currencies.Commands.UpdateCurrency;
using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;

namespace Family_Management.WPF.ViewModel.Currencies;

public sealed class CurrencyViewModel : BaseViewModel
{
    private readonly CreateCurrencyHandler _createCurrencyHandler;
    private readonly UpdateCurrencyHandler _updateCurrencyHandler;
    private readonly ActivateCurrencyHandler _activateCurrencyHandler;
    private readonly DeactivateCurrencyHandler _deactivateCurrencyHandler;
    private readonly SetDefaultCurrencyHandler _setDefaultCurrencyHandler;
    private readonly GetCurrenciesHandler _getCurrenciesHandler;

    private CurrencyDTO? _selectedCurrency;
    private CurrencyFormModel _form;

    public CurrencyViewModel(
        CreateCurrencyHandler createCurrencyHandler,
        UpdateCurrencyHandler updateCurrencyHandler,
        ActivateCurrencyHandler activateCurrencyHandler,
        DeactivateCurrencyHandler deactivateCurrencyHandler,
        SetDefaultCurrencyHandler setDefaultCurrencyHandler,
        GetCurrenciesHandler getCurrenciesHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _createCurrencyHandler = createCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(createCurrencyHandler));

        _updateCurrencyHandler = updateCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(updateCurrencyHandler));

        _activateCurrencyHandler = activateCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(activateCurrencyHandler));

        _deactivateCurrencyHandler = deactivateCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(deactivateCurrencyHandler));

        _setDefaultCurrencyHandler = setDefaultCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(setDefaultCurrencyHandler));

        _getCurrenciesHandler = getCurrenciesHandler
            ?? throw new ArgumentNullException(
                nameof(getCurrenciesHandler));

        Title = "Moedas";

        // O formulário é observável.
        // Sempre que uma propriedade do Form for alterada,
        // os estados dos comandos serão reavaliados.
        _form = CurrencyFormModel.CreateNew();
        SubscribeToForm(_form);

        SaveCommand =
            new AsyncRelayCommand(
                SaveCurrencyAsync,
                CanSave);

        ClearFormCommand =
            new RelayCommand(
                ClearForm);

        ToggleStatusCommand =
            new AsyncRelayCommand(
                ToggleSelectedCurrencyStatusAsync,
                CanToggleStatus);

        SetDefaultCommand =
            new AsyncRelayCommand(
                SetDefaultAsync,
                CanSetDefault);

        RefreshCommand =
            new AsyncRelayCommand(
                RefreshAsync,
                () => !IsBusy);
    }

    public ObservableCollection<CurrencyDTO> Currencies { get; }
        = new();

    public CurrencyDTO? SelectedCurrency
    {
        get => _selectedCurrency;

        set
        {
            if (!SetProperty(
                    ref _selectedCurrency,
                    value))
            {
                return;
            }

            if (value is null)
            {
                Form =
                    CurrencyFormModel.CreateNew();
            }
            else
            {
                Form =
                    CurrencyFormModel.FromCurrency(
                        value);
            }

            RaiseCommandStates();
        }
    }

    public CurrencyFormModel Form
    {
        get => _form;

        private set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(_form, value))
            {
                return;
            }

            UnsubscribeFromForm(_form);

            _form = value;

            SubscribeToForm(_form);

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditMode));

            RaiseCommandStates();
        }
    }

    public bool IsEditMode =>
        Form.IsEditMode;

    public ICommand SaveCommand { get; }

    public ICommand ClearFormCommand { get; }

    public ICommand ToggleStatusCommand { get; }

    public ICommand SetDefaultCommand { get; }

    public ICommand RefreshCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await LoadCurrenciesAsync(
            cancellationToken);

        ClearForm();

        IsInitialized = true;
    }

    private async Task RefreshAsync()
    {
        await RunSafeAsync(async () =>
        {
            await LoadCurrenciesAsync(
                CancellationToken.None);

            ClearForm();
        });

        RaiseCommandStates();
    }

    private async Task LoadCurrenciesAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await _getCurrenciesHandler.HandleAsync(
                new GetCurrenciesQuery(
                    OnlyActive: false),
                cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Moedas");

            return;
        }

        Currencies.Clear();

        foreach (var currency in result.Value
                     .OrderByDescending(
                         currency => currency.IsDefault)
                     .ThenBy(
                         currency => currency.Code))
        {
            Currencies.Add(currency);
        }
    }

    private bool CanSave()
    {
        if (IsBusy)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                Form.Name))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                Form.Symbol))
        {
            return false;
        }

        if (Form.DecimalPlaces > 8)
        {
            return false;
        }

        if (!Form.IsEditMode)
        {
            var code =
                Form.Code.Trim();

            if (string.IsNullOrWhiteSpace(code) ||
                code.Length != 3)
            {
                return false;
            }
        }

        return true;
    }

    private async Task SaveCurrencyAsync()
    {
        if (!CanSave())
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            if (Form.IsEditMode)
            {
                await UpdateCurrencyAsync();
            }
            else
            {
                await CreateCurrencyAsync();
            }
        });

        RaiseCommandStates();
    }

    private async Task CreateCurrencyAsync()
    {
        var command =
            new CreateCurrencyCommand(
                Code:
                    Form.Code
                        .Trim()
                        .ToUpperInvariant(),
                Name:
                    Form.Name.Trim(),
                Symbol:
                    Form.Symbol.Trim(),
                DecimalPlaces:
                    Form.DecimalPlaces,
                IsDefault:
                    Form.IsDefault);

        var result =
            await _createCurrencyHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Criar moeda");

            return;
        }

        await LoadCurrenciesAsync(
            CancellationToken.None);

        ClearForm();

        await NotificationService.ShowInformationAsync(
            "Moeda criada com sucesso.",
            "Sucesso");
    }

    private async Task UpdateCurrencyAsync()
    {
        if (!Form.CurrencyId.HasValue ||
            Form.CurrencyId.Value == Guid.Empty)
        {
            return;
        }

        var command =
            new UpdateCurrencyCommand(
                CurrencyId:
                    Form.CurrencyId.Value,
                Name:
                    Form.Name.Trim(),
                Symbol:
                    Form.Symbol.Trim(),
                DecimalPlaces:
                    Form.DecimalPlaces);

        var result =
            await _updateCurrencyHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Atualizar moeda");

            return;
        }

        ReplaceCurrency(
            result.Value);

        SelectedCurrency =
            result.Value;

        await NotificationService.ShowInformationAsync(
            "Moeda atualizada com sucesso.",
            "Sucesso");
    }

    private bool CanToggleStatus()
    {
        return !IsBusy &&
               SelectedCurrency is not null;
    }

    private async Task ToggleSelectedCurrencyStatusAsync()
    {
        if (SelectedCurrency is null)
        {
            return;
        }

        var currency =
            SelectedCurrency;

        var action =
            currency.IsActive
                ? "desativar"
                : "ativar";

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja {action} a moeda '{currency.Code}'?",
                "Confirmar");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                currency.IsActive
                    ? await _deactivateCurrencyHandler.HandleAsync(
                        new DeactivateCurrencyCommand(
                            currency.Id))
                    : await _activateCurrencyHandler.HandleAsync(
                        new ActivateCurrencyCommand(
                            currency.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    currency.IsActive
                        ? "Desativar moeda"
                        : "Ativar moeda");

                return;
            }

            await LoadCurrenciesAsync(
                CancellationToken.None);

            ClearForm();

            await NotificationService.ShowInformationAsync(
                currency.IsActive
                    ? "Moeda desativada com sucesso."
                    : "Moeda ativada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    private bool CanSetDefault()
    {
        return !IsBusy &&
               SelectedCurrency is
               {
                   IsActive: true,
                   IsDefault: false
               };
    }

    private async Task SetDefaultAsync()
    {
        if (SelectedCurrency is null)
        {
            return;
        }

        var currency =
            SelectedCurrency;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja definir '{currency.Code}' como moeda padrão?",
                "Moeda padrão");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _setDefaultCurrencyHandler.HandleAsync(
                    new SetDefaultCurrencyCommand(
                        currency.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Definir moeda padrão");

                return;
            }

            await LoadCurrenciesAsync(
                CancellationToken.None);

            ClearForm();

            await NotificationService.ShowInformationAsync(
                $"A moeda '{currency.Code}' foi definida como padrão.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    private void ClearForm()
    {
        _selectedCurrency = null;

        OnPropertyChanged(
            nameof(SelectedCurrency));

        Form =
            CurrencyFormModel.CreateNew();

        RaiseCommandStates();
    }

    private void ReplaceCurrency(
        CurrencyDTO updated)
    {
        ArgumentNullException.ThrowIfNull(updated);

        var existing =
            Currencies.FirstOrDefault(
                currency =>
                    currency.Id == updated.Id);

        if (existing is null)
        {
            return;
        }

        var index =
            Currencies.IndexOf(existing);

        Currencies[index] =
            updated;
    }

    private void SubscribeToForm(
        CurrencyFormModel form)
    {
        form.PropertyChanged +=
            OnFormPropertyChanged;
    }

    private void UnsubscribeFromForm(
        CurrencyFormModel form)
    {
        form.PropertyChanged -=
            OnFormPropertyChanged;
    }

    private void OnFormPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(CurrencyFormModel.CurrencyId))
        {
            OnPropertyChanged(
                nameof(IsEditMode));
        }

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (SaveCommand
            is AsyncRelayCommand save)
        {
            save.RaiseCanExecuteChanged();
        }

        if (ToggleStatusCommand
            is AsyncRelayCommand toggle)
        {
            toggle.RaiseCanExecuteChanged();
        }

        if (SetDefaultCommand
            is AsyncRelayCommand setDefault)
        {
            setDefault.RaiseCanExecuteChanged();
        }

        if (RefreshCommand
            is AsyncRelayCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }
    }
}