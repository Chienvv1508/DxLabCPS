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

        public LabBookingCrawlerService(string providerUrl, string contractAddress, string contractAbi, IUnitOfWork unitOfWork)
        {
            _web3 = new Web3(providerUrl);
            _contractAddress = contractAddress;
            _contract = _web3.Eth.GetContract(contractAbi, contractAddress);
            _unitOfWork = unitOfWork;
        }

        private async Task SaveBookingEventAsync(string bookingId, int blockNumber, string transactionHash, string roomId, byte slot, string userAddress, long timestamp, string eventType, string refundAmount = null)
        {
            // Kiểm tra xem sự kiện đã tồn tại chưa
            var existingDetail = await _unitOfWork.BookingDetailRepository.GetWithInclude(
                bd => bd.Booking != null && bd.Booking.BookingId.ToString() == bookingId && bd.Status == (eventType == "Created" ? 1 : eventType == "Cancelled" ? 0 : 2),
                bd => bd.Booking
            );

            if (existingDetail == null)
            {
                Console.WriteLine($"Saving new booking event with transactionHash: {transactionHash}");

                // Tìm hoặc tạo user
                var user = await _unitOfWork.UserRepository.Get(u => u.WalletAddress == userAddress);
                if (user == null && userAddress != null) // Đảm bảo userAddress không null
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

                // Tạo Booking nếu chưa tồn tại
                var booking = await _unitOfWork.BookingRepository.Get(b => b.BookingId.ToString() == bookingId);
                if (booking == null)
                {
                    booking = new Booking
                    {
                        UserId = user?.UserId, // Có thể null nếu userAddress không có
                        BookingCreatedDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime,
                        Price = eventType == "Created" ? 100m : 0m
                    };
                    await _unitOfWork.BookingRepository.Add(booking);
                    await _unitOfWork.CommitAsync();
                }

                // Tạo BookingDetail
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
                    AreaId = null,
                    PositionId = null,
                    Price = eventType == "Cancelled" && refundAmount != null ? decimal.Parse(refundAmount) : 100m
                };

                await _unitOfWork.BookingDetailRepository.Add(bookingDetail);
                await _unitOfWork.CommitAsync();
            }
            else
            {
                Console.WriteLine($"Booking event with transactionHash: {transactionHash} already exists.");
            }
        }

        public async Task CrawlBookingEventsAsync(int fromBlock, int toBlock)
        {
            try
            {
                // Xử lý sự kiện BookingCreated
                // Xử lý sự kiện BookingCreated
                var bookingCreatedEvent = _contract.GetEvent("BookingCreated"); // Lấy sự kiện từ contract
                var bookingCreatedFilter = bookingCreatedEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                var bookingCreatedLogs = await bookingCreatedEvent.GetAllChangesAsync<BookingCreatedEventDTO>(bookingCreatedFilter);

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

                // Xử lý sự kiện BookingCancelled
                var bookingCancelledEvent = _contract.GetEvent("BookingCancelled");
                var bookingCancelledFilter = bookingCancelledEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                var bookingCancelledLogs = await bookingCancelledEvent.GetAllChangesAsync<BookingCancelledEventDTO>(bookingCancelledFilter);

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
                            0,
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

                // Xử lý sự kiện BookingCheckedIn
                var bookingCheckedInEvent = _contract.GetEvent("BookingCheckedIn");
                var bookingCheckedInFilter = bookingCheckedInEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(fromBlock)), new BlockParameter(new HexBigInteger(toBlock)));
                var bookingCheckedInLogs = await bookingCheckedInEvent.GetAllChangesAsync<BookingCheckedInEventDTO>(bookingCheckedInFilter);

                Console.WriteLine($"Found {bookingCheckedInLogs.Count} BookingCheckedIn logs.");
                foreach (var log in bookingCheckedInLogs)
                {
                    try
                    {
                        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(log.Log.BlockNumber));

                        await SaveBookingEventAsync(
                            log.Event.BookingId,
                            (int)log.Log.BlockNumber.Value,
                            log.Log.TransactionHash,
                            null,
                            0,
                            null,
                            (long)block.Timestamp.Value,
                            "CheckedIn"
                        );
                    }
                    catch (Exception decodeError)
                    {
                        Console.WriteLine($"Error processing BookingCheckedIn log: {decodeError.Message}");
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
            try
            {
                var latestBlockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                Console.WriteLine($"Latest Block Number: {latestBlockNumber.Value}");
                return (int)latestBlockNumber.Value;
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error fetching the latest block number: {error.Message}");
                return null;
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
}