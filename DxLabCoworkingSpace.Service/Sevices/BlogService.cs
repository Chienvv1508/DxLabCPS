using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DxLabCoworkingSpace.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DxLabCoworkingSpace.Service.Sevices
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task Add(Blog entity)
        {
            // Gán BlogCreatedDate bằng thời gian thực tế trước khi lưu
            entity.BlogCreatedDate = DateTime.Now;
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
            return await _unitOfWork.BlogRepository.GetAllWithInclude(x => x.Images);
        }

        public async Task<IEnumerable<Blog>> GetAll(Expression<Func<Blog, bool>> expression)
        {
            return await GetAllWithInclude(expression, x => x.Images);
        }

        public async Task<Blog> GetById(int id)
        {
            return await _unitOfWork.BlogRepository.GetById(id);
        }

        public async Task<IEnumerable<Blog>> GetAllWithInclude(params Expression<Func<Blog, object>>[] includes)
        {
            return await _unitOfWork.BlogRepository.GetAllWithInclude(includes);
        }

        public async Task<Blog> GetWithInclude(Expression<Func<Blog, bool>> expression, params Expression<Func<Blog, object>>[] includes)
        {
            return await _unitOfWork.BlogRepository.GetWithInclude(expression, includes);
        }

        public async Task<IEnumerable<Blog>> GetAllWithInclude(Expression<Func<Blog, bool>> expression, params Expression<Func<Blog, object>>[] includes)
        {
            IQueryable<Blog> query = _unitOfWork.Context.Set<Blog>().AsQueryable();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.Where(expression).ToListAsync();
        }

        public async Task Update(Blog entity)
        {
            await _unitOfWork.BlogRepository.Update(entity);
            await _unitOfWork.CommitAsync();
        }

        public async Task Delete(int id)
        {
            var blog = await GetByIdWithUser(id);
            if (blog == null)
            {
                throw new Exception($"Blog với ID: {id} không tìm thấy!");
            }

            if (blog.Status != (int)BlogDTO.BlogStatus.Approve)
            {
                throw new Exception("Chỉ blog được duyệt mới có thể xóa!");
            }

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

                // Xóa các bản ghi ảnh trong cơ sở dữ liệu
                _unitOfWork.Context.Set<Image>().RemoveRange(blog.Images);
            }

            // Xóa blog
            await _unitOfWork.BlogRepository.Delete(id);
            await _unitOfWork.CommitAsync();
        }
        public async Task EditCancelledBlog(int id, Blog updatedBlog)
        {
            var blog = await GetWithInclude(b => b.BlogId == id, x => x.Images);
            if (blog == null)
            {
                throw new Exception("Không tìm thấy blog");
            }

            if (blog.Status != (int)BlogDTO.BlogStatus.Cancel)
            {
                throw new Exception("Chỉ blog có trạng thái Cancel mới được chỉnh sửa");
            }

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

                // Xóa các bản ghi ảnh trong cơ sở dữ liệu
                _unitOfWork.Context.Set<Image>().RemoveRange(blog.Images);
                blog.Images.Clear(); // Làm sạch danh sách ảnh trong bộ nhớ
            }

            // Cập nhật thông tin blog
            blog.BlogTitle = updatedBlog.BlogTitle;
            blog.BlogContent = updatedBlog.BlogContent;
            blog.Status = (int)BlogDTO.BlogStatus.Pending;

            // Thêm ảnh mới (nếu có)
            if (updatedBlog.Images != null)
            {
                blog.Images = updatedBlog.Images.Select(img => new Image { ImageUrl = img.ImageUrl }).ToList();
            }

            await _unitOfWork.BlogRepository.Update(blog);
            await _unitOfWork.CommitAsync();
        }
        public async Task ApproveBlog(int id)
        {
            var blog = await GetById(id);
            if (blog == null)
            {
                throw new Exception("Không tìm thấy blog");
            }
            if (blog.Status != (int)BlogDTO.BlogStatus.Pending)
            {
                throw new Exception("Chỉ blog có trạng thái Pending mới được duyệt");
            }
                
            blog.Status = (int)BlogDTO.BlogStatus.Approve;
            await _unitOfWork.BlogRepository.Update(blog);
            await _unitOfWork.CommitAsync();
        }

        public async Task CancelBlog(int id)
        {
            var blog = await GetById(id);
            if (blog == null)
            {
                throw new Exception("Không tìm thấy blog");
            }
            if (blog.Status != (int)BlogDTO.BlogStatus.Pending)
            {
                throw new Exception("Chỉ blog có trạng thái Pending mới được hủy");
            }    

            blog.Status = (int)BlogDTO.BlogStatus.Cancel;
            await _unitOfWork.BlogRepository.Update(blog);
            await _unitOfWork.CommitAsync();
        }

        public async Task<Blog> GetByIdWithUser(int id)
        {
            return await _unitOfWork.BlogRepository.GetWithInclude(b => b.BlogId == id, x => x.Images, x => x.User);
        }

        public async Task<IEnumerable<Blog>> GetAllWithUser(Expression<Func<Blog, bool>> expression)
        {
            return await GetAllWithInclude(expression, x => x.Images, x => x.User); // Sử dụng phương thức mới
        }
    }
}