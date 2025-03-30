using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IBlogService : IFaciStatusService<Blog>
    {
        Task EditCancelledBlog(int id, Blog updatedBlog);
        Task ApproveBlog(int id);
        Task CancelBlog(int id);
        Task<Blog> GetByIdWithUser(int id); // Thêm cho Admin
        Task<IEnumerable<Blog>> GetAllWithUser(Expression<Func<Blog, bool>> expression); // Thêm cho Admin
        Task<IEnumerable<Blog>> GetAllWithInclude(Expression<Func<Blog, bool>> expression, params Expression<Func<Blog, object>>[] includes);
    }
}
