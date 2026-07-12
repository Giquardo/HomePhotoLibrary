using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories;

public interface IShareLinkRepository
{
    Task<ShareLink> CreateAsync(ShareLink shareLink);
    Task<ShareLink?> GetByTokenAsync(string token);
    Task<ShareLink?> GetByTokenForOwnerAsync(string token, int ownerUserId);
    Task<IEnumerable<ShareLink>> GetActiveByOwnerAsync(int ownerUserId);
    Task SaveChangesAsync();
}

public class ShareLinkRepository : IShareLinkRepository
{
    private readonly PhotoAlbumContext _context;

    public ShareLinkRepository(PhotoAlbumContext context)
    {
        _context = context;
    }

    public async Task<ShareLink> CreateAsync(ShareLink shareLink)
    {
        await _context.ShareLinks.AddAsync(shareLink);
        await _context.SaveChangesAsync();
        return shareLink;
    }

    public async Task<ShareLink?> GetByTokenAsync(string token)
    {
        return await _context.ShareLinks.FirstOrDefaultAsync(s => s.Token == token);
    }

    public async Task<ShareLink?> GetByTokenForOwnerAsync(string token, int ownerUserId)
    {
        return await _context.ShareLinks
            .FirstOrDefaultAsync(s => s.Token == token && s.CreatedByUserId == ownerUserId);
    }

    public async Task<IEnumerable<ShareLink>> GetActiveByOwnerAsync(int ownerUserId)
    {
        var now = DateTime.UtcNow;
        return await _context.ShareLinks
            .Where(s => s.CreatedByUserId == ownerUserId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
