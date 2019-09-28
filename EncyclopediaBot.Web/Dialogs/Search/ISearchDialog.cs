using System;

namespace EncyclopediaBot.Web.Dialogs.Search
{
    public interface ISearchDialog
    {
        string Query { get; set; }
        Guid? RequestId { get; set; }
    }
}