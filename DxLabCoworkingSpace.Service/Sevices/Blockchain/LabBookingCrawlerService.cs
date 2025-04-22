using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

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
        public LabBookingCrawlerService(string providerCrawl, string contractAddress, string contractAbi, IUnitOfWork unitOfWork, IAreaService areaService, IAreaTypeService areaTypeService)
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(providerCrawl), httpClient);
            _web3 = new Web3(client);
            _contractAddress = contractAddress;
            _contract = _web3.Eth.GetContract(contractAbi, contractAddress);
            _unitOfWork = unitOfWork;
            _areaService = areaService;
            _areaTypeService = areaTypeService;
        }

        private async Task SaveBookingEventAsync(
            string bookingId = null,
            int blockNumber = 0,
            string transactionHash = null,
            string roomId = null,
            byte slot = 0,
            string userAddress = null,
            long timestamp = 0,
            string eventType = null,
            string refundAmount = null,
            string email = null,
            bool? isStaff = null)
        {
            if (eventType.StartsWith("User"))
            {
                // Xử lý sự kiện liên quan đến user
                var user = await _unitOfWork.UserRepository.Get(u => u.WalletAddress == userAddress);
                if (user == null && userAddress != null)
                {
                    user = new User
                    {
                        Email = email ?? $"{userAddress}@default.com",
                        FullName = "Unknown",
                        WalletAddress = userAddress,
                        Status = eventType == "UserBlocked" ? false : true
                    };
                    await _unitOfWork.UserRepository.Add(user);
                }
                else if (user != null)
                {
                    if (eventType == "UserRegistered" && email != null)
                    {
                        user.Email = email;
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
            else
            {
                // Chuyển bookingId (bytes32) từ blockchain thành BookingId (int)
                int parsedBookingId;
                try
                {
                    var bigIntBookingId = BigInteger.Parse("0" + bookingId.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                    parsedBookingId = (int)bigIntBookingId;
                    if (parsedBookingId <= 0)
                    {
                        throw new ArgumentException("BookingId must be a positive integer.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting bookingId {bookingId} to int: {ex.Message}");
                    return;
                }

                // Kiểm tra xem booking đã tồn tại trong database chưa
                var existingBooking = await _unitOfWork.BookingRepository.Get(b => b.BookingId == parsedBookingId);
                if (existingBooking != null)
                {
                    var existingDetail = await _unitOfWork.BookingDetailRepository.GetWithInclude(
                        bd => bd.BookingId == existingBooking.BookingId &&
                              bd.SlotId == slot &&
                              bd.Status == (eventType == "Created" ? 1 : eventType == "Cancelled" ? 0 : 2),
                        bd => bd.Booking
                    );

                    if (existingDetail != null)
                    {
                        Console.WriteLine($"Booking event with transactionHash: {transactionHash} and bookingId: {bookingId} already exists.");
                        return;
                    }
                }

                Console.WriteLine($"Processing booking event with transactionHash: {transactionHash}, bookingId: {bookingId}");

                var user = await _unitOfWork.UserRepository.Get(u => u.WalletAddress == userAddress);
                if (user == null && userAddress != null)
                {
                    user = new User
                    {
                        Email = $"{userAddress}@default.com",
                        FullName = "Unknown",
                        WalletAddress = userAddress,
                        Status = true
                    };
                    await _unitOfWork.UserRepository.Add(user);
                    await _unitOfWork.CommitAsync();
                }

                Booking booking;
                if (existingBooking == null)
                {
                    // Booking chưa tồn tại, tạo mới
                    booking = new Booking
                    {
                        BookingId = parsedBookingId, // Gán BookingId từ blockchain
                        UserId = user?.UserId,
                        BookingCreatedDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime,
                        Price = 0m // Sẽ được tính sau khi có BookingDetail
                    };
                    await _unitOfWork.BookingRepository.Add(booking);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    booking = existingBooking;
                }

                // Lấy giá từ AreaType thông qua Area
                decimal areaTypePrice = 0m;
                int? areaId = null;
                if (!string.IsNullOrEmpty(roomId))
                {
                    var area = await _areaService.GetWithInclude(
                        a => a.RoomId.ToString() == roomId && a.Status == 1,
                        a => a.AreaType);
                    if (area != null)
                    {
                        areaId = area.AreaId;
                        var areaType = await _areaTypeService.Get(at => at.AreaTypeId == area.AreaTypeId);
                        if (areaType != null)
                        {
                            areaTypePrice = areaType.Price;
                        }
                        else
                        {
                            Console.WriteLine($"No AreaType found for AreaId: {area.AreaId}. Using price 0.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No Area found for RoomId: {roomId}. Using price 0.");
                    }
                }

                var bookingDetail = new BookingDetail
                {
                    Status = eventType switch
                    {
                        "Created" => 1,
                        "Cancelled" => 0,
                        "CheckedIn" => 2,
                        _ => 1
                    },
                    CheckinTime = eventType == "CheckedIn" ? DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime : DateTime.MinValue,
                    CheckoutTime = DateTime.MinValue,
                    BookingId = booking.BookingId,
                    SlotId = slot,
                    AreaId = areaId,
                    PositionId = null,
                    Price = areaTypePrice
                };

                await _unitOfWork.BookingDetailRepository.Add(bookingDetail);
                await _unitOfWork.CommitAsync();

                // Cập nhật tổng giá trong Booking
                var allDetails = await _unitOfWork.BookingDetailRepository.GetAll(bd => bd.BookingId == booking.BookingId);
                booking.Price = allDetails.Sum(d => d.Price);
                _unitOfWork.BookingRepository.Update(booking);
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task CrawlBookingEventsAsync(int fromBlock, int toBlock)
        {
            try
            {
                // BookingCreated
                var bookingCreatedEvent = _contract.GetEvent("BookingCreated");
                var bookingCreatedFilter = bookingCreatedEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                //var bookingCreatedLogs = await bookingCreatedEvent.GetAllChangesAsync<BookingCreatedEventDTO>(bookingCreatedFilter);
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
                            log.Event.Slot,
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

                //// BookingCancelled
                //var bookingCancelledEvent = _contract.GetEvent("BookingCancelled");
                //var bookingCancelledFilter = bookingCancelledEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                ////var bookingCancelledLogs = await bookingCancelledEvent.GetAllChangesAsync<BookingCancelledEventDTO>(bookingCancelledFilter);
                //var bookingCancelledLogs = await RetryGetAllChangesAsync<BookingCancelledEventDTO>(bookingCancelledEvent, bookingCancelledFilter);
                //await Task.Delay(200);

                //Console.WriteLine($"Found {bookingCancelledLogs.Count} BookingCancelled logs.");
                //foreach (var log in bookingCancelledLogs)
                //{
                //    try
                //    {
                //        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                //        var refundAmount = Nethereum.Util.UnitConversion.Convert.FromWei(log.Event.RefundAmount).ToString();
                //        await SaveBookingEventAsync(
                //            log.Event.BookingId,
                //            (int)log.Log.BlockNumber.Value,
                //            log.Log.TransactionHash,
                //            null,
                //            0,
                //            null,
                //            (long)block.Timestamp.Value,
                //            "Cancelled",
                //            refundAmount
                //        );
                //    }
                //    catch (Exception decodeError)
                //    {
                //        Console.WriteLine($"Error processing BookingCancelled log: {decodeError.Message}");
                //    }
                //}

                //// BookingCheckedIn
                //var bookingCheckedInEvent = _contract.GetEvent("BookingCheckedIn");
                //var bookingCheckedInFilter = bookingCheckedInEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                ////var bookingCheckedInLogs = await bookingCheckedInEvent.GetAllChangesAsync<BookingCheckedInEventDTO>(bookingCheckedInFilter);
                //var bookingCheckedInLogs = await RetryGetAllChangesAsync<BookingCheckedInEventDTO>(bookingCheckedInEvent, bookingCheckedInFilter);
                //await Task.Delay(200);

                //Console.WriteLine($"Found {bookingCheckedInLogs.Count} BookingCheckedIn logs.");
                //foreach (var log in bookingCheckedInLogs)
                //{
                //    try
                //    {
                //        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                //        await SaveBookingEventAsync(
                //            log.Event.BookingId,
                //            (int)log.Log.BlockNumber.Value,
                //            log.Log.TransactionHash,
                //            null,
                //            0,
                //            null,
                //            (long)block.Timestamp.Value,
                //            "CheckedIn"
                //        );
                //    }
                //    catch (Exception decodeError)
                //    {
                //        Console.WriteLine($"Error processing BookingCheckedIn log: {decodeError.Message}");
                //    }
                //}

                //// UserBlocked
                //var userBlockedEvent = _contract.GetEvent("UserBlocked");
                //var userBlockedFilter = userBlockedEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                ////var userBlockedLogs = await userBlockedEvent.GetAllChangesAsync<UserBlockedEventDTO>(userBlockedFilter);
                //var userBlockedLogs = await RetryGetAllChangesAsync<UserBlockedEventDTO>(userBlockedEvent, userBlockedFilter);
                //await Task.Delay(200);

                //Console.WriteLine($"Found {userBlockedLogs.Count} UserBlocked logs.");
                //foreach (var log in userBlockedLogs)
                //{
                //    try
                //    {
                //        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                //        await SaveBookingEventAsync(
                //            null,
                //            (int)log.Log.BlockNumber.Value,
                //            log.Log.TransactionHash,
                //            null,
                //            0,
                //            log.Event.User,
                //            (long)block.Timestamp.Value,
                //            "UserBlocked"
                //        );
                //    }
                //    catch (Exception decodeError)
                //    {
                //        Console.WriteLine($"Error processing UserBlocked log: {decodeError.Message}");
                //    }
                //}

                //// UserRegistered
                //var userRegisteredEvent = _contract.GetEvent("UserRegistered");
                //var userRegisteredFilter = userRegisteredEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                ////var userRegisteredLogs = await userRegisteredEvent.GetAllChangesAsync<UserRegisteredEventDTO>(userRegisteredFilter);
                //var userRegisteredLogs = await RetryGetAllChangesAsync<UserRegisteredEventDTO>(userRegisteredEvent, userRegisteredFilter);
                //await Task.Delay(200);

                //Console.WriteLine($"Found {userRegisteredLogs.Count} UserRegistered logs.");
                //foreach (var log in userRegisteredLogs)
                //{
                //    try
                //    {
                //        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                //        await SaveBookingEventAsync(
                //            null,
                //            (int)log.Log.BlockNumber.Value,
                //            log.Log.TransactionHash,
                //            null,
                //            0,
                //            log.Event.User,
                //            (long)block.Timestamp.Value,
                //            "UserRegistered",
                //            null,
                //            log.Event.Email,
                //            log.Event.IsStaff
                //        );
                //    }
                //    catch (Exception decodeError)
                //    {
                //        Console.WriteLine($"Error processing UserRegistered log: {decodeError.Message}");
                //    }
                //}

                //// UserUnblocked
                //var userUnblockedEvent = _contract.GetEvent("UserUnblocked");
                //var userUnblockedFilter = userUnblockedEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                ////var userUnblockedLogs = await userUnblockedEvent.GetAllChangesAsync<UserUnblockedEventDTO>(userUnblockedFilter);
                //var userUnblockedLogs = await RetryGetAllChangesAsync<UserUnblockedEventDTO>(userUnblockedEvent, userUnblockedFilter);
                //await Task.Delay(200);

                //Console.WriteLine($"Found {userUnblockedLogs.Count} UserUnblocked logs.");
                //foreach (var log in userUnblockedLogs)
                //{
                //    try
                //    {
                //        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));
                //        await SaveBookingEventAsync(
                //            null,
                //            (int)log.Log.BlockNumber.Value,
                //            log.Log.TransactionHash,
                //            null,
                //            0,
                //            log.Event.User,
                //            (long)block.Timestamp.Value,
                //            "UserUnblocked"
                //        );
                //    }
                //    catch (Exception decodeError)
                //    {
                //        Console.WriteLine($"Error processing UserUnblocked log: {decodeError.Message}");
                //    }
                //}
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

        [Parameter("string", "roomId", 2, false)]
        public string RoomId { get; set; }

        [Parameter("uint8", "slot", 3, false)]
        public byte Slot { get; set; }

        [Parameter("address", "user", 4, false)]
        public string User { get; set; }

        [Parameter("uint256", "time", 5, false)]
        public BigInteger Time { get; set; }
    }

    [Event("BookingCancelled")]
    public class BookingCancelledEventDTO : IEventDTO
    {
        [Parameter("bytes32", "bookingId", 1, true)]
        public string BookingId { get; set; }

        [Parameter("uint256", "refundAmount", 2, false)]
        public BigInteger RefundAmount { get; set; }
    }

    [Event("BookingCheckedIn")]
    public class BookingCheckedInEventDTO : IEventDTO
    {
        [Parameter("bytes32", "bookingId", 1, true)]
        public string BookingId { get; set; }
    }
    [Event("UserBlocked")]
    public class UserBlockedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }
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

    [Event("UserUnblocked")]
    public class UserUnblockedEventDTO : IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }
    }
}