using SubscriberDatabase.Model;
using SubscriberService.Models.DTO;

namespace SubscriberService.Models.Mappers;

public static class SubscriberMapper
{
    public static Subscriber ToEntity(this SubscriberCreateDto dto)
    {
        return new Subscriber
        {
            Email = dto.Email,
            Region = dto.Region,
            SubscribedOn = DateTime.UtcNow
        };
    }

    public static void ApplyUpdate(this Subscriber entity, SubscriberUpdateDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            entity.Email = dto.Email;

        if (!string.IsNullOrEmpty(dto.Region))
            entity.Region = dto.Region;
    }

    public static SubscriberReadDto ToReadDto(this Subscriber entity)
    {
        return new SubscriberReadDto
        {
            Id = entity.Id,
            Email = entity.Email,
            Region = entity.Region,
            SubscribedOn = entity.SubscribedOn
        };
    }
}