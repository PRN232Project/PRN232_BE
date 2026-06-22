using OnlineLearningPlatformApi.Application.DTOs;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IClaimService
    {
        ClaimDTO GetUserClaim();
    }
}
