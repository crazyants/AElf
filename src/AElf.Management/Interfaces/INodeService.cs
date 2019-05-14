﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface INodeService
    {
        Task<List<NodeStateHistory>> GetHistoryState(string chainId);

        Task RecordBlockInfo(string chainId);
        
        Task RecordGetCurrentChainStatus(string chainId, DateTime time);
    }
}