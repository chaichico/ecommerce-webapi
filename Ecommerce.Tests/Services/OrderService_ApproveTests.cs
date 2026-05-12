using AutoMapper;
using Data;
using Models.Dtos.Requests;
using Models.Dtos.Responses;
using Models.Entities;
using Models.Enums;
using Moq;
using Repositories.Interfaces;
using Services;

namespace Ecommerce.Tests.Services;

public class OrderService_ApproveTests
{
    // ── Shared mock fields ──────────────────────────────────────────────
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly OrderService _sut;

    public OrderService_ApproveTests()
    {
        _sut = new OrderService(
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _productRepoMock.Object,
            _mapperMock.Object,
            _unitOfWorkMock.Object);
    }
}
