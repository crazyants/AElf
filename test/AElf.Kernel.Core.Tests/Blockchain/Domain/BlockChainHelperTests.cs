using AElf.Kernel.Blockchain.Helpers;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Domain
{
    public class BlockChainHelperTests : AElfKernelTestBase
    {
        [Fact]
        public void GenesisBlockBuilder_Test()
        {
            var builder = new GenesisBlockBuilder();
            builder = builder.Build(0);
            
            builder.Block.Header.Height.ShouldBe(Constants.GenesisBlockHeight);
            builder.Block.Header.PreviousBlockHash.ShouldBe(Hash.Empty);
            builder.Block.Header.ChainId.ShouldBe(0);
            
            builder.Block.Body.BlockHeader.ShouldBe(builder.Block.Header.GetHash());
        }
    }
}