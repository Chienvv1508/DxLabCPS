using DXLAB_Coworking_Space_Booking_System;
using DxLabCoworkingSpace;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        // Hàm kiểm tra Role để từ chối Admin
        private void RestrictAdminRole(Role role)
        {
            if (role?.RoleId == 1) // RoleId = 1 là Admin
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập tài khoản Admin!");
            }
        }

        // Hàm kiểm tra User để từ chối Admin
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
                                 .ToHashSet(); // Dùng HashSet để kiểm tra nhanh hơn

            var emailsInFile = users.Select(u => u.Email.Trim().ToLower()).ToList();

            // Kiểm tra email bị trùng trong file Excel
            var duplicateEmailsInFile = emailsInFile.GroupBy(e => e)
                                                   .Where(g => g.Count() > 1)
                                                   .Select(g => g.Key)
                                                   .ToList();

            if (duplicateEmailsInFile.Any())
            {
                throw new InvalidOperationException($"Email bị trùng trong file!");
            }

            // Kiểm tra email đã tồn tại trong database trước khi thêm bất kỳ dữ liệu nào
            var duplicateEmailsInDB = emailsInFile.Where(e => existingEmails.Contains(e)).ToList();
            if (duplicateEmailsInDB.Any())
            {
                throw new InvalidOperationException($"Email đã tồn tại trong database!");
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
                    validationErrors.AddRange(validationResults.Select(r => $"{r.ErrorMessage}"));
                    continue;
                }

                if (role == null)
                {
                    validationErrors.Add("RoleName không phải là Student hay Staff. Vui lòng sửa lại!");
                    continue;
                }

                RestrictAdminRole(role);

                user.Status = true;
                newUsers.Add(user);
            }

            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" ", validationErrors));
            }

            // Nếu không có lỗi nào, thêm tất cả vào database cùng một lúc
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
            return (await GetAllWithInclude(u => u.Role)).Where(u => u.RoleId == role.RoleId && u.Status);
        }

        async Task IGenericService<User>.Add(User entity)
        {
            throw new NotImplementedException();
        }

        // get all list theo status và role name(Student, Staff)
        async Task<IEnumerable<User>> IGenericService<User>.GetAll()
        {
            return (await GetAllWithInclude(u => u.Role)).Where(u => u.Status && (u.RoleId == 2 || u.RoleId == 3));
        }

        public async Task<User> Get(Expression<Func<User, bool>> expression)
        {
            return await Get(expression, u => u.Role);
        }

        public async Task<User> Get(Expression<Func<User, bool>> expression, params Expression<Func<User, object>>[] includes)
        {
            IQueryable<User> query = await GetAllQueryable();
            query = await ApplyIncludes(query, includes);
            var user = query.Where(expression).FirstOrDefault();
            if (user == null)
            {
                throw new InvalidOperationException("Không có người dùng trong database");
            }
            return user;
        }

        public async Task<IEnumerable<User>> GetAll(Expression<Func<User, bool>> expression)
        {
            throw new NotImplementedException();
        }

        async Task<User> IGenericService<User>.GetById(int id)
        {
            // Lấy user mà không lọc RoleId trước
            var user = await Get(u => u.UserId == id, u => u.Role);

            // Kiểm tra quyền truy cập
            RestrictAdminAccess(user);

            // Nếu không phải Admin, kiểm tra xem có phải Student/Staff không
            if (user.Role.RoleId != 2 && user.Role.RoleId != 3)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không phải là Student hoặc Staff!");
            }

            return user;
        }
        public async Task Update(User entity)
        {
            var existingUser = await Get(u => u.UserId == entity.UserId, u => u.Role);
            if (existingUser == null)
            {
                throw new InvalidOperationException($"Người dùng với ID {entity.UserId} không tìm thấy!");
            }
            RestrictAdminAccess(existingUser);

            // Kiểm tra RoleName từ entity.Role (nếu có)
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

                RestrictAdminRole(role); // Từ chối nếu cập nhật thành Admin
                existingUser.RoleId = role.RoleId;
                existingUser.Role = role; // Cập nhật Role
            }
            else if (entity.RoleId.HasValue) // Kiểm tra RoleId nếu không có RoleName
            {
                var role = await _unitOfWork.RoleRepository.GetById(entity.RoleId.Value);
                if (role == null)
                {
                    throw new InvalidOperationException($"Role với ID {entity.RoleId} không tìm thấy!");
                }

                RestrictAdminRole(role); // Từ chối nếu cập nhật thành Admin
                if (role.RoleId != 2 && role.RoleId != 3) // 2 và 3 là Student/Staff
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

            await _unitOfWork.UserRepository.Update(existingUser);
            _unitOfWork.Context.Entry(existingUser).State = EntityState.Modified;
            await _unitOfWork.CommitAsync();
        }
        async Task IGenericService<User>.Delete(int id)
        {
            var user = await Get(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }
            RestrictAdminAccess(user); // Từ chối nếu là Admin

            if (user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            await _unitOfWork.UserRepository.Delete(id); 
            await _unitOfWork.CommitAsync(); 
        }
        public async Task SoftDelete(int id)
        {
            var user = await Get(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            RestrictAdminAccess(user); // Từ chối nếu là Admin
            if (!user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} đã bị xóa trước đó!");
            }

            user.Status = false;
            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<User>> GetDeletedAccounts()
        {
            return (await GetAllWithInclude(u => u.Role)).Where(u => !u.Status);
        }
        public async Task Restore(int id)
        {
            var user = await Get(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }
            if (user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }
            user.Status = true;
            await _unitOfWork.UserRepository.Update(user); 
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<User>> GetAllWithInclude(params Expression<Func<User, object>>[] includes)
        {
            IQueryable<User> query = await GetAllQueryable();
            query = await ApplyIncludes(query, includes);
            return query.ToList();
        }
    }
}
