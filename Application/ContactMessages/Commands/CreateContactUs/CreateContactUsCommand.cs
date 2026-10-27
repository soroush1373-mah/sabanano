using AutoMapper;
using CleanArchitecture.Application.Common.Mappings;
using CleanArchitecture.Domain.Entities.HrManagment;
using Data.Contracts;
using MediatR;

namespace CleanArchitecture.Application.ContactMessages.Commands.CreateContactUs;

public record CreateContactUsCommand : BaseRecordDto<CreateContactUsCommand, ContactUs, int>, IRequest<int>
{
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CreateContactUsCommandHandler : IRequestHandler<CreateContactUsCommand, int>
{
    private readonly IRepository<ContactUs> _repository;
    private readonly IMapper _mapper;

    public CreateContactUsCommandHandler(IRepository<ContactUs> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<int> Handle(CreateContactUsCommand request, CancellationToken cancellationToken)
    {
        var entity = request.ToEntity(_mapper);              // command → ContactUs
        await _repository.AddAsync(entity, cancellationToken); // INSERT + save
        return entity.Id;
    }
}