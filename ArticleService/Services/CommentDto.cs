namespace ArticleService.Services;

public class CommentDto
{
    

        public int Id { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; }
    
        public int ArticleId { get; set; }
        public string Region { get; set; }
    }
