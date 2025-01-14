using Microsoft.Extensions.Logging;

namespace CanvasAppExtractorApp
{
    public class FormTextBoxLogger<T> : ILogger<T>
    {
        private readonly TextBox _textBox;

        public FormTextBoxLogger(TextBox textBox)
        {
            _textBox = textBox;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message))
            {
                _textBox.Invoke(() => _textBox.AppendText($"{logLevel}: {message}{Environment.NewLine}"));
            }
        }
    }
}
