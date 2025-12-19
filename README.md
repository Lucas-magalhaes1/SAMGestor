# SAMGestor - Sistema de Gestão de Retiros

## 🎯 Visão Geral do Sistema

O **SAMGestor** é um sistema completo de gestão de retiros espirituais que gerencia todo o ciclo de vida de um retiro, desde a inscrição dos participantes até a alocação em barracas e serviços. O sistema é construído com arquitetura de microserviços orientada a eventos, utilizando .NET 8, PostgreSQL e RabbitMQ.

### Principais Funcionalidades

- **Gestão de Inscrições**: Registro completo de participantes com validações de negócio
- **Sistema de Contemplação**: Sorteio aleatório com quotas regionais
- **Processamento de Pagamentos**: Integração com gateway de pagamento (fake/MercadoPago)
- **Geração de Famílias**: Criação automática de grupos 
- **Gestão de Grupos**: Criação e notificação de grupos de WhatsApp/Email
- **Alocação em Barracas**: Distribuição automática por gênero e capacidade
- **Gestão de Serviços**: Alocação de equipe de serviço em espaços específicos

### Padrões Arquiteturais

- **Clean Architecture**: Separação clara entre domínio, aplicação e infraestrutura
- **CQRS**: Separação de comandos e consultas usando MediatR
- **Event-Driven Architecture**: Comunicação assíncrona via RabbitMQ
- **Outbox Pattern**: Garantia de entrega de eventos com transações
- **Repository Pattern**: Abstração de acesso a dados
- **Unit of Work**: Gerenciamento de transações

### Tecnologias Principais

- **.NET 8**: Framework principal
- **PostgreSQL**: Banco de dados relacional
- **RabbitMQ**: Message broker para eventos
- **Entity Framework Core**: ORM
- **FluentValidation**: Validação de comandos
- **MediatR**: Mediador para CQRS






