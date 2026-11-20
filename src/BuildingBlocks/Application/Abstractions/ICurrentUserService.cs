namespace BuildingBlocks.Application.Abstractions
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        Guid? OrganizationId { get; }
        string? Role { get; }
    }
}