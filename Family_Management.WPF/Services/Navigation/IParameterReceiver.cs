namespace Family_Management.WPF.Services.Navigation;

public interface IParameterReceiver<in T>
{
    void ReceiveParameter(T parameter);
}