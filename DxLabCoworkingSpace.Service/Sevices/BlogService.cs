using DxLabCoworkingSpace.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(Blog entity)
        {
            if (entity.BlogCreatedDate > DateTime.UtcNow)
            {
                throw new ArgumentException("Ngày tạo blog không thể nằm trong tương lai!");
            }

            entity.Status = (int)BlogDTO.BlogStatus.Pending;
            await _unitOfWork.BlogRepository.Add(entity);
            await _unitOfWork.CommitAsync();
        }
        public async Task<Blog> Get(Expression<Func<Blog, bool>> expression)
        {
            return await _unitOfWork.BlogRepository.Get(expression);
        }

        public async Task<IEnumerable<Blog>> GetAll()
        {
            return await _unitOfWork.BlogRepository.GetAll();
        }

        public async Task<IEnumerable<Blog>> GetAll(Expression<Func<Blog, bool>> expression)
        {
            return await _unitOfWork.BlogRepository.GetAll(expression);
        }

        async Task<Blog> IGenericService<Blog>.GetById(int id)
        {
            return await _unitOfWork.BlogRepository.GetById(id);
        }

        public async Task Update(Blog entity)
        {
            await _unitOfWork.BlogRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        async Task IGenericService<Blog>.Delete(int id)
        {
            await _unitOfWork.BlogRepository.Delete(id);
            await _unitOfWork.CommitAsync();
        }

        // Phương thức đặc thù
        public async Task EditCancelledBlog(int id, Blog updatedBlog)
        {
            var blog = await _unitOfWork.BlogRepository.GetById(id);
            if (blog == null) throw new Exception("Không tìm thấy blog");
            if (blog.Status != (int)BlogDTO.BlogStatus.Cancel)
                throw new Exception("Chỉ blog có trạng thái Cancel mới được chỉnh sửa");

            blog.BlogTitle = updatedBlog.BlogTitle;
            blog.BlogContent = updatedBlog.BlogContent;
            blog.Status = (int)BlogDTO.BlogStatus.Pending;

            await _unitOfWork.BlogRepository.Update(blog);
            await _unitOfWork.CommitAsync();
        }

        public async Task ApproveBlog(int id)
        {
            var blog = await _unitOfWork.BlogRepository.GetById(id);
            if (blog == null) throw new Exception("Không tìm thấy blog");
            if (blog.Status != (int)BlogDTO.BlogStatus.Pending)
                throw new Exception("Chỉ blog có trạng thái Pending mới được duyệt");

            blog.Status = (int)BlogDTO.BlogStatus.Approve;
            await _unitOfWork.BlogRepository.Update(blog);
            await _unitOfWork.CommitAsync();
        }

        public async Task CancelBlog(int id)
        {
            var blog = await _unitOfWork.BlogRepository.GetById(id);
            if (blog == null) throw new Exception("Không tìm thấy blog");
            if (blog.Status != (int)BlogDTO.BlogStatus.Pending)
                throw new Exception("Chỉ blog có trạng thái Pending mới được hủy");

            blog.Status = (int)BlogDTO.BlogStatus.Cancel;
            await _unitOfWork.BlogRepository.Update(blog);
            await _unitOfWork.CommitAsync();
        }

        // Phương thức hỗ trợ join với User (dùng trong ApprovalBlogController)
        public async Task<Blog> GetByIdWithUser(int id)
        {
            var blog = await _unitOfWork.BlogRepository.GetById(id);
            if (blog != null && blog.UserId.HasValue)
            {
                blog.User = await _unitOfWork.UserRepository.GetById(blog.UserId.Value);
            }
            return blog;
        }

        public async Task<IEnumerable<Blog>> GetAllWithUser(Expression<Func<Blog, bool>> expression)
        {
            var blogs = await _unitOfWork.BlogRepository.GetAll(expression);
            var blogList = blogs.ToList();
            foreach (var blog in blogList)
            {
                if (blog.UserId.HasValue)
                {
                    blog.User = await _unitOfWork.UserRepository.GetById(blog.UserId.Value);
                }
            }
            return blogList;
        }
    }
}
