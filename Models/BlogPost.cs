using System;
using System.Collections.Generic;

namespace dotnetprojekt.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Excerpt { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public DateTime PublishedDate { get; set; }
        public string Author { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        
        // For display purposes
        public string FormattedDate => PublishedDate.ToString("MMMM d, yyyy");
    }
}