using Microsoft.Extensions.Configuration;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices.Blockchain
{
    public class UserTokenService : IUserTokenService
    {
        private readonly Web3 _web3;
        private readonly string _labBookingContractAddress;
        private readonly string _fptContractAddress;
        private readonly string _labBookingContractAbi;
        private readonly string _fptContractAbi;

        public UserTokenService(IConfiguration configuration)
        {
            var privateKey = configuration["PrivateKeyBlockchain:PRIVATE_KEY"];
            var rpcUrl = configuration["Network:providerCrawl"] ;
            _labBookingContractAddress = configuration["ContractAddresses:Sepolia:LabBookingSystem"];
            _fptContractAddress = configuration["ContractAddresses:Sepolia:FPTCurrency"];

            string labBookingPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "LabBookingSystem.json");
            string fptPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "FPTCurrency.json");

            if (!File.Exists(labBookingPath))
                throw new FileNotFoundException($"LabBookingSystem ABI file not found at {labBookingPath}");
            if (!File.Exists(fptPath))
                throw new FileNotFoundException($"FPTCurrency ABI file not found at {fptPath}");

            // Đọc và trích xuất ABI từ file
            var labBookingJson = File.ReadAllText(labBookingPath);
            var fptJson = File.ReadAllText(fptPath);

            // Parse JSON và lấy phần abi
            using var labBookingDoc = JsonDocument.Parse(labBookingJson);
            using var fptDoc = JsonDocument.Parse(fptJson);

            _labBookingContractAbi = labBookingDoc.RootElement.GetProperty("abi").GetRawText();
            _fptContractAbi = fptDoc.RootElement.GetProperty("abi").GetRawText();

            var account = new Account(privateKey);
            _web3 = new Web3(account, rpcUrl);
            Console.WriteLine($"Initialized Web3 with account: {account.Address}");
        }

        public async Task<string> RegisterUserAsync(string userAddress, string email, bool isStaff)
        {
            try
            {
                Console.WriteLine($"Calling registerUser for {userAddress}, email: {email}, isStaff: {isStaff}");
                var contract = _web3.Eth.GetContract(_labBookingContractAbi, _labBookingContractAddress);
                var registerUserFunction = contract.GetFunction("registerUser");
                var gas = new HexBigInteger(300000);
                var transactionHash = await registerUserFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    gas,
                    new HexBigInteger(0),
                    userAddress,
                    email,
                    isStaff
                );
                Console.WriteLine($"Register transaction sent: {transactionHash}");

                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                while (receipt == null)
                {
                    await Task.Delay(1000);
                    receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                }

                if (receipt.Status.Value == 1)
                {
                    Console.WriteLine("Register transaction confirmed");
                    //Minttoken from new user
                    return transactionHash;
                }
                else
                {
                    Console.WriteLine($"Register transaction failed: Status {receipt.Status}");
                    throw new Exception("Register transaction failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RegisterUserAsync: {ex.Message}");
                throw new Exception($"Error registering user on blockchain: {ex.Message}");
            }
        }

        public async Task<string> GrantTokenAsync(string userAddress, BigInteger amount)
        {
            try
            {
                Console.WriteLine($"Granting {amount} FPT to {userAddress}");
                var contract = _web3.Eth.GetContract(_fptContractAbi, _fptContractAddress);
                var transferFunction = contract.GetFunction("transfer");
                var gas = new HexBigInteger(100000);
                var transactionHash = await transferFunction.SendTransactionAsync(
                    _web3.TransactionManager.Account.Address,
                    gas,
                    new HexBigInteger(0),
                    userAddress,
                    amount
                );
                Console.WriteLine($"Grant transaction sent: {transactionHash}");

                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                while (receipt == null)
                {
                    await Task.Delay(1000);
                    receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                }

                if (receipt.Status.Value == 1)
                {
                    Console.WriteLine("Grant transaction confirmed");
                    return transactionHash;
                }
                else
                {
                    Console.WriteLine($"Grant transaction failed: Status {receipt.Status}");
                    throw new Exception("Grant transaction failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GrantTokenAsync: {ex.Message}");
                throw new Exception($"Error granting token: {ex.Message}");
            }
        }

        public async Task<BigInteger> GetFptBalanceAsync(string userAddress)
        {
            try
            {
                var contract = _web3.Eth.GetContract(_fptContractAbi, _fptContractAddress);
                var balanceOfFunction = contract.GetFunction("balanceOf");
                var balance = await balanceOfFunction.CallAsync<BigInteger>(userAddress);
                Console.WriteLine($"FPT Balance for {userAddress}: {balance}");
                return balance;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFptBalanceAsync: {ex.Message}");
                throw new Exception($"Error getting FPT balance: {ex.Message}");
            }
        }
    }
}