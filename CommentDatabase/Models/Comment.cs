namespace CommentDatabase.Models;

public class Comment
{
    public int Id { get; set; }
    public string Author { get; set; }
    public string Content { get; set; }
    public DateTime Date { get; set; }

    public Comment(string author, string content, DateTime date)
    {
        this.Author = author;
        this.Content = content;
        this.Date = date;
    }
    
}