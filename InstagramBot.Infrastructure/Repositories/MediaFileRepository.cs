using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class MediaFileRepository : IMediaFileRepository
    {
        private readonly ApplicationDbContext _context;

        public MediaFileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MediaFile> CreateAsync(MediaFile mediaFile)
        {
            _context.MediaFiles.Add(mediaFile);
            await _context.SaveChangesAsync();
            return mediaFile;
        }

        public async Task<MediaFile> GetByIdAsync(int id)
        {
            return await _context.MediaFiles
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        }

        public async Task<List<MediaFile>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.MediaFiles
                .Where(m => m.UserId == userId && !m.IsDeleted)
                .OrderByDescending(m => m.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<MediaFile> UpdateAsync(MediaFile mediaFile)
        {
            _context.MediaFiles.Update(mediaFile);
            await _context.SaveChangesAsync();
            return mediaFile;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var mediaFile = await GetByIdAsync(id);
            if (mediaFile == null) return false;

            _context.MediaFiles.Remove(mediaFile);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

