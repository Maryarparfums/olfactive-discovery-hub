using System;
using System.Collections.Generic;
using System.Data;
using Maryar.Api.Infrastructure;
using Maryar.Api.Models;
using Maryar.Api.Repositories.Interfaces;

namespace Maryar.Api.Repositories.MySql
{
    public class HomeBannerRepository : IHomeBannerRepository
    {
        private readonly IConnectionFactory _factory;

        public HomeBannerRepository(IConnectionFactory factory)
        {
            _factory = factory;
        }

        public HomeBannerRepository()
            : this(new MySqlConnectionFactory())
        {
        }

        private static string GetStringOrNull(IDataReader reader, string column)
        {
            return reader[column] == DBNull.Value
                ? null
                : reader[column].ToString();
        }

        private static HomeBanner MapRow(IDataReader reader)
        {
            return new HomeBanner
            {
                Id = Guid.Parse(reader["id"].ToString()),

                BackgroundImageUrl = reader["background_image_url"].ToString(),
                ImageLink = GetStringOrNull(reader, "image_link"),

                Eyebrow = GetStringOrNull(reader, "eyebrow"),
                Title = reader["title"].ToString(),
                TitleEmphasis = GetStringOrNull(reader, "title_emphasis"),
                Description = GetStringOrNull(reader, "description"),

                PrimaryButtonText = GetStringOrNull(reader, "primary_button_text"),
                PrimaryButtonUrl = GetStringOrNull(reader, "primary_button_url"),
                SecondaryButtonText = GetStringOrNull(reader, "secondary_button_text"),
                SecondaryButtonUrl = GetStringOrNull(reader, "secondary_button_url"),

                SortOrder = Convert.ToInt32(reader["sort_order"]),
                Active = Convert.ToBoolean(reader["active"]),
                PublishedAt = reader["published_at"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["published_at"]),

                CreatedAt = Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = Convert.ToDateTime(reader["updated_at"])
            };
        }

        public IEnumerable<HomeBanner> ListPublished()
        {
            var list = new List<HomeBanner>();

            using (var cn = _factory.Create())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT
                        id,
                        background_image_url,
                        image_link,
                        eyebrow,
                        title,
                        title_emphasis,
                        description,
                        primary_button_text,
                        primary_button_url,
                        secondary_button_text,
                        secondary_button_url,
                        sort_order,
                        active,
                        published_at,
                        created_at,
                        updated_at
                    FROM home_banners
                    WHERE active = 1
                      AND (
                          published_at IS NULL
                          OR published_at <= NOW()
                      )
                    ORDER BY sort_order ASC, published_at DESC, created_at DESC";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapRow(reader));
                    }
                }
            }

            return list;
        }
    }
}
