using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface INotificationService : IGenericeService<Notification>
    {
      Task<ResponseDTO<object>> Add(Notification noti, int userId);
    }
}
