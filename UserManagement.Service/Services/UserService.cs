using AutoMapper;
using UserManagement.Core.DTOs;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;
using UserManagement.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Service.Services
{
    public class UserService : Service<User>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public UserService(IGenericRepository<User> repository,
            IUnitOfWork unitOfWork,
            IUserRepository userRepository,
            IMapper mapper) :
            base(repository, unitOfWork)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }
    }
}
