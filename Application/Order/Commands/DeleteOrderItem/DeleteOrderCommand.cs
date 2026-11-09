using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Entities.HrManagment;
using CleanArchitecture.Domain.Entities.Orders;
using CleanArchitecture.Domain.Events;
using Common.Exceptions;
using Data.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Application.People.Commands.DeleteOrder;

public record DeleteOrderCommand : IRequest<long>
{
    public long Id { get; set; }
}

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, long>
{
    private readonly IRepository<Order> _OrderRepository;
    private readonly ICurrentUserService _currentUser;

    public DeleteOrderCommandHandler(IRepository<Order> OrderRepository,
        ICurrentUserService currentUser)
    {
        _OrderRepository = OrderRepository;
        _currentUser = currentUser;
    }

    public async Task<long> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var entity = await _OrderRepository.TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id);

        if (entity == null)
        {
            throw new NotFoundException(nameof(Order), request.Id);
        }

        // Only the person who made the order (or an admin) may delete it
        if (entity.CreatedBy != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
            throw new AppException("شما اجازه حذف این سفارش را ندارید");

        // Only orders still in the cart can be deleted, not ones already sent to the expert
        if (entity.OrderStatus.Id != OrderStatus.Initial.Id)
            throw new AppException("این سفارش ارسال شده است و قابل حذف نیست");

        await _OrderRepository.DeleteAsync(entity, cancellationToken);

        return request.Id;
    }
}