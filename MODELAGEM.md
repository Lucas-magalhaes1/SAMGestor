# Modelagem do Sistema SAMGestor

## 5.1 Atores do Sistema

O sistema SAMGestor foi desenvolvido para atender diferentes perfis de usuários, cada um com permissões e responsabilidades específicas:

| Ator | Descrição | Permissões |
|------|-----------|------------|
| **Administrador** | Usuário com acesso total ao sistema, responsável pela gestão completa de retiros, usuários e configurações | Criar/editar/excluir retiros, gerenciar usuários, configurar sistema, executar sorteios, gerenciar famílias, equipes e barracas, acessar relatórios completos, gerenciar notificações e pagamentos |
| **Coordenador (Manager)** | Usuário responsável pela gestão operacional de retiros e participantes | Visualizar e editar inscrições, gerenciar famílias e equipes, executar sorteios, gerenciar barracas, acessar relatórios, enviar notificações (sem permissão para excluir usuários) |
| **Consultor (Consultant)** | Usuário com acesso somente leitura para consulta de informações | Visualizar retiros, inscrições, famílias, equipes, barracas e relatórios (sem permissões de edição ou exclusão) |
| **Participante** | Pessoa que realizou inscrição no sistema através do formulário público | Preencher formulário de inscrição, visualizar status da inscrição, realizar pagamento via link recebido por email |

## 5.2 Requisitos Funcionais

Os requisitos funcionais implementados no sistema SAMGestor foram organizados por serviço responsável:

| ID | Descrição | Serviço | Status |
|----|-----------|---------|--------|
| RF001 | O sistema permitiu o cadastro de retiros com informações completas (nome, edição, tema, datas, vagas por gênero, taxas) | Core | Implementado |
| RF002 | O sistema permitiu a configuração de janela de inscrições por retiro (data início e fim) | Core | Implementado |
| RF003 | O sistema permitiu o cadastro de inscrições de participantes com dados pessoais, médicos e consentimentos | Core | Implementado |
| RF004 | O sistema validou unicidade de CPF por retiro durante o cadastro de inscrições | Core | Implementado |
| RF005 | O sistema bloqueou inscrições de CPFs cadastrados na lista de bloqueio | Core | Implementado |
| RF006 | O sistema permitiu upload de fotos de participantes com armazenamento em serviço externo | Core | Implementado |
| RF007 | O sistema implementou sorteio automático de participantes respeitando vagas por gênero e percentuais regionais | Core | Implementado |
| RF008 | O sistema permitiu seleção manual de participantes por administradores | Core | Implementado |
| RF009 | O sistema gerenciou status de inscrições (NotSelected, Selected, PaymentConfirmed, Confirmed, Cancelled) | Core | Implementado |
| RF010 | O sistema permitiu criação de famílias com composição fixa de 2 homens e 2 mulheres | Core | Implementado |
| RF011 | O sistema validou regras de composição de famílias (sem sobrenomes repetidos, mesma cidade, diferença de idade) | Core | Implementado |
| RF012 | O sistema permitiu criação e gestão de equipes com coordenadores e membros | Core | Implementado |
| RF013 | O sistema permitiu criação e gestão de barracas por gênero com capacidade configurável | Core | Implementado |
| RF014 | O sistema permitiu alocação de participantes em barracas respeitando gênero e capacidade | Core | Implementado |
| RF015 | O sistema permitiu bloqueio de edição de famílias, equipes e barracas por retiro | Core | Implementado |
| RF016 | O sistema implementou versionamento otimista para famílias, equipes e barracas | Core | Implementado |
| RF017 | O sistema permitiu cadastro de inscrições para serviço (Servir) com espaços de atuação | Core | Implementado |
| RF018 | O sistema permitiu gestão de espaços de serviço com capacidade mínima e máxima | Core | Implementado |
| RF019 | O sistema permitiu alocação de participantes em espaços de serviço com papéis (Coordenador, Vice, Membro) | Core | Implementado |
| RF020 | O sistema implementou autenticação JWT com refresh tokens | Core | Implementado |
| RF021 | O sistema implementou confirmação de email com definição de senha inicial | Core | Implementado |
| RF022 | O sistema implementou recuperação de senha via email | Core | Implementado |
| RF023 | O sistema implementou controle de acesso baseado em papéis (Admin, Manager, Consultant) | Core | Implementado |
| RF024 | O sistema permitiu geração de relatórios customizados com filtros e exportação | Core | Implementado |
| RF025 | O sistema registrou logs de alterações (ChangeLog) para auditoria | Core | Implementado |
| RF026 | O sistema publicou eventos de domínio via Outbox Pattern para comunicação entre serviços | Core | Implementado |
| RF027 | O sistema consumiu eventos de confirmação de pagamento para atualizar status de inscrições | Core | Implementado |
| RF028 | O sistema consumiu eventos de criação de grupos de família para registro de links | Core | Implementado |
| RF029 | O serviço de pagamento criou registros de pagamento com idempotência por inscrição | Payment | Implementado |
| RF030 | O serviço de pagamento gerou links de pagamento via provedor externo (MercadoPago) | Payment | Implementado |
| RF031 | O serviço de pagamento publicou eventos de criação de link de pagamento | Payment | Implementado |
| RF032 | O serviço de pagamento publicou eventos de confirmação de pagamento | Payment | Implementado |
| RF033 | O serviço de pagamento processou webhooks de confirmação de pagamento com idempotência | Payment | Implementado |
| RF034 | O serviço de notificação manteve projeção de participantes selecionados | Notification | Implementado |
| RF035 | O serviço de notificação enviou emails de notificação de seleção com link de pagamento | Notification | Implementado |
| RF036 | O serviço de notificação criou grupos de WhatsApp para famílias via API externa | Notification | Implementado |
| RF037 | O serviço de notificação enviou convites de grupo de WhatsApp para membros de famílias | Notification | Implementado |
| RF038 | O serviço de notificação registrou logs de envio de notificações para auditoria | Notification | Implementado |
| RF039 | Todos os serviços processaram eventos via Outbox Pattern com garantia de entrega | Todos | Implementado |
| RF040 | Todos os serviços implementaram consumidores de eventos com idempotência | Todos | Implementado |

