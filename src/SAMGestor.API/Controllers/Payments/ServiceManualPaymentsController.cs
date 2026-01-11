using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Features.Payments.ServicePayments;
using SAMGestor.Domain.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.ServicePayments;

[ApiController]
[Route("admin/service-registrations")]
[SwaggerTag("Operações de pagamento manual - Servir (Admin, Gestor)")]
[Authorize(Policy = Policies.ManagerOrAbove)]
public class ServiceManualPaymentsController(
    IMediator mediator,
    IManualPaymentProofRepository proofRepo,
    IStorageService storage
) : ControllerBase
{
    /// <summary>
    /// Registra um pagamento manual com upload de comprovante (Servir).
    /// </summary>
    
    [HttpPost("{serviceRegistrationId:guid}/manual-payment")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [SwaggerOperation(
        Summary = "Registra pagamento manual (Servir)",
        Description = "Anexa comprovante de pagamento manual e confirma a inscrição de serviço."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Pagamento registrado com sucesso.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou arquivo não permitido.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Usuário sem permissão.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Inscrição não encontrada.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Inscrição já possui pagamento manual registrado.")]
    public async Task<IActionResult> RegisterManualPayment(
        [FromRoute] Guid serviceRegistrationId,
        [FromForm] ServiceManualPaymentRequest request,
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
        
        var command = new RegisterServiceManualPaymentCommand(
            ServiceRegistrationId: serviceRegistrationId,
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
            routeName: nameof(GetServicePaymentProof), // ← RENOMEADO
            routeValues: new { serviceRegistrationId },
            value: result
        );
    }

    /// <summary>
    /// Retorna os dados do comprovante de pagamento manual de uma inscrição de serviço.
    /// </summary>
    
    [HttpGet("{serviceRegistrationId:guid}/manual-payment", Name = nameof(GetServicePaymentProof))] // ← RENOMEADO
    [SwaggerOperation(
        Summary = "Consulta comprovante (Servir)",
        Description = "Retorna metadados do comprovante ou 404 se não existir."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Comprovante encontrado.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Comprovante não encontrado.")]
    public async Task<IActionResult> GetServicePaymentProof( // ← RENOMEADO
        [FromRoute] Guid serviceRegistrationId,
        CancellationToken ct)
    {
        var proof = await proofRepo.GetByServiceRegistrationIdAsync(serviceRegistrationId, ct);
        if (proof is null)
            return NotFound(new { error = "Comprovante não encontrado" });

        var downloadUrl = Url.Action(
            action: nameof(DownloadServiceProof), // ← RENOMEADO
            controller: "ServiceManualPayments",
            values: new { serviceRegistrationId },
            protocol: Request.Scheme);

        return Ok(new
        {
            proofId = proof.Id,
            serviceRegistrationId = proof.ServiceRegistrationId,
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
            downloadUrl
        });
    }

    /// <summary>
    /// Faz download do arquivo de comprovante (Servir).
    /// </summary>
    
    [HttpGet("{serviceRegistrationId:guid}/manual-payment/download", Name = nameof(DownloadServiceProof))] // ← RENOMEADO
    [SwaggerOperation(
        Summary = "Download do comprovante (Servir)",
        Description = "Baixa o arquivo do comprovante (PDF/imagem)."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Arquivo retornado.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Comprovante não encontrado.")]
    public async Task<IActionResult> DownloadServiceProof( // ← RENOMEADO
        [FromRoute] Guid serviceRegistrationId,
        CancellationToken ct)
    {
        var proof = await proofRepo.GetByServiceRegistrationIdAsync(serviceRegistrationId, ct);
        if (proof is null)
            return NotFound(new { error = "Comprovante não encontrado" });

        var filePath = Path.Combine("wwwroot/uploads", proof.ProofStorageKey.Replace('/', Path.DirectorySeparatorChar));
        
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Arquivo não encontrado no storage" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        var fileName = $"comprovante-servir-{serviceRegistrationId}{Path.GetExtension(proof.ProofStorageKey)}";

        return File(fileBytes, proof.ProofContentType ?? "application/octet-stream", fileName);
    }
}

/// <summary>
/// Request para upload de comprovante de pagamento manual (Servir)
/// </summary>
public class ServiceManualPaymentRequest
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
