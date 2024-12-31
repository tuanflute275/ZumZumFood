﻿using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using ZumZumFood.Domain.Entities;

namespace ZumZumFood.Domain.Abstracts
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync(Expression<Func<User, bool>> expression = null,
           Func<IQueryable<User>, IIncludableQueryable<User, object>>? include = null);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> SaveOrUpdateAsync(User user);
        Task<bool> DeleteAsync(User user);
    }
}
