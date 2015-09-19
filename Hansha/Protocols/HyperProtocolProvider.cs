namespace Hansha
{
    public class HyperProtocolProvider : IProtocolProvider
    {
        #region Implementation of IProtocolProvider

        public IProtocol GetProtocol(IProtocolStream protocolStream)
        {
            return new HyperProtocol(protocolStream);
        }

        #endregion
    }
}