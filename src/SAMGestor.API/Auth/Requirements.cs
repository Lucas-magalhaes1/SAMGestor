using Microsoft.AspNetCore.Authorization;

namespace SAMGestor.API.Auth;

public sealed class ReadOnlyRequirement : IAuthorizationRequirement { }

public sealed class ManageAllButDeleteUsersRequirement : IAuthorizationRequirement { }

public sealed class AdminOnlyRequirement : IAuthorizationRequirement { }

public sealed class EmailConfirmedRequirement : IAuthorizationRequirement { }