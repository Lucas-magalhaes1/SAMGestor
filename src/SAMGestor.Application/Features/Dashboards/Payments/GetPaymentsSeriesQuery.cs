// SAMGestor.Application/Features/Dashboards/Payments/GetPaymentsSeriesQuery.cs
using System.Linq;
using FluentValidation;
using MediatR;
using SAMGestor.Application.Dtos.Dashboards;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;
using AppTimeInterval = SAMGestor.Application.Dtos.Dashboards.TimeInterval;

namespace SAMGestor.Application.Features.Dashboards.Payments;

public sealed record GetPaymentsSeriesQuery(
    Guid RetreatId,
    AppTimeInterval Interval = AppTimeInterval.Daily,
    DateOnly? From = null,
    DateOnly? To = null
) : IRequest<PaymentPointDto[]>;

public sealed class GetPaymentsSeriesValidator : AbstractValidator<GetPaymentsSeriesQuery>
{
    public GetPaymentsSeriesValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'to' deve ser maior/igual a 'from'.");
    }
}

public sealed class GetPaymentsSeriesHandler : IRequestHandler<GetPaymentsSeriesQuery, PaymentPointDto[]>
{
    private readonly IReportingReadDb _db;
    public GetPaymentsSeriesHandler(IReportingReadDb db) => _db = db;

    public async Task<PaymentPointDto[]> Handle(GetPaymentsSeriesQuery request, CancellationToken ct)
    {
        var rid = request.RetreatId;

        var regs = await _db.ToListAsync(
            _db.AsNoTracking().Registrations
               .Where(r => r.Status == RegistrationStatus.Confirmed && r.RetreatId == rid)
               .Select(r => new { r.Id, r.RegistrationDate }), ct);

        var regIds = regs.Select(r => r.Id).ToHashSet();

        var pays = await _db.ToListAsync(
            _db.AsNoTracking().Payments
               .Where(p => p.Status == PaymentStatus.Paid)
               .Select(p => new { p.RegistrationId, p.PaidAt }), ct);

        var paysRetreat = pays.Where(p => regIds.Contains(p.RegistrationId)).ToList();
        var paidSet = paysRetreat.Select(p => p.RegistrationId).ToHashSet();

        var (from, to) = ResolveRange(
            regs.Select(r => DateOnly.FromDateTime(r.RegistrationDate)),
            paysRetreat.Select(p => p.PaidAt),
            request.From, request.To);

        if (request.Interval == AppTimeInterval.Daily)
        {
            var days = DatesBetween(from, to).ToArray();

            var paidByDay = paysRetreat
                .Where(p => p.PaidAt.HasValue)
                .GroupBy(p => DateOnly.FromDateTime(p.PaidAt!.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            var pendingByDay = regs
                .Where(r => !paidSet.Contains(r.Id))
                .GroupBy(r => DateOnly.FromDateTime(r.RegistrationDate))
                .ToDictionary(g => g.Key, g => g.Count());

            return days.Select(d => new PaymentPointDto
            {
                Date = d.ToString("yyyy-MM-dd"),
                Paid = paidByDay.TryGetValue(d, out var a) ? a : 0,
                Pending = pendingByDay.TryGetValue(d, out var b) ? b : 0
            }).ToArray();
        }
        else // Weekly
        {
            var buckets = new Dictionary<(int Year, int Week), (int paid, int pending)>();

            foreach (var p in paysRetreat.Where(p => p.PaidAt.HasValue))
            {
                var d = DateOnly.FromDateTime(p.PaidAt!.Value);
                var key = IsoWeekKey(d);
                buckets.TryGetValue(key, out var curr);
                buckets[key] = (curr.paid + 1, curr.pending);
            }

            foreach (var r in regs.Where(r => !paidSet.Contains(r.Id)))
            {
                var d = DateOnly.FromDateTime(r.RegistrationDate);
                var key = IsoWeekKey(d);
                buckets.TryGetValue(key, out var curr);
                buckets[key] = (curr.paid, curr.pending + 1);
            }

            return buckets
                .OrderBy(k => k.Key.Year).ThenBy(k => k.Key.Week)
                .Select(k => new PaymentPointDto
                {
                    Date = $"{k.Key.Year}-W{k.Key.Week:D2}",
                    Paid = k.Value.paid,
                    Pending = k.Value.pending
                })
                .ToArray();
        }
    }

    private static (int Year, int Week) IsoWeekKey(DateOnly d)
    {
        var dayOfWeek = (int)d.DayOfWeek;
        dayOfWeek = (dayOfWeek == 0) ? 7 : dayOfWeek; // domingo = 7
        var monday = d.AddDays(1 - dayOfWeek);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(monday.ToDateTime(TimeOnly.MinValue));
        var year = monday.Year;
        return (year, week);
    }

    private static (DateOnly from, DateOnly to) ResolveRange(
        IEnumerable<DateOnly> regDates,
        IEnumerable<DateTime?> paidDates,
        DateOnly? fromReq,
        DateOnly? toReq)
    {
        if (fromReq.HasValue && toReq.HasValue) return (fromReq.Value, toReq.Value);

        var minReg = regDates.DefaultIfEmpty(DateOnly.FromDateTime(DateTime.UtcNow)).Min();
        var minPay = paidDates.Where(d => d.HasValue).Select(d => DateOnly.FromDateTime(d!.Value)).DefaultIfEmpty(minReg).Min();

        // parênteses para evitar o erro do operador '??'
        var from = fromReq ?? (minReg < minPay ? minReg : minPay);
        var to = toReq ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (to < from) to = from;
        return (from, to);
    }

    private static IEnumerable<DateOnly> DatesBetween(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }
}
