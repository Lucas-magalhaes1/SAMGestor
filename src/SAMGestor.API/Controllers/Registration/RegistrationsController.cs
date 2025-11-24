using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Extensions;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Registrations.GetAll;
using SAMGestor.Application.Features.Registrations.GetById;
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

    /// <summary>
    /// Cria uma nova inscrição para um retiro.
    /// </summary>
    
    [HttpPost]
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
    /// </summary>
    
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await mediator.Send(new GetRegistrationByIdQuery(id), CT);
        return dto is null ? NotFound() : Ok(dto);
    }
    
    /// <summary>
    /// Lista as inscrições de um retiro.
    /// </summary>

    [HttpGet]
    public async Task<IActionResult> List(
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
        [FromQuery] int take = 20)
    {
        // Evita NRE se alguém resolver "normalizar" string no futuro:
        static string? Clean(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var response = await mediator.Send(
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
            CT
        );

        return Ok(response);
    }

    /// <summary>
    ///  Faz upload da foto do inscrito.
    /// </summary>
    
    
    // ----------------- Upload de FOTO -----------------
[HttpPost("{id:guid}/photo")]
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
        /// </summary>

// ----------------- Upload de DOCUMENTO -----------------
[HttpPost("{id:guid}/document")]
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
    /// Retorna as opções de enums e restrições para inscrições.
    /// </summary>
            
    [HttpGet("options")]
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
