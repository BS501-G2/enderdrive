using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Google.Apis.Services;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Text.Json;
using Commons.Collections;
using Commons.Services;
using Core;
using Resources;

public sealed record class GoogleContext
{
  public required WaitQueue<GoogleService.Feed> Feed;
}

public sealed partial class GoogleService(EnderDriveServer server)
  : Service<GoogleContext>("Google API", server)
{
  public EnderDriveServer Server => server;
  public ResourceManager Resources => Server.Resources;

  public abstract record Feed
  {
    private Feed() { }

    public sealed record GetPayload(TaskCompletionSource<byte[]> TaskCompletionSource, string Token)
      : Feed();

    public sealed record GetAccountInfo(
      TaskCompletionSource<GoogleAccountInfo> TaskCompletionSource,
      string Token
    ) : Feed();
  }

  protected override Task<GoogleContext> OnStart(
    CancellationToken startupCancellationToken,
    CancellationToken serviceCancellationToken
  )
  {
    BaseClientService.Initializer baseClientService =
      new() { ApiKey = "", ApplicationName = "EnderDrive", };

    return Task.FromResult<GoogleContext>(new() { Feed = new() });
  }

  protected override async Task OnRun(
    GoogleContext context,
    CancellationToken serviceCancellationToken
  )
  {
    await foreach (Feed feed in context.Feed.WithCancellation(serviceCancellationToken))
    {
      switch (feed)
      {
        case Feed.GetPayload(TaskCompletionSource<byte[]> source, string token):
        {
          try
          {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
              token,
              new() { }
            );

            source.SetResult(Encoding.UTF8.GetBytes(payload.Subject));
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }

          break;
        }

        case Feed.GetAccountInfo(TaskCompletionSource<GoogleAccountInfo> source, string token):
        {
          try
          {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
              token,
              new() { }
            );

            source.SetResult(new() { Name = payload.Name, Email = payload.Email });
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }

          break;
        }
      }
    }
  }

  public async Task<byte[]> GetPayload(string token, CancellationToken cancellationToken)
  {
    TaskCompletionSource<byte[]> source = new();

    await GetContext().Feed.Enqueue(new Feed.GetPayload(source, token), cancellationToken);

    return await source.Task;
  }

  public async Task<GoogleAccountInfo> GetAccountInfo(
    string token,
    CancellationToken cancellationToken
  )
  {
    TaskCompletionSource<GoogleAccountInfo> source = new();

    await GetContext().Feed.Enqueue(new Feed.GetAccountInfo(source, token), cancellationToken);

    return await source.Task;
  }
}

public sealed record GoogleAccountInfo
{
  public required string Name;
  public required string Email;
}
