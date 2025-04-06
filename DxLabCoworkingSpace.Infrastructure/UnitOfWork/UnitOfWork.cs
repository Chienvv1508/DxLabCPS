
using DxLabCoworkingSpace;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DxLabSystemContext _dbContext;
        private IGenericRepository<Role> _roleRepository;
        private IGenericRepository<Slot> _slotRepository;
        private IGenericRepository<User> _userRepository;
        private IGenericRepository<Blog> _blogRepository;
        private IGenericRepository<Facility> _facilityRepository;
        private IGenericRepository<Room> _roomRepository;
        private IGenericRepository<AreaType> _areaTypeRepository;
        private IGenericRepository<Area> _areaRepository;
        private IGenericRepository<Booking> _bookingRepository;
        private IGenericRepository<BookingDetail> _bookingDetailRepository;
        private IGenericRepository<UsingFacility> _usingRepository;
        private IGenericRepository<FacilitiesStatus> _facilityStatusRepository;
        private IGenericRepository<SumaryExpense> _sumaryExpenseRepository;
        private IGenericRepository<ContractCrawl> _contractCrawlRepository;
        private IGenericRepository<Report> _reportRepository;
        private IGenericRepository<AreaTypeCategory> _areaTypeCategoryRepository { get; }
        public UnitOfWork(DxLabSystemContext dbContext) 
        {
            _dbContext = dbContext;
        }
        public IGenericRepository<Role> RoleRepository => _roleRepository ?? new GenericRepository<Role>(_dbContext);
        public IGenericRepository<Slot> SlotRepository => _slotRepository ?? new GenericRepository<Slot>(_dbContext);
        public IGenericRepository<User> UserRepository => _userRepository ?? new GenericRepository<User>(_dbContext);
        public IGenericRepository<Blog> BlogRepository => _blogRepository ?? new GenericRepository<Blog>(_dbContext);
        public IGenericRepository<Facility> FacilityRepository => _facilityRepository ?? new GenericRepository<Facility>(_dbContext);
        public IGenericRepository<Room> RoomRepository => _roomRepository ?? new GenericRepository<Room>(_dbContext);
        public IGenericRepository<AreaType> AreaTypeRepository => _areaTypeRepository ?? new GenericRepository<AreaType>(_dbContext);
        public IGenericRepository<Area> AreaRepository => _areaRepository ?? new GenericRepository<Area>(_dbContext);
        public IGenericRepository<Booking> BookingRepository => _bookingRepository ?? new GenericRepository<Booking>(_dbContext);
        public IGenericRepository<BookingDetail> BookingDetailRepository => _bookingDetailRepository ?? new GenericRepository<BookingDetail>(_dbContext);
        public IGenericRepository<UsingFacility> UsingFacilityRepository => _usingRepository ?? new GenericRepository<UsingFacility>(_dbContext);
        public IGenericRepository<FacilitiesStatus> FacilitiesStatusRepository => _facilityStatusRepository ?? new GenericRepository<FacilitiesStatus>(_dbContext);
        public IGenericRepository<SumaryExpense> SumaryExpenseRepository => _sumaryExpenseRepository ?? new GenericRepository<SumaryExpense>(_dbContext);
        public IGenericRepository<ContractCrawl> ContractCrawlRepository => _contractCrawlRepository ?? new GenericRepository<ContractCrawl>(_dbContext);
        public IGenericRepository<AreaTypeCategory> AreaTypeCategoryRepository => _areaTypeCategoryRepository ?? new GenericRepository<AreaTypeCategory>(_dbContext);
        public IGenericRepository<Report> ReportRepository => _reportRepository ?? new GenericRepository<Report>(_dbContext);
        public DbContext Context => _dbContext;

        public async Task CommitAsync()
        {

            //await _dbContext.SaveChangesAsync();
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw; // Ném lại exception để controller xử lý
                }
            }
        }
        public async Task RollbackAsync()
        {
            await _dbContext.DisposeAsync();
        }
    }
}
