using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.Consensus.DPoS;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.OS;
using AElf.Runtime.CSharp;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;
using AElf.Kernel.Consensus.AElfConsensus.Application;

namespace AElf.Kernel.Consensus.AElfConsensus
{
    [DependsOn(
        typeof(KernelCoreTestAElfModule),
        typeof(AElfConsensusAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoSConsensusTestAElfModule : TestBaseAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //Account service
            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelpers.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });
            context.Services.AddTransient(builder =>
            {
                var consensusService = new Mock<IConsensusService>();
                consensusService.Setup(m=>m.ValidateConsensusBeforeExecutionAsync(It.IsAny<ChainContext>(),
                        It.IsAny<byte[]>()))
                    .Returns(Task.FromResult(true));
                consensusService.Setup(m=>m.ValidateConsensusAfterExecutionAsync(It.IsAny<ChainContext>(),
                        It.IsAny<byte[]>()))
                    .Returns(Task.FromResult(true));
                
                return consensusService.Object;
            });
            context.Services.AddTransient(o => Mock.Of<ConsensusControlInformation>());
            Configure<ConsensusOptions>(o =>
            {
                o.InitialMiners = new List<string>()
                {
                    ecKeyPair.PublicKey.ToHex()
                };
                o.InitialTermNumber = 1;
                o.MiningInterval = 2000;
            });
            context.Services.AddTransient<IBlockExtraDataProvider, ConsensusExtraDataProvider>();
            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(m=>m.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(Address.Generate);

                return mockService.Object;
            });
            context.Services.AddTransient(o=>
            {
                var mockService = new Mock<ITransactionReadOnlyExecutionService>();
                mockService.Setup(m=>m.ExecuteAsync(It.IsAny<ChainContext>(), It.IsAny<Transaction>(),It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(new TransactionTrace
                    {
                        ExecutionStatus = ExecutionStatus.Executed,
                        ReturnValue = new DPoSHeaderInformation
                        {
                            Behaviour = DPoSBehaviour.UpdateValue,
                            Round = new Round(),
                            SenderPublicKey = ByteString.CopyFromUtf8("test")
                        }.ToByteString() 
                    }));

                return mockService.Object;
            });
            context.Services.AddTransient<IConsensusInformationGenerationService, AElfConsensusInformationGenerationService>();
        }
    }
    
    [DependsOn(
        typeof(KernelTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule),
        typeof(AElfConsensusAElfModule))]
    public class LibTestModule : AElfModule
    {
    }
}