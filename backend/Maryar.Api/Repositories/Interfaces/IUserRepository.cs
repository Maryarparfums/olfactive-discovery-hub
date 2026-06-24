using System;
using Maryar.Api.Dtos;
using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IUserRepository
    {
        User GetByEmail(string email);
        User GetById(Guid id);
        Guid Create(User u);
        void UpdatePassword(Guid id, string passwordHash);
        void UpdateProfile(Guid id, UserProfileDto dto);
        Guid UpsertByEmail(UserProfileDto dto);
        void MarkEmailVerified(Guid id);
        void UpdateEmail(Guid id, string newEmail);
        void SetPendingEmail(Guid userId, string pendingEmail);
        string GetPendingEmail(Guid userId);
    }
}
