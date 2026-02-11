using Maken.Api.DTOs.Requests;
using Maken.Api.DTOs.Responses;
using Maken.Application.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maken.Api.Controllers;

/// <summary>
/// Controller for authentication operations (login, refresh token).
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response with access and refresh tokens.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, cancellationToken);

            var response = new LoginResponse(
                AccessToken: result.AccessToken,
                RefreshToken: result.RefreshToken,
                UserId: result.UserId,
                Email: result.Email,
                Role: result.Role
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New access and refresh tokens.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _mediator.Send(command, cancellationToken);

            var response = new LoginResponse(
                AccessToken: result.AccessToken,
                RefreshToken: result.RefreshToken,
                UserId: Guid.Empty, // Not included in refresh response
                Email: string.Empty, // Not included in refresh response
                Role: string.Empty // Not included in refresh response
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