## 5.3 Requisitos Não Funcionais

Os requisitos não funcionais implementados garantiram qualidade, escalabilidade e manutenibilidade do sistema:

| ID | Descrição | Implementação |
|----|-----------|---------------|
| RNF001 | O sistema foi desenvolvido em .NET 8 com arquitetura limpa | Implementado com separação em camadas Domain, Application, Infrastructure e API |
| RNF002 | O sistema utilizou PostgreSQL como banco de dados relacional | Implementado com Entity Framework Core e migrations versionadas |
| RNF003 | O sistema implementou validações de entrada com FluentValidation | Implementado em todos os comandos com pipeline behavior do MediatR |
| RNF004 | O sistema utilizou MediatR para implementação de CQRS | Implementado com separação de comandos e queries |
| RNF005 | O sistema implementou tratamento centralizado de exceções | Implementado com middleware de exception handling e respostas padronizadas |
| RNF006 | O sistema documentou APIs com Swagger/OpenAPI | Implementado com Swashbuckle e documentação de endpoints |
| RNF007 | O sistema implementou logging estruturado | Implementado com Serilog e logs em JSON |
| RNF008 | O sistema utilizou injeção de dependências nativa do .NET | Implementado em todos os serviços e repositórios |
| RNF009 | O sistema implementou testes unitários com cobertura mínima | Implementado com xUnit e Moq para componentes críticos |
| RNF010 | O sistema utilizou Value Objects para validação de domínio | Implementado para CPF, Email, FullName, Money, etc. |
| RNF011 | O sistema implementou Repository Pattern para acesso a dados | Implementado com interfaces e implementações no Infrastructure |
| RNF012 | O sistema implementou Unit of Work para transações | Implementado com controle transacional em operações críticas |
| RNF013 | O sistema utilizou migrations para versionamento de schema | Implementado com EF Core Migrations em todos os bancos |
| RNF014 | O sistema implementou paginação em listagens | Implementado com skip/take em queries de listagem |
| RNF015 | O sistema implementou filtros avançados em consultas | Implementado com expressões LINQ e queries dinâmicas |
| RNF016 | O sistema utilizou DTOs para transferência de dados | Implementado com records imutáveis para requests e responses |
| RNF017 | O sistema implementou upload de arquivos com validação | Implementado com integração a serviço de storage externo |
| RNF018 | O sistema implementou rate limiting para APIs públicas | Implementado com middleware de throttling |
| RNF019 | O sistema utilizou HTTPS para comunicação segura | Implementado com certificados SSL/TLS |
| RNF020 | O sistema implementou CORS configurável | Implementado com políticas de origem permitidas |
| RNF021 | O sistema implementou isolamento de bancos de dados por serviço (Database per Service) | Cada microserviço possui seu próprio schema PostgreSQL: Core (schema: core), Payment (schema: payment), Notification (schema: notification) |
| RNF022 | O sistema implementou comunicação assíncrona via mensageria (RabbitMQ) | Implementado com exchanges topic, filas dedicadas por serviço e routing keys versionadas para eventos de domínio |
| RNF023 | O sistema permitiu deploy independente via Docker | Cada serviço possui Dockerfile próprio e pode ser implantado separadamente via Docker Compose |
| RNF024 | O sistema implementou tolerância a falhas isoladas por serviço | Falhas em Payment ou Notification não afetam o Core; cada serviço possui health checks independentes |
| RNF025 | O sistema garantiu entrega de eventos via Outbox Pattern | Eventos são persistidos atomicamente com a transação de negócio e processados por dispatcher em background com retry automático |
| RNF026 | O sistema implementou idempotência nos consumidores de eventos | Consumidores verificam duplicatas por RegistrationId/PaymentId antes de processar; operações são seguras para reprocessamento |
| RNF027 | O sistema implementou políticas de retry configuráveis | Outbox dispatcher tenta reenviar mensagens falhadas com incremento de contador de tentativas e registro de último erro |

