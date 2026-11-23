using Inventory.Application.Common.Interfaces;
using Inventory.Contracts.DTOs.Batch;
using Inventory.Contracts.UseCases.BatchCases;

namespace Inventory.Application.Bindings.BatchCases;

public record RegisterBatchCommandMediatR(RegisterBatchCommand Contract) : ICommand<BatchDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
