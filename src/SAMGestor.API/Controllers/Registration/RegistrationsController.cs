using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.API.Extensions;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Registrations.GetAll;
using SAMGestor.Application.Features.Registrations.GetById;
using SAMGestor.Application.Features.Registrations.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Registration;

[ApiController]
[Route("api/[controller]")]
[SwaggerOrder(10)] // ordem deste controller no Swagger
[SwaggerTag("Operações relacionadas às inscrições em retiros.")] // texto que aparece na tag


public class RegistrationsController(
    IMediator mediator,
    IStorageService storage,
    IRegistrationRepository regRepo,
    IUnitOfWork uow
) : ControllerBase
{
    private CancellationToken CT => HttpContext?.RequestAborted ?? CancellationToken.None;
    
    public sealed class UpdateRegistrationRequest
{
    public string Name { get; set; } = default!;
    public string Cpf { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public DateOnly BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string City { get; set; } = default!;
    public MaritalStatus MaritalStatus { get; set; }
    public PregnancyStatus Pregnancy { get; set; }
    public ShirtSize ShirtSize { get; set; }
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }
    public string Profession { get; set; } = default!;
    public string StreetAndNumber { get; set; } = default!;
    public string Neighborhood { get; set; } = default!;
    public UF State { get; set; }
    public string? Whatsapp { get; set; }
    public string? FacebookUsername { get; set; }
    public string? InstagramHandle { get; set; }
    public string NeighborPhone { get; set; } = default!;
    public string RelativePhone { get; set; } = default!;
    public ParentStatus FatherStatus { get; set; }
    public string? FatherName { get; set; }
    public string? FatherPhone { get; set; }
    public ParentStatus MotherStatus { get; set; }
    public string? MotherName { get; set; }
    public string? MotherPhone { get; set; }
    public bool HadFamilyLossLast6Months { get; set; }
    public string? FamilyLossDetails { get; set; }
    public bool HasRelativeOrFriendSubmitted { get; set; }
    public RelationshipDegree SubmitterRelationship { get; set; }
    public string? SubmitterNames { get; set; }
    public string Religion { get; set; } = default!;
    public RahaminAttempt PreviousUncalledApplications { get; set; }
    public RahaminVidaEdition RahaminVidaCompleted { get; set; }
    public AlcoholUsePattern AlcoholUse { get; set; }
    public bool Smoker { get; set; }
    public bool UsesDrugs { get; set; }
    public string? DrugUseFrequency { get; set; }
    public bool HasAllergies { get; set; }
    public string? AllergiesDetails { get; set; }
    public bool HasMedicalRestriction { get; set; }
    public string? MedicalRestrictionDetails { get; set; }
    public bool TakesMedication { get; set; }
    public string? MedicationsDetails { get; set; }
    public string? PhysicalLimitationDetails { get; set; }
    public string? RecentSurgeryOrProcedureDetails { get; set; }
    public IdDocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
}


    /// <summary>
    /// Cria uma nova inscrição para um retiro.
    /// (Público)
    /// </summary>
    
    [HttpPost]
    [AllowAnonymous] 
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        Summary = "Cria uma nova inscrição",
        Description = "Registra um novo participante em um retiro e retorna os dados da inscrição criada."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Inscrição criada com sucesso.", typeof(CreateRegistrationResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Request inválido ou erros de validação.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Já existe inscrição para o CPF/E-mail informado.")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro inesperado ao criar a inscrição.")]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationCommand command)
    {
        if (command is null)
            return BadRequest();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var enriched = command with
        {
            ClientIp = ip,
            UserAgent = userAgent
        };

        var result = await mediator.Send(enriched, CT);

        return CreatedAtRoute(
            routeName: nameof(GetById),
            routeValues: new { id = result.RegistrationId },
            value: result
        );
    }
    
    /// <summary>
    /// Retorna a incrição complea de um incrito pelo id  
    /// (Admin,Gestor,Consultor)
    /// </summary>
    
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Authorize(Policy = Policies.ReadOnly)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await mediator.Send(new GetRegistrationByIdQuery(id), CT);
        return dto is null ? NotFound() : Ok(dto);
    }
    
    /// <summary>
    /// Lista as inscrições de um retiro.
    /// (Admin,Gestor,Consultor)
    /// </summary>

    [HttpGet]
    [Authorize(Policy = Policies.ReadOnly)] 
    public async Task<ActionResult<PagedResult<RegistrationDto>>> List(
        [FromQuery] Guid retreatId,
        [FromQuery] string? status = null,
        [FromQuery] Gender? gender = null,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] string? city = null,
        [FromQuery] UF? state = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? hasPhoto = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        static string? Clean(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var result = await mediator.Send(
            new GetAllRegistrationsQuery(
                retreatId,
                Clean(status),
                gender,
                minAge,
                maxAge,
                Clean(city),
                state,
                Clean(search),
                hasPhoto,
                skip,
                take
            ),
            ct
        );

        return Ok(result);
    }

    /// <summary>
    ///  Faz upload da foto do inscrito.
    ///  (Público)
    /// </summary>
    
    
    // ----------------- Upload de FOTO -----------------
