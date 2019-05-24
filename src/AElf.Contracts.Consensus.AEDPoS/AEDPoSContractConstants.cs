namespace AElf.Contracts.Consensus.AEDPoS
{
    public static class AEDPoSContractConstants
    {
        public const int TinyBlocksNumber = 8;
        private const int RemainSlots = 2;
        public const int TotalSlots = TinyBlocksNumber + RemainSlots;
    }
}