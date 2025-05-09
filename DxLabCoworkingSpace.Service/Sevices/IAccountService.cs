﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IAccountService : IGenericeService<User>
    {
        Task<IEnumerable<User>> GetUsersByRoleName(string roleName);
        Task AddFromExcel(List<User> users);
        Task SoftDelete(int id);
        Task<IEnumerable<User>> GetDeletedAccounts();
        Task Restore(int id);

        // Phương thức để lấy IQueryable cho eager loading
        Task<IQueryable<User>> GetAllQueryable();
    }
}