[HttpPost("{id:guid}/photo")]
[AllowAnonymous]
public async Task<IActionResult> UploadPhoto(Guid id, IFormFile? file) 
{
    if (file is null || file.Length == 0)
        return BadRequest("Arquivo de foto é obrigatório.");

    var contentType = file.ContentType?.ToLowerInvariant();
    if (contentType is not ("image/jpeg" or "image/png"))
        return BadRequest("A foto deve ser JPG ou PNG.");

    const int MaxPhotoBytes = 5 * 1024 * 1024; // 5MB
    if (file.Length > MaxPhotoBytes)
        return BadRequest("A foto deve ter no máximo 5MB.");
    
    var reg = await regRepo.GetByIdForUpdateAsync(id, CT);
    if (reg is null) return NotFound();

    var ext = Path.GetExtension(file.FileName);
    if (string.IsNullOrWhiteSpace(ext))
        ext = contentType == "image/png" ? ".png" : ".jpg";

    var key = $"retreats/{reg.RetreatId}/regs/{reg.Id}/photo{ext}";
    using var stream = file.OpenReadStream();
    var (savedKey, size) = await storage.SaveAsync(stream, key, contentType!, CT);

    var publicUrl = new UrlAddress(storage.GetPublicUrl(savedKey));
    reg.SetPhoto(savedKey, contentType, size, DateTime.UtcNow, publicUrl);

    await uow.SaveChangesAsync(CT);

    return Created(publicUrl.Value, new { key = savedKey, url = publicUrl.Value, size });
}
    
        /// <summary>
        ///  Faz upload do documento de identificação do inscrito.
        ///  (Público)
        /// </summary>

// ----------------- Upload de DOCUMENTO -----------------
[HttpPost("{id:guid}/document")]
[AllowAnonymous]
public async Task<IActionResult> UploadDocument(
    Guid id,
    IFormFile? file,                     
    [FromForm] IdDocumentType type,
    [FromForm] string? number)
    {
    if (file is null || file.Length == 0)
        return BadRequest("Arquivo de documento é obrigatório.");

    var contentType = file.ContentType?.ToLowerInvariant();
    if (contentType is not ("image/jpeg" or "image/png" or "application/pdf"))
        return BadRequest("Documento deve ser JPG, PNG ou PDF.");

    const int MaxDocBytes = 10 * 1024 * 1024; // 10MB
    if (file.Length > MaxDocBytes)
        return BadRequest("O documento deve ter no máximo 10MB.");
    
    var reg = await regRepo.GetByIdForUpdateAsync(id, CT);
    if (reg is null) return NotFound();

    var ext = Path.GetExtension(file.FileName);
    if (string.IsNullOrWhiteSpace(ext))
    {
        ext = contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
    }

    var key = $"retreats/{reg.RetreatId}/regs/{reg.Id}/id{ext}";
    using var stream = file.OpenReadStream();
    var (savedKey, size) = await storage.SaveAsync(stream, key, contentType!, CT);

    var publicUrl = new UrlAddress(storage.GetPublicUrl(savedKey));
    reg.SetIdDocument(type, number, savedKey, contentType, size, DateTime.UtcNow, publicUrl);

    await uow.SaveChangesAsync(CT);

    return Created(publicUrl.Value, new { key = savedKey, url = publicUrl.Value, size });
}
        
   /// <summary>
