using UserManagement.Core.Model;

namespace UserManagement.Core.Repositories
{
    public interface ITokenRepository : IGenericRepository<Token>
    {
        Task<User> GetUserByUserNameAsync(string userName);
        Task<User> GetUserByIdAsync(int userId);
        Task<Token> GetTokenAsync(int userId);
        Task<Token> GetTokenByUserNameAsync(string userName);
        Task<Token> GetByRefreshTokenAsync(string refreshToken);
        Task<IEnumerable<Token>> GetAllUserTokensAsync(int userId);
    }
}
