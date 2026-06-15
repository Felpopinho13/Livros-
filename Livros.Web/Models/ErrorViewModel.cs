namespace Livros.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? Path { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
