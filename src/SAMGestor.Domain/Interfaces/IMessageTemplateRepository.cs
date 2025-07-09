using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IMessageTemplateRepository
{
    MessageTemplate? GetById(Guid id);
    IEnumerable<MessageTemplate> GetAll();
    void Add(MessageTemplate template);
    void Update(MessageTemplate template);
    void Delete(Guid id);
}