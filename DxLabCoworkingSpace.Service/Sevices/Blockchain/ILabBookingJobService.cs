using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ILabBookingJobService
    {
        void ScheduleJob();
        Task RunBookingLogJobAsync();
        Task ExecuteMintingJob();
        Task<bool> MintTokenForUser(string walletAddress);
        //Task<bool> PayForBooking(string walletAddress, decimal amount);

    }
}
