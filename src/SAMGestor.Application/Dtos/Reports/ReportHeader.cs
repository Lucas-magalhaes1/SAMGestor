namespace SAMGestor.Application.Dtos.Reports;

public sealed record ReportHeader(
    string Id, 
    string Title, 
    DateTime DateCreation,
    Guid? RetreatId,       
    string? RetreatName        
);