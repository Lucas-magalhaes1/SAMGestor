using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Entities;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Reports;

public sealed class ReportingReadDb : IReportingReadDb
{
    private readonly SAMContext _db;
    private readonly bool _asNoTracking;

    public ReportingReadDb(SAMContext db, bool asNoTracking = false)
    {
        _db = db;
        _asNoTracking = asNoTracking;
    }

    private IQueryable<T> Set<T>() where T : class =>
        _asNoTracking ? _db.Set<T>().AsNoTracking() : _db.Set<T>();

    // -------- Registros principais (Fazer) --------
    public IQueryable<Registration>   Registrations    => Set<Registration>();
    public IQueryable<Payment>        Payments         => Set<Payment>();
    public IQueryable<Family>         Families         => Set<Family>();
    public IQueryable<FamilyMember>   FamilyMembers    => Set<FamilyMember>();
    public IQueryable<Tent>           Tents            => Set<Tent>();
    public IQueryable<TentAssignment> TentAssignments  => Set<TentAssignment>();

    // -------- Serviço (NOVOS) --------
    public IQueryable<ServiceSpace>               ServiceSpaces               => Set<ServiceSpace>();
    public IQueryable<ServiceRegistration>        ServiceRegistrations        => Set<ServiceRegistration>();
    public IQueryable<ServiceAssignment>          ServiceAssignments          => Set<ServiceAssignment>();
    public IQueryable<ServiceRegistrationPayment> ServiceRegistrationPayments => Set<ServiceRegistrationPayment>();

    // -------- Utilitários --------
    public IReportingReadDb AsNoTracking() => new ReportingReadDb(_db, asNoTracking: true);

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct)
        => query.ToListAsync(ct);
}