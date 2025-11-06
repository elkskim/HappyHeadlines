using ArticleDatabase.Models;
using ArticleService.Controllers;
using ArticleService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ArticleService.Tests.Controllers;

/// <summary>
/// Tests for ArticleController - the gatekeeper between HTTP requests and our service layer.
/// We verify that requests are properly validated, responses correctly formatted,
/// and that the controller handles success and failure with equal grace.
/// The circuit breaker to CommentService is tested here as well.
/// </summary>
public class ArticleControllerTests
{
    private readonly Mock<IArticleAppService> _mockService;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly ArticleController _controller;

    public ArticleControllerTests()
    {
        _mockService = new Mock<IArticleAppService>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _controller = new ArticleController(_mockService.Object, _mockHttpClientFactory.Object);
    }

    [Fact]
    public async Task Get_ExistingArticle_ReturnsOkWithArticle()
    {
        // Arrange: An article exists in the depths
        var article = new Article("Test Article", "Content here", "Author")
        {
            Id = 1,
            Region = "Europe"
        };

        _mockService
            .Setup(s => s.GetArticleAsync(1, "Europe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act: Request the article
        var result = await _controller.Get(1, "Europe", CancellationToken.None);

        // Assert: Success should be returned
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedArticle = Assert.IsType<Article>(okResult.Value);
        Assert.Equal(article.Title, returnedArticle.Title);
    }

    [Fact]
    public async Task Get_NonExistentArticle_ReturnsNotFound()
    {
        // Arrange: The void returns null
        _mockService
            .Setup(s => s.GetArticleAsync(999, "Europe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        // Act: Search for what does not exist
        var result = await _controller.Get(999, "Europe", CancellationToken.None);

        // Assert: NotFound is the only honest answer
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ReadArticles_ValidRegion_ReturnsArticles()
    {
        // Arrange: A collection of articles awaits
        var articles = new List<Article>
        {
            new("Article 1", "Content 1", "Author 1") { Id = 1, Region = "Asia" },
            new("Article 2", "Content 2", "Author 2") { Id = 2, Region = "Asia" }
        };

        _mockService
            .Setup(s => s.GetArticles("Asia", It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        // Act: Retrieve all articles
        var result = await _controller.ReadArticles("Asia", CancellationToken.None);

        // Assert: The collection should be returned whole
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateArticle_ValidArticle_ReturnsAccepted()
    {
        // Arrange: A new article ready to be born
        var newArticle = new Article("New Article", "Fresh content", "New Author");

        var createdArticle = new Article("New Article", "Fresh content", "New Author")
        {
            Id = 10,
            Region = "Africa"
        };

        _mockService
            .Setup(s => s.CreateArticleAsync(It.IsAny<Article>(), "Africa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdArticle);

        // Act: Create the article
        var result = await _controller.CreateArticle(newArticle, "Africa");

        // Assert: Accepted with the created article
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var returnedArticle = Assert.IsType<Article>(acceptedResult.Value);
        Assert.Equal(newArticle.Title, returnedArticle.Title);
    }

    [Fact]
    public async Task UpdateArticle_ExistingArticle_ReturnsOkWithUpdatedArticle()
    {
        // Arrange: An article awaits modification
        var updates = new Article("Updated Title", "Updated Content", "Ignored");

        var updatedArticle = new Article("Updated Title", "Updated Content", "Original Author")
        {
            Id = 5,
            Region = "Europe"
        };

        _mockService
            .Setup(s => s.UpdateArticleAsync(5, updates, "Europe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedArticle);

        // Act: Update the article
        var result = await _controller.UpdateArticle(5, "Europe", updates, CancellationToken.None);

        // Assert: The updated article should be returned
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedArticle = Assert.IsType<Article>(okResult.Value);
        Assert.Equal("Updated Title", returnedArticle.Title);
    }

    [Fact]
    public async Task UpdateArticle_NonExistentArticle_ReturnsNotFound()
    {
        // Arrange: Attempting to update the nonexistent
        var updates = new Article("Does Not Matter", "Content", "Author");

        _mockService
            .Setup(s => s.UpdateArticleAsync(999, updates, "Europe", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        // Act: Try to update what isn't there
        var result = await _controller.UpdateArticle(999, "Europe", updates, CancellationToken.None);

        // Assert: NotFound is the truth
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteArticle_ExistingArticle_ReturnsNoContent()
    {
        // Arrange: An article marked for deletion
        _mockService
            .Setup(s => s.DeleteArticleAsync(3, "Asia", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act: Delete the article
        var result = await _controller.DeleteArticle(3, "Asia", CancellationToken.None);

        // Assert: NoContent indicates successful deletion
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteArticle_NonExistentArticle_ReturnsNotFound()
    {
        // Arrange: Attempting to delete what never was
        _mockService
            .Setup(s => s.DeleteArticleAsync(999, "Europe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act: Try to delete the void
        var result = await _controller.DeleteArticle(999, "Europe", CancellationToken.None);

        // Assert: NotFound is the honest response
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetArticleComments_SuccessfulRequest_ReturnsComments()
    {
        // Arrange: CommentService responds with comments
        var comments = new List<CommentDto>
        {
            new() { Id = 1, ArticleId = 1, Content = "Great article!" },
            new() { Id = 2, ArticleId = 1, Content = "Interesting read" }
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(comments))
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8004/api/Comment/")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("CommentsService"))
            .Returns(httpClient);

        // Act: Request comments for article
        var result = await _controller.GetArticleComments(1, "Europe");

        // Assert: Comments should be returned
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedComments = Assert.IsAssignableFrom<IEnumerable<CommentDto>>(okResult.Value);
        Assert.Equal(2, returnedComments.Count());
    }

    [Fact]
    public async Task GetArticleComments_CommentServiceDown_ReturnsServiceUnavailable()
    {
        // Arrange: CommentService is unreachable (circuit breaker territory)
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8004/api/Comment/")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("CommentsService"))
            .Returns(httpClient);

        // Act: Attempt to get comments when service is down
        var result = await _controller.GetArticleComments(1, "Europe");

        // Assert: ServiceUnavailable (503) should be returned
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetArticleComments_CommentServiceReturnsError_ReturnsInternalServerError()
    {
        // Arrange: CommentService responds with an error
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8004/api/Comment/")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient("CommentsService"))
            .Returns(httpClient);

        // Act: Request comments when service returns error
        var result = await _controller.GetArticleComments(1, "Europe");

        // Assert: InternalServerError (500) should be returned
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}

