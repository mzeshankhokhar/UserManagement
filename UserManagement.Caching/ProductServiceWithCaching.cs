using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UserManagement.Core.DTOs;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;
using UserManagement.Service.Exceptions;

namespace NLayerArhitecture.Caching
{
    public class ProductServiceWithCaching : IUserService
    {
        private const string CacheUserKey = "usersCache";
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache;
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductServiceWithCaching(IUnitOfWork unitOfWork, IUserRepository repository, IMemoryCache memoryCache, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _memoryCache = memoryCache;
            _mapper = mapper;

            if (!_memoryCache.TryGetValue(CacheUserKey, out _))
            {
                _memoryCache.Set(CacheUserKey, _repository.GetAll());
            }


        }

        public async Task<User> AddAsync(User entity)
        {
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync();
            return entity;
        }

        public async Task<IEnumerable<User>> AddRangeAsync(IEnumerable<User> entities)
        {
            await _repository.AddRangeAsync(entities);
            await _unitOfWork.CommitAsync();
            return entities;
        }

        public Task<bool> AnyAsync(Expression<Func<User, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {

            var products = _memoryCache.Get<IEnumerable<User>>(CacheUserKey);
            return Task.FromResult(products);
        }

        public Task<User> GetByIdAsync(int id)
        {
            var product = _memoryCache.Get<List<User>>(CacheUserKey).FirstOrDefault(x => x.Id == id);

            if (product == null)
            {
                throw new NotFoundException($"{typeof(User).Name}({id}) not found");
            }

            return Task.FromResult(product);
        }

        public Task<UserDto> GetUserByUserName(string userName)
        {
            throw new NotImplementedException();
        }

        Task<User> IService<User>.GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public IQueryable<User> Where(Expression<Func<User, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(User entity)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(User entity)
        {
            throw new NotImplementedException();
        }

        public Task RemoveRangeAsync(IEnumerable<User> entities)
        {
            throw new NotImplementedException();
        }

        public Task<List<UserDto>> GetUserByFirstName(string firstName)
        {
            throw new NotImplementedException();
        }
    }
}
