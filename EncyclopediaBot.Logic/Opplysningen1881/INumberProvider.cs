using System;
using System.Threading.Tasks;

namespace EncyclopediaBot.Logic.Opplysningen1881
{
    public interface INumberProvider
    {
        Guid? RequestId { get; set; }

        Task<NumberLookupResult> LookupAsync(string phoneNumber);
    }
}