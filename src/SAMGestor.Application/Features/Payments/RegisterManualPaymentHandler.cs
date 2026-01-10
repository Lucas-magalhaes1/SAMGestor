using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Payments;

public sealed class RegisterManualPaymentHandler 
    : IRequestHandler<RegisterManualPaymentCommand, RegisterManualPaymentResult>
{
    private readonly IRegistrationRepository _registrations;
    private readonly IManualPaymentProofRepository _proofs;
    private readonly IStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _bus;
    private readonly ILogger<RegisterManualPaymentHandler> _logger;

    public RegisterManualPaymentHandler(
        IRegistrationRepository registrations,
        IManualPaymentProofRepository proofs,
        IStorageService storage,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IEventBus bus,
        ILogger<RegisterManualPaymentHandler> logger)
    {
        _registrations = registrations;
        _proofs = proofs;
        _storage = storage;
        _uow = uow;
        _currentUser = currentUser;
        _bus = bus;
        _logger = logger;
    }

    public async Task<RegisterManualPaymentResult> Handle(
        RegisterManualPaymentCommand cmd, 
        CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");
        

        var currentUserId = _currentUser.UserId.Value;
        // 1. Validar permissões (apenas Manager ou Admin)
        var userRole = _currentUser.Role?.ToLowerInvariant();
        if (userRole is not ("manager" or "administrator" or "admin"))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem permissão tentou registrar pagamento manual", 
                _currentUser.UserId);
            throw new ForbiddenException("Apenas gestores podem registrar pagamentos manuais");
        }

        // 2. Buscar inscrição
        var registration = await _registrations.GetByIdForUpdateAsync(cmd.RegistrationId, ct);
        if (registration is null)
            throw new NotFoundException("Registration", cmd.RegistrationId);

        // 3. Validar se está contemplada (Called = Selected no seu sistema)
        if (registration.Status != RegistrationStatus.Selected)
        {
            throw new InvalidOperationException(
                $"Apenas inscrições contempladas ou não pagas ainda podem ter pagamento manual. Status atual: {registration.Status}");
        }

        // 4. Validar se já não tem comprovante
        var existingProof = await _proofs.ExistsByRegistrationIdAsync(cmd.RegistrationId, ct);
        if (existingProof)
        {
            throw new InvalidOperationException("Esta inscrição já possui um comprovante de pagamento manual");
        }

        // 5. Validar arquivo
        if (cmd.FileStream is null || cmd.FileSizeBytes == 0)
            throw new ArgumentException("Comprovante é obrigatório");

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException(
                $"Formato não permitido. Use: {string.Join(", ", allowedExtensions)}");
        }

        const long maxSizeBytes = 5 * 1024 * 1024; // 5MB
        if (cmd.FileSizeBytes > maxSizeBytes)
            throw new ArgumentException("Arquivo muito grande. Tamanho máximo: 5MB");

        var allowedContentTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowedContentTypes.Contains(cmd.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException("Tipo de arquivo não permitido");
        }

        // 6. Validar método de pagamento
        if (!Enum.TryParse<PaymentMethod>(cmd.PaymentMethod, true, out var paymentMethod))
        {
            throw new ArgumentException($"Método de pagamento inválido: {cmd.PaymentMethod}");
        }

        // Validar que é método manual
        var manualMethods = new[] { 
            PaymentMethod.Cash, 
            PaymentMethod.BankTransfer, 
            PaymentMethod.Check, 
            PaymentMethod.Other 
        };
        if (!manualMethods.Contains(paymentMethod))
        {
            throw new ArgumentException(
                $"Método {paymentMethod} não é válido para pagamento manual");
        }

        // 7. Salvar arquivo no storage (seguindo seu padrão)
        var storageKey = $"retreats/{registration.RetreatId}/regs/{registration.Id}/payment-proof{fileExtension}";
        
        var (savedKey, sizeBytes) = await _storage.SaveAsync(
            cmd.FileStream, 
            storageKey, 
            cmd.ContentType, 
            ct);

        // 8. Criar entidade ManualPaymentProof
        var amount = new Money(cmd.Amount, cmd.Currency ?? "BRL");
        var proof = new ManualPaymentProof(
            registrationId: cmd.RegistrationId,
            amount: amount,
            method: paymentMethod,
            paymentDate: cmd.PaymentDate,
            proofStorageKey: savedKey,
            proofContentType: cmd.ContentType,
            proofSizeBytes: sizeBytes,
            notes: cmd.Notes,
            registeredByUserId: currentUserId
        );

        await _proofs.AddAsync(proof, ct);

        // 9. Confirmar pagamento na inscrição
        registration.ConfirmManualPayment();

        // 10. Publicar evento (seguindo seu padrão Outbox + RabbitMQ)
        var evt = new ManualPaymentConfirmedV1(
            RegistrationId: registration.Id,
            ProofId: proof.Id,
            RetreatId: registration.RetreatId,
            Name: registration.Name.Value,
            Email: registration.Email.Value,
            Amount: amount.Amount,
            Currency: amount.Currency,
            PaymentMethod: TranslatePaymentMethod(paymentMethod),
            PaymentDate: cmd.PaymentDate,
            RegisteredBy: _currentUser.Email ?? "Sistema"
        );

        await _bus.EnqueueAsync(
            type: EventTypes.ManualPaymentConfirmedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        // 11. Commit (salva tudo + Outbox)
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Pagamento manual registrado: Inscrição={RegistrationId}, Comprovante={ProofId}, Gestor={UserId}",
            registration.Id, proof.Id, _currentUser.UserId);

        return new RegisterManualPaymentResult(
            ProofId: proof.Id,
            RegistrationId: registration.Id,
            StorageKey: savedKey,
            Status: "PaymentConfirmed"
        );
    }

    private static string TranslatePaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Dinheiro",
        PaymentMethod.BankTransfer => "Transferência Bancária",
        PaymentMethod.Check => "Cheque",
        PaymentMethod.Pix => "PIX",
        PaymentMethod.BankSlip => "Boleto",
        PaymentMethod.Card => "Cartão",
        PaymentMethod.Other => "Outro",
        _ => method.ToString()
    };
}
