using System;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IUserRepository
    {
        User GetByEmail(string email);
        User GetById(Guid id);
        Guid Create(User u);
        void UpdatePassword(Guid id, string passwordHash);
        void UpsertByEmail(UserProfileDto dto);
    }
}
