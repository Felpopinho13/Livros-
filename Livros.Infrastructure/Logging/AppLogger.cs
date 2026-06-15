using Livros.Application.Common.Logging;
using Microsoft.Extensions.Logging;

namespace Livros.Infrastructure.Logging {
    public sealed class AppLogger<TCategoryName> : IAppLogger<TCategoryName> {
        private readonly ILogger<TCategoryName> _logger;

        public AppLogger(ILogger<TCategoryName> logger) {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args) {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args) {
            _logger.LogWarning(message, args);
        }

        public void LogError(Exception exception, string message, params object[] args) {
            _logger.LogError(exception, message, args);
        }
    }
}
