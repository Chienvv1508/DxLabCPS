using DXLAB_Coworking_Space_Booking_System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public interface IAccountService : IGenericService<User>
    {
        IEnumerable<User> GetUsersByRoleName(string roleName);
        Task AddFromExcel(List<User> users);
        Task SoftDelete(int id);
        IEnumerable<User> GetDeletedAccounts();
        Task Restore(int id);

        // Phương thức hỗ trợ eager loading
        IEnumerable<User> GetAllWithInclude(params Expression<Func<User, object>>[] includes);

        // Mở rộng Get để hỗ trợ eager loading
        User Get(Expression<Func<User, bool>> expression, params Expression<Func<User, object>>[] includes);

        // Phương thức để lấy IQueryable cho eager loading
        IQueryable<User> GetAllQueryable();
    }
}
