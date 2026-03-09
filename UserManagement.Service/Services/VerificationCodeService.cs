using Microsoft.EntityFrameworkCore;
using UserManagement.Core.Model;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;
using UserManagement.Repository;
using System.Security.Cryptography;

namespace UserManagement.Service.Services
{
    public class VerificationCodeService : IVerificationCodeService
    {
        private readonly AppDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private const int CODE_LENGTH = 6;
        private const int RATE_LIMIT_SECONDS = 60; // 1 minute between requests

        public VerificationCodeService(AppDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateCodeAsync(int userId, string sentTo, string type, int expiryMinutes = 15)
        {
            // Invalidate any existing codes for this user and type
            await InvalidateExistingCodesAsync(userId, type);

            // Generate a secure 6-digit code
            var code = GenerateSecureCode();

            var verificationCode = new VerificationCode
            {
                UserId = userId,
                Code = code,
                Type = type,
                SentTo = sentTo,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                IsUsed = false,
                Attempts = 0,
                MaxAttempts = 5,
                CreatedDate = DateTime.UtcNow
            };

            await _context.VerificationCodes.AddAsync(verificationCode);
            await _unitOfWork.CommitAsync();

            return code;
        }

        public async Task<(bool IsValid, string Message)> VerifyCodeAsync(string sentTo, string code, string type)
        {
            var verificationCode = await _context.VerificationCodes
                .Where(v => v.SentTo == sentTo && 
                           v.Type == type && 
                           !v.IsUsed)
                .OrderByDescending(v => v.CreatedDate)
                .FirstOrDefaultAsync();

            if (verificationCode == null)
            {
                return (false, "No verification code found. Please request a new code.");
            }

            // Check if expired
            if (verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return (false, "Verification code has expired. Please request a new code.");
            }

            // Check attempts
            if (verificationCode.Attempts >= verificationCode.MaxAttempts)
            {
                return (false, "Too many failed attempts. Please request a new code.");
            }

            // Increment attempts
            verificationCode.Attempts++;
            _context.VerificationCodes.Update(verificationCode);

            // Verify code
            if (verificationCode.Code != code)
            {
                await _unitOfWork.CommitAsync();
                var remaining = verificationCode.MaxAttempts - verificationCode.Attempts;
                return (false, $"Invalid verification code. {remaining} attempts remaining.");
            }

            // Mark as used
            verificationCode.IsUsed = true;
            verificationCode.UpdatedDate = DateTime.UtcNow;
            _context.VerificationCodes.Update(verificationCode);
            await _unitOfWork.CommitAsync();

            return (true, "Code verified successfully.");
        }

        public async Task<VerificationCode> GetValidCodeAsync(string sentTo, string code, string type)
        {
            var verificationCode = await _context.VerificationCodes
                .Where(v => v.SentTo == sentTo && 
                           v.Code == code &&
                           v.Type == type && 
                           !v.IsUsed &&
                           v.ExpiresAt > DateTime.UtcNow &&
                           v.Attempts < v.MaxAttempts)
                .OrderByDescending(v => v.CreatedDate)
                .FirstOrDefaultAsync();

            return verificationCode;
        }

        public async Task InvalidateExistingCodesAsync(int userId, string type)
        {
            var existingCodes = await _context.VerificationCodes
                .Where(v => v.UserId == userId && v.Type == type && !v.IsUsed)
                .ToListAsync();

            foreach (var code in existingCodes)
            {
                code.IsUsed = true;
                code.UpdatedDate = DateTime.UtcNow;
            }

            if (existingCodes.Any())
            {
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task<(bool CanRequest, int WaitSeconds)> CanRequestNewCodeAsync(int userId, string type)
        {
            var lastCode = await _context.VerificationCodes
                .Where(v => v.UserId == userId && v.Type == type)
                .OrderByDescending(v => v.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastCode == null)
            {
                return (true, 0);
            }

            var timeSinceLastCode = (DateTime.UtcNow - lastCode.CreatedDate).TotalSeconds;
            
            if (timeSinceLastCode < RATE_LIMIT_SECONDS)
            {
                var waitSeconds = (int)(RATE_LIMIT_SECONDS - timeSinceLastCode);
                return (false, waitSeconds);
            }

            return (true, 0);
        }

        private static string GenerateSecureCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString("D6"); // Pad with zeros to ensure 6 digits
        }
    }
}
