using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

/// <summary>Every endpoint is versioned under /api/v1 and requires authentication unless marked [AllowAnonymous].</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase;
