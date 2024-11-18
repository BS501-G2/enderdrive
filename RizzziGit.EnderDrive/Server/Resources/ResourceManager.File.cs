using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace RizzziGit.EnderDrive.Server.Resources;

using System.Collections.Generic;
using Services;

public enum FileType
{
  File,
  Folder,
}

public enum TrashOptions
{
  NonInclusive,
  Inclusive,
  Exclusive,
}

public record class File : ResourceData
{
  [JsonProperty("parentId")]
  public required ObjectId? ParentId;

  [JsonProperty("ownerUserId")]
  public required ObjectId OwnerUserId;

  [JsonProperty("name")]
  public required string Name;

  [JsonProperty("type")]
  public required FileType Type;

  [JsonProperty("createTime")]
  [BsonRepresentation(BsonType.DateTime)]
  public required DateTimeOffset CreateTime;

  [JsonProperty("updateTime")]
  [BsonRepresentation(BsonType.DateTime)]
  public required DateTimeOffset UpdateTime;

  [JsonProperty("trashTime")]
  [BsonRepresentation(BsonType.DateTime)]
  public required DateTimeOffset? TrashTime;

  [JsonIgnore]
  public required byte[] EncryptedAesKey;

  [JsonIgnore]
  public required byte[] AdminEncryptedAesKey;
}

public sealed partial class ResourceManager
{
  public const int FILE_BUFFER_SIZE = 1024 * 256;

  public async Task<UnlockedFile> GetRootFolder(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication
  )
  {
    Resource<File>? file = await Query<File>(
        transaction,
        (query) =>
          query.Where((item) => item.OwnerUserId == user.Id && item.Id == user.Data.RootFileId)
      )
      .FirstOrDefaultAsync(transaction);

    if (file != null)
    {
      return UnlockedFile.Unlock(file, userAuthentication);
    }

    byte[] aesKey = RandomNumberGenerator.GetBytes(32);
    byte[] encryptedAesKey = KeyManager.Encrypt(user.Data, aesKey);
    byte[] adminEcnryptedAesKey = KeyManager.Encrypt(AdminManager.AdminKey, aesKey);

    file = ToResource<File>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,
        ParentId = null,
        OwnerUserId = user.Id,

        Name = ".ROOT",
        Type = FileType.Folder,

        EncryptedAesKey = encryptedAesKey,
        AdminEncryptedAesKey = adminEcnryptedAesKey,

        CreateTime = DateTimeOffset.UtcNow,
        UpdateTime = DateTimeOffset.UtcNow,
        TrashTime = null,
      }
    );

    await file.Save(transaction);
    user.Data.RootFileId = file.Id;
    await user.Save(transaction);

    return new(file) { AesKey = aesKey };
  }

  public async Task<UnlockedFile> GetTrashFolder(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedUserAuthentication userAuthentication
  )
  {
    Resource<File>? file = await Query<File>(
        transaction,
        (query) =>
          query.Where((item) => item.OwnerUserId == user.Id && item.Id == user.Data.TrashFileId)
      )
      .FirstOrDefaultAsync(transaction);

    if (file != null)
    {
      return UnlockedFile.Unlock(file, userAuthentication);
    }

    byte[] aesKey = RandomNumberGenerator.GetBytes(32);
    byte[] encryptedAesKey = KeyManager.Encrypt(user.Data, aesKey);
    byte[] adminEcnryptedAesKey = KeyManager.Encrypt(AdminManager.AdminKey, aesKey);

    file = ToResource<File>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,
        ParentId = null,
        OwnerUserId = user.Id,

        Name = ".TRASH",
        Type = FileType.Folder,

        EncryptedAesKey = encryptedAesKey,
        AdminEncryptedAesKey = adminEcnryptedAesKey,

        CreateTime = DateTimeOffset.UtcNow,
        UpdateTime = DateTimeOffset.UtcNow,
        TrashTime = null,
      }
    );

    await file.Save(transaction);
    user.Data.TrashFileId = file.Id;
    await user.Save(transaction);

    return new(file) { AesKey = aesKey };
  }

  public async Task<UnlockedFile> CreateFile(
    ResourceTransaction transaction,
    Resource<User> user,
    UnlockedFile parent,
    FileType type,
    string name
  )
  {
    byte[] aesKey = RandomNumberGenerator.GetBytes(32);
    byte[] encryptedAesKey = KeyManager.Encrypt(parent, aesKey);
    byte[] adminEcnryptedAesKey = KeyManager.Encrypt(AdminManager.AdminKey, aesKey);

    Resource<File> file = ToResource<File>(
      transaction,
      new()
      {
        Id = ObjectId.Empty,
        ParentId = parent.File.Id,
        OwnerUserId = user.Id,

        Name = name,
        Type = type,

        EncryptedAesKey = encryptedAesKey,
        AdminEncryptedAesKey = adminEcnryptedAesKey,

        CreateTime = DateTimeOffset.UtcNow,
        UpdateTime = DateTimeOffset.UtcNow,
        TrashTime = null,
      }
    );

    await file.Save(transaction);

    return new(file) { AesKey = aesKey };
  }

  public async ValueTask MoveFile(
    ResourceTransaction transaction,
    UnlockedFile file,
    UnlockedFile destinationFolder
  )
  {
    byte[] fileAesKey = file.AesKey;
    byte[] encryptedFileAesKey = KeyManager.Encrypt(destinationFolder, fileAesKey);

    await file.File.Update(
      (file) =>
      {
        file.ParentId = destinationFolder.File.Id;
        file.EncryptedAesKey = encryptedFileAesKey;
        return file;
      },
      transaction
    );
  }

  public async Task Delete(ResourceTransaction transaction, Resource<File> file)
  {
    await foreach (
      Resource<FileContent> content in Query<FileContent>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, content);
    }

    await foreach (
      Resource<FileSnapshot> snapshot in Query<FileSnapshot>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, snapshot);
    }

    await foreach (
      Resource<FileBuffer> fileBuffer in Query<FileBuffer>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, fileBuffer);
    }

    await foreach (
      Resource<FileAccess> fileAccess in Query<FileAccess>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, fileAccess);
    }

    await foreach (
      Resource<FileStar> fileStar in Query<FileStar>(
        transaction,
        (query) => query.Where((item) => item.FileId == file.Id)
      )
    )
    {
      await Delete(transaction, fileStar);
    }

    await Delete<File>(transaction, file);
  }
}

public static class IQueryableExtensions
{
  public static IQueryable<T> ApplyFilter<T>(
    this IQueryable<T> query,
    Func<IQueryable<T>, IQueryable<T>>? onQuery
  )
    where T : ResourceData
  {
    if (onQuery != null)
    {
      return onQuery(query);
    }

    return query;
  }
}
