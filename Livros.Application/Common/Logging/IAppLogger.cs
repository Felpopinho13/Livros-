namespace Livros.Application.Common.Logging {
    public interface IAppLogger<TCategoryName> {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
    }
}
