using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RizzziGit.EnderDrive.Server.Resources;

using Services;

public record class UserAdminBackdoor
  : ResourceData
{
  public required ObjectId UserId;

  public required byte[] EncryptedUserPrivateRsaKey;

  public UnlockedUserAdminBackdoor Unlocked(
    UnlockedAdminAccess adminAccess
  )
  {
    byte[] userAesKey =
      KeyManager.Decrypt(
        adminAccess,
        EncryptedUserPrivateRsaKey
      );

    return new()
    {
      Id =
        Id,

      Original =
        this,
      UserPrivateRsaKey =
        userAesKey,

      UserId =
        UserId,
      EncryptedUserPrivateRsaKey =
        EncryptedUserPrivateRsaKey,
    };
  }
}

public record class UnlockedUserAdminBackdoor
  : UserAdminBackdoor
{
  public static implicit operator byte[](
    UnlockedUserAdminBackdoor userAdminBackdoor
  ) =>
    userAdminBackdoor.UserPrivateRsaKey;

  public required UserAdminBackdoor Original;

  public required byte[] UserPrivateRsaKey;
}

public sealed partial class ResourceManager
{
  public async Task<UnlockedUserAdminBackdoor> CreateUserAdminBackdoor(
    ResourceTransaction transaction,
    User user,
    UnlockedUserAuthentication userAuthentication,
    UnlockedAdminKey adminKey
  )
  {
    byte[] userPrivateRsaKey =
      userAuthentication.UserRsaPrivateKey;
    UserAdminBackdoor item =
      new()
      {
        Id =
          ObjectId.GenerateNewId(),

        UserId =
          user.Id,
        EncryptedUserPrivateRsaKey =
          KeyManager.Encrypt(
            adminKey,
            userPrivateRsaKey
          ),
      };

    await Insert(
      transaction,
      item
    );

    return new()
    {
      Id =
        item.Id,
      UserId =
        item.UserId,
      EncryptedUserPrivateRsaKey =
        item.EncryptedUserPrivateRsaKey,
      UserPrivateRsaKey =
        userPrivateRsaKey,

      Original =
        item,
    };
  }

  public IQueryable<UserAdminBackdoor> GetUserAdminBackdoors(
    ResourceTransaction transaction,
    ObjectId? userId =
      null
  ) =>
    Query<UserAdminBackdoor>(
      transaction,
      (
        query
      ) =>
        query.Where(
          (
            item
          ) =>
            userId
              == null
            || item.UserId
              == userId
        )
    );
}
