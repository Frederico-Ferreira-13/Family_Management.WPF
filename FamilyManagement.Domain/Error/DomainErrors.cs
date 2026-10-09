namespace FamilyManagement.Domain.Errors;

public static class DomainErrors
{
    public static class Money
    {
        public static Error InvalidAmount =>
            Error.Validation(
                "O montante não pode ser negativo.");

        public static Error InvalidCurrency =>
            Error.Validation(
                "A moeda deve conter três letras, por exemplo EUR.");

        public static Error MissingAmount =>
            Error.Validation(
                "O montante é obrigatório.");

        public static Error CurrencyMismatch =>
            Error.BusinessRule(
                "money.currency_mismatch",
                "Não é possível operar montantes de moedas diferentes.");

        public static Error NegativeResult =>
            Error.BusinessRule(
                "money.negative_result",
                "O resultado da operação não pode ser negativo.");

        public static Error Overflow =>
            Error.BusinessRule(
                "money.overflow",
                "O resultado excede o valor monetário suportado.");
    }

    public static class Transaction
    {
        public static Error InvalidAmount =>
            Error.BusinessRule(
                "transaction.invalid_amount",
                "O valor da transação deve ser superior a zero.");

        public static Error NotFound =>
            Error.NotFound(
                "transaction.not_found",
                "Transação não encontrada.");
    }

    public static class Account
    {
        public static Error NotFound =>
            Error.NotFound(
                "account.not_found",
                "Conta não encontrada.");

        public static Error Inactive =>
            Error.Conflict(
                "account.inactive",
                "A conta está inativa.");

        public static Error InsufficientFunds =>
            Error.BusinessRule(
                "account.insufficient_funds",
                "Saldo insuficiente.");
    }
}