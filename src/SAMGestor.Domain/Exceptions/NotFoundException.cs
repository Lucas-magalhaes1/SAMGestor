namespace SAMGestor.Domain.Exceptions;

public class NotFoundException(string entity, object key) : Exception($"{entity} with key '{key}' was not found.");