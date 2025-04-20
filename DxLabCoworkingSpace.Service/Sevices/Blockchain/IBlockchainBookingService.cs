using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace DxLabCoworkingSpace.Service
{
    public interface IBlockchainBookingService
    {
        Task<(bool Success, string TransactionHash)> BookOnBlockchain(int bookingId, string userWalletAddress, byte slot, decimal totalPrice);
        Task<BigInteger> GetUserBalance(string walletAddress);
    }
}
