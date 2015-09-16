namespace Hansha
{
    public interface IProtocolProvider
    {
        IProtocol GetProtocol(IProtocolStream protocolStream);
    }
}