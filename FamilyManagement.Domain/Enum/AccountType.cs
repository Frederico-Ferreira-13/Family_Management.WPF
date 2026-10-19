using System.ComponentModel;

namespace FamilyManagement.Domain.Enums;

public enum AccountType
{
    [Description("Não especificado")]
    NotSpecified = 0,

    [Description("Conta à ordem")]
    Checking = 1,

    [Description("Conta poupança")]
    Savings = 2,

    [Description("Cartão de crédito")]
    CreditCard = 3,

    [Description("Numerário")]
    Cash = 4,

    [Description("Conta de investimento")]
    Investment = 5,

    [Description("Empréstimo")]
    Loan = 6,

    [Description("Crédito habitação")]
    Mortgage = 7
}