/// Atualiza os dados de uma inscrição existente.
/// Aceita dados via multipart/form-data incluindo opcionalmente photo e document (IFormFile).
/// (Admin, Gestor)
/// </summary>

[HttpPut("{id:guid}")]
[Authorize(Policy = Policies.ManagerOrAbove)]
[ApiExplorerSettings(IgnoreApi = true)]  //  Oculto do Swagger
public async Task<IActionResult> Update(
    Guid id,
    [FromForm] UpdateRegistrationRequest request,
    [FromForm] IFormFile? photo,
    [FromForm] IFormFile? document)
{
    if (request is null)
        return BadRequest("Request body is required.");

    // Validar e processar foto
    string? photoKey = null, photoContentType = null, photoUrl = null;
    long? photoSize = null;
    
    if (photo is not null)
    {
        if (photo.Length == 0)
            return BadRequest("Arquivo de foto está vazio.");

        var photoType = photo.ContentType?.ToLowerInvariant();
        if (photoType is not ("image/jpeg" or "image/png"))
            return BadRequest("A foto deve ser JPG ou PNG.");

        const int MaxPhotoBytes = 5 * 1024 * 1024;
        if (photo.Length > MaxPhotoBytes)
            return BadRequest("A foto deve ter no máximo 5MB.");
        
        var reg = await regRepo.GetByIdAsync(id, CT);
        if (reg is null) return NotFound("Inscrição não encontrada.");

        var ext = Path.GetExtension(photo.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = photoType == "image/png" ? ".png" : ".jpg";

        photoKey = $"retreats/{reg.RetreatId}/regs/{reg.Id}/photo{ext}";
        using var photoStream = photo.OpenReadStream();
        var (savedPhotoKey, savedPhotoSize) = await storage.SaveAsync(photoStream, photoKey, photoType!, CT);

        photoKey = savedPhotoKey;
        photoSize = savedPhotoSize;
        photoContentType = photoType;
        photoUrl = storage.GetPublicUrl(savedPhotoKey);
    }

    // Validar e processar documento
    string? docKey = null, docContentType = null, docUrl = null;
    long? docSize = null;
    
    if (document is not null)
    {
        if (document.Length == 0)
            return BadRequest("Arquivo de documento está vazio.");

        var docType = document.ContentType?.ToLowerInvariant();
        if (docType is not ("image/jpeg" or "image/png" or "application/pdf"))
            return BadRequest("Documento deve ser JPG, PNG ou PDF.");

        const int MaxDocBytes = 10 * 1024 * 1024;
        if (document.Length > MaxDocBytes)
            return BadRequest("O documento deve ter no máximo 10MB.");
        
        if (request.DocumentType is null)
            return BadRequest("DocumentType é obrigatório ao enviar um documento.");

        var reg = await regRepo.GetByIdAsync(id, CT);
        if (reg is null) return NotFound("Inscrição não encontrada.");

        var ext = Path.GetExtension(document.FileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = docType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "application/pdf" => ".pdf",
                _ => ".bin"
            };
        }

        docKey = $"retreats/{reg.RetreatId}/regs/{reg.Id}/id{ext}";
        using var docStream = document.OpenReadStream();
        var (savedDocKey, savedDocSize) = await storage.SaveAsync(docStream, docKey, docType!, CT);

        docKey = savedDocKey;
        docSize = savedDocSize;
        docContentType = docType;
        docUrl = storage.GetPublicUrl(savedDocKey);
    }

    var command = new UpdateRegistrationCommand(
        id,
        new FullName(request.Name),
        new CPF(request.Cpf),
        new EmailAddress(request.Email),
        request.Phone,
        request.BirthDate,
        request.Gender,
        request.City,
        request.MaritalStatus,
        request.Pregnancy,
        request.ShirtSize,
        request.WeightKg,
        request.HeightCm,
        request.Profession,
        request.StreetAndNumber,
        request.Neighborhood,
        request.State,
        request.Whatsapp,
        request.FacebookUsername,
        request.InstagramHandle,
        request.NeighborPhone,
        request.RelativePhone,
        request.FatherStatus,
        request.FatherName,
        request.FatherPhone,
        request.MotherStatus,
        request.MotherName,
        request.MotherPhone,
        request.HadFamilyLossLast6Months,
        request.FamilyLossDetails,
        request.HasRelativeOrFriendSubmitted,
        request.SubmitterRelationship,
        request.SubmitterNames,
        request.Religion,
        request.PreviousUncalledApplications,
        request.RahaminVidaCompleted,
        request.AlcoholUse,
        request.Smoker,
        request.UsesDrugs,
        request.DrugUseFrequency,
        request.HasAllergies,
        request.AllergiesDetails,
        request.HasMedicalRestriction,
        request.MedicalRestrictionDetails,
        request.TakesMedication,
        request.MedicationsDetails,
        request.PhysicalLimitationDetails,
        request.RecentSurgeryOrProcedureDetails,
        photoKey,
        photoContentType,
        photoSize,
        photoUrl,
        request.DocumentType,
        request.DocumentNumber,
        docKey,
        docContentType,
        docSize,
        docUrl
    );

    var result = await mediator.Send(command, CT);
    return Ok(result);
}

        
        
     /// <summary>
    /// Retorna as opções de enums e restrições para inscrições.
    /// (Público)
    /// </summary>
            
    [HttpGet("options")]
    [AllowAnonymous]
    public IActionResult GetOptions()
    {
        return Ok(new
        {
            enums = new
            {
                gender = MapEnum<Gender>(),
                maritalStatus = MapEnum<MaritalStatus>(),
                pregnancy = MapEnum<PregnancyStatus>(),
                shirtSize = MapEnum<ShirtSize>(),
                uf = MapEnum<UF>(),
                parentStatus = MapEnum<ParentStatus>(),
                alcoholUse = MapEnum<AlcoholUsePattern>(),
                relationshipDegree = MapEnum<RelationshipDegree>(flags: true),
                rahaminAttempt = MapEnum<RahaminAttempt>(flags: true),
                rahaminVidaCompleted = MapEnum<RahaminVidaEdition>(flags: true)
            },
            constraints = new
            {
                phoneDigitsMin = 10,
                phoneDigitsMax = 11,
                maxPhotoBytes = 5 * 1024 * 1024,
                acceptedPhotoTypes = new[] { "image/jpeg", "image/png" },
                maxDocBytes = 10 * 1024 * 1024,
                acceptedDocTypes = new[] { "image/jpeg", "image/png", "application/pdf" }
            },
            rules = new
            {
                pregnancyVisibleForGender = "Female"
            }
        });
    }
    
    private static object MapEnum<T>(bool flags = false) where T : Enum
    {
        var type = typeof(T);
        var isFlags = flags || type.GetCustomAttribute<FlagsAttribute>() != null;
        var items = Enum.GetValues(type).Cast<Enum>()
            .Where(v => !isFlags || IsSingleFlag(Convert.ToInt32(v)))
            .Select(v => new EnumOption
            {
                Name = v.ToString(),
                Value = Convert.ToInt32(v),
                Label = ToLabel(v.ToString())
            })
            .ToList();

        return new EnumGroup { IsFlags = isFlags, Items = items };
    }

    private static bool IsSingleFlag(int x) => x == 0 || (x & (x - 1)) == 0;

    private static string ToLabel(string name) => name.Replace('_', ' ');

    private sealed class EnumGroup
    {
        public bool IsFlags { get; set; }
        public List<EnumOption> Items { get; set; } = new();
    }
    private sealed class EnumOption
    {
        public string Name { get; set; } = default!;
        public int Value { get; set; }
        public string Label { get; set; } = default!;
    }
}
