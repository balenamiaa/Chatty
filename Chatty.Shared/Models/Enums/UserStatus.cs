using MessagePack;

namespace Chatty.Shared.Models.Enums;

public enum UserStatus
{
    [Key(0)] Offline = 0,

    [Key(1)] Online = 1,

    [Key(2)] Idle = 2,

    [Key(3)] DoNotDisturb = 3,

    [Key(4)] Invisible = 4
}
