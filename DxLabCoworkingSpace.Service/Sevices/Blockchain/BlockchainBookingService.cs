using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace DxLabCoworkingSpace.Service
{
    public class BlockchainBookingService : IBlockchainBookingService
    {
        private readonly Web3 _web3;
        private readonly Contract _bookingContract;
        private readonly Contract _tokenContract;
        private readonly string _bookingContractAddress;
        private readonly string _tokenContractAddress;

        public BlockchainBookingService(IConfiguration configuration)
        {
            var privateKey = configuration.GetSection("PrivateKeyBlockchain")["PRIVATE_KEY"]
                ?? throw new ArgumentNullException("PrivateKeyBlockchain:PRIVATE_KEY not configured");

            _bookingContractAddress = configuration.GetSection("ContractAddresses:Sepolia")["Booking"]
                ?? throw new ArgumentNullException("ContractAddresses:Sepolia:Booking not configured");

            _tokenContractAddress = configuration.GetSection("ContractAddresses:Sepolia")["DXLABCoin"]
                ?? throw new ArgumentNullException("ContractAddresses:Sepolia:DXLABCoin not configured");

            var sepoliaRpcUrl = configuration.GetSection("Network")["ProviderCrawl"]
                ?? "https://sepolia.infura.io/v3/9d13fab540c243ca9514d4ab4fe7e9e1";

            string labBookingPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "Booking.json");
            string tokenPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "DXLABCoin.json");

            if (!File.Exists(labBookingPath))
                throw new FileNotFoundException($"Booking ABI file not found at {labBookingPath}");
            if (!File.Exists(tokenPath))
                throw new FileNotFoundException($"DXLABCoin ABI file not found at {tokenPath}");

            var labBookingJson = File.ReadAllText(labBookingPath);
            var tokenJson = File.ReadAllText(tokenPath);

            using var labBookingDoc = JsonDocument.Parse(labBookingJson);
            using var tokenDoc = JsonDocument.Parse(tokenJson);
            var labBookingAbi = labBookingDoc.RootElement.GetProperty("abi").GetRawText();
            var tokenAbi = tokenDoc.RootElement.GetProperty("abi").GetRawText();

            var account = new Nethereum.Web3.Accounts.Account(privateKey, 11155111); // Chain ID của Sepolia
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(sepoliaRpcUrl), httpClient);
            _web3 = new Web3(account, client);
            _bookingContract = _web3.Eth.GetContract(labBookingAbi, _bookingContractAddress);
            _tokenContract = _web3.Eth.GetContract(tokenAbi, _tokenContractAddress);
        }
        public async Task<(bool Success, string TransactionHash)> BookOnBlockchain(int bookingId, string userWalletAddress, byte slot, decimal totalPrice)
        {
            try
            {
                // Kiểm tra số dư token của user
                var userBalance = await GetUserBalance(userWalletAddress);
                var requiredTokens = Nethereum.Util.UnitConversion.Convert.ToWei(totalPrice);

                if (userBalance < requiredTokens)
                {
                    Console.WriteLine($"User {userWalletAddress} has insufficient balance: {userBalance} < {requiredTokens}");
                    return (false, null);
                }

                // Chuyển BookingId (int) thành bytes32
                var bookingIdBytes32 = "0x" + bookingId.ToString("X64"); // Chuyển thành hex 32 bytes

                // Gọi hàm book trên smart contract
                var bookFunction = _bookingContract.GetFunction("book");
                var gasEstimate = await bookFunction.EstimateGasAsync(userWalletAddress, null, null, slot);
                var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100);
                var gasPrice = new HexBigInteger(2000000000);

                var txHash = await bookFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    gasLimit,
                    gasPrice,
                    new HexBigInteger(0),
                    bookingIdBytes32, // Truyền bookingId dưới dạng bytes32
                    slot
                );

                // Chờ giao dịch được xác nhận
                var receipt = await WaitForReceipt(_web3, txHash);
                if (receipt?.Status.Value != 1)
                {
                    Console.WriteLine($"Transaction failed for booking {bookingId}: {(receipt == null ? "No receipt" : "Status = 0")}");
                    return (false, txHash);
                }

                Console.WriteLine($"Successfully booked on blockchain for booking {bookingId}, txHash: {txHash}");
                return (true, txHash);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error booking on blockchain for booking {bookingId}: {ex.Message}");
                return (false, null);
            }
        }

        public async Task<BigInteger> GetUserBalance(string walletAddress)
        {
            var balanceFunction = _tokenContract.GetFunction("balanceOf");
            var balance = await balanceFunction.CallAsync<BigInteger>(walletAddress);
            return balance;
        }

        private async Task<TransactionReceipt> WaitForReceipt(Web3 web3, string transactionHash)
        {
            if (string.IsNullOrEmpty(transactionHash))
                return null;

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            int attempts = 0;
            const int maxAttempts = 20;

            while (receipt == null && attempts < maxAttempts)
            {
                await Task.Delay(2000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                attempts++;
            }

            return receipt;
        }
    }
}
