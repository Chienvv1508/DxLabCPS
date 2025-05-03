
using DxLabCoworkingSpace;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DxLabCoworkingSpace
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        private async Task<IQueryable<User>> ApplyIncludes(IQueryable<User> query, params Expression<Func<User, object>>[] includes)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return query;
        }

        public async Task<IQueryable<User>> GetAllQueryable()
        {
            return await Task.FromResult(_unitOfWork.Context.Set<User>().AsQueryable());
        }

        private void RestrictAdminRole(Role role)
        {
            if (role?.RoleId == 1) // RoleId = 1 là Admin
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập tài khoản Admin!");
            }
        }

        private void RestrictAdminAccess(User user)
        {
            if (user?.RoleId == 1) // RoleId = 1 là Admin
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập tài khoản Admin!");
            }
        }

        public async Task AddFromExcel(List<User> users)
        {
            if (users == null || !users.Any())
            {
                throw new ArgumentException("Danh sách người dùng không được rỗng hoặc null");
            }

            var existingEmails = (await _unitOfWork.UserRepository.GetAll())
                                 .Select(u => u.Email.Trim().ToLower())
                                 .ToHashSet();

            var emailsInFile = users.Select(u => u.Email.Trim().ToLower()).ToList();
            var duplicateEmailsInFile = emailsInFile.GroupBy(e => e)
                                                   .Where(g => g.Count() > 1)
                                                   .Select(g => g.Key)
                                                   .ToList();

            if (duplicateEmailsInFile.Any())
            {
                throw new InvalidOperationException("Email bị trùng trong file");
            }

            var duplicateEmailsInDB = emailsInFile.Where(e => existingEmails.Contains(e)).ToList();
            if (duplicateEmailsInDB.Any())
            {
                throw new InvalidOperationException("Email đã tồn tại trong database");
            }

            var validationErrors = new List<string>();
            var roleIds = users.Where(u => u.RoleId.HasValue).Select(u => u.RoleId.Value).Distinct().ToList();
            var roles = (await _unitOfWork.RoleRepository.GetAll())
                         .Where(r => roleIds.Contains(r.RoleId))
                         .ToDictionary(r => r.RoleId, r => r);

            var newUsers = new List<User>();

            foreach (var user in users)
            {
                Role role = null;
                if (user.RoleId.HasValue && roles.ContainsKey(user.RoleId.Value))
                {
                    role = roles[user.RoleId.Value];
                }

                var dto = new AccountDTO
                {
                    Email = user.Email.Trim().ToLower(),
                    FullName = user.FullName,
                    RoleName = role?.RoleName ?? string.Empty,
                    Status = user.Status
                };

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    validationErrors.AddRange(validationResults.Select(r => $"{dto.Email}: {r.ErrorMessage}"));
                    continue;
                }

                if (role == null)
                {
                    validationErrors.Add($"{dto.Email}: RoleName không phải là Student hay Staff. Vui lòng sửa lại!");
                    continue;
                }

                RestrictAdminRole(role);
                user.Status = true;
                newUsers.Add(user);
            }

            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" | ", validationErrors));
            }

            foreach (var newUser in newUsers)
            {
                await _unitOfWork.UserRepository.Add(newUser);
            }
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleName(string roleName)
        {
            var role = (await _unitOfWork.RoleRepository.GetAll()).FirstOrDefault(r => r.RoleName == roleName);
            if (role == null)
            {
                throw new InvalidOperationException($"Role với tên {roleName} không tìm thấy");
            }

            RestrictAdminRole(role);
            if (role.RoleId != 2 && role.RoleId != 3)
            {
                throw new InvalidOperationException("Chỉ có thể truy xuất tài khoản với Role là Student hoặc Staff!");
            }

            return (await this.GetAllWithInclude(u => u.Role))
                .Where(u => u.RoleId == role.RoleId && u.Status);
        }

        public async Task<User> Get(Expression<Func<User, bool>> expression)
        {
            var user = await _unitOfWork.UserRepository.Get(expression);
            if (user == null)
            {
                throw new InvalidOperationException("Không tìm thấy người dùng phù hợp.");
            }
            return user;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return (await this.GetAllWithInclude(u => u.Role))
                .Where(u => u.Status && (u.RoleId == 2 || u.RoleId == 3));
        }

        public async Task<IEnumerable<User>> GetAll(Expression<Func<User, bool>> expression)
        {
            return await _unitOfWork.UserRepository.GetAll(expression);
        }

        public async Task<User> GetById(int id)
        {
            var user = await _unitOfWork.UserRepository.GetWithInclude(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            RestrictAdminAccess(user);
            if (user.RoleId != 2 && user.RoleId != 3)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không phải là Student hoặc Staff!");
            }
            return user;
        }

        public async Task<IEnumerable<User>> GetAllWithInclude(params Expression<Func<User, object>>[] includes)
        {
            return await _unitOfWork.UserRepository.GetAllWithInclude(includes);
        }
        public async Task<User> GetWithInclude(Expression<Func<User, bool>> expression, params Expression<Func<User, object>>[] includes)
        {
            var user = await _unitOfWork.UserRepository.GetWithInclude(expression, includes);
            if (user == null)
            {
                throw new InvalidOperationException("Không tìm thấy người dùng phù hợp.");
            }
            return user;
        }

        public async Task Add(User entity)
        {
            throw new NotImplementedException("Sử dụng AddFromExcel để thêm người dùng.");
        }

        public async Task Update(User entity)
        {
            var existingUser = await this.GetWithInclude(u => u.UserId == entity.UserId, u => u.Role);
            if (existingUser == null)
            {
                throw new InvalidOperationException($"Người dùng với ID {entity.UserId} không tìm thấy!");
            }
            RestrictAdminAccess(existingUser);

            if (entity.Role != null && !string.IsNullOrEmpty(entity.Role.RoleName))
            {
                var validRoles = new[] { "Student", "Staff" };
                if (!validRoles.Contains(entity.Role.RoleName))
                {
                    throw new InvalidOperationException("RoleName phải là 'Student' hoặc 'Staff'!");
                }

                var role = (await _unitOfWork.RoleRepository.GetAll()).FirstOrDefault(r => r.RoleName == entity.Role.RoleName);
                if (role == null)
                {
                    throw new InvalidOperationException($"Role với tên {entity.Role.RoleName} không tìm thấy!");
                }

                RestrictAdminRole(role);
                existingUser.RoleId = role.RoleId;
                existingUser.Role = role;
            }
            else if (entity.RoleId.HasValue)
            {
                var role = await _unitOfWork.RoleRepository.GetById(entity.RoleId.Value);
                if (role == null)
                {
                    throw new InvalidOperationException($"Role với ID {entity.RoleId} không tìm thấy!");
                }

                RestrictAdminRole(role);
                if (role.RoleId != 2 && role.RoleId != 3)
                {
                    throw new InvalidOperationException("Chỉ có thể cập nhật Role thành Student hoặc Staff!");
                }
                existingUser.RoleId = role.RoleId;
                existingUser.Role = role;
            }
            else
            {
                throw new InvalidOperationException("RoleName hoặc RoleId là bắt buộc để cập nhật role!");
            }

            if (!string.IsNullOrEmpty(entity.Email))
            {
                existingUser.Email = entity.Email;
            }
            if (!string.IsNullOrEmpty(entity.FullName))
            {
                existingUser.FullName = entity.FullName;
            }
            if (entity.Status != existingUser.Status) 
            {
                existingUser.Status = entity.Status;
            }

            await _unitOfWork.UserRepository.Update(existingUser);
            await _unitOfWork.CommitAsync();
        }

        public async Task Delete(int id)
        {
            var user = await this.GetWithInclude(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }
            RestrictAdminAccess(user);

            if (user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} chưa được xóa mềm. Sử dụng SoftDelete.");
            }

            // Lấy tất cả Blog của User với Images liên quan
            var blogs = await _unitOfWork.Context.Set<Blog>()
                .Where(b => b.UserId == id)
                .Include(b => b.Images)
                .ToListAsync();

            foreach (var blog in blogs)
            {
                // Xóa các file ảnh vật lý trong wwwroot/images (nếu có)
                if (blog.Images != null && blog.Images.Any())
                {
                    foreach (var image in blog.Images)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                    // Xóa các bản ghi Image trong cơ sở dữ liệu
                    _unitOfWork.Context.Set<Image>().RemoveRange(blog.Images);
                }
                // Xóa Blog
                await _unitOfWork.BlogRepository.Delete(blog.BlogId); 
            }

            // Lấy tất cả Bookings của User
            var bookings = await _unitOfWork.Context.Set<Booking>()
                .Where(b => b.UserId == id)
                .Include(b => b.BookingDetails) // Bao gồm BookingDetails
                .ToListAsync();

            foreach (var booking in bookings)
            {
                // Xử lý BookingDetails
                if (booking.BookingDetails != null && booking.BookingDetails.Any())
                {
                    // Lấy tất cả Reports liên quan đến BookingDetails
                    var bookingDetailIds = booking.BookingDetails.Select(bd => bd.BookingDetailId).ToList();
                    var reports = await _unitOfWork.Context.Set<Report>()
                        .Where(r => r.BookingDetailId != null && bookingDetailIds.Contains(r.BookingDetailId.Value))
                        .ToListAsync();

                    // Xóa các bản ghi Reports
                    if (reports.Any())
                    {
                        _unitOfWork.Context.Set<Report>().RemoveRange(reports);
                    }

                    // Xóa các bản ghi BookingDetails
                    _unitOfWork.Context.Set<BookingDetail>().RemoveRange(booking.BookingDetails);
                }
            }

            // Xóa các bản ghi Bookings
            _unitOfWork.Context.Set<Booking>().RemoveRange(bookings);

            // Xóa các bản ghi Notifications (trực tiếp liên quan đến User)
            var userNotifications = await _unitOfWork.Context.Set<Notification>()
                .Where(n => n.UserId == id)
                .ToListAsync();
            _unitOfWork.Context.Set<Notification>().RemoveRange(userNotifications);

            // Xóa các bản ghi Reports (trực tiếp liên quan đến User)
            var userReports = await _unitOfWork.Context.Set<Report>()
                .Where(r => r.UserId == id)
                .ToListAsync();
            _unitOfWork.Context.Set<Report>().RemoveRange(userReports);

            // Xóa User
            await _unitOfWork.UserRepository.Delete(id);
            await _unitOfWork.CommitAsync();
        }

        public async Task SoftDelete(int id)
        {
            var user = await this.GetWithInclude(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            RestrictAdminAccess(user);
            if (!user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} đã bị xóa mềm trước đó!");
            }

            user.Status = false;
            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<User>> GetDeletedAccounts()
        {
            return (await this.GetAllWithInclude(u => u.Role)).Where(u => !u.Status);
        }

        public async Task Restore(int id)
        {
            var user = await this.GetWithInclude(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            if (user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không ở trạng thái xóa mềm!");
            }

            user.Status = true;
            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();
        }


    }
}