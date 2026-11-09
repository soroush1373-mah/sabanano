using AutoMapper;
using CleanArchitecture.Application.Article.Queries;
using CleanArchitecture.Application.Common.Mappings;
using CleanArchitecture.Application.TodoItems.Queries.GetPersonWithPagination;
using CleanArchitecture.Domain.Entities.Articles;
using CleanArchitecture.Entities.Articles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ArticleDto : BaseAuditableDto<ArticleDto, Article, int>
{
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string HeaderName { get; set; }
    public string CategoryTitle { get; set; }
    public string? Excerpt { get; set; }   // only the first paragraph, for the small cards
    public List<ArticleDetailDto> ArticleDetails { get; set; }
    public List<ArticleCommentListDto> ArticleComments { get; set; }
    public override void CustomMappings(IMappingExpression<Article, ArticleDto> mapping)
    {
        mapping.ForMember(dest => dest.ImageUrl, conf => conf.MapFrom(src => "../img/article/" + src.Image.FileName));
        mapping.ForMember(dest => dest.CategoryTitle, conf => conf.MapFrom(src =>  src.Category.Title));
        // ExplicitExpansion = don't load these lists unless the query asks for them (only the single article page does)
        mapping.ForMember(dest => dest.ArticleDetails, conf => { conf.MapFrom(src => src.ArticleDetails.OrderBy(x => x.OrderBy)); conf.ExplicitExpansion(); });
        mapping.ForMember(dest => dest.ArticleComments, conf => conf.ExplicitExpansion());
        // The database sends only the first paragraph that is not empty
        mapping.ForMember(dest => dest.Excerpt, conf => conf.MapFrom(src => src.ArticleDetails.OrderBy(x => x.OrderBy).Select(x => x.Paragraph).Where(p => p != null && p != "").FirstOrDefault()));
    }
}

public class ArticleBriefDto : BaseAuditableDto<ArticleBriefDto, Article, int>
{
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string HeaderName { get; set; }
    public string CategoryTitle { get; set; }
    public override void CustomMappings(IMappingExpression<Article, ArticleBriefDto> mapping)
    {
        mapping.ForMember(dest => dest.ImageUrl, conf => conf.MapFrom(src => "../img/article/" + src.Image.FileName));
        mapping.ForMember(dest => dest.CategoryTitle, conf => conf.MapFrom(src => src.Category.Title));
    }
}

