using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IPasswordResetRepository
    {
        void Create(PasswordResetToken token);
        PasswordResetToken GetByToken(string token);
        void MarcarComoUsado(int id);
        void InvalidarAnteriores(string userId, string type);
    }
}