## 5.4 Regras de Negócio

As regras de negócio implementadas no sistema foram distribuídas entre os serviços conforme suas responsabilidades:

| ID | Descrição | Serviço Responsável | Status |
|----|-----------|---------------------|--------|
| RN001 | Um CPF pôde ser cadastrado apenas uma vez por retiro | Core | Implementado |
| RN002 | CPFs bloqueados não puderam realizar inscrições | Core | Implementado |
| RN003 | Inscrições só foram aceitas dentro da janela de inscrições do retiro | Core | Implementado |
| RN004 | Participantes precisaram ter no mínimo 2 nomes (nome e sobrenome) | Core | Implementado |
| RN005 | Participantes precisaram aceitar os termos de uso para completar inscrição | Core | Implementado |
| RN006 | O sorteio respeitou as vagas disponíveis por gênero (masculino e feminino) | Core | Implementado |
| RN007 | O sorteio aplicou percentuais regionais configurados (Oeste vs Outras Regiões) | Core | Implementado |
| RN008 | Famílias foram compostas obrigatoriamente por 2 homens e 2 mulheres | Core | Implementado |
| RN009 | Famílias não puderam ter membros com sobrenomes repetidos | Core | Implementado |
| RN010 | Famílias não puderam ter membros da mesma cidade (warning, não bloqueante) | Core | Implementado |
| RN011 | Famílias não puderam ter membros com diferença de idade superior a 10 anos (warning) | Core | Implementado |
| RN012 | Um participante pôde pertencer a apenas uma família por retiro | Core | Implementado |
| RN013 | Apenas participantes com status Confirmed ou PaymentConfirmed puderam ser alocados em famílias | Core | Implementado |
| RN014 | Famílias bloqueadas não puderam ser editadas ou excluídas | Core | Implementado |
| RN015 | Barracas respeitaram segregação por gênero (masculino ou feminino) | Core | Implementado |
| RN016 | Barracas não puderam exceder sua capacidade máxima de ocupantes | Core | Implementado |
| RN017 | Barracas bloqueadas não puderam receber novas alocações | Core | Implementado |
| RN018 | Equipes respeitaram limite mínimo e máximo de membros configurado | Core | Implementado |
| RN019 | Cada equipe teve no máximo um coordenador e um vice-coordenador | Core | Implementado |
| RN020 | Espaços de serviço respeitaram capacidade mínima e máxima configurada | Core | Implementado |
| RN021 | Inscrições para serviço exigiram seleção de espaço preferido quando espaços estavam ativos | Core | Implementado |
| RN022 | Usuários precisaram confirmar email antes de acessar funcionalidades protegidas | Core | Implementado |
| RN023 | Senhas temporárias foram geradas automaticamente no cadastro de usuários | Core | Implementado |
| RN024 | Tokens de confirmação de email e recuperação de senha expiraram após período configurado | Core | Implementado |
| RN025 | Apenas administradores puderam excluir usuários do sistema | Core | Implementado |
| RN026 | Um pagamento foi criado por inscrição (idempotência por RegistrationId) | Payment | Implementado |
| RN027 | Links de pagamento tiveram data de expiração configurável | Payment | Implementado |
| RN028 | Pagamentos confirmados não puderam ser revertidos para status anterior | Payment | Implementado |
| RN029 | Webhooks de pagamento foram processados com idempotência por ProviderPaymentId | Payment | Implementado |
| RN030 | Notificações de seleção foram enviadas apenas para participantes com status Selected | Notification | Implementado |
| RN031 | Emails de notificação incluíram link de pagamento gerado pelo serviço Payment | Notification | Implementado |
| RN032 | Grupos de WhatsApp foram criados apenas para famílias completas (4 membros) | Notification | Implementado |
| RN033 | Convites de grupo foram enviados apenas para membros com telefone válido | Notification | Implementado |
| RN034 | Eventos de domínio foram processados exatamente uma vez (idempotência) | Todos | Implementado |

