using MediatR;
using Nop.Core;

namespace Nop.Services.Common.Queries
{
    public class GetEntityByIdQuery<T> : IRequest<T> where T : BaseEntity
    {
        public int Id { get; set; }
    }
}