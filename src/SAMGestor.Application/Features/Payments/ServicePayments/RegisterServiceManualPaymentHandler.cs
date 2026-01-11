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

namespace SAMGestor.Application.Features.Payments.ServicePayments;

public sealed class RegisterServiceManualPaymentHandler 
    : IRequestHandler<RegisterServiceManualPaymentCommand, RegisterServiceManualPaymentResult>
{
    private readonly IServiceRegistrationRepository _serviceRegistrations;
    private readonly IManualPaymentProofRepository _proofs;
    private readonly IStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _bus;
    private readonly ILogger<RegisterServiceManualPaymentHandler> _logger;

    public RegisterServiceManualPaymentHandler(
        IServiceRegistrationRepository serviceRegistrations,
        IManualPaymentProofRepository proofs,
        IStorageService storage,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IEventBus bus,
        ILogger<RegisterServiceManualPaymentHandler> logger)
    {
        _serviceRegistrations = serviceRegistrations;
        _proofs = proofs;
        _storage = storage;
        _uow = uow;
        _currentUser = currentUser;
        _bus = bus;
        _logger = logger;
    }

    public async Task<RegisterServiceManualPaymentResult> Handle(
        RegisterServiceManualPaymentCommand cmd, 
        CancellationToken ct)
    {

        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var currentUserId = _currentUser.UserId.Value;
        
        var userRole = _currentUser.Role?.ToLowerInvariant();
        if (userRole is not ("manager" or "administrator" or "admin"))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem permissão tentou registrar pagamento manual de serviço", 
                currentUserId);
            throw new ForbiddenException("Apenas gestores podem registrar pagamentos manuais");
        }

     
        var serviceReg = await _serviceRegistrations.GetByIdForUpdateAsync(cmd.ServiceRegistrationId, ct);
        if (serviceReg is null)
            throw new NotFoundException("ServiceRegistration", cmd.ServiceRegistrationId);

        if (serviceReg.Status == ServiceRegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Esta inscrição já está confirmada");
        }

        if (serviceReg.Status == ServiceRegistrationStatus.Cancelled)
        {
            throw new InvalidOperationException("Não é possível confirmar pagamento de inscrição cancelada");
        }

        if (serviceReg.Status == ServiceRegistrationStatus.Declined)
        {
            throw new InvalidOperationException("Não é possível confirmar pagamento de inscrição recusada");
        }
        
        var existingProof = await _proofs.ExistsByServiceRegistrationIdAsync(cmd.ServiceRegistrationId, ct);
        if (existingProof)
        {
            throw new InvalidOperationException("Esta inscrição já possui um comprovante de pagamento manual");
        }

      
        if (cmd.FileStream is null || cmd.FileSizeBytes == 0)
            throw new ArgumentException("Comprovante é obrigatório");

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException(
                $"Formato não permitido. Use: {string.Join(", ", allowedExtensions)}");
        }

        const long maxSizeBytes = 5 * 1024 * 1024; 
        if (cmd.FileSizeBytes > maxSizeBytes)
            throw new ArgumentException("Arquivo muito grande. Tamanho máximo: 5MB");

        var allowedContentTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowedContentTypes.Contains(cmd.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException("Tipo de arquivo não permitido");
        }
        
        if (!Enum.TryParse<PaymentMethod>(cmd.PaymentMethod, true, out var paymentMethod))
        {
            throw new ArgumentException($"Método de pagamento inválido: {cmd.PaymentMethod}");
        }

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
        
        var storageKey = $"retreats/{serviceReg.RetreatId}/service-regs/{serviceReg.Id}/payment-proof{fileExtension}";
        
        var (savedKey, sizeBytes) = await _storage.SaveAsync(
            cmd.FileStream, 
            storageKey, 
            cmd.ContentType, 
            ct);

        var amount = new Money(cmd.Amount, cmd.Currency ?? "BRL");
        var proof = ManualPaymentProof.CreateForService(
            serviceRegistrationId: cmd.ServiceRegistrationId,
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
        
        serviceReg.ConfirmManualPayment();

        var evt = new ManualPaymentConfirmedV1(
            RegistrationId: cmd.ServiceRegistrationId, 
            ProofId: proof.Id,
            RetreatId: serviceReg.RetreatId,
            Name: serviceReg.Name.Value,
            Email: serviceReg.Email.Value,
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

        // 12. Commit
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Pagamento manual de serviço registrado: ServiceRegistration={ServiceRegId}, Comprovante={ProofId}, Gestor={UserId}",
            serviceReg.Id, proof.Id, currentUserId);

        return new RegisterServiceManualPaymentResult(
            ProofId: proof.Id,
            ServiceRegistrationId: serviceReg.Id,
            StorageKey: savedKey,
            Status: "Confirmed"
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
