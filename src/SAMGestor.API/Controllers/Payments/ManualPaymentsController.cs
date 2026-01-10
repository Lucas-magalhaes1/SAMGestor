using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Payments;
using SAMGestor.Domain.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Payments;

[ApiController]
[Route("admin")]
[SwaggerTag("Operações de pagamento manual (Admin, Gestor)")]
[Authorize(Policy = Policies.ManagerOrAbove)]
public class ManualPaymentsController(
    IMediator mediator,
    IManualPaymentProofRepository proofRepo,
    IStorageService storage
) : ControllerBase
{
    /// <summary>
    /// Registra um pagamento manual com upload de comprovante.
    /// </summary>
    
    [HttpPost("registrations/{registrationId:guid}/manual-payment")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [SwaggerOperation(
        Summary = "Registra pagamento manual",
        Description = "Anexa comprovante de pagamento manual e confirma a inscrição."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Pagamento registrado com sucesso.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou arquivo não permitido.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Inscrição não encontrada.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Inscrição já possui pagamento manual registrado.")]
    public async Task<IActionResult> RegisterManualPayment(
        [FromRoute] Guid registrationId,
        [FromForm] ManualPaymentRequest request,
        CancellationToken ct)
    {
        if (request.ProofFile is null || request.ProofFile.Length == 0)
            return BadRequest(new { error = "Comprovante é obrigatório" });

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(request.ProofFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest(new { error = $"Formato não permitido. Use: {string.Join(", ", allowedExtensions)}" });
        }

        const long maxSize = 5 * 1024 * 1024; // 5MB
        if (request.ProofFile.Length > maxSize)
            return BadRequest(new { error = "Arquivo muito grande. Tamanho máximo: 5MB" });

        await using var stream = request.ProofFile.OpenReadStream();
        
        var command = new RegisterManualPaymentCommand(
            RegistrationId: registrationId,
            PaymentMethod: request.PaymentMethod,
            PaymentDate: request.PaymentDate,
            Amount: request.Amount,
            Currency: request.Currency,
            FileStream: stream,
            FileName: request.ProofFile.FileName,
            ContentType: request.ProofFile.ContentType,
            FileSizeBytes: request.ProofFile.Length,
            Notes: request.Notes
        );

        var result = await mediator.Send(command, ct);

        return CreatedAtRoute(
            routeName: nameof(GetPaymentProof),
            routeValues: new { registrationId },
            value: result
        );
    }

    /// <summary>
    /// Retorna os dados do comprovante de pagamento manual de uma inscrição.
    /// </summary>
    /// <remarks>
    /// Use este endpoint para verificar se existe comprovante e exibir os metadados no perfil do participante.
    /// </remarks>
    
    [HttpGet("registrations/{registrationId:guid}/manual-payment", Name = nameof(GetPaymentProof))]
    [SwaggerOperation(
        Summary = "Consulta comprovante de uma inscrição",
        Description = "Retorna metadados do comprovante ou 404 se não existir."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Comprovante encontrado.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Comprovante não encontrado.")]
    public async Task<IActionResult> GetPaymentProof(
        [FromRoute] Guid registrationId,
        CancellationToken ct)
    {
        var proof = await proofRepo.GetByRegistrationIdAsync(registrationId, ct);
        if (proof is null)
            return NotFound(new { error = "Comprovante não encontrado" });

        var downloadUrl = Url.Action(
            nameof(DownloadProof), 
            "ManualPayments", 
            new { registrationId }, 
            Request.Scheme);

        return Ok(new
        {
            proofId = proof.Id,
            registrationId = proof.RegistrationId,
            amount = proof.Amount.Amount,
            currency = proof.Amount.Currency,
            method = proof.Method.ToString(),
            paymentDate = proof.PaymentDate,
            contentType = proof.ProofContentType,
            sizeBytes = proof.ProofSizeBytes,
            uploadedAt = proof.ProofUploadedAt,
            notes = proof.Notes,
            registeredBy = proof.RegisteredByUserId,
            registeredAt = proof.RegisteredAt,
            downloadUrl // ← Front pode usar direto
        });
    }

    /// <summary>
    /// Faz download do arquivo de comprovante.
    /// </summary>
    /// <remarks>
    /// Use para exibir o PDF/imagem no front ou fazer download.
    /// </remarks>
    
    [HttpGet("registrations/{registrationId:guid}/manual-payment/download", Name = nameof(DownloadProof))]
    [SwaggerOperation(
        Summary = "Download do comprovante",
        Description = "Baixa o arquivo do comprovante (PDF/imagem)."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Arquivo retornado.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Comprovante não encontrado.")]
    public async Task<IActionResult> DownloadProof(
        [FromRoute] Guid registrationId,
        CancellationToken ct)
    {
        var proof = await proofRepo.GetByRegistrationIdAsync(registrationId, ct);
        if (proof is null)
            return NotFound(new { error = "Comprovante não encontrado" });

        var filePath = Path.Combine("wwwroot/uploads", proof.ProofStorageKey.Replace('/', Path.DirectorySeparatorChar));
        
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Arquivo não encontrado no storage" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        var fileName = $"comprovante-{registrationId}{Path.GetExtension(proof.ProofStorageKey)}";

        return File(fileBytes, proof.ProofContentType ?? "application/octet-stream", fileName);
    }

    /// <summary>
    /// Lista todos os comprovantes manuais de um retiro.
    /// </summary>
    /// <remarks>
    /// Útil para exibir uma tabela de todos os pagamentos manuais confirmados no retiro.
    /// </remarks>
    
    [HttpGet("retreats/{retreatId:guid}/manual-payments")]
    [SwaggerOperation(
        Summary = "Lista comprovantes de um retiro",
        Description = "Retorna todos os pagamentos manuais de um retiro específico."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Lista de comprovantes.")]
    public async Task<IActionResult> ListByRetreat(
        [FromRoute] Guid retreatId,
        [FromServices] IRegistrationRepository regRepo,
        CancellationToken ct)
    {
        // Buscar todas as inscrições confirmadas do retiro
        var registrations = await regRepo.ListPaidByRetreatAsync(retreatId, ct);
        var registrationIds = registrations.Select(r => r.Id).ToList();

        // Buscar comprovantes (você precisa adicionar esse método no repo)
        var allProofs = new List<object>();
        
        foreach (var regId in registrationIds)
        {
            var proof = await proofRepo.GetByRegistrationIdAsync(regId, ct);
            if (proof is null) continue;

            var reg = registrations.First(r => r.Id == regId);
            
            allProofs.Add(new
            {
                proofId = proof.Id,
                registrationId = proof.RegistrationId,
                participantName = reg.Name.Value,
                participantEmail = reg.Email.Value,
                amount = proof.Amount.Amount,
                currency = proof.Amount.Currency,
                method = proof.Method.ToString(),
                paymentDate = proof.PaymentDate,
                uploadedAt = proof.ProofUploadedAt,
                registeredBy = proof.RegisteredByUserId,
                downloadUrl = Url.Action(
                    action: nameof(DownloadProof), 
                    controller: "ManualPayments",  
                    values: new { registrationId = regId }, 
                    protocol: Request.Scheme)
            });
        }

        return Ok(new
        {
            retreatId,
            totalManualPayments = allProofs.Count,
            payments = allProofs
        });
    }
}

public class ManualPaymentRequest
{
    [SwaggerSchema("Método: Cash, BankTransfer, Check, Other")]
    public string PaymentMethod { get; set; } = null!;

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public IFormFile ProofFile { get; set; } = null!;

    [SwaggerSchema("Observações adicionais")]
    public string? Notes { get; set; }
}
