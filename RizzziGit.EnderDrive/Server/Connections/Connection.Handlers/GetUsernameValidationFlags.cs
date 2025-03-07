using MongoDB.Bson.Serialization.Attributes;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private sealed record class GetUsernameValidationFlagsRequest
  {
    public required string Username;
  }

  private sealed record class GetUsernameValidationFlagsResponse
  {
    public required UsernameValidationFlags Flags;
  }

  private RequestHandler<
    GetUsernameValidationFlagsRequest,
    GetUsernameValidationFlagsResponse
  > GetUsernameValidationFlags =>
    async (request, cancellationToken) =>
    {
      return new() { Flags = Resources.ValidateUsername(request.Username) };
    };
}
