namespace GameSpyEmulator
{
    internal enum MessageType : byte
    {
        CHALLENGE_RESPONSE = 0x01,
        HEARTBEAT = 0x03,
        KEEPALIVE = 0x08,
        AVAILABLE = 0x09,
        RESPONSE_CORRECT = 0x0A
    }
}
