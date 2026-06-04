using Maryar.Api.Models;

namespace Maryar.Api.Repositories.Interfaces
{
    public interface IPaymentEventRepository
    {
        // Insere se eventId não existir (idempotência). Retorna true se inseriu.
        bool TryInsert(PaymentEvent evt);
    }
}
