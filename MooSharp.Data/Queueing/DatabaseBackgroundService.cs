using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data.Queueing;

internal sealed class DatabaseBackgroundService(
    ChannelReader<DatabaseRequest> reader,
    EfPlayerStore playerStore,
    EfWorldStore worldStore,
    ILogger<DatabaseBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                switch (request)
                {
                    case SaveNewPlayerRequest newPlayerRequest:
                        await playerStore.SaveNewPlayerAsync(newPlayerRequest.Player, stoppingToken);
                        break;
                    case SavePlayerRequest savePlayerRequest:
                        await playerStore.SavePlayerAsync(savePlayerRequest.Snapshot, stoppingToken);
                        break;
                    case SaveRoomRequest saveRoomRequest:
                        await worldStore.SaveRoomAsync(saveRoomRequest.Room, stoppingToken);
                        break;
                    case SaveRoomsRequest saveRoomsRequest:
                        await worldStore.SaveRoomsAsync(saveRoomsRequest.Rooms, stoppingToken);
                        break;
                    case SaveExitRequest saveExitRequest:
                        await worldStore.SaveExitAsync(saveExitRequest.FromRoomId, saveExitRequest.ToRoomId, string.Empty, stoppingToken);
                        break;
                    case UpdateRoomDescriptionRequest updateRoomDescriptionRequest:
                        await worldStore.UpdateRoomDescriptionAsync(
                            updateRoomDescriptionRequest.RoomId,
                            updateRoomDescriptionRequest.Description,
                            updateRoomDescriptionRequest.LongDescription,
                            stoppingToken);
                        break;
                    case RenameRoomRequest renameRoomRequest:
                        await worldStore.RenameRoomAsync(renameRoomRequest.RoomId, renameRoomRequest.Name, stoppingToken);
                        break;
                    case RenameObjectRequest renameObjectRequest:
                        await worldStore.RenameObjectAsync(renameObjectRequest.ObjectId, renameObjectRequest.Name, stoppingToken);
                        break;
                    default:
                        logger.LogWarning("Unhandled database request type {RequestType}", request.GetType().Name);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing database request {RequestType}", request.GetType().Name);
            }
        }
    }
}
