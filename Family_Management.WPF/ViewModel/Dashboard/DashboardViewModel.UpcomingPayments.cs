using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.Dashboard;

namespace Family_Management.WPF.ViewModel.Dashboard;

public sealed partial class DashboardViewModel
{
    private readonly GetUpcomingRecurringPaymentsHandler _upcomingPayments;

    public ObservableCollection<UpcomingRecurringPaymentDTO>
        UpcomingPayments { get; } = new();

    public string UpcomingPaymentsTitle { get; private set; } =
        "Pagamentos do próximo mês";

    public string UpcomingPaymentsSummary { get; private set; } =
        "A carregar os pagamentos…";

    private void ResetUpcomingPayments()
    {
        UpcomingPayments.Clear();

        var today = DateTime.Today;

        var nextMonth = new DateTime(
            today.Year,
            today.Month,
            1).AddMonths(1);

        UpcomingPaymentsTitle =
            "Pagamentos trimestrais e semestrais · "
            + nextMonth.ToString(
                "MMMM yyyy",
                CultureInfo.GetCultureInfo("pt-PT"));

        UpcomingPaymentsSummary =
            "A carregar os pagamentos…";
    }

    private async Task LoadUpcomingPaymentsAsync(
        Guid userId,
        Guid? familyId,
        DateTime currentMonth,
        CancellationToken cancellationToken)
    {
        UpcomingPayments.Clear();

        var nextMonth = new DateTime(
            currentMonth.Year,
            currentMonth.Month,
            1).AddMonths(1);

        var culture = CultureInfo.GetCultureInfo("pt-PT");

        UpcomingPaymentsTitle =
            "Pagamentos trimestrais e semestrais · "
            + nextMonth.ToString("MMMM yyyy", culture);

        try
        {
            var result = await _upcomingPayments.HandleAsync(
                userId,
                familyId,
                nextMonth,
                cancellationToken);

            if (result.IsFailure)
            {
                UpcomingPaymentsSummary =
                    "Não foi possível carregar o aviso. "
                    + result.Error.Message;

                return;
            }

            foreach (var payment in result.Value)
            {
                UpcomingPayments.Add(payment);
            }

            if (UpcomingPayments.Count == 0)
            {
                UpcomingPaymentsSummary =
                    "Não existem pagamentos trimestrais ou semestrais "
                    + "previstos para o próximo mês.";

                return;
            }

            var pending = UpcomingPayments
                .Where(payment => !payment.IsConfirmed)
                .ToList();

            if (pending.Count == 0)
            {
                UpcomingPaymentsSummary =
                    "Todos os pagamentos apresentados estão confirmados.";

                return;
            }

            var totals = pending
                .GroupBy(payment => payment.Currency)
                .OrderBy(group => group.Key)
                .Select(group =>
                    group.Sum(payment => payment.Amount)
                        .ToString("N2", culture)
                    + " "
                    + group.Key);

            var countText = pending.Count == 1
                ? "1 pagamento por confirmar"
                : $"{pending.Count} pagamentos por confirmar";

            UpcomingPaymentsSummary =
                countText
                + " · Total previsto: "
                + string.Join(" · ", totals);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            UpcomingPayments.Clear();

            UpcomingPaymentsSummary =
                "Não foi possível carregar os pagamentos. "
                + "Clique em Atualizar para tentar novamente.";
        }
        finally
        {
            OnPropertyChanged(
                nameof(UpcomingPaymentsTitle),
                nameof(UpcomingPaymentsSummary));
        }
    }
}