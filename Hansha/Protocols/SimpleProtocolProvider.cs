namespace Hansha
{
    public class SimpleProtocolProvider : IProtocolProvider
    {
        #region Implementation of IProtocolProvider

        public IProtocol GetProtocol(IProtocolStream protocolStream)
        {
            return new SimpleProtocol(protocolStream);
        }

        #endregion
    }
}