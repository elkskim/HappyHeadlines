using SubscriberDatabase.Model;
using SubscriberService.Models.DTO;

namespace SubscriberService.Services;

public interface ISubscriberAppService
{
        public Task<IEnumerable<SubscriberReadDto>?> GetSubscribers();
        public Task<SubscriberReadDto?> GetById(int id);
        public Task<SubscriberReadDto?> Create(SubscriberCreateDto entity);
        public Task<SubscriberReadDto?> Update(int id, SubscriberUpdateDto newEntity);
        public Task<bool> Delete(int id);
}