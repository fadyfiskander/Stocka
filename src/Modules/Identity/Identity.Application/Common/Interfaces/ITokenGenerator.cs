using Identity.Contracts.DTOs;

namespace Identity.Application.Common.Interfaces
{
    /// <summary>
    /// Issues JWT security tokens for identity users.
    /// Infrastructure-internal capability — consumed by the module's own services;
    /// it is not part of the Application contracts on purpose.
    /// </summary>
    public interface ITokenGenerator
    {
        string GenerateToken(TokenGenerationDto tokenGeneration);
    }
}
