using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public sealed class CrossChainIndexingTransactionGeneratorTest : CrossChainWithChainTestBase
    {
        private readonly ISystemTransactionGenerator _crossChainIndexingTransactionGenerator;
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainIndexingTransactionGeneratorTest()
        {
            _crossChainIndexingTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
            _crossChainDataProvider = GetRequiredService<ICrossChainDataProvider>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }
        
        [Fact]
        public async Task GenerateTransactions_Test()
        {
            var transactions = new List<Transaction>();
            _crossChainIndexingTransactionGenerator.GenerateTransactions(Address.Zero,0,Hash.Empty, ref transactions);
            transactions.Count.ShouldBe(0);

            var chainId = _kernelTestHelper.BestBranchBlockList[0].Header.ChainId;
            var previousBlockHash = _kernelTestHelper.BestBranchBlockList[3].GetHash();
            var previousBlockHeight = _kernelTestHelper.BestBranchBlockList[3].Height;
            
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, previousBlockHeight);
            
            var blockInfoCache = new List<BlockCacheEntity>();
            var cachingCount = CrossChainConstants.MaximalCountForIndexingSideChainBlock;
            for (var i = 1; i <= cachingCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = previousBlockHeight + i,
                };
                blockInfoCache.Add(new BlockCacheEntity()
                {
                    Height = sideChainBlockData.SideChainHeight,
                    ChainId = sideChainBlockData.SideChainId,
                    Payload = sideChainBlockData.ToByteString()
                });
            }

            var fakeCache = new Dictionary<int, List<BlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var smartContractAddress = Address.FromString("CrossChainContract");

            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);

            await _crossChainDataProvider.GetCrossChainBlockDataForNextMiningAsync(previousBlockHash, previousBlockHeight);
            
            _crossChainIndexingTransactionGenerator.GenerateTransactions(Address.Zero,previousBlockHeight,previousBlockHash, ref transactions);
            
            transactions.Count.ShouldBe(1);
            transactions[0].From.ShouldBe(Address.Zero);
            transactions[0].To.ShouldBe(smartContractAddress);
            transactions[0].RefBlockNumber.ShouldBe(previousBlockHeight);
            transactions[0].RefBlockPrefix.ShouldBe(ByteString.CopyFrom(previousBlockHash.Value.Take(4).ToArray()));
            transactions[0].MethodName.ShouldBe(CrossChainConstants.CrossChainIndexingMethodName);
            
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(transactions[0].Params);
            crossChainBlockData.PreviousBlockHeight.ShouldBe(previousBlockHeight);
            crossChainBlockData.ParentChainBlockData.Count.ShouldBe(0);
            crossChainBlockData.SideChainBlockData.Count.ShouldBe(
                CrossChainConstants.MaximalCountForIndexingSideChainBlock -
                CrossChainConstants.MinimalBlockCacheEntityCount);
            crossChainBlockData.SideChainBlockData[0].SideChainId.ShouldBe(chainId);
            crossChainBlockData.SideChainBlockData[0].SideChainHeight.ShouldBe(previousBlockHeight + 1);
        }
    }
}