using SAMGestor.Domain.Entities;

namespace SAMGestor.Application.Interfaces.Reports;

/// Porta mínima para consultas de relatório (read-only).
public interface IReportingReadDb
{
    IQueryable<Registration> Registrations { get; }
    IQueryable<Payment>      Payments      { get; }
    IQueryable<Family>       Families      { get; }
    IQueryable<FamilyMember> FamilyMembers { get; }
    IQueryable<Tent>         Tents         { get; }
    IQueryable<TentAssignment> TentAssignments { get; }
    
    IQueryable<ServiceSpace>                 ServiceSpaces               { get; }
    IQueryable<ServiceRegistration>          ServiceRegistrations        { get; }
    IQueryable<ServiceAssignment>            ServiceAssignments          { get; }
    IQueryable<ServiceRegistrationPayment>   ServiceRegistrationPayments { get; }

    IReportingReadDb AsNoTracking();

    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct);
}