using System;
using System.Threading.Tasks;

namespace EncyclopediaBot.Logic.Snl
{
    public interface IArticleProvider
    {
        Guid? RequestId { get; set; }

        Task<Article> GetArticleAsync(ArticleRequest articleRequest);
        Article GetArticle(ArticleRequest articleRequest);
        Task<SearchResult> SearchAsync(SearchRequest searchRequest);
        SearchResult Search(SearchRequest searchRequest);
    }
}