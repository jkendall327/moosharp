using System.Data;
using Dapper;
using MooSharp.Actors;

namespace MooSharp.Persistence;

public class RoomIdTypeHandler : SqlMapper.TypeHandler<RoomId>
{
    public override RoomId Parse(object value) => new(Convert.ToString(value) ?? string.Empty);

    public override void SetValue(IDbDataParameter parameter, RoomId value)
    {
        parameter.Value = value.Value;
    }
}

public static class DapperTypeHandlerConfiguration
{
    private static bool _roomIdHandlerConfigured;

    public static void ConfigureRoomIdHandler()
    {
        if (_roomIdHandlerConfigured)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new RoomIdTypeHandler());
        _roomIdHandlerConfigured = true;
    }
}
