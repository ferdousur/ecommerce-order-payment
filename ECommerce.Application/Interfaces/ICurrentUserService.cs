namespace ECommerce.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}