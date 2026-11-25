using Inventory.Application.Common.Interfaces;
using Inventory.Contracts.DTOs.Stock;
using Inventory.Contracts.DTOs.StockMovement;
using Inventory.Contracts.UseCases.StockCases;
using MediatR;

namespace Inventory.Application.Bindings.StockCases;

public record AdjustStockCommandMediatR(AdjustStockCommand Contract) : ICommand<AdjustmentResultDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record AllocateStockCommandMediatR(AllocateStockCommand Contract) : ICommand<AllocateStockResultDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record TransferStockCommandMediatR(TransferStockCommand Contract) : ICommand<TransferStockResultDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record WriteOffDamagedStockCommandMediatR(WriteOffDamagedStockCommand Contract) : ICommand<StockMovementDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record ReverseStockMovementCommandMediatR(ReverseStockMovementCommand Contract) : ICommand<StockMovementDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record RecordSaleOutCommandMediatR(RecordSaleOutCommand Contract) : ICommand<StockMovementDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record RecordCustomerReturnCommandMediatR(RecordCustomerReturnCommand Contract) : ICommand<StockMovementDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}