## 5.5 Modelagem de Domínio

### 5.5.1 Modelo Entidade-Relacionamento Geral

O sistema SAMGestor utilizou um modelo de dados relacional implementado com Entity Framework Core. O schema principal (`core`) contém as entidades de domínio do serviço Core, que gerencia retiros, inscrições, famílias, equipes e barracas.

O diagrama ER completo foi gerado automaticamente pelo Entity Framework Core através das migrations e reflete a estrutura atual do banco de dados com todas as relações, chaves estrangeiras e índices implementados.

### 5.5.2 Segregação por Serviço

O sistema implementou o padrão **Database per Service**, onde cada microserviço possui seu próprio schema isolado no PostgreSQL. Esta abordagem garantiu autonomia dos serviços e permitiu evolução independente dos schemas.

#### Core Database (schema: core)

O banco de dados Core contém as entidades principais do sistema:

| Tabela | Descrição |
|--------|-----------|
| `retreats` | Retiros espirituais com configurações de vagas, datas e taxas |
| `registrations` | Inscrições de participantes (Fazer) com dados pessoais e médicos |
| `service_registrations` | Inscrições de participantes para serviço (Servir) |
| `families` | Famílias compostas por 4 participantes (2M + 2F) |
| `family_members` | Relacionamento entre famílias e participantes |
| `teams` | Equipes de trabalho do retiro |
| `team_members` | Relacionamento entre equipes e participantes |
| `tents` | Barracas para acomodação de participantes |
| `tent_assignments` | Alocação de participantes em barracas |
| `service_spaces` | Espaços de atuação para participantes de serviço |
| `service_assignments` | Alocação de participantes em espaços de serviço |
| `service_registration_payments` | Pagamentos de inscrições de serviço |
| `users` | Usuários do sistema (Admin, Manager, Consultant) |
| `refresh_tokens` | Tokens de refresh para autenticação JWT |
| `email_confirmation_tokens` | Tokens para confirmação de email |
| `password_reset_tokens` | Tokens para recuperação de senha |
| `blocked_cpfs` | Lista de CPFs bloqueados |
| `region_configs` | Configurações regionais para sorteio |
| `waiting_list_items` | Lista de espera de participantes |
| `message_templates` | Templates de mensagens do sistema |
| `messages_sent` | Histórico de mensagens enviadas |
| `change_logs` | Logs de auditoria de alterações |
| `reports` | Definições de relatórios customizados |
| `report_instances` | Instâncias de execução de relatórios |
| `payments` | Pagamentos de inscrições (sincronizado do Payment) |
| `outbox_messages` | Eventos de domínio pendentes de publicação |

#### Payment Database (schema: payment)

O banco de dados Payment contém as entidades relacionadas a pagamentos:

| Tabela | Descrição |
|--------|-----------|
| `payments` | Registros de pagamento com status, valores e links |
| `outbox_messages` | Eventos de domínio pendentes de publicação |

#### Notification Database (schema: notification)

O banco de dados Notification contém as entidades relacionadas a notificações:

| Tabela | Descrição |
|--------|-----------|
| `notification_messages` | Mensagens de notificação (email, WhatsApp) |
| `notification_dispatch_logs` | Logs de tentativas de envio de notificações |
| `selected_registrations` | Projeção de participantes selecionados (read model) |

### 5.5.3 Eventos de Domínio e Outbox

#### Estrutura da Tabela OutboxMessage

A tabela `outbox_messages` foi implementada de forma idêntica em todos os serviços para garantir entrega confiável de eventos:

| Nome da Coluna | Tipo de Dados | Descrição | Nullable |
|----------------|---------------|-----------|----------|
| `Id` | UUID | Identificador único da mensagem | Não |
| `Type` | VARCHAR(200) | Tipo do evento (routing key) | Não |
| `Source` | VARCHAR(100) | Serviço de origem (sam.core, sam.payment, sam.notification) | Não |
| `TraceId` | VARCHAR(100) | Identificador de rastreamento para correlação | Não |
| `Data` | TEXT/JSONB | Payload completo do evento serializado em JSON | Não |
| `CreatedAt` | TIMESTAMP WITH TIME ZONE | Data/hora de criação da mensagem | Não |
| `ProcessedAt` | TIMESTAMP WITH TIME ZONE | Data/hora de processamento (null = pendente) | Sim |
| `Attempts` | INTEGER | Contador de tentativas de envio (default: 0) | Não |
| `LastError` | TEXT | Mensagem do último erro ocorrido | Sim |

**Índices:**
- `ix_outbox_processed`: Índice em `ProcessedAt` para consultas de mensagens pendentes
- `ix_outbox_type_created`: Índice composto em `Type` e `CreatedAt` para consultas por tipo de evento


