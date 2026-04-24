namespace Vortex.Protocol.Enums;

public enum PacketType : byte
{
    ClientAuth,
    ClientAuthResponse,

    ClientIdentity,
    ClientIdentityResponse,

    GenerateWorldMap,
    GenerateWorldMapResponse,

    BroadcastMessage,
    BroadcastMessageResponse,
    PrivateMessage,
    PrivateMessageResponse,

    ExecuteCommand,
    ExecuteCommandResponse,

    QueryPlayerInventory,
    QueryPlayerInventoryResponse,
    RegisterAccount,
    RegisterAccountResponse,
    ResetPlayerPassword,
    ResetPlayerPasswordResponse,
    QueryAccount,
    QueryAccountResponse,
    ExportPlayer,
    ExportPlayerResponse,

    ResetServer,
    ResetServerResponse,
    RestartServer,
    RestartServerResponse,
    GetServerStatus,
    GetServerStatusResponse,

    GetGameProgress,
    GetGameProgressResponse,
    GetDeathRank,
    GetDeathRankResponse,
    GetOnlineRank,
    GetOnlineRankResponse,
    GetServerOnline,
    GetServerOnlineResponse,
    GetPlayerStrikeBoss,
    GetPlayerStrikeBossResponse,

    GetMapImage,
    GetMapImageResponse,
    UploadWorldFile,
    UploadWorldFileResponse,

    SocketConnectStatus,
    SocketConnectStatusResponse,

    PlayerMessage,
    PlayerLeave,
    PlayerJoin,
    HeartBeat
}
