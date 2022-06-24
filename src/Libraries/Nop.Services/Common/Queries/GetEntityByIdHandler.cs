using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nop.Core;
using Nop.Data;

namespace Nop.Services.Common.Queries
{
    public class GetEntityByIdHandler<TEntity> : IRequestHandler<GetEntityByIdQuery<TEntity>, TEntity> where TEntity : BaseEntity
    {
        private readonly IRepository<TEntity> _repository;
        public GetEntityByIdHandler(IRepository<TEntity> repository)
        {
            _repository = repository;
        }

        public Task<TEntity> Handle(GetEntityByIdQuery<TEntity> request, CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id);
        }
    }
}