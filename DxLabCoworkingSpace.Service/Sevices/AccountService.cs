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

        private IQueryable<User> ApplyIncludes(IQueryable<User> query, params Expression<Func<User, object>>[] includes)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return query;
        }

        public IQueryable<User> GetAllQueryable()
        {
            return _unitOfWork.Context.Set<User>().AsQueryable();
        }

        public async Task AddFromExcel(List<User> users)
        {
            if (users == null || !users.Any())
            {
                throw new ArgumentException("Danh sách người dùng không được rỗng hoặc null");
            }

            var existingEmails = _unitOfWork.UserRepository.GetAll().Select(u => u.Email).ToList();
            var validationErrors = new List<string>();

            var emailsInFile = users.Select(u => u.Email).ToList();
            var duplicateEmailsInFile = emailsInFile.GroupBy(e => e)
                                                   .Where(g => g.Count() > 1)
                                                   .Select(g => g.Key)
                                                   .ToList();

            if (duplicateEmailsInFile.Any())
            {
                throw new InvalidOperationException($"Email bị trùng: {string.Join(", ", duplicateEmailsInFile)}.");
            }

            var roleIds = users.Where(u => u.RoleId.HasValue).Select(u => u.RoleId.Value).Distinct().ToList();
            var roles = _unitOfWork.RoleRepository.GetAll().Where(r => roleIds.Contains(r.RoleId)).ToDictionary(r => r.RoleId, r => r);

            foreach (var user in users)
            {
                Role role = null;
                if (user.RoleId.HasValue && roles.ContainsKey(user.RoleId.Value))
                {
                    role = roles[user.RoleId.Value];
                }

                var dto = new AccountDTO
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    RoleName = role?.RoleName ?? string.Empty,
                    Avatar = user.Avatar,
                    WalletAddress = user.WalletAddress,
                    Status = user.Status
                };

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    validationErrors.AddRange(validationResults.Select(r => $"{r.ErrorMessage} (Email: {user.Email})"));
                    continue;
                }

                if (existingEmails.Contains(user.Email))
                {
                    throw new InvalidOperationException($"Email đã tồn tại trong database");
                }

                if (role == null)
                {
                    throw new InvalidOperationException($"RoleName không phải là Student hay Staff. Vui lòng sửa lại!");
                }

                user.Status = true;
                user.AccessToken = Guid.NewGuid().ToString();
                _unitOfWork.UserRepository.Add(user); 
            }

            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" ", validationErrors));
            }

            await _unitOfWork.CommitAsync();
        }

        public IEnumerable<User> GetUsersByRoleName(string roleName)
        {
            var role = _unitOfWork.RoleRepository.GetAll().FirstOrDefault(r => r.RoleName == roleName);
            if (role == null)
            {
                throw new InvalidOperationException($"Role với tên {roleName} không tìm thấy");
            }

            return GetAllWithInclude(u => u.Role).Where(u => u.RoleId == role.RoleId && u.Status);
        }

        async Task IGenericService<User>.Add(User entity)
        {
            throw new NotImplementedException();
        }

        IEnumerable<User> IGenericService<User>.GetAll()
        {
            return GetAllWithInclude(u => u.Role).Where(u => u.Status);
        }

        public User Get(Expression<Func<User, bool>> expression)
        {
            return Get(expression, u => u.Role);
        }

        public User Get(Expression<Func<User, bool>> expression, params Expression<Func<User, object>>[] includes)
        {
            IQueryable<User> query = GetAllQueryable();
            query = ApplyIncludes(query, includes);
            var user = query.Where(expression).FirstOrDefault();
            if (user == null)
            {
                throw new InvalidOperationException("Không có người dùng trong database");
            }
            return user;
        }

        public IEnumerable<User> GetAll(Expression<Func<User, bool>> expression)
        {
            throw new NotImplementedException();
        }

        User IGenericService<User>.GetById(int id)
        {
            return Get(u => u.UserId == id, u => u.Role);
        }
        public async Task Update(User entity)
        {
            var existingUser = Get(u => u.UserId == entity.UserId, u => u.Role);
            if (existingUser == null)
            {
                throw new InvalidOperationException($"Người dùng với ID {entity.UserId} không tìm thấy!");
            }

            if (existingUser.Email != entity.Email || existingUser.FullName != entity.FullName ||
                existingUser.Avatar != entity.Avatar || existingUser.WalletAddress != entity.WalletAddress ||
                existingUser.Status != entity.Status || existingUser.AccessToken != entity.AccessToken)
            {
                throw new InvalidOperationException("Chỉ có Role mới thay đổi được. Những trường khác không thay đổi được!");
            }

            if (!string.IsNullOrEmpty(entity.Role?.RoleName))
            {
                var role = _unitOfWork.RoleRepository.GetAll().FirstOrDefault(r => r.RoleName == entity.Role.RoleName);
                if (role != null)
                {
                    existingUser.RoleId = role.RoleId;
                    existingUser.Role = role; // Cập nhật Role để giữ RoleName mới
                }
                else if (entity.RoleId.HasValue)
                {
                    role = _unitOfWork.RoleRepository.GetById(entity.RoleId.Value);
                    if (role != null)
                    {
                        existingUser.RoleId = role.RoleId;
                        existingUser.Role = role;
                    }
                }
                if (role == null)
                {
                    throw new InvalidOperationException($"Role với tên {entity.Role?.RoleName} hoặc ID {entity.RoleId} không tìm thấy!");
                }
            }

            await _unitOfWork.UserRepository.Update(existingUser);
            _unitOfWork.Context.Entry(existingUser).State = EntityState.Modified;
            await _unitOfWork.CommitAsync(); 
        }


        async Task IGenericService<User>.Delete(int id)
        {
            var user = Get(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            if (user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }

            await _unitOfWork.UserRepository.Delete(id); 
            await _unitOfWork.CommitAsync(); 
        }
        public async Task SoftDelete(int id)
        {
            var user = Get(u => u.UserId == id, u => u.Role);
            if (user == null)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }
            if (!user.Status)
            {
                throw new InvalidOperationException($"Người dùng với ID: {id} không tìm thấy!");
            }
            user.Status = false;
            await _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();
        }

        public IEnumerable<User> GetDeletedAccounts()
        {
            return GetAllWithInclude(u => u.Role).Where(u => !u.Status);
        }
        public async Task Restore(int id)
        {
            var user = Get(u => u.UserId == id, u => u.Role);
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

        public IEnumerable<User> GetAllWithInclude(params Expression<Func<User, object>>[] includes)
        {
            IQueryable<User> query = GetAllQueryable();
            query = ApplyIncludes(query, includes);
            return query.ToList();
        }
    }
}
