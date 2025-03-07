using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;
using Utilities;

public sealed partial class Connection
{
  private sealed record class GetFilesRequest
  {
    public required string? SearchString;
    public required ObjectId? ParentFolderId;
    public required FileType? FileType;
    public required ObjectId? OwnerUserId;
    public required string? Name;
    public required ObjectId? Id;
    public TrashOptions TrashOptions = TrashOptions.NonInclusive;
    public required PaginationOptions? Pagination;
  }

  private sealed record class GetFilesResponse
  {
    public required string[] Files;
  }

  private AuthenticatedRequestHandler<GetFilesRequest, GetFilesResponse> GetFiles =>
    async (transaction, request, userAuthentication, me, _) =>
    {
      Resource<File>? parentFolder = await Internal_GetFile(
        transaction,
        me,
        userAuthentication,
        request.ParentFolderId
      );

      FileAccessResult? parentFolderAccess =
        parentFolder != null
          ? await Internal_UnlockFile(
            transaction,
            parentFolder,
            me,
            userAuthentication,
            FileAccessLevel.Read
          )
          : null;

      if (
        (
          request.OwnerUserId != null
          && request.OwnerUserId != me.Id
          && !await Resources
            .Query<AdminAccess>(transaction, (query) => query.Where((item) => item.UserId == me.Id))
            .AnyAsync(transaction.CancellationToken)
        )
      )
      {
        throw new InvalidOperationException("Owner User ID must be set to self when not an admin.");
      }

      Resource<User>? ownerUser =
        request.OwnerUserId != null
          ? await Resources
            .Query<User>(
              transaction,
              (query) => query.Where((item) => item.Id == request.OwnerUserId)
            )
            .FirstOrDefaultAsync(transaction.CancellationToken)
          : null;

      Resource<File>[] files = await Resources
        .Query<File>(
          transaction,
          (query) =>
            query
              .Where(
                (item) =>
                  (
                    request.SearchString == null
                    || item.Name.Contains(
                      request.SearchString,
                      StringComparison.CurrentCultureIgnoreCase
                    )
                  )
                  && (
                    request.TrashOptions == TrashOptions.Exclusive
                    || parentFolder == null
                    || item.ParentId == parentFolder.Id
                  )
                  && (request.FileType == null || item.Type == request.FileType)
                  && (ownerUser == null || item.OwnerUserId == ownerUser.Id)
                  && (
                    request.Name == null
                    || item.Name.Equals(request.Name, StringComparison.CurrentCultureIgnoreCase)
                  )
                  && (request.Id == null || item.Id == request.Id)
                  && (
                    request.TrashOptions == TrashOptions.Exclusive
                      ? item.TrashTime != null
                      : request.TrashOptions == TrashOptions.Inclusive
                        || (
                          request.TrashOptions == TrashOptions.NonInclusive
                          && item.TrashTime == null
                        )
                  )
              )
              .OrderByDescending((file) => file.Type)
              .ApplyPagination(request.Pagination)
        )
        .ToArrayAsync(transaction.CancellationToken);

      return new() { Files = [.. files.ToJson()] };
    };
}
