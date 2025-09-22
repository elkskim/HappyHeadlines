namespace ArticleDatabase.Models;

public class Article
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }

    public Article(string? title, string? content, string? author)
    {
        this.Title = title;
        this.Content = content;
        this.Author = author;
    }
}