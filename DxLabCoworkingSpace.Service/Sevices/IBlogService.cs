using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public interface IBlogService : IGenericService<Blog>
    {
        Task EditCancelledBlog(int id, Blog updatedBlog);
        Task ApproveBlog(int id);
        Task CancelBlog(int id);
        Task<Blog> GetByIdWithUser(int id); // Thêm cho Admin
        Task<IEnumerable<Blog>> GetAllWithUser(Expression<Func<Blog, bool>> expression); // Thêm cho Admin
    }
}
