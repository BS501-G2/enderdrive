using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private static ConnectionPacket DeserializePacket(byte[] bytes)
  {
    BsonDocument document;
    {
      using MemoryStream stream = new(bytes);
      using BsonBinaryReader reader = new(stream);

      document = BsonSerializer.Deserialize<BsonDocument>(reader);
    }

    if (!document.TryGetValue("type", out BsonValue type))
    {
      throw new InvalidDataException("Failed to deserialize");
    }

    document.Remove("type");
    return type.AsString switch
    {
      "request" => BsonSerializer.Deserialize<ConnectionPacket.Request>(document),
      "response" => BsonSerializer.Deserialize<ConnectionPacket.Response>(document),
      "error" => BsonSerializer.Deserialize<ConnectionPacket.Error>(document),

      _ => throw new InvalidDataException("Invalid type: " + type.AsString)
    };
  }

  private static byte[] SerializePacket<T>(T packet)
    where T : ConnectionPacket
  {
    BsonDocument document = packet.ToBsonDocument();

    document.Add(
      "type",
      BsonValue.Create(
        packet switch
        {
          ConnectionPacket.Request => "request",
          ConnectionPacket.Response => "response",
          ConnectionPacket.Error => "error",

          _ => throw new InvalidDataException("Invalid type: " + typeof(T).Name)
        }
      )
    );

    return document.ToBson();
  }
}
