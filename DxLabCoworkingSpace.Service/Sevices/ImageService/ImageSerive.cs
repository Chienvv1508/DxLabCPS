using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DxLabCoworkingSpace
{
    public static class ImageSerive
    {
        public static async Task<Tuple<bool,List<string>>> AddImage(List<IFormFile> fromFiles)
        {
            var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(imagesDir))
            {
                Directory.CreateDirectory(imagesDir);
            }

            List<string> images = new List<string>();
            // Xử lý upload ảnh
            if (fromFiles != null && fromFiles.Any())
            {
                
                foreach (var file in fromFiles)
                {
                    if (file.Length > 0)
                    {
                        // Kiểm tra loại file (chỉ cho phép .jpg, .png)
                        if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".png"))
                        {
                            return new Tuple<bool, List<string>>(false, null);
                        }

                        // Kiểm tra kích thước file (giới hạn 5MB)
                        if (file.Length > 5 * 1024 * 1024)
                        {
                            return new Tuple<bool, List<string>>(false, null);
                        }

                        // Lấy tên file gốc để hiển thị
                        var originalFileName = Path.GetFileName(file.FileName); // Tên gốc: "myphoto.jpg"

                        // Tạo tên file duy nhất để lưu trên server
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(imagesDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        images.Add($"/Images/{uniqueFileName}");
  
                    }
                }
            }

            return new Tuple<bool, List<string>>(true, images);

        }
        public static Tuple<bool,string> RemoveImage(string relativeImagePath)
        {
            try
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeImagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                    return new Tuple<bool,string>(true,$"/Images/{relativeImagePath}");
                }

                return new Tuple<bool, string>(false, $"/Images/{relativeImagePath}");
            }
            catch
            {
                return new Tuple<bool, string>(false, $"/Images/{relativeImagePath}");
            }
        }
    }
}
