using AutoMapper;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;

namespace UserManagement.Service.Services
{
    public class UserInvitationService : Service<UserInvitation>, IUserInvitationService
    {
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public UserInvitationService(IGenericRepository<UserInvitation> repository,
            IUnitOfWork unitOfWork,
            IUserInvitationRepository userInvitationRepository,
            IMapper mapper) :
            base(repository, unitOfWork)
        {
            _userInvitationRepository = userInvitationRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        
    }
}
