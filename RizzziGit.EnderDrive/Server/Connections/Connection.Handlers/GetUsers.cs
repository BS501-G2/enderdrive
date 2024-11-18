using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetUsersRequest
  {
    [BsonElement("searchString")]
    public required string? SearchString;

    [BsonElement("includeRole")]
    public required UserRole[]? IncludeRole;

    [BsonElement("excludeRole")]
    public required UserRole[]? ExcludeRole;

    [BsonElement("username")]
    public required string? Username;

    [BsonElement("id")]
    public required ObjectId? Id;

    [BsonElement("pagination")]
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetUsersResponse
  {
    [BsonElement("users")]
    public required string[] Users;
  }

  private AuthenticatedRequestHandler<GetUsersRequest, GetUsersResponse> GetUsers =>
    async (transaction, request, _, _, _) =>
    {
      Resource<User>[] users = await Resources
        .Query<User>(
          transaction,
          (query) =>
            query
              .Where(
                (user) =>
                  (
                    request.SearchString == null
                    || (
                      user.Username.Contains(
                        request.SearchString,
                        System.StringComparison.OrdinalIgnoreCase
                      )
                      || user.FirstName.Contains(
                        request.SearchString,
                        System.StringComparison.OrdinalIgnoreCase
                      )
                      || (
                        user.MiddleName == null
                        || user.MiddleName.Contains(
                          request.SearchString,
                          System.StringComparison.OrdinalIgnoreCase
                        )
                      )
                      || user.LastName.Contains(
                        request.SearchString,
                        System.StringComparison.OrdinalIgnoreCase
                      )
                      || (
                        user.DisplayName == null
                        || user.DisplayName.Contains(
                          request.SearchString,
                          System.StringComparison.OrdinalIgnoreCase
                        )
                      )
                    )
                  )
                  && (
                    request.IncludeRole == null || request.IncludeRole.Intersect(user.Roles).Any()
                  )
                  && (
                    request.ExcludeRole == null || !request.ExcludeRole.Intersect(user.Roles).Any()
                  )
                  && (
                    request.Username == null
                    || user.Username.Equals(
                      request.Username,
                      System.StringComparison.OrdinalIgnoreCase
                    )
                  )
                  && (request.Id == null || user.Id == request.Id)
              )
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { Users = [.. users.ToJson()] };
    };
}
