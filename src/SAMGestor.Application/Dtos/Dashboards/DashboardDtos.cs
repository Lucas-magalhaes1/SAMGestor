// SAMGestor.Application/Dtos/Dashboards/DashboardDtos.cs
using System;

namespace SAMGestor.Application.Dtos.Dashboards;

public sealed class DashboardOverviewDto
{
    public required OverviewRetreatDto Retreat { get; init; }
    public required OverviewKpisDto Kpis { get; init; }
    public required OverviewGenderDto Gender { get; init; }
    public required BreakdownItemDto[] Shirts { get; init; }
    public required BreakdownItemDto[] CitiesTop { get; init; }
    public required OverviewFamiliesDto Families { get; init; }
    public required OverviewTentsDto Tents { get; init; }
    public required OverviewPaymentsDto Payments { get; init; }
    public required OverviewServiceDto Service { get; init; } // agora com KPIs
}

public sealed class OverviewRetreatDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Edition { get; init; } = "";
}

public sealed class OverviewKpisDto
{
    public int TotalConfirmed { get; init; }
    public int TotalPaid { get; init; }
    public int TotalPending { get; init; }
    public int Capacity { get; init; }
    public double OccupancyPercent { get; init; }
}

public sealed class OverviewGenderDto
{
    public double Male { get; init; }
    public double Female { get; init; }
}

public sealed class OverviewFamiliesDto
{
    public int Count { get; init; }
    public BreakdownItemDto[] TopByPaid { get; init; } = Array.Empty<BreakdownItemDto>();
}

public sealed class OverviewTentsDto
{
    public int Total { get; init; }
    public int Occupied { get; init; }
    public double OccupancyPercent { get; init; }
}

public sealed class OverviewPaymentsDto
{
    public BreakdownItemDto[] ByMethod { get; init; } = Array.Empty<BreakdownItemDto>();
    public PaymentPointDto[] TimeSeries { get; init; } = Array.Empty<PaymentPointDto>();
}

public sealed class PaymentPointDto
{
    public required string Date { get; init; } // YYYY-MM-DD
    public int Paid { get; init; }
    public int Pending { get; init; }
}

public sealed class BreakdownItemDto
{
    public required string Label { get; init; }
    public int Value { get; init; }
}

// ---------- Families list ----------
public sealed class FamiliesListDto
{
    public int Total { get; init; }
    public FamilyRowDto[] Items { get; init; } = Array.Empty<FamilyRowDto>();
}

public sealed class FamilyRowDto
{
    public required string Family { get; init; }
    public int Confirmed { get; init; }
    public int Paid { get; init; }
    public int Pending { get; init; }
    public double AvgAge { get; init; }
    public double FemalePercent { get; init; }
}

// ---------- Payments time series ----------
public enum TimeInterval
{
    Daily,
    Weekly
}

// ---------- Service (novo/atualizado) ----------
public sealed class OverviewServiceDto
{
    public required ServiceKpisDto Kpis { get; init; }
    public ServiceSpaceItemDto[] Spaces { get; init; } = Array.Empty<ServiceSpaceItemDto>();
}

public sealed class ServiceKpisDto
{
    public int Submitted { get; init; }
    public int Confirmed { get; init; }
    public int Declined { get; init; }
    public int Cancelled { get; init; }
    public int Assigned { get; init; }
    public int Paid { get; init; }
}

public sealed class ServiceSpaceItemDto
{
    public required string Label { get; init; }
    public int Capacity { get; init; }      // MaxPeople
    public int Submitted { get; init; }
    public int Confirmed { get; init; }
    public int Assigned { get; init; }
    public double OccupancyPercent { get; init; }
}
