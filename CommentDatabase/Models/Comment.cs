namespace CommentDatabase.Models;

public class Comment
{
    public Comment(string author, string content, int articleId, string region)
    {
        Author = author;
        Content = content;
        Date = DateTime.Now;
        ArticleId = articleId;
        Region = region;
    }

    public int Id { get; set; }
    public string Author { get; set; }
    public string Content { get; set; }
    public DateTime Date { get; set; }
    
    public int ArticleId { get; set; }
    public string Region { get; set; }
}