// namespace MooSharp;
//
// public interface ILoginManager
// {
//     Task<StreamBasedPlayerConnection?> AttemptLoginAsync(Stream stream, CancellationToken token = default);
// }
//
// public class LoginManager
// {
//     public Task<StreamBasedPlayerConnection?> AttemptLoginAsync(Stream stream, CancellationToken token = default)
//     {
//         var player = new Player
//         {
//             Username = "janedoe"
//         };
//         
//         var conn = new StreamBasedPlayerConnection(stream, player);
//
//         return Task.FromResult<StreamBasedPlayerConnection?>(conn);
//     }
// }