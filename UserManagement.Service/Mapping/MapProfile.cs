using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using UserManagement.Core.DTOs;
using UserManagement.Core.Model;

namespace UserManagement.Service.Mapping
{
    public class MapProfile : Profile
    {
        public MapProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserContactDto, UserContact>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<UserContact, UserContactDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
