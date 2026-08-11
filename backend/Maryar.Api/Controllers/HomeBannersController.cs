using System.Linq;
using System.Web.Http;
using Maryar.Api.Dtos;
using Maryar.Api.Repositories.Interfaces;
using Maryar.Api.Repositories.MySql;

namespace Maryar.Api.Controllers
{
    [RoutePrefix("home-banners")]
    public class HomeBannersController : ApiController
    {
        private readonly IHomeBannerRepository _repo;

        public HomeBannersController()
            : this(new HomeBannerRepository())
        {
        }

        public HomeBannersController(IHomeBannerRepository repo)
        {
            _repo = repo;
        }

        // GET /api/home-banners
        [HttpGet]
        [Route("")]
        public IHttpActionResult List()
        {
            var banners = _repo
                .ListPublished()
                .Select(b => new HomeBannerDto
                {
                    Id = b.Id,
                    BackgroundImageUrl = b.BackgroundImageUrl,
                    ImageLink = b.ImageLink,
                    Eyebrow = b.Eyebrow,
                    Title = b.Title,
                    TitleEmphasis = b.TitleEmphasis,
                    Description = b.Description,
                    PrimaryButtonText = b.PrimaryButtonText,
                    PrimaryButtonUrl = b.PrimaryButtonUrl,
                    SecondaryButtonText = b.SecondaryButtonText,
                    SecondaryButtonUrl = b.SecondaryButtonUrl,
                    SortOrder = b.SortOrder,
                    PublishedAt = b.PublishedAt
                })
                .ToList();

            return Ok(banners);
        }
    }
}
