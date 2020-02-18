namespace fnbot.shop.Backend
{
    public enum PostResponse : byte
    {
        SUCCESS,
        UNAUTHORIZED,
        TIMEOUT,
        SERVER_ERROR,
        UNSUPPORTED_TYPE,
        UNSUPPORTED_CONSTRAINTS,

        UNKNOWN = 0xFF
    }
}
