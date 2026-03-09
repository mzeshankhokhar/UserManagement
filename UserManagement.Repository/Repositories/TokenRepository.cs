using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;

namespace UserManagement.Repository.Repositories
{
    public class TokenRepository : GenericRepository<Token>, ITokenRepository
    {
        public TokenRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User> GetUserByUserNameAsync(string userName)
        {
            var user = await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RoleClaims)
                .ThenInclude(x => x.Claim)
                .FirstOrDefaultAsync(x => x.UserName == userName);
            return user;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<Token> GetTokenAsync(int userId)
        {
            var token = await _context.Tokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsRevoked);
            return token;
        }

        public async Task<Token> GetTokenByUserNameAsync(string userName)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserName == userName);

            if (user == null) return null;

            var token = await _context.Tokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsRevoked);
            return token;
        }

        public async Task<Token> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Tokens
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        }

        public async Task<IEnumerable<Token>> GetAllUserTokensAsync(int userId)
        {
            return await _context.Tokens
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }
    }
}
