using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System.Net.Http;

namespace DxLabCoworkingSpace
{
    public class LabBookingCrawlerService : ILabBookingCrawlerService
    {
        private readonly Web3 _web3;
        private readonly Contract _contract;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _contractAddress;
        private readonly IAreaService _areaService;
        private readonly IAreaTypeService _areaTypeService;
        private readonly ISlotService _slotService;

        public LabBookingCrawlerService(
            string providerCrawl,
            string contractAddress,
            string contractAbi,
            IUnitOfWork unitOfWork,
            IAreaService areaService,
            IAreaTypeService areaTypeService,
            ISlotService slotService)
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(providerCrawl), httpClient);
            _web3 = new Web3(client);
            _contractAddress = contractAddress;
            _contract = _web3.Eth.GetContract(contractAbi, contractAddress);
            _unitOfWork = unitOfWork;
            _areaService = areaService;
            _areaTypeService = areaTypeService;
            _slotService = slotService;
        }

        private async Task SaveBookingEventAsync(
            string bookingId = null,
            int blockNumber = 0,
            string transactionHash = null,
            BigInteger? roomId = null,
            BigInteger? areaTypeId = null,
            BigInteger? slotId = null,
            string userAddress = null,
            long timestamp = 0,
            string eventType = null,
            string refundAmount = null,
            string email = null,
            bool? isStaff = null)
        {
            if (eventType.StartsWith("User"))
            {
                var user = await _unitOfWork.UserRepository.Get(u => u.WalletAddress.ToLower() == userAddress.ToLower());
                if (user == null && userAddress != null)
                {
                    // Tạo người dùng mới
                    user = new User
                    {
                        Email = string.IsNullOrEmpty(email) ? $"{userAddress}@default.com" : email,
                        FullName = "Unknown",
                        WalletAddress = userAddress,
                        RoleId = isStaff.HasValue && isStaff.Value ? 2 : 3, // 2 cho Staff, 3 cho Student
                        Status = eventType == "UserBlocked" ? false : true,
                        //IsRegister = eventType == "UserRegistered" // Đặt IsRegistered = true cho UserRegistered
                    };
                    await _unitOfWork.UserRepository.Add(user);
                }
                else if (user != null)
                {
                    // Cập nhật người dùng hiện có
                    if (eventType == "UserRegistered")
                    {
                        if (!string.IsNullOrEmpty(email))
                        {
                            user.Email = email;
                        }
                        //user.IsRegister = true; // Cập nhật IsRegistered = true
                        user.RoleId = isStaff.HasValue && isStaff.Value ? 2 : 3; // Cập nhật RoleId
                        user.Status = true;
                    }
                    else if (eventType == "UserBlocked")
                    {
                        user.Status = false;
                    }
                    else if (eventType == "UserUnblocked")
                    {
                        user.Status = true;
                    }
                    _unitOfWork.UserRepository.Update(user);
                }
                await _unitOfWork.CommitAsync();
                Console.WriteLine($"Processed {eventType} for user {userAddress}, txHash: {transactionHash}");
            }
            else if (eventType == "Created")
            {
                Console.WriteLine($"Processing BookingCreated event with transactionHash: {transactionHash}, bookingId: {bookingId}");

                var user = await _unitOfWork.UserRepository.Get(u => u.WalletAddress.ToLower() == userAddress.ToLower());
                if (user == null && userAddress != null)
                {
                    user = new User
                    {
                        Email = $"{userAddress}@default.com",
                        FullName = "Unknown",
                        WalletAddress = userAddress,
                        RoleId = 3, // Mặc định là Student
                        Status = true,
                        //IsRegister = false // Người dùng mới từ BookingCreated chưa chắc đã đăng ký
                    };
                    await _unitOfWork.UserRepository.Add(user);
                    await _unitOfWork.CommitAsync();
                }

                var slot = await _slotService.Get(s => s.SlotNumber == (int)slotId.Value);
                if (slot == null)
                {
                    Console.WriteLine($"Slot with SlotNumber {slotId} not found in database. Skipping.");
                    return;
                }

                var area = await _areaService.GetWithInclude(
                    a => a.RoomId == (int)roomId.Value && a.AreaTypeId == (int)areaTypeId.Value && a.Status == 1,
                    a => a.AreaType);
                if (area == null)
                {
                    Console.WriteLine($"Area with RoomId {roomId} and AreaTypeId {areaTypeId} not found. Skipping.");
                    return;
                }

                if (user == null)
                {
                    Console.WriteLine($"User with WalletAddress {userAddress} not found. Skipping.");
                    return;
                }

                var bookingDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                var existingBookingDetail = await _unitOfWork.BookingDetailRepository.GetWithInclude(
                    bd => bd.SlotId == slot.SlotId &&
                          bd.Booking.BookingCreatedDate.Date == bookingDate.Date &&
                          bd.AreaId == area.AreaId &&
                          bd.Booking.UserId == user.UserId,
                    bd => bd.Booking);

                if (existingBookingDetail == null)
                {
                    Console.WriteLine($"No matching booking detail found for RoomId {roomId}, AreaTypeId {areaTypeId}, SlotId {slot.SlotId}, Time {timestamp}. Skipping.");
                    return;
                }

                //existingBookingDetail.BookingGenerate = bookingId;
                //existingBookingDetail.TransactionHash = transactionHash;
                _unitOfWork.BookingDetailRepository.Update(existingBookingDetail);
                await _unitOfWork.CommitAsync();

                Console.WriteLine($"Updated BookingDetail {existingBookingDetail.BookingDetailId} with BookingGenerate: {bookingId}, TransactionHash: {transactionHash}");
            }
            //else if (eventType == "Cancelled")
            //{
            //    var parsedBookingId = bookingId;
            //    var existingBookingDetail = await _unitOfWork.BookingDetailRepository.Get(bd => bd.BookingGenerate == parsedBookingId);
            //    if (existingBookingDetail == null)
            //    {
            //        Console.WriteLine($"BookingDetail with BookingGenerate {parsedBookingId} not found for cancellation. Skipping.");
            //        return;
            //    }

            //    existingBookingDetail.Status = 0; // Hủy
            //    _unitOfWork.BookingDetailRepository.Update(existingBookingDetail);
            //    await _unitOfWork.CommitAsync();

            //    Console.WriteLine($"Processed BookingCancelled for BookingGenerate {parsedBookingId}, txHash: {transactionHash}");
            //}
        }

        public async Task CrawlBookingEventsAsync(int fromBlock, int toBlock)
        {
            try
            {
                var bookingCreatedEvent = _contract.GetEvent("BookingCreated");
                var bookingCreatedFilter = bookingCreatedEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                var bookingCreatedLogs = await RetryGetAllChangesAsync<BookingCreatedEventDTO>(bookingCreatedEvent, bookingCreatedFilter);
                await Task.Delay(200);

                Console.WriteLine($"Found {bookingCreatedLogs.Count} BookingCreated logs.");
                foreach (var log in bookingCreatedLogs)
                {
                    try
                    {
                        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                        await SaveBookingEventAsync(
                            log.Event.BookingId,
                            (int)log.Log.BlockNumber.Value,
                            log.Log.TransactionHash,
                            log.Event.RoomId,
                            log.Event.AreaTypeId,
                            log.Event.SlotId,
                            log.Event.User,
                            (long)block.Timestamp.Value,
                            "Created"
                        );
                    }
                    catch (Exception decodeError)
                    {
                        Console.WriteLine($"Error processing BookingCreated log: {decodeError.Message}");
                    }
                }

                var bookingCancelledEvent = _contract.GetEvent("BookingCancelled");
                var bookingCancelledFilter = bookingCancelledEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                var bookingCancelledLogs = await RetryGetAllChangesAsync<BookingCancelledEventDTO>(bookingCancelledEvent, bookingCancelledFilter);
                await Task.Delay(200);

                Console.WriteLine($"Found {bookingCancelledLogs.Count} BookingCancelled logs.");
                foreach (var log in bookingCancelledLogs)
                {
                    try
                    {
                        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                        var refundAmount = Nethereum.Util.UnitConversion.Convert.FromWei(log.Event.RefundAmount).ToString();
                        await SaveBookingEventAsync(
                            log.Event.BookingId,
                            (int)log.Log.BlockNumber.Value,
                            log.Log.TransactionHash,
                            null,
                            null,
                            null,
                            null,
                            (long)block.Timestamp.Value,
                            "Cancelled",
                            refundAmount
                        );
                    }
                    catch (Exception decodeError)
                    {
                        Console.WriteLine($"Error processing BookingCancelled log: {decodeError.Message}");
                    }
                }

                var userRegisteredEvent = _contract.GetEvent("UserRegistered");
                var userRegisteredFilter = userRegisteredEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                var userRegisteredLogs = await RetryGetAllChangesAsync<UserRegisteredEventDTO>(userRegisteredEvent, userRegisteredFilter);
                await Task.Delay(200);

                Console.WriteLine($"Found {userRegisteredLogs.Count} UserRegistered logs.");
                foreach (var log in userRegisteredLogs)
                {
                    try
                    {
                        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                        await SaveBookingEventAsync(
                            null,
                            (int)log.Log.BlockNumber.Value,
                            log.Log.TransactionHash,
                            null,
                            null,
                            null,
                            log.Event.User,
                            (long)block.Timestamp.Value,
                            "UserRegistered",
                            null,
                            log.Event.Email,
                            log.Event.IsStaff
                        );
                    }
                    catch (Exception decodeError)
                    {
                        Console.WriteLine($"Error processing UserRegistered log: {decodeError.Message}");
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error fetching logs: {error.Message}");
            }
        }

        public async Task<int?> GetDeploymentBlockAsync(string transactionHash)
        {
            try
            {
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                if (receipt != null && receipt.BlockNumber != null)
                {
                    Console.WriteLine($"Contract deployed at block: {receipt.BlockNumber}");
                    return (int)receipt.BlockNumber.Value;
                }
                else
                {
                    Console.WriteLine("Transaction receipt not found or block number is not available.");
                    return null;
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error fetching transaction receipt: {error.Message}");
                return null;
            }
        }

        public async Task<int?> GetLatestBlockNumberAsync()
        {
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var latestBlockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    Console.WriteLine($"Latest Block Number: {latestBlockNumber.Value}");
                    return (int)latestBlockNumber.Value;
                }
                catch (Exception error)
                {
                    Console.WriteLine($"Retry {i + 1}/{maxRetries} - Error get LastestBlock: {error.Message}");
                    if (i == maxRetries - 1)
                    {
                        Console.WriteLine("Out of attempts, returning null!");
                        return null;
                    }
                    await Task.Delay(2000 * (i + 1));
                }
            }
            return null;
        }

        private async Task<List<EventLog<TEventDTO>>> RetryGetAllChangesAsync<TEventDTO>(
            Event @event,
            NewFilterInput filter,
            int maxRetries = 3,
            int delayBetweenRetriesMs = 1000
        ) where TEventDTO : IEventDTO, new()
        {
            int retry = 0;
            while (true)
            {
                try
                {
                    return await @event.GetAllChangesAsync<TEventDTO>(filter);
                }
                catch (Exception ex)
                {
                    retry++;
                    Console.WriteLine($"Retry {retry}/{maxRetries} for {typeof(TEventDTO).Name} - Error: {ex.Message}");

                    if (retry >= maxRetries)
                    {
                        Console.WriteLine($"Max retry reached. Skipping {typeof(TEventDTO).Name}.");
                        return new List<EventLog<TEventDTO>>();
                    }

                    await Task.Delay(delayBetweenRetriesMs);
                }
            }
        }
    }

    [Event("BookingCreated")]
    public class BookingCreatedEventDTO : IEventDTO
    {
        [Parameter("bytes32", "bookingId", 1, true)]
        public string BookingId { get; set; }

        [Parameter("uint256", "roomId", 2, false)]
        public BigInteger RoomId { get; set; }

        [Parameter("uint256", "areaTypeId", 3, false)]
        public BigInteger AreaTypeId { get; set; }

        [Parameter("uint8", "slot", 4, false)]
        public BigInteger SlotId { get; set; }

        [Parameter("address", "user", 5, true)]
        public string User { get; set; }

        [Parameter("uint256", "time", 6, false)]
        public BigInteger Time { get; set; }
    }

    [Event("BookingCancelled")]
    public class BookingCancelledEventDTO : IEventDTO
    {
        [Parameter("bytes32", "bookingId", 1, true)]
        public string BookingId { get; set; }

        [Parameter("uint256", "roomId", 2, false)]
        public BigInteger RoomId { get; set; }

        [Parameter("uint256", "refundAmount", 3, false)]
        public BigInteger RefundAmount { get; set; }
    }

    [Event("UserRegistered")]
    public class UserRegisteredEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }

        [Parameter("string", "email", 2, false)]
        public string Email { get; set; }

        [Parameter("bool", "isStaff", 3, false)]
        public bool IsStaff { get; set; }
    }
}