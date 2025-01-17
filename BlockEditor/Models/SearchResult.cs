﻿using System;
using System.Globalization;
using System.Text;

namespace BlockEditor.Models
{
    public class SearchResult
    {
        public int ID { get; set; }
        public string Title { get; set; }

        public string CreatedBy { get; set; }

        public int? PlayCount { get; set; }

        private double? _rating;
        public double? Rating
        {
            get
            {
                if(_rating == null)
                    return null;

                if(_rating.Value >= 100)
                    return _rating.Value / 100.0;

                if (_rating.Value >= 10)
                    return _rating.Value / 10.0;

                return _rating.Value;
            }

            set
            {
                _rating = value;
            }
        }


        public SearchResult(int id, string title, string createdBy, int? playCount, double? rating)
        {
            ID = id;
            Title = title;
            CreatedBy = createdBy;
            PlayCount = playCount;
            Rating = rating;
        }

        public static readonly SearchResult SLOW_DOWN = new SearchResult(int.MinValue, string.Empty, string.Empty, null, null);


        public string GetToolTip()
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(CreatedBy))
                builder.Append("Created by:  " + CreatedBy);

            if (PlayCount != null)
                builder.Append(Environment.NewLine + "Play Count:  " + PlayCount);

            if (Rating != null)
                builder.Append(Environment.NewLine + "Ratings:  " + Math.Round(Rating.Value, 2).ToString("N2", CultureInfo.InvariantCulture));

            return builder.ToString();

        }
    }
}
