using System.Windows.Input;

namespace Family_Management.WPF.Commands;

public interface IRelayCommand : ICommand
{
    void RaiseCanExecuteChanged();
}