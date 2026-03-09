using Microsoft.AspNetCore.Mvc;
using UserManagement.Core.DTOs;
using UserManagement.Core.Services;
using UserManagement.Core.Model;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;

namespace UserManagement.API.Controllers
{
    /// <summary>
    /// Handles user contact operations
    /// </summary>
    [Authorize]
    public class UserContactController : CustomBaseController
    {
        private readonly IUserContactService _userContactService;
        private readonly IMapper _mapper;

        public UserContactController(
            IUserContactService userContactService,
            IMapper mapper)
        {
            _userContactService = userContactService;
            _mapper = mapper;
        }

        /// <summary>
        /// Save or update user contacts
        /// </summary>
        [HttpPost("[action]")]
        public async Task<IActionResult> SaveContacts([FromBody] List<UserContactDto> userContactDtos)
        {
            if (!TryGetUserId(out var userId))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(401, "Invalid user ID"));
            }

            var contacts = _mapper.Map<List<UserContact>>(userContactDtos);
            await _userContactService.AddOrUpdateContactsAsync(contacts, userId);

            return CreateActionResult(CustomResponseDto<string>.Success(200, "Contacts saved successfully"));
        }

        /// <summary>
        /// Get all contacts for current user
        /// </summary>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetContacts()
        {
            if (!TryGetUserId(out var userId))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(401, "Invalid user ID"));
            }

            var userContacts = await _userContactService.GetUserContacts(userId);
            var userContactsDto = _mapper.Map<List<UserContactDto>>(userContacts);

            return CreateActionResult(CustomResponseDto<List<UserContactDto>>.Success(200, userContactsDto));
        }

        /// <summary>
        /// Get a specific contact by ID
        /// </summary>
        [HttpGet("[action]/{contactId}")]
        public async Task<IActionResult> GetContactById(int contactId)
        {
            if (!TryGetUserId(out var userId))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(401, "Invalid user ID"));
            }

            var contact = await _userContactService.GetUserContactById(contactId, userId);
            if (contact == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "Contact not found"));
            }

            var contactDto = _mapper.Map<UserContactDto>(contact);
            return CreateActionResult(CustomResponseDto<UserContactDto>.Success(200, contactDto));
        }

        #region Private Helpers

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claim = User.Claims.FirstOrDefault(x => x.Type == "sid")?.Value;
            return int.TryParse(claim, out userId);
        }

        #endregion
    }
}