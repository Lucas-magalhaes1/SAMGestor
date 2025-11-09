// SAMGestor.Domain/Entities/Report.cs

using SAMGestor.Domain.Commom;


namespace SAMGestor.Domain.Entities;

public class Report : Entity<Guid>
{
    // Título que o gestor vê (“Relatório de Camisetas”)
    public string Title { get; private set; } = default!;
    // Chave técnica que aponta para o template/engine (ex.: "shirts.by-size")
    public string TemplateKey { get; private set; } = default!;
    public Guid? RetreatId { get; private set; }
    public DateTime DateCreation { get; private set; } = DateTime.UtcNow;
    public DateTime? LastUpdate  { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    //parâmetros default serializados (ex.: filtros). 
    public string? DefaultParamsJson { get; private set; }

    private Report() { }

    public Report(string title, string templateKey, Guid? retreatId = null, Guid? createdByUserId = null, string? defaultParamsJson = null)
    {
        Id             = Guid.NewGuid();
        Title          = title.Trim();
        TemplateKey    = templateKey.Trim();
        RetreatId      = retreatId;
        CreatedByUserId = createdByUserId;
        DefaultParamsJson = string.IsNullOrWhiteSpace(defaultParamsJson) ? null : defaultParamsJson;
        DateCreation   = DateTime.UtcNow;
    }

    public void Rename(string title)
    {
        Title = title.Trim();
        LastUpdate = DateTime.UtcNow;
    }

    public void ChangeTemplate(string templateKey)
    {
        TemplateKey = templateKey.Trim();
        LastUpdate = DateTime.UtcNow;
    }

    public void SetDefaultParams(string? json)
    {
        DefaultParamsJson = string.IsNullOrWhiteSpace(json) ? null : json;
        LastUpdate = DateTime.UtcNow;
    }
}