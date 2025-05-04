using DxLabCoworkingSpace.Service;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class BlockchainBookingService : IBlockchainBookingService
{
    private readonly Web3 _web3;
    private readonly Contract _bookingContract;
    private readonly Contract _tokenContract;
    private readonly string _bookingContractAddress;
    private readonly string _tokenContractAddress;
    private BigInteger _currentNonce;

    public BlockchainBookingService(IConfiguration configuration)
    {
        var privateKey = configuration.GetSection("PrivateKeyBlockchain")["PRIVATE_KEY"]
            ?? throw new ArgumentNullException("PrivateKeyBlockchain:PRIVATE_KEY not configured");

        _bookingContractAddress = configuration.GetSection("ContractAddresses:Sepolia")["Booking"]
            ?? throw new ArgumentNullException("ContractAddresses:Sepolia:Booking not configured");

        _tokenContractAddress = configuration.GetSection("ContractAddresses:Sepolia")["DXLABCoin"]
            ?? throw new ArgumentNullException("ContractAddresses:Sepolia:DXLABCoin not configured");

        var sepoliaRpcUrl = configuration.GetSection("Network")["ProviderCrawl"]
            ?? "https://sepolia.infura.io/v3/ce5f177778e547a19055596b216fd743";

        string labBookingPath = Path.Combine(Directory.GetCurrentDirectory(), "contracts", "Booking.json");
        string tokenPath = Path.Combine(Directory.GetCurrentDirectory(), "contracts", "DXLABCoin.json");

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

        var account = new Nethereum.Web3.Accounts.Account(privateKey, 11155111);
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(sepoliaRpcUrl), httpClient);
        _web3 = new Web3(account, client);
        _bookingContract = _web3.Eth.GetContract(labBookingAbi, _bookingContractAddress);
        _tokenContract = _web3.Eth.GetContract(tokenAbi, _tokenContractAddress);
    }

    private async Task<BigInteger> GetNextNonce()
    {
        const int maxRetries = 5; // Tăng số lần thử lại
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var pendingNonce = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                    _web3.TransactionManager.Account.Address,
                    BlockParameter.CreatePending()
                );
                var confirmedNonce = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                    _web3.TransactionManager.Account.Address,
                    BlockParameter.CreateLatest()
                );

                var nextNonce = BigInteger.Max(pendingNonce.Value, confirmedNonce.Value);
                _currentNonce = BigInteger.Max(nextNonce, _currentNonce + 1);

                Console.WriteLine($"Using nonce: {_currentNonce}");
                return _currentNonce;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Retry {retry}/{maxRetries} - Error getting nonce: {ex.Message}");
                if (retry == maxRetries)
                {
                    throw new Exception($"Failed to get nonce after {maxRetries} retries: {ex.Message}");
                }
                await Task.Delay(5000 * retry);
            }
        }
        throw new Exception("Failed to get nonce after maximum retries.");
    }

    public async Task<(bool Success, string TransactionHash)> BookOnBlockchain(
        int bookingId,
        string userWalletAddress,
        byte slot,
        string roomId,
        string roomName,
        string areaId,
        string areaName,
        string position,
        long timestamp,
        decimal requiredTokens)
    {
        const int maxRetries = 10;
        var requiredTokensWei = Nethereum.Util.UnitConversion.Convert.ToWei(requiredTokens);

        // Reset nonce trước khi bắt đầu chuỗi giao dịch
        _currentNonce = BigInteger.Zero;
        await GetNextNonce();

        // Kiểm tra tham số đầu vào
        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(roomName) ||
            string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(areaName) ||
            string.IsNullOrEmpty(position))
        {
            Console.WriteLine($"Invalid parameters: roomId={roomId}, roomName={roomName}, areaId={areaId}, areaName={areaName}, position={position}");
            return (false, null);
        }

        // Kiểm tra chain ID
        var chainId = await _web3.Eth.ChainId.SendRequestAsync();
        if (chainId.Value != 11155111)
        {
            Console.WriteLine($"Chain ID mismatch! Expected 11155111 (Sepolia), but got {chainId.Value}.");
            return (false, null);
        }

        // Kiểm tra số dư ETH của backend
        var ethBalance = await _web3.Eth.GetBalance.SendRequestAsync(_web3.TransactionManager.Account.Address);
        decimal ethBalanceInEth = Nethereum.Util.UnitConversion.Convert.FromWei(ethBalance.Value);
        if (ethBalanceInEth < 0.01m)
        {
            Console.WriteLine($"Insufficient ETH balance: {ethBalanceInEth} ETH (< 0.01 ETH).");
            return (false, null);
        }

        // Kiểm tra số dư token của user
        var balanceOfFunction = _tokenContract.GetFunction("balanceOf");
        var userBalance = await balanceOfFunction.CallAsync<BigInteger>(userWalletAddress);
        if (userBalance < requiredTokensWei)
        {
            Console.WriteLine($"Insufficient token balance for {userWalletAddress}: {Nethereum.Util.UnitConversion.Convert.FromWei(userBalance)} < {requiredTokens} tokens.");
            return (false, null);
        }

        // Thử đăng ký user nếu chưa đăng ký (bỏ kiểm tra getUser)
        var registerFunction = _bookingContract.GetFunction("registerUser");
        string txHashRegister = null;

        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var nonce = await GetNextNonce();
                var gasEstimate = await registerFunction.EstimateGasAsync(
                    _web3.TransactionManager.Account.Address,
                    null,
                    new HexBigInteger(0),
                    userWalletAddress,
                    $"{userWalletAddress}@default.com",
                    false
                );

                var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100);
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                var adjustedGasPrice = new HexBigInteger(gasPrice.Value * 120 / 100);

                var txInput = new TransactionInput(
                    registerFunction.GetData(userWalletAddress, $"{userWalletAddress}@default.com", false),
                    _bookingContractAddress,
                    _web3.TransactionManager.Account.Address,
                    gasLimit,
                    adjustedGasPrice,
                    new HexBigInteger(0)
                )
                {
                    Nonce = new HexBigInteger(nonce)
                };

                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                txHashRegister = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);

                var receipt = await WaitForReceipt(_web3, txHashRegister);
                if (receipt?.Status.Value != 1)
                {
                    Console.WriteLine($"Registration failed: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                    return (false, txHashRegister);
                }

                Console.WriteLine($"User {userWalletAddress} registered successfully with txHash: {txHashRegister}");
                break; // Đăng ký thành công, thoát vòng lặp
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("User already registered"))
                {
                    Console.WriteLine($"User {userWalletAddress} is already registered. Proceeding to book...");
                    break; // User đã đăng ký, tiếp tục gọi hàm book
                }
                Console.WriteLine($"Error registering user {userWalletAddress} (retry {retry}/{maxRetries}): {ex.Message}");
                if (retry == maxRetries)
                {
                    return (false, txHashRegister);
                }
                await Task.Delay(5000 * retry);
            }
        }

        // Gọi hàm book với userWalletAddress làm msg.sender
        var bookFunction = _bookingContract.GetFunction("book");
        bool booked = false;
        string txHashBook = null;

        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var nonce = await GetNextNonce();
                var gasEstimate = await bookFunction.EstimateGasAsync(
                    userWalletAddress, // Sử dụng địa chỉ user làm msg.sender
                    null,
                    new HexBigInteger(0),
                    roomId,
                    roomName,
                    areaId,
                    areaName,
                    position,
                    slot,
                    timestamp
                );

                var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100);
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                var adjustedGasPrice = new HexBigInteger(gasPrice.Value * 120 / 100);

                var txInput = new TransactionInput(
                    bookFunction.GetData(roomId, roomName, areaId, areaName, position, slot, timestamp),
                    _bookingContractAddress,
                    _web3.TransactionManager.Account.Address, // Backend ký giao dịch
                    gasLimit,
                    adjustedGasPrice,
                    new HexBigInteger(0)
                )
                {
                    Nonce = new HexBigInteger(nonce),
                    From = userWalletAddress // Đặt userWalletAddress làm msg.sender
                };

                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                txHashBook = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);

                var receipt = await WaitForReceipt(_web3, txHashBook);
                if (receipt?.Status.Value == 1)
                {
                    // Chuyển đổi JArray thành FilterLog[]
                    var logs = Newtonsoft.Json.JsonConvert.DeserializeObject<FilterLog[]>(receipt.Logs.ToString());

                    var bookingCreatedEvent = _bookingContract.GetEvent<BookingCreatedEventDTO>();
                    var eventTopic = bookingCreatedEvent.EventABI.Sha3Signature;
                    var filteredLogs = logs.Where(log => log.Topics[0].ToString().ToLower() == eventTopic.ToLower()).ToArray();
                    var eventLogs = Event<BookingCreatedEventDTO>.DecodeAllEvents(filteredLogs);

                    bool bookingConfirmed = false;
                    foreach (var log in eventLogs)
                    {
                        var bookingIdFromEvent = (int)log.Event.BookingId;
                        var userFromEvent = log.Event.User;
                        if (bookingIdFromEvent == bookingId && userFromEvent.Equals(userWalletAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Booking {bookingId} confirmed on-chain for user {userWalletAddress}.");
                            bookingConfirmed = true;
                            break;
                        }
                    }

                    if (!bookingConfirmed)
                    {
                        Console.WriteLine($"Booking {bookingId} transaction succeeded, but BookingCreated event not found for user {userWalletAddress}.");
                        return (false, txHashBook);
                    }

                    booked = true;
                }
                else
                {
                    Console.WriteLine($"Booking failed: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                    return (false, txHashBook);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error booking (retry {retry}/{maxRetries}): {ex.Message}");
                if (retry == maxRetries)
                {
                    return (false, txHashBook);
                }
                await Task.Delay(5000 * retry);
            }
        }

        return (booked, txHashBook);
    }

    private async Task<TransactionReceipt> WaitForReceipt(Web3 web3, string transactionHash)
    {
        if (string.IsNullOrEmpty(transactionHash))
            return null;

        const int maxAttempts = 60; // Tăng số lần thử lại
        const int delayMs = 5000;
        TransactionReceipt receipt = null;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            try
            {
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                if (receipt != null && receipt.BlockHash != null)
                {
                    if (receipt.Status.Value == 1)
                    {
                        Console.WriteLine($"Transaction {transactionHash} confirmed successfully with status {receipt.Status.Value}.");
                        return receipt;
                    }
                    else
                    {
                        Console.WriteLine($"Transaction {transactionHash} failed with status {receipt.Status.Value}.");
                        return receipt;
                    }
                }

                Console.WriteLine($"Waiting for transaction {transactionHash} to be confirmed (attempt {attempts + 1}/{maxAttempts})...");
                await Task.Delay(delayMs);
                attempts++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while waiting for transaction {transactionHash} (attempt {attempts + 1}/{maxAttempts}): {ex.Message}");
                if (attempts == maxAttempts - 1)
                {
                    Console.WriteLine($"Max attempts reached for transaction {transactionHash}. Aborting wait.");
                    return null;
                }
                await Task.Delay(delayMs);
                attempts++;
            }
        }

        Console.WriteLine($"Transaction {transactionHash} not confirmed after {maxAttempts} attempts.");
        return null;
    }

    public async Task<BigInteger> GetUserBalance(string walletAddress)
    {
        var balanceOfFunction = _tokenContract.GetFunction("balanceOf");
        return await balanceOfFunction.CallAsync<BigInteger>(walletAddress);
    }
}

[Event("BookingCreated")]
public class BookingCreatedEventDTO : IEventDTO
{
    [Parameter("uint256", "bookingId", 1, true)]
    public BigInteger BookingId { get; set; }

    [Parameter("address", "user", 2, true)]
    public string User { get; set; }

    [Parameter("string", "roomId", 3, false)]
    public string RoomId { get; set; }

    [Parameter("uint8", "slot", 4, false)]
    public byte Slot { get; set; }

    [Parameter("uint256", "timestamp", 5, false)]
    public BigInteger Timestamp { get; set; }
}