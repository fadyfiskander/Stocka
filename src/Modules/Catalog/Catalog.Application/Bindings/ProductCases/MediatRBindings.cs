using Catalog.Application.Common.Interfaces;
using Catalog.Contracts.DTOs.Product;
using Catalog.Contracts.UseCases.ProductCases;
using MediatR;

namespace Catalog.Application.Bindings.ProductCases;

public record RegisterProductCommandMediatR(RegisterProductCommand Contract) : ICommand<ProductDto?>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record UpdateProductCommandMediatR(UpdateProductCommand Contract) : ICommand<ProductDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record ActivateProductCommandMediatR(ActivateProductCommand Contract) : ICommand<ProductDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record DeactivateProductCommandMediatR(DeactivateProductCommand Contract) : ICommand<ProductDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record RetrieveProductQueryMediatR(RetrieveProductQuery Contract) : IRequest<ProductDto>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record RetrieveProductByNameQueryMediatR(RetrieveProductByNameQuery Contract) : IRequest<IEnumerable<ProductDto>>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
public record ListProductsQueryMediatR() : IRequest<IEnumerable<ProductDto>>, IContractRequest
{
    object IContractRequest.Contract => new();
}

public record ListProductsByIdsQueryMediatR(ListProductsByIdsQuery Contract) : IRequest<IEnumerable<ProductDto>>, IContractRequest
{
    object IContractRequest.Contract => Contract;
}
