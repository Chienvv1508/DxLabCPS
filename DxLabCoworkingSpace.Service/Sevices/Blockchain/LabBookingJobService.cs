using DxLabCoworkingSpace;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;

public class LabBookingJobService : ILabBookingJobService
{
    private readonly ILabBookingCrawlerService _crawler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _privateKey;
    private readonly string _contractAddress;
    private readonly string _sepoliaRpcUrl;
    private readonly string _labBookingContractAbi;
    private readonly string _fptContractAbi;
    private Web3 _web3;
    private Contract _contract;
    // Thêm ConcurrentDictionary để lưu thời gian cấp ETH gần nhất
    private static readonly ConcurrentDictionary<string, DateTime> _ethGrantedUsers = new ConcurrentDictionary<string, DateTime>();

    public LabBookingJobService(
        ILabBookingCrawlerService crawler,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _crawler = crawler;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _privateKey = _configuration.GetSection("PrivateKeyBlockchain")["PRIVATE_KEY"]
            ?? throw new ArgumentNullException("PrivateKeyBlockchain:PRIVATE_KEY not configured");
        _contractAddress = _configuration.GetSection("ContractAddresses:Sepolia")["DXLABCoin"]
            ?? throw new ArgumentNullException("ContractAddresses:Sepolia:DXLABCoin not configured");
        _sepoliaRpcUrl = _configuration.GetSection("Network")["ProviderCrawl"]
            ?? "https://sepolia.infura.io/v3/ce5f177778e547a19055596b216fd743";

        string labBookingPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "Booking.json");
        string fptPath = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "DXLABCoin.json");

        if (!File.Exists(labBookingPath))
            throw new FileNotFoundException($"Booking ABI file not found at {labBookingPath}");
        if (!File.Exists(fptPath))
            throw new FileNotFoundException($"DXLABCoin ABI file not found at {fptPath}");

        var labBookingJson = File.ReadAllText(labBookingPath);
        var fptJson = File.ReadAllText(fptPath);

        using var labBookingDoc = JsonDocument.Parse(labBookingJson);
        using var fptDoc = JsonDocument.Parse(fptJson);
        _labBookingContractAbi = labBookingDoc.RootElement.GetProperty("abi").GetRawText();
        _fptContractAbi = fptDoc.RootElement.GetProperty("abi").GetRawText();

        var account = new Nethereum.Web3.Accounts.Account(_privateKey, 11155111);
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(300) }; // Tăng timeout lên 300s
        var client = new RpcClient(new Uri(_sepoliaRpcUrl), httpClient);
        _web3 = new Web3(account, client);
        _contract = _web3.Eth.GetContract(_fptContractAbi, _contractAddress);
    }

    public void ScheduleJob()
    {
        RecurringJob.AddOrUpdate(
            "booking-log-job",
            () => RunBookingLogJobAsync(),
            "*/5 * * * *",
            TimeZoneInfo.Local
        );

        Console.WriteLine("Booking log job was scheduled!");
    }

    public async Task RunBookingLogJobAsync()
    {
        Console.WriteLine("Starting to crawl booking logs...");

        try
        {
            var lastBlockRecord = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "Booking");
            int fromBlock = lastBlockRecord != null ? int.Parse(lastBlockRecord.LastBlock) : 0;
            var latestBlock = await _crawler.GetLatestBlockNumberAsync();

            if (!latestBlock.HasValue)
            {
                Console.WriteLine("Could not get latest block.");
                return;
            }

            int toBlock = (int)latestBlock.Value;
            const int initialBatchSize = 2000000;
            const int maxRetry = 3;

            for (int i = fromBlock; i <= toBlock; i += initialBatchSize)
            {
                int batchFrom = i;
                int batchTo = Math.Min(i + initialBatchSize - 1, toBlock);

                Console.WriteLine($"Crawling from block {batchFrom} to {batchTo}...");

                for (int retry = 1; retry <= maxRetry; retry++)
                {
                    try
                    {
                        await _crawler.CrawlBookingEventsAsync(batchFrom, batchTo);
                        break;
                    }
                    catch (RpcResponseException rpcEx)
                    {
                        Console.WriteLine($"RPC error (retry {retry}/{maxRetry}) for blocks {batchFrom}-{batchTo}: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                        if (retry == maxRetry)
                        {
                            Console.WriteLine($"Skip batch {batchFrom}-{batchTo} after {maxRetry} tries.");
                        }
                        await Task.Delay(5000 * retry); // Tăng thời gian chờ lên 5000ms
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error from block {batchFrom} to {batchTo}: {ex.Message}");
                        break;
                    }
                }
            }

            var contractCrawl = await _unitOfWork.ContractCrawlRepository.Get(c => c.ContractName == "Booking");
            if (contractCrawl != null)
            {
                contractCrawl.LastBlock = latestBlock.Value.ToString();
                await _unitOfWork.CommitAsync();
                Console.WriteLine($"Updated LastBlock: {latestBlock.Value}");
            }
            else
            {
                await _unitOfWork.ContractCrawlRepository.Add(new ContractCrawl
                {
                    ContractName = "Booking",
                    LastBlock = latestBlock.Value.ToString()
                });
                await _unitOfWork.CommitAsync();
                Console.WriteLine($"Created new ContractCrawl with LastBlock: {latestBlock.Value}");
            }

            Console.WriteLine("Crawl successfully!");
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error occurred while crawling booking logs: {error.Message}\nStackTrace: {error.StackTrace}");
        }
    }

    public async Task ExecuteMintingJob()
    {
        Console.WriteLine("ExecuteMintingJob is no longer used for minting tokens.");
    }

    public async Task<bool> MintTokenForUser(string walletAddress)
    {
        if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(walletAddress))
        {
            Console.WriteLine($"Invalid wallet address: {walletAddress}");
            return false;
        }

        const int maxRetries = 10; // Tăng số lần thử lại lên 10
        var amountToMint = Nethereum.Util.UnitConversion.Convert.ToWei(100m); // 100 token
        var mintFunction = _contract.GetFunction("mintForUser");

        // Kiểm tra chain ID
        var chainId = new HexBigInteger(0);
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                chainId = await _web3.Eth.ChainId.SendRequestAsync();
                Console.WriteLine($"Current chain ID: {chainId}");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_chainId: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_chainId. Aborting minting.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_chainId: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_chainId. Aborting minting.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (chainId.Value != 11155111)
        {
            Console.WriteLine($"Chain ID mismatch! Expected 11155111 (Sepolia), but got {chainId.Value}. Aborting minting.");
            return false;
        }

        // Kiểm tra số dư ETH của ví owner
        decimal ethBalance = 0;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(_web3.TransactionManager.Account.Address);
                ethBalance = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                Console.WriteLine($"ETH Balance of sender (owner) {_web3.TransactionManager.Account.Address}: {ethBalance} ETH");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getBalance: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getBalance. Aborting minting.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getBalance: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getBalance. Aborting minting.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (ethBalance < 0.01m)
        {
            Console.WriteLine("Insufficient ETH balance for transactions! At least 0.01 ETH is required.");
            return false;
        }

        // Thêm logic cấp ETH cho user
        decimal userEthBalanceInEth = 0;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var userEthBalance = await _web3.Eth.GetBalance.SendRequestAsync(walletAddress);
                userEthBalanceInEth = Nethereum.Util.UnitConversion.Convert.FromWei(userEthBalance.Value);
                Console.WriteLine($"ETH Balance of user {walletAddress}: {userEthBalanceInEth} ETH");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getBalance (user): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getBalance (user). Proceeding with minting without granting ETH.");
                    break;
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getBalance (user): {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getBalance (user). Proceeding with minting without granting ETH.");
                    break;
                }
                await Task.Delay(5000 * retry);
            }
        }

        // Kiểm tra thời gian cấp ETH gần nhất
        DateTime currentTime = DateTime.UtcNow;
        bool shouldGrantEth = true;
        if (_ethGrantedUsers.TryGetValue(walletAddress, out var lastEthGrantedTime))
        {
            if ((currentTime - lastEthGrantedTime).TotalDays < 1)
            {
                shouldGrantEth = false;
                Console.WriteLine($"ETH not granted to user {walletAddress}. Last granted: {lastEthGrantedTime}.");
            }
        }

        if (shouldGrantEth && userEthBalanceInEth < 0.005m)
        {
            Console.WriteLine($"User {walletAddress} has low ETH balance ({userEthBalanceInEth} ETH). Sending 0.01 ETH...");

            // Lấy nonce cho giao dịch gửi ETH
            HexBigInteger ethNonce = null;
            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    ethNonce = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                        _web3.TransactionManager.Account.Address,
                        BlockParameter.CreatePending()
                    );
                    Console.WriteLine($"Current nonce for sender (owner) {_web3.TransactionManager.Account.Address} (ETH grant): {ethNonce.Value}");
                    break;
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getTransactionCount (ETH grant): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine("Max retries reached for eth_getTransactionCount (ETH grant). Proceeding with minting without granting ETH.");
                        break;
                    }
                    await Task.Delay(5000 * retry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getTransactionCount (ETH grant): {ex.Message}\nStackTrace: {ex.StackTrace}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine("Max retries reached for eth_getTransactionCount (ETH grant). Proceeding with minting without granting ETH.");
                        break;
                    }
                    await Task.Delay(5000 * retry);
                }
            }

            if (ethNonce != null)
            {
                bool ethSent = false;
                var ethToSend = Nethereum.Util.UnitConversion.Convert.ToWei(0.01m); // 0.01 ETH
                for (int retry = 1; retry <= maxRetries && !ethSent; retry++)
                {
                    try
                    {
                        var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                        var adjustedGasPrice = new HexBigInteger(gasPrice.Value * 120 / 100); // Tăng 20% để ưu tiên giao dịch

                        var transactionInput = new TransactionInput
                        {
                            From = _web3.TransactionManager.Account.Address,
                            To = walletAddress,
                            Value = new HexBigInteger(ethToSend),
                            GasPrice = adjustedGasPrice,
                            Gas = new HexBigInteger(21000), // Gas limit cho giao dịch gửi ETH
                            Nonce = ethNonce
                        };

                        var signedTx = await _web3.TransactionManager.SignTransactionAsync(transactionInput);
                        var ethTxHash = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx); // Sửa tên biến thành ethTxHash

                        Console.WriteLine($"ETH transaction sent: Sender (owner) {_web3.TransactionManager.Account.Address} sent 0.01 ETH to user {walletAddress}. TxHash: {ethTxHash}");

                        var receipt = await WaitForReceipt(_web3, ethTxHash);
                        if (receipt?.Status.Value == 1)
                        {
                            Console.WriteLine($"Successfully sent 0.01 ETH to user {walletAddress}. TxHash: {ethTxHash}");
                            ethSent = true;
                            _ethGrantedUsers[walletAddress] = currentTime; // Cập nhật thời gian cấp ETH
                        }
                        else
                        {
                            Console.WriteLine($"Failed to send ETH to user {walletAddress}: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                        }
                    }
                    catch (RpcResponseException rpcEx)
                    {
                        Console.WriteLine($"RPC Error for user {walletAddress} (retry {retry}/{maxRetries}) (ETH grant): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                        if (rpcEx.Message.Contains("replacement transaction underpriced"))
                        {
                            ethNonce = new HexBigInteger(ethNonce.Value + 1);
                            Console.WriteLine($"Incrementing nonce to {ethNonce.Value} due to underpriced replacement transaction (ETH grant).");
                        }
                        else if (retry == maxRetries)
                        {
                            Console.WriteLine($"Max retries reached for user {walletAddress} (ETH grant). Proceeding with minting without granting ETH.");
                        }
                        await Task.Delay(5000 * retry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error for user {walletAddress} (retry {retry}/{maxRetries}) (ETH grant): {ex.Message}\nStackTrace: {ex.StackTrace}");
                        if (retry == maxRetries)
                        {
                            Console.WriteLine($"Max retries reached for user {walletAddress} (ETH grant). Proceeding with minting without granting ETH.");
                        }
                        await Task.Delay(5000 * retry);
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"User {walletAddress} has sufficient ETH balance ({userEthBalanceInEth} ETH) or ETH was granted recently. No ETH granted.");
        }

        // Kiểm tra owner của hợp đồng
        var contractOwner = await GetContractOwnerAsync();
        if (string.IsNullOrEmpty(contractOwner))
        {
            Console.WriteLine("Unable to retrieve contract owner. Aborting minting.");
            return false;
        }

        var callerAddress = _web3.TransactionManager.Account.Address.ToLower();
        if (contractOwner != callerAddress)
        {
            Console.WriteLine($"Caller {callerAddress} is not the contract owner ({contractOwner}). Aborting minting.");
            return false;
        }
        else
        {
            Console.WriteLine($"Caller {callerAddress} is the contract owner. Proceeding to mint tokens for user {walletAddress}.");
        }

        // Kiểm tra tổng cung và số dư của ví
        var totalSupply = await GetTotalSupplyAsync();
        if (totalSupply == BigInteger.MinusOne)
        {
            Console.WriteLine("Unable to retrieve total supply. Proceeding with caution.");
        }
        else
        {
            Console.WriteLine($"Current total supply: {Nethereum.Util.UnitConversion.Convert.FromWei(totalSupply)} tokens");
        }

        var userBalance = await GetBalanceOfAsync(walletAddress);
        if (userBalance == BigInteger.MinusOne)
        {
            Console.WriteLine($"Unable to retrieve balance of {walletAddress}. Proceeding with caution.");
        }
        else
        {
            Console.WriteLine($"Current balance of {walletAddress}: {Nethereum.Util.UnitConversion.Convert.FromWei(userBalance)} tokens");
        }

        // Lấy nonce
        HexBigInteger nonce = null;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                nonce = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                    _web3.TransactionManager.Account.Address,
                    BlockParameter.CreatePending()
                );
                Console.WriteLine($"Current nonce for sender (owner) {_web3.TransactionManager.Account.Address}: {nonce.Value}");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting minting.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting minting.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (nonce == null)
        {
            Console.WriteLine("Failed to retrieve nonce. Aborting minting.");
            return false;
        }

        // Hủy các giao dịch đang chờ nếu có
        await CancelPendingTransactionsIfAny();

        // Thực hiện mint
        bool minted = false;
        string txHash = null;
        for (int retry = 1; retry <= maxRetries && !minted; retry++)
        {
            try
            {
                Console.WriteLine($"Processing mint for user {walletAddress} (attempt {retry}/{maxRetries}) using sender (owner) {_web3.TransactionManager.Account.Address}...");

                // Ước lượng gas
                var gasEstimate = await mintFunction.EstimateGasAsync(
                    _web3.TransactionManager.Account.Address,
                    null,
                    new HexBigInteger(0), // Không gửi ETH
                    walletAddress,
                    amountToMint);

                var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100); // Tăng 20% để tránh lỗi out-of-gas
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                var adjustedGasPrice = new HexBigInteger(gasPrice.Value * 120 / 100); // Tăng 20% để ưu tiên giao dịch
                Console.WriteLine($"Using gas price: {adjustedGasPrice.Value} Wei, Gas limit: {gasLimit.Value}");

                // Tạo TransactionInput
                var txInput = new TransactionInput(
                    mintFunction.GetData(walletAddress, amountToMint),
                    _contractAddress,
                    _web3.TransactionManager.Account.Address,
                    gasLimit,
                    adjustedGasPrice,
                    new HexBigInteger(0) // Đặt value = 0 để không gửi ETH
                )
                {
                    Nonce = nonce
                };

                // Ký giao dịch cục bộ
                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                Console.WriteLine($"Transaction signed locally: {signedTx}");

                // Gửi giao dịch đã ký bằng eth_sendRawTransaction
                txHash = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);

                Console.WriteLine($"Transaction sent: Sender (owner) {_web3.TransactionManager.Account.Address} minting for user {walletAddress}. TxHash: {txHash}");

                // Chờ receipt
                var receipt = await WaitForReceipt(_web3, txHash);
                if (receipt?.Status.Value == 1)
                {
                    Console.WriteLine($"Minted successfully: Sender (owner) {_web3.TransactionManager.Account.Address} minted for user {walletAddress}");
                    minted = true;
                }
                else
                {
                    Console.WriteLine($"Mint failed for user {walletAddress}: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                    try
                    {
                        var callInput = new TransactionInput(
                            mintFunction.GetData(walletAddress, amountToMint),
                            _contractAddress,
                            _web3.TransactionManager.Account.Address,
                            gasLimit,
                            adjustedGasPrice,
                            new HexBigInteger(0)
                        );

                        var error = await _web3.Eth.Transactions.Call.SendRequestAsync(callInput, BlockParameter.CreateLatest());
                        if (string.IsNullOrEmpty(error) || error == "0x")
                        {
                            Console.WriteLine("Revert reason not available or empty. Check the contract logic on Remix for more details.");
                        }
                        else
                        {
                            var revertMessage = new FunctionCallDecoder().DecodeFunctionErrorMessage(error);
                            Console.WriteLine($"Revert reason: {revertMessage}");
                        }
                    }
                    catch (RpcResponseException rpcEx)
                    {
                        Console.WriteLine($"Failed to retrieve revert reason: execution reverted - {rpcEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to retrieve revert reason: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                    return false;
                }
            }
            catch (SmartContractRevertException revertEx)
            {
                Console.WriteLine($"SmartContractRevertException for user {walletAddress}: {revertEx.RevertMessage}");
                return false;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error for user {walletAddress} (retry {retry}/{maxRetries}): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, URL: {_sepoliaRpcUrl})");
                if (rpcEx.Message.Contains("replacement transaction underpriced"))
                {
                    nonce = new HexBigInteger(nonce.Value + 1);
                    Console.WriteLine($"Incrementing nonce to {nonce.Value} due to underpriced replacement transaction.");
                }
                else if (retry == maxRetries)
                {
                    Console.WriteLine($"Max retries reached for user {walletAddress}. Skipping.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error for user {walletAddress} (retry {retry}/{maxRetries}): {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine($"Max retries reached for user {walletAddress}. Skipping.");
                    return false;
                }
                await Task.Delay(5000 * retry);
            }
        }

        return minted;  
    }

    private async Task CancelPendingTransactionsIfAny()
    {
        try
        {
            var pendingTxCount = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                _web3.TransactionManager.Account.Address,
                BlockParameter.CreatePending()
            );
            var confirmedTxCount = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                _web3.TransactionManager.Account.Address,
                BlockParameter.CreateLatest()
            );

            if (pendingTxCount.Value <= confirmedTxCount.Value)
            {
                Console.WriteLine($"No pending transactions found for {_web3.TransactionManager.Account.Address}.");
                return;
            }

            Console.WriteLine($"Found {pendingTxCount.Value - confirmedTxCount.Value} pending transactions for {_web3.TransactionManager.Account.Address}. Attempting to cancel...");

            for (BigInteger nonce = confirmedTxCount.Value; nonce < pendingTxCount.Value; nonce++)
            {
                var txInput = new TransactionInput
                {
                    From = _web3.TransactionManager.Account.Address,
                    To = _web3.TransactionManager.Account.Address,
                    Value = new HexBigInteger(0),
                    GasPrice = new HexBigInteger(40000000000),
                    Gas = new HexBigInteger(21000),
                    Nonce = new HexBigInteger(nonce)
                };

                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                var cancelTxHash = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);

                Console.WriteLine($"Cancel transaction sent for nonce {nonce}: {cancelTxHash}");

                var receipt = await WaitForReceipt(_web3, cancelTxHash);
                if (receipt?.Status.Value == 1)
                {
                    Console.WriteLine($"Cancel transaction {cancelTxHash} confirmed.");
                }
                else
                {
                    Console.WriteLine($"Cancel transaction {cancelTxHash} failed: {(receipt == null ? "No receipt" : "Status = 0")}");
                }

                await Task.Delay(5000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/cancelling pending transactions: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    private async Task<string> GetContractOwnerAsync()
    {
        try
        {
            var ownerFunction = _contract.GetFunction("owner");
            var ownerAddress = await ownerFunction.CallAsync<string>();
            Console.WriteLine($"Contract owner retrieved: {ownerAddress}");
            return ownerAddress?.ToLower();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving contract owner: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return null;
        }
    }

    private async Task<BigInteger> GetTotalSupplyAsync()
    {
        try
        {
            var totalSupplyFunction = _contract.GetFunction("totalSupply");
            var totalSupply = await totalSupplyFunction.CallAsync<BigInteger>();
            Console.WriteLine($"Total supply: {totalSupply}");
            return totalSupply;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving total supply: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return BigInteger.MinusOne;
        }
    }

    private async Task<BigInteger> GetBalanceOfAsync(string walletAddress)
    {
        try
        {
            var balanceOfFunction = _contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(walletAddress);
            Console.WriteLine($"Balance of {walletAddress}: {balance}");
            return balance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving balance of {walletAddress}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return BigInteger.MinusOne;
        }
    }

    private async Task<TransactionReceipt> WaitForReceipt(Web3 web3, string transactionHash)
    {
        if (string.IsNullOrEmpty(transactionHash))
        {
            Console.WriteLine("Transaction hash is empty. Cannot wait for receipt.");
            return null;
        }

        var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
        int attempts = 0;
        const int maxAttempts = 60;

        while (receipt == null && attempts < maxAttempts)
        {
            await Task.Delay(2000);
            receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            attempts++;
            Console.WriteLine($"Waiting for receipt of transaction {transactionHash} (attempt {attempts}/{maxAttempts})...");
        }

        if (receipt == null)
        {
            Console.WriteLine($"Failed to get receipt for transaction {transactionHash} after {maxAttempts} attempts.");
            return null;
        }

        return receipt;
    }
}

public class ContractAddresses
{
    public string Booking { get; set; }
    public string DXLABCoin { get; set; }
}