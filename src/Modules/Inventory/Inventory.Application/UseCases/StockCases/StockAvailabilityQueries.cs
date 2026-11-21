using BuildingBlocks.Application.Abstractions;
using Inventory.Application.Bindings.StockCases;
using Inventory.Application.Repositories.Queries;
using Inventory.Contracts.DTOs.Reports;
using Inventory.Contracts.UseCases.StockCases;
using Inventory.Domain.Batches;
using Inventory.Domain.Warehouses;
using MediatR;

namespace Inventory.Application.UseCases.StockCases;

#region GetProductStockQuery
/// <summary>
/// Handles the product stock distribution: aggregates the organization-wide distribution (optionally
/// filtered down to one warehouse) with the total counted units.
/// </summary>
public class GetProductStockQueryHandler : IRequestHandler<GetProductStockQueryMediatR, ProductStockDistributionDto>
{
    private readonly IInventoryReportQueryRepository _reportRepository;
    private readonly ICurrentUserService _currentUser;

    public GetProductStockQueryHandler(IInventoryReportQueryRepository reportRepository, ICurrentUserService currentUser)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
    }

    public async Task<ProductStockDistributionDto> Handle(GetProductStockQueryMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.ProductId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(request.Contract.ProductId));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var rows = await _reportRepository.GetProductStockDistributionAsync(organizationId.Value, request.Contract.ProductId);
        if (request.Contract.WarehouseId is { } wid && wid != Guid.Empty)
            rows = rows.Where(r => r.WarehouseId == wid).ToList();

        var total = await _reportRepository.GetProductTotalUnitsAsync(organizationId.Value, request.Contract.ProductId);

        return new ProductStockDistributionDto(request.Contract.ProductId, total, rows.ToList());
    }
}
#endregion

#region GetProductUnallocatedUnitsQuery
/// <summary>Handles the unallocated-units count for one product across all its batches.</summary>
public class GetProductUnallocatedUnitsQueryHandler : IRequestHandler<GetProductUnallocatedUnitsQueryMediatR, int>
{
    private readonly IInventoryReportQueryRepository _reportRepository;
    private readonly ICurrentUserService _currentUser;

    public GetProductUnallocatedUnitsQueryHandler(IInventoryReportQueryRepository reportRepository, ICurrentUserService currentUser)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetProductUnallocatedUnitsQueryMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.ProductId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(request.Contract.ProductId));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        return await _reportRepository.GetProductUnallocatedUnitsAsync(organizationId.Value, request.Contract.ProductId);
    }
}
#endregion

#region GetBatchQuantityInWarehouseQuery
/// <summary>Handles the current quantity held by one batch in one warehouse.</summary>
public class GetBatchQuantityInWarehouseQueryHandler : IRequestHandler<GetBatchQuantityInWarehouseQueryMediatR, int>
{
    private readonly IWarehouseBatchQueryRepository _warehouseBatchQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public GetBatchQuantityInWarehouseQueryHandler(IWarehouseBatchQueryRepository warehouseBatchQueryRepository, ICurrentUserService currentUser)
    {
        _warehouseBatchQueryRepository = warehouseBatchQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetBatchQuantityInWarehouseQueryMediatR request, CancellationToken cancellationToken)
    {
        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        return await _warehouseBatchQueryRepository.GetQuantityInWarehouseAsync(
            organizationId.Value,
            WarehouseId.From(request.Contract.WarehouseId),
            BatchId.From(request.Contract.BatchId));
    }
}
#endregion

#region GetWarehouseTotalUnitsQuery
/// <summary>Handles the total current units held in one warehouse.</summary>
public class GetWarehouseTotalUnitsQueryHandler : IRequestHandler<GetWarehouseTotalUnitsQueryMediatR, int>
{
    private readonly IWarehouseBatchQueryRepository _warehouseBatchQueryRepository;
    private readonly ICurrentUserService _currentUser;

    public GetWarehouseTotalUnitsQueryHandler(IWarehouseBatchQueryRepository warehouseBatchQueryRepository, ICurrentUserService currentUser)
    {
        _warehouseBatchQueryRepository = warehouseBatchQueryRepository;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetWarehouseTotalUnitsQueryMediatR request, CancellationToken cancellationToken)
    {
        if (request.Contract.WarehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID cannot be empty.", nameof(request.Contract.WarehouseId));

        var organizationId = _currentUser.OrganizationId;
        if (organizationId is null)
            throw new ArgumentNullException("OrganizationId is null, OrganizationId cannot be null");

        var placements = await _warehouseBatchQueryRepository.GetAllAsync(organizationId.Value, WarehouseId.From(request.Contract.WarehouseId));
        return placements.Sum(p => p.Quantity);
    }
}
#endregion
