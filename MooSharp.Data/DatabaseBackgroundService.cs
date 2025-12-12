using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooSharp.Data.Players;
using MooSharp.Data.Worlds;

namespace MooSharp.Data;

internal sealed class DatabaseBackgroundService(
    ChannelReader<DatabaseRequest> reader,
    IPlayerStore playerRepository,
    EfWorldRepository worldRepository,
    ILogger<DatabaseBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessRequest(request, stoppingToken);
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

    private async Task ProcessRequest(DatabaseRequest request, CancellationToken stoppingToken)
    {
        switch (request)
        {
            case SaveNewPlayerRequest newPlayerRequest:
                await playerRepository.SaveNewPlayerAsync(newPlayerRequest.Player, stoppingToken);
                break;
            case SavePlayerRequest savePlayerRequest:
                await playerRepository.SavePlayerAsync(savePlayerRequest.Snapshot, stoppingToken);
                break;
            case SaveRoomRequest saveRoomRequest:
                await worldRepository.SaveRoomAsync(saveRoomRequest.Room, stoppingToken);
                break;
            case SaveRoomsRequest saveRoomsRequest:
                await worldRepository.SaveRoomsAsync(saveRoomsRequest.Rooms, stoppingToken);
                break;
            case SaveExitRequest saveExitRequest:
                await worldRepository.SaveExitAsync(saveExitRequest.FromRoomId, saveExitRequest.Exit, stoppingToken);
                break;
            case UpdateRoomDescriptionRequest updateRoomDescriptionRequest:
                await worldRepository.UpdateRoomDescriptionAsync(
                    updateRoomDescriptionRequest.RoomId,
                    updateRoomDescriptionRequest.Description,
                    updateRoomDescriptionRequest.LongDescription,
                    stoppingToken);
                break;
            case RenameRoomRequest renameRoomRequest:
                await worldRepository.RenameRoomAsync(renameRoomRequest.RoomId, renameRoomRequest.Name, stoppingToken);
                break;
            case RenameObjectRequest renameObjectRequest:
                await worldRepository.RenameObjectAsync(renameObjectRequest.ObjectId, renameObjectRequest.Name, stoppingToken);
                break;
            case UpdatePlayerDescriptionRequest updatePlayerDescriptionRequest:
                await playerRepository.UpdatePlayerDescriptionAsync(
                    updatePlayerDescriptionRequest.PlayerId,
                    updatePlayerDescriptionRequest.Description,
                    stoppingToken);
                break;
            default:
                logger.LogWarning("Unhandled database request type {RequestType}", request.GetType().Name);
                break;
        }
    }
}
