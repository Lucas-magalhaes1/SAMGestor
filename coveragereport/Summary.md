# Summary

|||
|:---|:---|
| Generated on: | 05/11/2025 - 21:10:07 |
| Coverage date: | 05/11/2025 - 20:56:25 - 05/11/2025 - 21:06:18 |
| Parser: | MultiReport (5x Cobertura) |
| Assemblies: | 5 |
| Classes: | 380 |
| Files: | 305 |
| **Line coverage:** | 92.1% (19647 of 21332) |
| Covered lines: | 19647 |
| Uncovered lines: | 1685 |
| Coverable lines: | 21332 |
| Total lines: | 27881 |
| **Branch coverage:** | 67.9% (1332 of 1959) |
| Covered branches: | 1332 |
| Total branches: | 1959 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| SAMGestor.Application | SAMGestor.Application.Features.Tents.TentRoster.Update.UpdateTentRosterHandler | Handle() | 4692 | 68 || SAMGestor.API | SAMGestor.API.Extensions.SwaggerExtensions | SwaggerOrderSelector(...) | 272 | 16 || SAMGestor.API | SAMGestor.API.Controllers.AdminServiceNotificationsController | NotifyOne() | 156 | 12 || SAMGestor.API | SAMGestor.API.Controllers.AdminOutboxController | List() | 110 | 10 || SAMGestor.API | SAMGestor.API.Controllers.AdminServiceNotificationsController | NotifySelectedForRetreat() | 110 | 10 || SAMGestor.Application | SAMGestor.Application.Features.Service.Registrations.Confirmed.GetConfirmedServiceRegistrationsHandler | Handle() | 91 | 18 || SAMGestor.API | SAMGestor.API.Controllers.RegistrationsController | UploadDocument() | 73 | 28 || SAMGestor.Application | SAMGestor.Application.Features.Families.Generate.GenerateFamiliesHandler | Handle() | 70 | 70 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Consumers.PaymentConfirmedConsumer | NormalizeMethod(...) | 70 | 32 || SAMGestor.Application | SAMGestor.Application.Features.Families.Update.UpdateFamiliesHandler | Handle() | 69 | 68 || SAMGestor.Application | SAMGestor.Application.Features.Service.Roster.Update.UpdateServiceRosterHandler | Handle() | 50 | 50 || SAMGestor.Application | SAMGestor.Application.Features.Tents.TentRoster.Assign.UpdateTentRosterHandler | Handle() | 50 | 50 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Consumers.ServicePaymentConfirmedConsumer | NormalizeMethod(...) | 43 | 32 || SAMGestor.API | SAMGestor.API.Controllers.RegistrationsController | MapEnum(...) | 42 | 6 || SAMGestor.Application | SAMGestor.Application.Features.Families.Create.CreateFamilyHandler | Handle() | 42 | 42 || SAMGestor.Domain | SAMGestor.Domain.Entities.Retreat | SetPrivacyPolicy(...) | 42 | 6 || SAMGestor.Domain | SAMGestor.Domain.Entities.ServiceSpace | UpdateBasics(...) | 42 | 6 || SAMGestor.Application | SAMGestor.Application.Features.Service.Alerts.GetAll.GetServiceAlertsHandler | Handle() | 36 | 36 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Outbox.OutboxDispatcher | ExecuteAsync() | 36 | 16 || SAMGestor.Application | SAMGestor.Application.Features.Tents.Update.UpdateTentHandler | Handle() | 34 | 34 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Consumers.FamilyGroupCreateFailedConsumer | ExecuteAsync() | 34 | 14 || SAMGestor.Application | SAMGestor.Application.Features.Registrations.GetById.GetRegistrationByIdHandler | Handle() | 30 | 30 || SAMGestor.Application | SAMGestor.Application.Features.Tents.Locking.SetTentLockHandler | Handle() | 30 | 30 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Consumers.ServicePaymentConfirmedConsumer | TryAutoAssignAsync() | 30 | 30 || SAMGestor.Application | SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentsHandler | Handle() | 28 | 28 || SAMGestor.Application | SAMGestor.Application.Features.Tents.Locking.SetTentsGlobalLockHandler | Handle() | 27 | 26 || SAMGestor.Application | SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityHandler | Handle() | 24 | 24 || SAMGestor.API | SAMGestor.API.Controllers.RegistrationsController | UploadPhoto() | 24 | 22 || SAMGestor.Application | SAMGestor.Application.Features.Families.Groups.CreateFamilyGroupsBulkHandler | Handle() | 22 | 22 || SAMGestor.Application | SAMGestor.Application.Features.Service.Registrations.Create.CreateServiceRegistrationHandler | Handle() | 22 | 22 || SAMGestor.Application | SAMGestor.Application.Features.Tents.TentRoster.AutoAssign.AutoAssignTentsHandler | AssignBatch(...) | 22 | 22 || SAMGestor.Application | SAMGestor.Application.Features.Tents.Create.CreateTentHandler | Handle() | 20 | 20 || SAMGestor.Application | SAMGestor.Application.Common.Retreat.BaseRetreatValidator<T> | .ctor() | 18 | 18 || SAMGestor.Application | SAMGestor.Application.Features.Registrations.GetAll.GetAllRegistrationsHandler | Handle() | 18 | 18 || SAMGestor.Application | SAMGestor.Application.Features.Tents.Delete.DeleteTentHandler | Handle() | 18 | 18 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Consumers.ServicePaymentConfirmedConsumer | HandleAsync() | 18 | 18 || SAMGestor.Application | SAMGestor.Application.Features.Families.Groups.ListByStatus.ListFamiliesByGroupStatusHandler | Handle() | 16 | 16 || SAMGestor.Application | SAMGestor.Application.Features.Families.Groups.Notify.NotifyFamilyGroupHandler | Handle() | 16 | 16 || SAMGestor.Application | SAMGestor.Application.Features.Service.Spaces.Locking.LockServiceSpaceHandler | Handle() | 16 | 16 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Messaging.Consumers.PaymentConfirmedConsumer | HandleAsync() | 18 | 16 || SAMGestor.Infrastructure | SAMGestor.Infrastructure.Services.HeuristicRelationshipService | AreDirectRelativesAsync() | 24 | 16 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **SAMGestor.API** | **444** | **304** | **748** | **1629** | **59.3%** | **68** | **170** | **40%** |
| Program | 39 | 0 | 39 | 60 | 100% | 2 | 2 | 100% |
| SAMGestor.API.Controllers.AdminFamilyGroupsController | 9 | 0 | 9 | 43 | 100% | 0 | 0 |  |
| SAMGestor.API.Controllers.AdminNotificationsController | 72 | 0 | 72 | 102 | 100% | 6 | 6 | 100% |
| SAMGestor.API.Controllers.AdminOutboxController | 0 | 40 | 40 | 84 | 0% | 0 | 14 | 0% |
| SAMGestor.API.Controllers.AdminServiceNotificationsController | 0 | 92 | 92 | 146 | 0% | 0 | 22 | 0% |
| SAMGestor.API.Controllers.RegistrationsController | 73 | 61 | 134 | 233 | 54.4% | 33 | 68 | 48.5% |
| SAMGestor.API.Controllers.RetreatFamiliesController | 61 | 0 | 61 | 178 | 100% | 11 | 14 | 78.5% |
| SAMGestor.API.Controllers.RetreatGroupsController | 14 | 1 | 15 | 58 | 93.3% | 2 | 2 | 100% |
| SAMGestor.API.Controllers.RetreatLotteryController | 13 | 0 | 13 | 45 | 100% | 0 | 0 |  |
| SAMGestor.API.Controllers.RetreatsController | 18 | 0 | 18 | 54 | 100% | 0 | 0 |  |
| SAMGestor.API.Controllers.RetreatTentsController | 0 | 61 | 61 | 170 | 0% | 0 | 8 | 0% |
| SAMGestor.API.Controllers.ServiceAlertsController | 10 | 0 | 10 | 25 | 100% | 5 | 6 | 83.3% |
| SAMGestor.API.Controllers.ServiceRegistrationsController | 16 | 0 | 16 | 55 | 100% | 0 | 0 |  |
| SAMGestor.API.Controllers.ServiceSpacesController | 43 | 2 | 45 | 121 | 95.5% | 2 | 2 | 100% |
| SAMGestor.API.Controllers.TentRosterController | 0 | 27 | 27 | 83 | 0% | 0 | 0 |  |
| SAMGestor.API.Extensions.SwaggerExtensions | 22 | 9 | 31 | 64 | 70.9% | 2 | 20 | 10% |
| SAMGestor.API.Extensions.SwaggerOrderAttribute | 0 | 2 | 2 | 9 | 0% | 0 | 0 |  |
| SAMGestor.API.Middlewares.ExceptionHandlingMiddleware | 54 | 9 | 63 | 99 | 85.7% | 5 | 6 | 83.3% |
| **SAMGestor.Application** | **4047** | **353** | **4400** | **9968** | **91.9%** | **1017** | **1251** | **81.2%** |
| CreateFamilyRequest | 5 | 0 | 5 | 5 | 100% | 0 | 0 |  |
| OutboxMessageDto | 0 | 8 | 8 | 8 | 0% | 0 | 0 |  |
| SAMGestor.Application.Common.Families.FamilyRead | 37 | 0 | 37 | 72 | 100% | 16 | 28 | 57.1% |
| SAMGestor.Application.Common.Retreat.BaseRetreatValidator<T> | 53 | 0 | 53 | 87 | 100% | 15 | 18 | 83.3% |
| SAMGestor.Application.Features.Families.Create.CreateFamilyCommand | 6 | 0 | 6 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Create.CreateFamilyHandler | 101 | 0 | 101 | 173 | 100% | 54 | 56 | 96.4% |
| SAMGestor.Application.Features.Families.Create.CreateFamilyResult | 6 | 0 | 6 | 17 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Create.CreateFamilyWarningDto | 2 | 4 | 6 | 17 | 33.3% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Delete.DeleteFamilyCommand | 1 | 0 | 1 | 5 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Delete.DeleteFamilyHandler | 21 | 0 | 21 | 40 | 100% | 10 | 10 | 100% |
| SAMGestor.Application.Features.Families.Generate.FamilyAlertDto | 6 | 0 | 6 | 36 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Generate.GeneratedFamilyDto | 11 | 0 | 11 | 36 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Generate.GeneratedMemberDto | 7 | 0 | 7 | 36 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Generate.GenerateFamiliesCommand | 6 | 0 | 6 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Generate.GenerateFamiliesHandler | 181 | 0 | 181 | 291 | 100% | 82 | 102 | 80.3% |
| SAMGestor.Application.Features.Families.Generate.GenerateFamiliesResponse | 4 | 0 | 4 | 36 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Generate.GenerateFamiliesValidator | 7 | 0 | 7 | 18 | 100% | 1 | 2 | 50% |
| SAMGestor.Application.Features.Families.GetAll.FamilyAlertDto | 6 | 0 | 6 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetAll.FamilyDto | 11 | 0 | 11 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetAll.GetAllFamiliesHandler | 58 | 0 | 58 | 82 | 100% | 11 | 12 | 91.6% |
| SAMGestor.Application.Features.Families.GetAll.GetAllFamiliesQuery | 1 | 0 | 1 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetAll.GetAllFamiliesResponse | 5 | 0 | 5 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetAll.GetAllFamiliesValidator | 4 | 0 | 4 | 12 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetAll.MemberDto | 7 | 0 | 7 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetById.FamilyAlertDto | 6 | 0 | 6 | 44 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetById.FamilyDto | 21 | 0 | 21 | 44 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetById.GetFamilyByIdHandler | 58 | 0 | 58 | 75 | 100% | 8 | 8 | 100% |
| SAMGestor.Application.Features.Families.GetById.GetFamilyByIdQuery | 1 | 0 | 1 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetById.GetFamilyByIdResponse | 4 | 0 | 4 | 44 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetById.GetFamilyByIdValidator | 4 | 0 | 4 | 12 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.GetById.MemberDto | 7 | 0 | 7 | 44 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Create.CreateFamilyGroupsCommand | 5 | 0 | 5 | 15 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Create.CreateFamilyGroupsResponse | 5 | 0 | 5 | 15 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.CreateFamilyGroupsBulkHandler | 59 | 0 | 59 | 96 | 100% | 22 | 24 | 91.6% |
| SAMGestor.Application.Features.Families.Groups.ListByStatus.FamilyGroupItem | 11 | 0 | 11 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.ListByStatus.ListFamiliesByGroupStatusHandler | 27 | 0 | 27 | 43 | 100% | 18 | 18 | 100% |
| SAMGestor.Application.Features.Families.Groups.ListByStatus.ListFamiliesByGroupStatusQuery | 1 | 0 | 1 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.ListByStatus.ListFamiliesByGroupStatusResponse | 1 | 0 | 1 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Notify.NotifyFamilyGroupCommand | 5 | 0 | 5 | 15 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Notify.NotifyFamilyGroupHandler | 44 | 0 | 44 | 69 | 100% | 17 | 18 | 94.4% |
| SAMGestor.Application.Features.Families.Groups.Notify.NotifyFamilyGroupResponse | 5 | 0 | 5 | 15 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Resend.ResendFamilyGroupCommand | 4 | 0 | 4 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Resend.ResendFamilyGroupHandler | 43 | 0 | 43 | 67 | 100% | 11 | 12 | 91.6% |
| SAMGestor.Application.Features.Families.Groups.Resend.ResendFamilyGroupResponse | 1 | 0 | 1 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.RetryFailed.RetryFailedGroupsCommand | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.RetryFailed.RetryFailedGroupsHandler | 53 | 0 | 53 | 77 | 100% | 13 | 14 | 92.8% |
| SAMGestor.Application.Features.Families.Groups.RetryFailed.RetryFailedGroupsResponse | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Status.GetGroupsStatusSummaryHandler | 19 | 0 | 19 | 37 | 100% | 9 | 9 | 100% |
| SAMGestor.Application.Features.Families.Groups.Status.GetGroupsStatusSummaryQuery | 1 | 0 | 1 | 13 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Groups.Status.GetGroupsStatusSummaryResponse | 7 | 0 | 7 | 13 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Lock.LockFamiliesCommand | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Lock.LockFamiliesHandler | 12 | 0 | 12 | 26 | 100% | 4 | 4 | 100% |
| SAMGestor.Application.Features.Families.Lock.LockFamiliesResponse | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Lock.LockSingleFamilyCommand | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Lock.LockSingleFamilyHandler | 19 | 0 | 19 | 38 | 100% | 8 | 8 | 100% |
| SAMGestor.Application.Features.Families.Lock.LockSingleFamilyResponse | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Reset.ResetFamiliesCommand | 1 | 0 | 1 | 12 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Reset.ResetFamiliesHandler | 43 | 0 | 43 | 77 | 100% | 16 | 16 | 100% |
| SAMGestor.Application.Features.Families.Reset.ResetFamiliesResponse | 5 | 0 | 5 | 12 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Unassigned.GetUnassignedHandler | 31 | 0 | 31 | 55 | 100% | 13 | 14 | 92.8% |
| SAMGestor.Application.Features.Families.Unassigned.GetUnassignedQuery | 6 | 0 | 6 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Unassigned.GetUnassignedResponse | 1 | 0 | 1 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Unassigned.UnassignedMemberDto | 5 | 2 | 7 | 20 | 71.4% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.FamilyAlertDto | 6 | 0 | 6 | 42 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.FamilyDto | 11 | 0 | 11 | 42 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.FamilyErrorDto | 6 | 0 | 6 | 42 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.MemberDto | 7 | 0 | 7 | 42 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.UpdateFamiliesCommand | 6 | 0 | 6 | 24 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.UpdateFamiliesHandler | 186 | 10 | 196 | 300 | 94.8% | 73 | 90 | 81.1% |
| SAMGestor.Application.Features.Families.Update.UpdateFamiliesResponse | 6 | 0 | 6 | 42 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.UpdateFamiliesValidator | 28 | 0 | 28 | 39 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.UpdateFamilyDto | 6 | 0 | 6 | 24 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Families.Update.UpdateMemberDto | 4 | 0 | 4 | 24 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Lottery.LotteryCommitCommand | 1 | 0 | 1 | 5 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Lottery.LotteryCommitHandler | 21 | 0 | 21 | 48 | 100% | 5 | 6 | 83.3% |
| SAMGestor.Application.Features.Lottery.LotteryPreviewHandler | 19 | 0 | 19 | 41 | 100% | 3 | 4 | 75% |
| SAMGestor.Application.Features.Lottery.LotteryPreviewQuery | 1 | 0 | 1 | 5 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Lottery.LotteryResultDto | 5 | 0 | 5 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Lottery.ManualSelectCommand | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Lottery.ManualSelectHandler | 24 | 4 | 28 | 58 | 85.7% | 8 | 14 | 57.1% |
| SAMGestor.Application.Features.Lottery.ManualUnselectCommand | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Lottery.ManualUnselectHandler | 11 | 1 | 12 | 30 | 91.6% | 4 | 6 | 66.6% |
| SAMGestor.Application.Features.Lottery.Shuffler | 4 | 0 | 4 | 15 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Registrations.Create.CreateRegistrationCommand | 62 | 0 | 62 | 68 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.Create.CreateRegistrationHandler | 57 | 4 | 61 | 93 | 93.4% | 6 | 10 | 60% |
| SAMGestor.Application.Features.Registrations.Create.CreateRegistrationResponse | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.Create.CreateRegistrationValidator | 88 | 0 | 88 | 117 | 100% | 8 | 12 | 66.6% |
| SAMGestor.Application.Features.Registrations.GetAll.GetAllRegistrationsHandler | 51 | 2 | 53 | 83 | 96.2% | 29 | 34 | 85.2% |
| SAMGestor.Application.Features.Registrations.GetAll.GetAllRegistrationsQuery | 17 | 0 | 17 | 22 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetAll.GetAllRegistrationsResponse | 4 | 1 | 5 | 7 | 80% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetAll.RegistrationDto | 7 | 5 | 12 | 14 | 58.3% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.ConsentDto | 9 | 0 | 9 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.ContactsDto | 7 | 0 | 7 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.FamilyInfoDto | 13 | 0 | 13 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.FamilyMembershipDto | 5 | 0 | 5 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.GetRegistrationByIdHandler | 119 | 2 | 121 | 141 | 98.3% | 23 | 30 | 76.6% |
| SAMGestor.Application.Features.Registrations.GetById.GetRegistrationByIdQuery | 1 | 0 | 1 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.GetRegistrationByIdResponse | 28 | 0 | 28 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.HealthDto | 14 | 0 | 14 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.MediaDto | 15 | 0 | 15 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.PersonalDto | 11 | 0 | 11 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Registrations.GetById.ReligionHistoryDto | 5 | 0 | 5 | 117 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Create.CreateRetreatCommand | 15 | 0 | 15 | 21 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Create.CreateRetreatHandler | 28 | 0 | 28 | 58 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Retreats.Create.CreateRetreatResponse | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Create.CreateRetreatValidator | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Delete.DeleteRetreatCommand | 1 | 0 | 1 | 5 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Delete.DeleteRetreatHandler | 8 | 0 | 8 | 26 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Retreats.Delete.DeleteRetreatResponse | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.GetAll.ListRetreatsHandler | 14 | 0 | 14 | 33 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Retreats.GetAll.ListRetreatsQuery | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.GetAll.ListRetreatsResponse | 5 | 0 | 5 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.GetAll.RetreatDto | 6 | 0 | 6 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.GetById.GetRetreatByIdHandler | 20 | 0 | 20 | 35 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Retreats.GetById.GetRetreatByIdQuery | 1 | 0 | 1 | 5 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.GetById.GetRetreatByIdResponse | 15 | 0 | 15 | 18 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Update.UpdateRetreatCommand | 16 | 0 | 16 | 22 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Update.UpdateRetreatHandler | 25 | 0 | 25 | 46 | 100% | 7 | 8 | 87.5% |
| SAMGestor.Application.Features.Retreats.Update.UpdateRetreatResponse | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Retreats.Update.UpdateRetreatValidator | 3 | 0 | 3 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Alerts.GetAll.GetServiceAlertsHandler | 98 | 0 | 98 | 154 | 100% | 36 | 36 | 100% |
| SAMGestor.Application.Features.Service.Alerts.GetAll.GetServiceAlertsQuery | 4 | 0 | 4 | 15 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Alerts.GetAll.GetServiceAlertsResponse | 5 | 0 | 5 | 25 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Alerts.GetAll.ServiceAlertItem | 5 | 0 | 5 | 25 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Alerts.GetAll.ServiceSpaceAlertView | 11 | 0 | 11 | 25 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.Confirmed.GetConfirmedServiceRegistrationsHandler | 21 | 18 | 39 | 63 | 53.8% | 2 | 18 | 11.1% |
| SAMGestor.Application.Features.Service.Registrations.Confirmed.GetConfirmedServiceRegistrationsQuery | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.Confirmed.GetConfirmedServiceRegistrationsResponse | 0 | 11 | 11 | 15 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.Create.CreateServiceRegistrationCommand | 12 | 0 | 12 | 18 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.Create.CreateServiceRegistrationHandler | 37 | 0 | 37 | 70 | 100% | 22 | 22 | 100% |
| SAMGestor.Application.Features.Service.Registrations.Create.CreateServiceRegistrationResponse | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.Create.CreateServiceRegistrationValidator | 12 | 0 | 12 | 25 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.GetById.GetServiceRegistrationHandler | 31 | 0 | 31 | 49 | 100% | 9 | 10 | 90% |
| SAMGestor.Application.Features.Service.Registrations.GetById.GetServiceRegistrationQuery | 4 | 0 | 4 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.GetById.GetServiceRegistrationResponse | 17 | 0 | 17 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Registrations.GetById.PreferredSpaceView | 4 | 0 | 4 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Get.GetServiceRosterHandler | 44 | 0 | 44 | 67 | 100% | 12 | 12 | 100% |
| SAMGestor.Application.Features.Service.Roster.Get.GetServiceRosterQuery | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Get.GetServiceRosterResponse | 4 | 0 | 4 | 27 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Get.RosterMemberView | 7 | 0 | 7 | 27 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Get.RosterSpaceView | 10 | 0 | 10 | 27 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Unassigned.GetUnassignedServiceMembersHandler | 37 | 6 | 43 | 71 | 86% | 12 | 20 | 60% |
| SAMGestor.Application.Features.Service.Roster.Unassigned.GetUnassignedServiceMembersQuery | 6 | 0 | 6 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Unassigned.GetUnassignedServiceMembersResponse | 4 | 0 | 4 | 16 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Unassigned.UnassignedItem | 9 | 0 | 9 | 16 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.MemberInput | 5 | 0 | 5 | 23 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.RosterError | 6 | 0 | 6 | 33 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.RosterWarning | 5 | 0 | 5 | 33 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.SpaceInput | 5 | 0 | 5 | 23 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.SpaceResult | 9 | 0 | 9 | 33 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.UpdateServiceRosterCommand | 6 | 0 | 6 | 23 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.UpdateServiceRosterHandler | 134 | 0 | 134 | 206 | 100% | 46 | 46 | 100% |
| SAMGestor.Application.Features.Service.Roster.Update.UpdateServiceRosterResponse | 6 | 0 | 6 | 33 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Roster.Update.UpdateServiceRosterValidator | 14 | 0 | 14 | 22 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityCommand | 8 | 0 | 8 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityHandler | 40 | 0 | 40 | 73 | 100% | 24 | 24 | 100% |
| SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityItem | 5 | 0 | 5 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityRequest | 6 | 0 | 6 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityResponse | 5 | 0 | 5 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.BulkCapacity.UpdateServiceSpacesCapacityValidator | 22 | 0 | 22 | 32 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Create.CreateServiceSpaceCommand | 8 | 0 | 8 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Create.CreateServiceSpaceHandler | 24 | 0 | 24 | 43 | 100% | 8 | 8 | 100% |
| SAMGestor.Application.Features.Service.Spaces.Create.CreateServiceSpaceRequest | 7 | 0 | 7 | 9 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Create.CreateServiceSpaceResponse | 1 | 0 | 1 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Create.CreateServiceSpaceValidator | 9 | 0 | 9 | 18 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Delete.DeleteServiceSpaceCommand | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Delete.DeleteServiceSpaceHandler | 23 | 0 | 23 | 44 | 100% | 8 | 8 | 100% |
| SAMGestor.Application.Features.Service.Spaces.Delete.DeleteServiceSpaceResponse | 1 | 0 | 1 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Detail.GetServiceSpaceDetailHandler | 64 | 0 | 64 | 92 | 100% | 16 | 16 | 100% |
| SAMGestor.Application.Features.Service.Spaces.Detail.GetServiceSpaceDetailQuery | 7 | 0 | 7 | 41 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Detail.GetServiceSpaceDetailResponse | 8 | 0 | 8 | 41 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Detail.MemberItem | 7 | 0 | 7 | 41 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Detail.SpaceView | 12 | 0 | 12 | 41 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.List.ListItem | 10 | 0 | 10 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.List.ListServiceSpacesHandler | 32 | 0 | 32 | 52 | 100% | 14 | 14 | 100% |
| SAMGestor.Application.Features.Service.Spaces.List.ListServiceSpacesQuery | 6 | 0 | 6 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.List.ListServiceSpacesResponse | 4 | 0 | 4 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Locking.LockAllServiceSpacesCommand | 4 | 0 | 4 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Locking.LockAllServiceSpacesHandler | 19 | 0 | 19 | 38 | 100% | 14 | 14 | 100% |
| SAMGestor.Application.Features.Service.Spaces.Locking.LockAllServiceSpacesResponse | 1 | 0 | 1 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Locking.LockServiceSpaceCommand | 5 | 0 | 5 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Locking.LockServiceSpaceHandler | 21 | 0 | 21 | 40 | 100% | 16 | 16 | 100% |
| SAMGestor.Application.Features.Service.Spaces.Locking.LockServiceSpaceResponse | 1 | 0 | 1 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Locking.ToggleServiceSpaceLockRequest | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.PublicList.PublicItem | 1 | 0 | 1 | 13 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.PublicList.PublicListServiceSpacesHandler | 13 | 0 | 13 | 27 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Service.Spaces.PublicList.PublicListServiceSpacesQuery | 1 | 0 | 1 | 13 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.PublicList.PublicListServiceSpacesResponse | 4 | 0 | 4 | 13 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Summary.GetServiceSpacesSummaryHandler | 36 | 0 | 36 | 59 | 100% | 6 | 6 | 100% |
| SAMGestor.Application.Features.Service.Spaces.Summary.GetServiceSpacesSummaryQuery | 1 | 0 | 1 | 24 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Summary.GetServiceSpacesSummaryResponse | 4 | 0 | 4 | 24 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Service.Spaces.Summary.SpaceSummaryItem | 12 | 0 | 12 | 24 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreatedTentView | 4 | 5 | 9 | 28 | 44.4% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentError | 4 | 2 | 6 | 28 | 66.6% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentItemDto | 6 | 0 | 6 | 16 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentsCommand | 4 | 0 | 4 | 16 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentsHandler | 76 | 3 | 79 | 132 | 96.2% | 23 | 28 | 82.1% |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentsResponse | 7 | 0 | 7 | 28 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.BulkCreate.BulkCreateTentsValidator | 38 | 0 | 38 | 51 | 100% | 8 | 10 | 80% |
| SAMGestor.Application.Features.Tents.Create.CreateTentCommand | 7 | 0 | 7 | 12 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Create.CreateTentHandler | 37 | 3 | 40 | 81 | 92.5% | 16 | 20 | 80% |
| SAMGestor.Application.Features.Tents.Create.CreateTentResponse | 4 | 0 | 4 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Create.CreateTentValidator | 24 | 0 | 24 | 41 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Features.Tents.Delete.DeleteTentCommand | 4 | 0 | 4 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Delete.DeleteTentHandler | 31 | 0 | 31 | 54 | 100% | 16 | 18 | 88.8% |
| SAMGestor.Application.Features.Tents.Delete.DeleteTentResponse | 5 | 0 | 5 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.GetById.GetTentByIdHandler | 21 | 0 | 21 | 35 | 100% | 4 | 4 | 100% |
| SAMGestor.Application.Features.Tents.GetById.GetTentByIdQuery | 4 | 0 | 4 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.GetById.GetTentByIdResponse | 11 | 0 | 11 | 15 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.List.ListTentsHandler | 33 | 0 | 33 | 50 | 100% | 6 | 6 | 100% |
| SAMGestor.Application.Features.Tents.List.ListTentsQuery | 5 | 0 | 5 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.List.ListTentsResponse | 0 | 4 | 4 | 16 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.List.TentListItem | 7 | 3 | 10 | 14 | 70% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.List.TentListItemDto | 0 | 9 | 9 | 16 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Locking.SetTentLockCommand | 5 | 0 | 5 | 9 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Locking.SetTentLockHandler | 32 | 2 | 34 | 64 | 94.1% | 19 | 30 | 63.3% |
| SAMGestor.Application.Features.Tents.Locking.SetTentLockResponse | 5 | 1 | 6 | 8 | 83.3% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Locking.SetTentLockValidator | 0 | 4 | 4 | 12 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Locking.SetTentsGlobalLockCommand | 4 | 0 | 4 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Locking.SetTentsGlobalLockHandler | 22 | 2 | 24 | 48 | 91.6% | 15 | 26 | 57.6% |
| SAMGestor.Application.Features.Tents.Locking.SetTentsGlobalLockResponse | 4 | 1 | 5 | 7 | 80% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Locking.SetTentsGlobalLockValidator | 0 | 3 | 3 | 11 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.TentRosterError | 3 | 3 | 6 | 31 | 50% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.TentRosterMemberItem | 4 | 0 | 4 | 19 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.TentRosterMemberView | 4 | 3 | 7 | 31 | 57.1% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.TentRosterSnapshot | 4 | 0 | 4 | 19 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.TentRosterSpaceView | 7 | 1 | 8 | 31 | 87.5% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.UpdateTentRosterCommand | 5 | 0 | 5 | 19 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.UpdateTentRosterHandler | 123 | 0 | 123 | 206 | 100% | 49 | 52 | 94.2% |
| SAMGestor.Application.Features.Tents.TentRoster.Assign.UpdateTentRosterResponse | 5 | 0 | 5 | 31 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.AutoAssign.AutoAssignTentsCommand | 4 | 0 | 4 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.AutoAssign.AutoAssignTentsHandler | 94 | 2 | 96 | 165 | 97.9% | 33 | 36 | 91.6% |
| SAMGestor.Application.Features.Tents.TentRoster.AutoAssign.AutoAssignTentsResponse | 4 | 0 | 4 | 8 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Get.GetTentRosterHandler | 56 | 0 | 56 | 83 | 100% | 10 | 12 | 83.3% |
| SAMGestor.Application.Features.Tents.TentRoster.Get.GetTentRosterQuery | 1 | 0 | 1 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Get.GetTentRosterResponse | 4 | 0 | 4 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Unassign.UnassignFromTentCommand | 4 | 0 | 4 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Unassign.UnassignFromTentHandler | 37 | 0 | 37 | 65 | 100% | 13 | 14 | 92.8% |
| SAMGestor.Application.Features.Tents.TentRoster.Unassign.UnassignFromTentResponse | 5 | 0 | 5 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Unassigned.GetTentUnassignedHandler | 20 | 0 | 20 | 35 | 100% | 4 | 4 | 100% |
| SAMGestor.Application.Features.Tents.TentRoster.Unassigned.GetTentUnassignedQuery | 5 | 0 | 5 | 21 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Unassigned.GetTentUnassignedResponse | 4 | 0 | 4 | 21 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Unassigned.UnassignedMemberView | 6 | 0 | 6 | 21 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.RosterError | 0 | 6 | 6 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.RosterWarning | 0 | 5 | 5 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.TentResult | 0 | 7 | 7 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.UpdateTentMember | 0 | 4 | 4 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.UpdateTentRosterCommand | 0 | 6 | 6 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.UpdateTentRosterHandler | 0 | 185 | 185 | 283 | 0% | 0 | 68 | 0% |
| SAMGestor.Application.Features.Tents.TentRoster.Update.UpdateTentRosterResponse | 0 | 6 | 6 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.TentRoster.Update.UpdateTentSnapshot | 0 | 4 | 4 | 48 | 0% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Update.UpdateTentCommand | 9 | 0 | 9 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Update.UpdateTentHandler | 54 | 1 | 55 | 92 | 98.1% | 32 | 36 | 88.8% |
| SAMGestor.Application.Features.Tents.Update.UpdateTentResponse | 5 | 0 | 5 | 7 | 100% | 0 | 0 |  |
| SAMGestor.Application.Features.Tents.Update.UpdateTentValidator | 25 | 0 | 25 | 42 | 100% | 2 | 2 | 100% |
| SAMGestor.Application.Services.DefaultServiceSpaces | 20 | 0 | 20 | 54 | 100% | 0 | 0 |  |
| SAMGestor.Application.Services.ServiceSpacesSeeder | 10 | 0 | 10 | 54 | 100% | 6 | 6 | 100% |
| ValidationBehavior<T1, T2> | 15 | 0 | 15 | 36 | 100% | 8 | 8 | 100% |
| **SAMGestor.Contracts** | **48** | **48** | **96** | **173** | **50%** | **2** | **2** | **100%** |
| SAMGestor.Contracts.EventEnvelope<T> | 10 | 0 | 10 | 38 | 100% | 2 | 2 | 100% |
| SAMGestor.Contracts.FamilyGroupCreatedV1 | 8 | 0 | 8 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Contracts.FamilyGroupCreateFailedV1 | 0 | 7 | 7 | 9 | 0% | 0 | 0 |  |
| SAMGestor.Contracts.FamilyGroupCreateRequestedV1 | 7 | 0 | 7 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Contracts.FamilyGroupNotifyRequestedV1 | 7 | 0 | 7 | 11 | 100% | 0 | 0 |  |
| SAMGestor.Contracts.NotificationEmailFailedV1 | 0 | 7 | 7 | 18 | 0% | 0 | 0 |  |
| SAMGestor.Contracts.NotificationEmailSentV1 | 0 | 6 | 6 | 18 | 0% | 0 | 0 |  |
| SAMGestor.Contracts.PaymentConfirmedV1 | 7 | 1 | 8 | 10 | 87.5% | 0 | 0 |  |
| SAMGestor.Contracts.PaymentLinkCreatedV1 | 0 | 9 | 9 | 11 | 0% | 0 | 0 |  |
| SAMGestor.Contracts.PaymentRequestedV1 | 0 | 9 | 9 | 11 | 0% | 0 | 0 |  |
| SAMGestor.Contracts.SelectionParticipantSelectedV1 | 9 | 0 | 9 | 13 | 100% | 0 | 0 |  |
| SAMGestor.Contracts.ServingParticipantSelectedV1 | 0 | 9 | 9 | 13 | 0% | 0 | 0 |  |
| **SAMGestor.Domain** | **532** | **245** | **777** | **1324** | **68.4%** | **77** | **182** | **42.3%** |
| SAMGestor.Domain.Commom.Entity<T> | 1 | 0 | 1 | 6 | 100% | 0 | 0 |  |
| SAMGestor.Domain.Commom.ValueObject | 1 | 1 | 2 | 7 | 50% | 2 | 4 | 50% |
| SAMGestor.Domain.Entities.BlockedCpf | 0 | 8 | 8 | 19 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Entities.ChangeLog | 0 | 14 | 14 | 26 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Entities.Family | 39 | 11 | 50 | 85 | 78% | 2 | 4 | 50% |
| SAMGestor.Domain.Entities.FamilyMember | 14 | 1 | 15 | 29 | 93.3% | 0 | 0 |  |
| SAMGestor.Domain.Entities.MessageSent | 0 | 14 | 14 | 27 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Entities.MessageTemplate | 0 | 10 | 10 | 21 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Entities.Payment | 16 | 1 | 17 | 33 | 94.1% | 0 | 0 |  |
| SAMGestor.Domain.Entities.RegionConfig | 0 | 12 | 12 | 23 | 0% | 0 | 2 | 0% |
| SAMGestor.Domain.Entities.Registration | 179 | 3 | 182 | 256 | 98.3% | 33 | 64 | 51.5% |
| SAMGestor.Domain.Entities.Retreat | 78 | 13 | 91 | 137 | 85.7% | 5 | 16 | 31.2% |
| SAMGestor.Domain.Entities.ServiceAssignment | 14 | 9 | 23 | 41 | 60.8% | 0 | 0 |  |
| SAMGestor.Domain.Entities.ServiceRegistration | 46 | 17 | 63 | 94 | 73% | 2 | 12 | 16.6% |
| SAMGestor.Domain.Entities.ServiceRegistrationPayment | 8 | 1 | 9 | 17 | 88.8% | 0 | 0 |  |
| SAMGestor.Domain.Entities.ServiceSpace | 30 | 10 | 40 | 64 | 75% | 10 | 28 | 35.7% |
| SAMGestor.Domain.Entities.Team | 0 | 24 | 24 | 43 | 0% | 0 | 4 | 0% |
| SAMGestor.Domain.Entities.TeamMember | 0 | 10 | 10 | 21 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Entities.Tent | 21 | 12 | 33 | 57 | 63.6% | 4 | 12 | 33.3% |
| SAMGestor.Domain.Entities.TentAssignment | 13 | 10 | 23 | 39 | 56.5% | 0 | 0 |  |
| SAMGestor.Domain.Entities.User | 0 | 19 | 19 | 33 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Entities.WaitingListItem | 0 | 13 | 13 | 23 | 0% | 0 | 0 |  |
| SAMGestor.Domain.Enums.SlotPolicy | 7 | 1 | 8 | 15 | 87.5% | 0 | 0 |  |
| SAMGestor.Domain.Exceptions.BusinessRuleException | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Domain.Exceptions.NotFoundException | 1 | 0 | 1 | 3 | 100% | 0 | 0 |  |
| SAMGestor.Domain.Exceptions.UniqueConstraintViolationException | 1 | 1 | 2 | 7 | 50% | 0 | 0 |  |
| SAMGestor.Domain.ValueObjects.CPF | 7 | 2 | 9 | 16 | 77.7% | 3 | 4 | 75% |
| SAMGestor.Domain.ValueObjects.EmailAddress | 6 | 3 | 9 | 16 | 66.6% | 0 | 0 |  |
| SAMGestor.Domain.ValueObjects.FamilyName | 8 | 2 | 10 | 19 | 80% | 2 | 4 | 50% |
| SAMGestor.Domain.ValueObjects.FullName | 15 | 1 | 16 | 34 | 93.7% | 4 | 6 | 66.6% |
| SAMGestor.Domain.ValueObjects.Money | 9 | 3 | 12 | 25 | 75% | 3 | 6 | 50% |
| SAMGestor.Domain.ValueObjects.PasswordHash | 0 | 9 | 9 | 23 | 0% | 0 | 4 | 0% |
| SAMGestor.Domain.ValueObjects.Percentage | 6 | 3 | 9 | 24 | 66.6% | 2 | 4 | 50% |
| SAMGestor.Domain.ValueObjects.TentNumber | 5 | 4 | 9 | 22 | 55.5% | 1 | 2 | 50% |
| SAMGestor.Domain.ValueObjects.UrlAddress | 6 | 3 | 9 | 16 | 66.6% | 4 | 6 | 66.6% |
| **SAMGestor.Infrastructure** | **14576** | **735** | **15311** | **17737** | **95.1%** | **168** | **354** | **47.4%** |
| LocalStorageService | 14 | 0 | 14 | 25 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Extensions.ServiceCollectionExtensions | 54 | 1 | 55 | 123 | 98.1% | 9 | 16 | 56.2% |
| SAMGestor.Infrastructure.Messaging.Consumers.FamilyGroupCreatedConsumer | 39 | 7 | 46 | 88 | 84.7% | 10 | 14 | 71.4% |
| SAMGestor.Infrastructure.Messaging.Consumers.FamilyGroupCreateFailedConsumer | 28 | 20 | 48 | 87 | 58.3% | 3 | 14 | 21.4% |
| SAMGestor.Infrastructure.Messaging.Consumers.PaymentConfirmedConsumer | 67 | 17 | 84 | 165 | 79.7% | 21 | 58 | 36.2% |
| SAMGestor.Infrastructure.Messaging.Consumers.ServicePaymentConfirmedConsumer | 123 | 13 | 136 | 264 | 90.4% | 61 | 110 | 55.4% |
| SAMGestor.Infrastructure.Messaging.Options.ServiceAutoAssignOptions | 2 | 0 | 2 | 10 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Messaging.Outbox.OutboxDispatcher | 49 | 19 | 68 | 124 | 72% | 11 | 20 | 55% |
| SAMGestor.Infrastructure.Messaging.Outbox.OutboxEventBus | 14 | 0 | 14 | 29 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Messaging.Outbox.OutboxMessage | 9 | 0 | 9 | 14 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Messaging.RabbitMq.EventPublisher | 24 | 0 | 24 | 38 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Messaging.RabbitMq.RabbitMqConnection | 13 | 0 | 13 | 25 | 100% | 4 | 4 | 100% |
| SAMGestor.Infrastructure.Messaging.RabbitMq.RabbitMqOptions | 6 | 0 | 6 | 12 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.AddOutbox_Core | 954 | 4 | 958 | 1031 | 99.5% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.AddTentsAndAssignments | 1478 | 41 | 1519 | 1619 | 97.3% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.Core_Initial | 1205 | 43 | 1248 | 1357 | 96.5% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.ExpandRegistrationV2 | 2141 | 264 | 2405 | 2612 | 89% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.Families_GroupColumns | 1059 | 33 | 1092 | 1178 | 96.9% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.Families_JoinFamilyMember_V1 | 1078 | 58 | 1136 | 1233 | 94.8% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.Families_Lock_And_UniqueName | 983 | 13 | 996 | 1071 | 98.6% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.Families_RowLock | 977 | 5 | 982 | 1053 | 99.4% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.SAMContextModelSnapshot | 1698 | 0 | 1698 | 1757 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Migrations.ServirModule_Initial | 1485 | 21 | 1506 | 1607 | 98.6% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.BlockedCpfConfiguration | 15 | 0 | 15 | 31 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.ChangeLogConfiguration | 8 | 0 | 8 | 20 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.FamilyConfiguration | 55 | 0 | 55 | 85 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.FamilyMemberConfiguration | 36 | 0 | 36 | 58 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.MessageSentConfiguration | 7 | 0 | 7 | 19 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.MessageTemplateConfiguration | 6 | 0 | 6 | 18 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.OutboxMessageConfiguration | 10 | 0 | 10 | 22 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.PaymentConfiguration | 12 | 0 | 12 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.RegionConfigConfiguration | 11 | 0 | 11 | 26 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.RegistrationConfiguration | 273 | 0 | 273 | 364 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.RetreatConfiguration | 109 | 0 | 109 | 140 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.ServiceAssignmentConfiguration | 35 | 0 | 35 | 57 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.ServiceRegistrationConfiguration | 81 | 0 | 81 | 111 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.ServiceRegistrationPaymentConfiguration | 13 | 0 | 13 | 29 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.ServiceSpaceConfiguration | 15 | 0 | 15 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.TeamConfiguration | 17 | 0 | 17 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.TeamMemberConfiguration | 7 | 0 | 7 | 21 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.TentAssignmentConfiguration | 28 | 0 | 28 | 49 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.TentConfiguration | 36 | 0 | 36 | 59 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.UserConfiguration | 19 | 0 | 19 | 34 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.Configurations.WaitingListItemConfiguration | 17 | 0 | 17 | 36 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Persistence.DesignTimeDbContextFactory | 0 | 12 | 12 | 28 | 0% | 0 | 4 | 0% |
| SAMGestor.Infrastructure.Persistence.SAMContext | 18 | 10 | 28 | 41 | 64.2% | 2 | 2 | 100% |
| SAMGestor.Infrastructure.Repositories.FamilyMemberRepository | 38 | 3 | 41 | 76 | 92.6% | 4 | 6 | 66.6% |
| SAMGestor.Infrastructure.Repositories.FamilyRepository | 19 | 1 | 20 | 46 | 95% | 1 | 2 | 50% |
| SAMGestor.Infrastructure.Repositories.RegistrationRepository | 45 | 48 | 93 | 168 | 48.3% | 10 | 24 | 41.6% |
| SAMGestor.Infrastructure.Repositories.RetreatRepository | 17 | 0 | 17 | 50 | 100% | 0 | 0 |  |
| SAMGestor.Infrastructure.Repositories.ServiceAssignmentRepository | 26 | 38 | 64 | 111 | 40.6% | 3 | 12 | 25% |
| SAMGestor.Infrastructure.Repositories.ServiceRegistrationRepository | 35 | 6 | 41 | 81 | 85.3% | 2 | 2 | 100% |
| SAMGestor.Infrastructure.Repositories.ServiceSpaceRepository | 19 | 3 | 22 | 55 | 86.3% | 0 | 0 |  |
| SAMGestor.Infrastructure.Repositories.TentAssignmentRepository | 0 | 20 | 20 | 54 | 0% | 0 | 2 | 0% |
| SAMGestor.Infrastructure.Repositories.TentRepository | 0 | 21 | 21 | 60 | 0% | 0 | 6 | 0% |
| SAMGestor.Infrastructure.Services.HeuristicRelationshipService | 28 | 6 | 34 | 86 | 82.3% | 19 | 38 | 50% |
| SAMGestor.Infrastructure.UnitOfWork.EfUnitOfWork | 21 | 8 | 29 | 86 | 72.4% | 8 | 20 | 40% |

