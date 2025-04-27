using DxLabCoworkingSpace.Service;
using Microsoft.Extensions.Configuration;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using System.Text.Json;

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
            ?? "https://sepolia.infura.io/v3/ce5f177778e547a19055596b216fd743";

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

    public async Task<(bool Success, string TransactionHash)> BookOnBlockchain(
    int bookingId,
    string userWalletAddress,
    byte slot,
    string roomId,
    string roomName,
    string areaId,
    string areaName,
    string position,
    long timestamp)
    {
        const int maxRetries = 10; // Tăng số lần thử lại như MintTokenForUser
        const decimal requiredTokens = 5m; // SLOT_PRICE = 5 token
        var requiredTokensWei = Nethereum.Util.UnitConversion.Convert.ToWei(requiredTokens);

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
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_chainId: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data},)");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_chainId. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_chainId: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_chainId. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (chainId.Value != 11155111)
        {
            Console.WriteLine($"Chain ID mismatch! Expected 11155111 (Sepolia), but got {chainId.Value}. Aborting booking.");
            return (false, null);
        }

        // Kiểm tra số dư ETH của ví backend
        decimal ethBalance = 0;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(_web3.TransactionManager.Account.Address);
                ethBalance = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                Console.WriteLine($"ETH Balance of sender (backend) {_web3.TransactionManager.Account.Address}: {ethBalance} ETH");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getBalance: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, )");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getBalance. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getBalance: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getBalance. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (ethBalance < 0.01m)
        {
            Console.WriteLine("Insufficient ETH balance for transactions! At least 0.01 ETH is required.");
            return (false, null);
        }

        // Kiểm tra số dư token của user
        var userBalance = BigInteger.Zero;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                var balanceOfFunction = _tokenContract.GetFunction("balanceOf");
                userBalance = await balanceOfFunction.CallAsync<BigInteger>(userWalletAddress);
                Console.WriteLine($"Balance of {userWalletAddress}: {Nethereum.Util.UnitConversion.Convert.FromWei(userBalance)} tokens");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for balanceOf: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data},)");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for balanceOf. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for balanceOf: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for balanceOf. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (userBalance < requiredTokensWei)
        {
            Console.WriteLine($"User {userWalletAddress} has insufficient balance: {Nethereum.Util.UnitConversion.Convert.FromWei(userBalance)} < {requiredTokens} tokens");
            return (false, null);
        }

        // Đăng ký người dùng nếu chưa đăng ký
        var userFunction = _bookingContract.GetFunction("users");
        var userData = await userFunction.CallDeserializingToObjectAsync<UserData>(userWalletAddress);
        if (!userData.IsRegistered)
        {
            Console.WriteLine($"User {userWalletAddress} not registered. Proceeding to register...");

            var registerFunction = _bookingContract.GetFunction("registerUser");

            // Lấy nonce cho giao dịch đăng ký
            HexBigInteger nonceRegister = null;
            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    nonceRegister = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                        _web3.TransactionManager.Account.Address,
                        BlockParameter.CreatePending()
                    );
                    Console.WriteLine($"Current nonce for sender (backend) {_web3.TransactionManager.Account.Address}: {nonceRegister.Value}");
                    break;
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data},)");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting registration.");
                        return (false, null);
                    }
                    await Task.Delay(5000 * retry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting registration.");
                        return (false, null);
                    }
                    await Task.Delay(5000 * retry);
                }
            }

            if (nonceRegister == null)
            {
                Console.WriteLine("Failed to retrieve nonce for registration. Aborting booking.");
                return (false, null);
            }

            // Hủy các giao dịch pending nếu có
            await CancelPendingTransactionsIfAny();

            // Thực hiện đăng ký
            bool registered = false;
            string txHashRegister = null;
            for (int retry = 1; retry <= maxRetries && !registered; retry++)
            {
                try
                {
                    Console.WriteLine($"Processing registration for user {userWalletAddress} (attempt {retry}/{maxRetries}) using sender (backend) {_web3.TransactionManager.Account.Address}...");

                    // Ước lượng gas
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
                    Console.WriteLine($"Using gas price: {adjustedGasPrice.Value} Wei, Gas limit: {gasLimit.Value}");

                    // Tạo TransactionInput
                    var txInput = new TransactionInput(
                        registerFunction.GetData(userWalletAddress, $"{userWalletAddress}@default.com", false),
                        _bookingContractAddress,
                        _web3.TransactionManager.Account.Address,
                        gasLimit,
                        adjustedGasPrice,
                        new HexBigInteger(0)
                    )
                    {
                        Nonce = nonceRegister
                    };

                    // Ký giao dịch cục bộ
                    var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                    Console.WriteLine($"Registration transaction signed locally: {signedTx}");

                    // Gửi giao dịch đã ký
                    txHashRegister = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);
                    Console.WriteLine($"Registration transaction sent: Sender (backend) {_web3.TransactionManager.Account.Address} registering user {userWalletAddress}. TxHash: {txHashRegister}");

                    // Chờ receipt
                    var receipt = await WaitForReceipt(_web3, txHashRegister);
                    if (receipt?.Status.Value == 1)
                    {
                        Console.WriteLine($"Registered successfully: Sender (backend) {_web3.TransactionManager.Account.Address} registered user {userWalletAddress}");
                        registered = true;
                    }
                    else
                    {
                        Console.WriteLine($"Registration failed for user {userWalletAddress}: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                        try
                        {
                            var callInput = new TransactionInput(
                                registerFunction.GetData(userWalletAddress, $"{userWalletAddress}@default.com", false),
                                _bookingContractAddress,
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
                        return (false, txHashRegister);
                    }
                }
                catch (SmartContractRevertException revertEx)
                {
                    Console.WriteLine($"SmartContractRevertException for user {userWalletAddress}: {revertEx.RevertMessage}");
                    return (false, txHashRegister);
                }
                catch (RpcResponseException rpcEx)
                {
                    Console.WriteLine($"RPC Error for user {userWalletAddress} (retry {retry}/{maxRetries}): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, ");
                    if (rpcEx.Message.Contains("replacement transaction underpriced"))
                    {
                        nonceRegister = new HexBigInteger(nonceRegister.Value + 1);
                        Console.WriteLine($"Incrementing nonce to {nonceRegister.Value} due to underpriced replacement transaction.");
                    }
                    else if (retry == maxRetries)
                    {
                        Console.WriteLine($"Max retries reached for user {userWalletAddress} registration. Aborting.");
                        return (false, txHashRegister);
                    }
                    await Task.Delay(5000 * retry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for user {userWalletAddress} (retry {retry}/{maxRetries}): {ex.Message}\nStackTrace: {ex.StackTrace}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine($"Max retries reached for user {userWalletAddress} registration. Aborting.");
                        return (false, txHashRegister);
                    }
                    await Task.Delay(5000 * retry);
                }
            }

            if (!registered)
            {
                Console.WriteLine($"Failed to register user {userWalletAddress} after all retries.");
                return (false, txHashRegister);
            }
        }
        else
        {
            Console.WriteLine($"User {userWalletAddress} already registered. Skipping registration.");
        }

        // Phê duyệt token (approve) cho hợp đồng Booking
        var approveFunction = _tokenContract.GetFunction("approve");

        // Lấy nonce cho giao dịch phê duyệt
        HexBigInteger nonceApprove = null;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                nonceApprove = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                    _web3.TransactionManager.Account.Address,
                    BlockParameter.CreatePending()
                );
                Console.WriteLine($"Current nonce for sender (backend) {_web3.TransactionManager.Account.Address}: {nonceApprove.Value}");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data},)");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting approval.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting approval.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (nonceApprove == null)
        {
            Console.WriteLine("Failed to retrieve nonce for approval. Aborting booking.");
            return (false, null);
        }

        // Hủy các giao dịch pending nếu có
        await CancelPendingTransactionsIfAny();

        // Thực hiện phê duyệt
        bool approved = false;
        string txHashApprove = null;
        for (int retry = 1; retry <= maxRetries && !approved; retry++)
        {
            try
            {
                Console.WriteLine($"Processing approval for user {userWalletAddress} (attempt {retry}/{maxRetries}) using sender (backend) {_web3.TransactionManager.Account.Address}...");

                var gasEstimate = await approveFunction.EstimateGasAsync(
                    _web3.TransactionManager.Account.Address,
                    null,
                    new HexBigInteger(0),
                    _bookingContractAddress,
                    requiredTokensWei
                );

                var gasLimit = new HexBigInteger(gasEstimate.Value * 120 / 100);
                var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
                var adjustedGasPrice = new HexBigInteger(gasPrice.Value * 120 / 100);
                Console.WriteLine($"Using gas price: {adjustedGasPrice.Value} Wei, Gas limit: {gasLimit.Value}");

                var txInput = new TransactionInput(
                    approveFunction.GetData(_bookingContractAddress, requiredTokensWei),
                    _tokenContractAddress,
                    _web3.TransactionManager.Account.Address,
                    gasLimit,
                    adjustedGasPrice,
                    new HexBigInteger(0)
                )
                {
                    Nonce = nonceApprove
                };

                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                Console.WriteLine($"Approval transaction signed locally: {signedTx}");

                txHashApprove = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);
                Console.WriteLine($"Approval transaction sent: Sender (backend) {_web3.TransactionManager.Account.Address} approving for user {userWalletAddress}. TxHash: {txHashApprove}");

                var receipt = await WaitForReceipt(_web3, txHashApprove);
                if (receipt?.Status.Value == 1)
                {
                    Console.WriteLine($"Approved successfully: Sender (backend) {_web3.TransactionManager.Account.Address} approved tokens for user {userWalletAddress}");
                    approved = true;
                }
                else
                {
                    Console.WriteLine($"Approval failed for user {userWalletAddress}: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                    return (false, txHashApprove);
                }
            }
            catch (SmartContractRevertException revertEx)
            {
                Console.WriteLine($"SmartContractRevertException for approval: {revertEx.RevertMessage}");
                return (false, txHashApprove);
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error for approval (retry {retry}/{maxRetries}): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, )");
                if (rpcEx.Message.Contains("replacement transaction underpriced"))
                {
                    nonceApprove = new HexBigInteger(nonceApprove.Value + 1);
                    Console.WriteLine($"Incrementing nonce to {nonceApprove.Value} due to underpriced replacement transaction.");
                }
                else if (retry == maxRetries)
                {
                    Console.WriteLine($"Max retries reached for approval. Aborting.");
                    return (false, txHashApprove);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error for approval (retry {retry}/{maxRetries}): {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine($"Max retries reached for approval. Aborting.");
                    return (false, txHashApprove);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (!approved)
        {
            Console.WriteLine($"Failed to approve tokens for user {userWalletAddress} after all retries.");
            return (false, txHashApprove);
        }

        // Gọi hàm book trên smart contract
        var bookFunction = _bookingContract.GetFunction("book");

        // Lấy nonce cho giao dịch đặt chỗ
        HexBigInteger nonceBook = null;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                nonceBook = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                    _web3.TransactionManager.Account.Address,
                    BlockParameter.CreatePending()
                );
                Console.WriteLine($"Current nonce for sender (backend) {_web3.TransactionManager.Account.Address}: {nonceBook.Value}");
                break;
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data}, )");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error (retry {retry}/{maxRetries}) for eth_getTransactionCount: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine("Max retries reached for eth_getTransactionCount. Aborting booking.");
                    return (false, null);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (nonceBook == null)
        {
            Console.WriteLine("Failed to retrieve nonce for booking. Aborting.");
            return (false, null);
        }

        // Hủy các giao dịch pending nếu có
        await CancelPendingTransactionsIfAny();

        // Thực hiện đặt chỗ
        bool booked = false;
        string txHashBook = null;
        for (int retry = 1; retry <= maxRetries && !booked; retry++)
        {
            try
            {
                Console.WriteLine($"Processing booking for booking {bookingId} (attempt {retry}/{maxRetries}) using sender (backend) {_web3.TransactionManager.Account.Address}...");

                var gasEstimate = await bookFunction.EstimateGasAsync(
                    _web3.TransactionManager.Account.Address,
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
                Console.WriteLine($"Using gas price: {adjustedGasPrice.Value} Wei, Gas limit: {gasLimit.Value}");

                var txInput = new TransactionInput(
                    bookFunction.GetData(roomId, roomName, areaId, areaName, position, slot, timestamp),
                    _bookingContractAddress,
                    _web3.TransactionManager.Account.Address,
                    gasLimit,
                    adjustedGasPrice,
                    new HexBigInteger(0)
                )
                {
                    Nonce = nonceBook
                };

                var signedTx = await _web3.TransactionManager.SignTransactionAsync(txInput);
                Console.WriteLine($"Booking transaction signed locally: {signedTx}");

                txHashBook = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + signedTx);
                Console.WriteLine($"Booking transaction sent: Sender (backend) {_web3.TransactionManager.Account.Address} booking for user {userWalletAddress}. TxHash: {txHashBook}");

                var receipt = await WaitForReceipt(_web3, txHashBook);
                if (receipt?.Status.Value == 1)
                {
                    Console.WriteLine($"Booked successfully: Sender (backend) {_web3.TransactionManager.Account.Address} booked for user {userWalletAddress}, booking {bookingId}");
                    booked = true;
                }
                else
                {
                    Console.WriteLine($"Booking failed for booking {bookingId}: {(receipt == null ? "No receipt" : $"Status = {receipt.Status.Value}")}");
                    return (false, txHashBook);
                }
            }
            catch (SmartContractRevertException revertEx)
            {
                Console.WriteLine($"SmartContractRevertException for booking {bookingId}: {revertEx.RevertMessage}");
                return (false, txHashBook);
            }
            catch (RpcResponseException rpcEx)
            {
                Console.WriteLine($"RPC Error for booking {bookingId} (retry {retry}/{maxRetries}): {rpcEx.Message} (Code: {rpcEx.RpcError?.Code}, Data: {rpcEx.RpcError?.Data},)");
                if (rpcEx.Message.Contains("replacement transaction underpriced"))
                {
                    nonceBook = new HexBigInteger(nonceBook.Value + 1);
                    Console.WriteLine($"Incrementing nonce to {nonceBook.Value} due to underpriced replacement transaction.");
                }
                else if (retry == maxRetries)
                {
                    Console.WriteLine($"Max retries reached for booking {bookingId}. Aborting.");
                    return (false, txHashBook);
                }
                await Task.Delay(5000 * retry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error for booking {bookingId} (retry {retry}/{maxRetries}): {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (retry == maxRetries)
                {
                    Console.WriteLine($"Max retries reached for booking {bookingId}. Aborting.");
                    return (false, txHashBook);
                }
                await Task.Delay(5000 * retry);
            }
        }

        if (!booked)
        {
            Console.WriteLine($"Failed to book for booking {bookingId} after all retries.");
            return (false, txHashBook);
        }

        return (true, txHashBook);
    }

    // Thêm phương thức CancelPendingTransactionsIfAny
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

// DTO để deserialize dữ liệu user từ mapping users
[FunctionOutput]
public class UserData
{
    [Parameter("string", "email", 1)]
    public string Email { get; set; }

    [Parameter("bool", "isStaff", 2)]
    public bool IsStaff { get; set; }

    [Parameter("bool", "isRegistered", 3)]
    public bool IsRegistered { get; set; }

    [Parameter("bool", "isBlocked", 4)]
    public bool IsBlocked { get; set; }
}