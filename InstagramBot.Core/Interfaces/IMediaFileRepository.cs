using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    
    public interface IMediaFileRepository
    {
        Task<MediaFile> CreateAsync(MediaFile mediaFile);
        Task<MediaFile> GetByIdAsync(int id);
        Task<List<MediaFile>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<MediaFile> UpdateAsync(MediaFile mediaFile);
        Task<bool> DeleteAsync(int id);
    }

}
