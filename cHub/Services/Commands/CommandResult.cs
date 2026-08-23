namespace cHub.Services.Commands
{
    public class CommandResult
    {
        public bool Success { get; }

        public string Message { get; }

        public bool ShouldReply { get; }

        private CommandResult(
            bool success,
            string message,
            bool shouldReply)
        {
            Success = success;
            Message = message ?? string.Empty;
            ShouldReply = shouldReply;
        }

        // =========================================================
        // SUCCESS
        // =========================================================

        public static CommandResult Ok(
            string message = "")
        {
            return new CommandResult(
                true,
                message,
                !string.IsNullOrWhiteSpace(message)
            );
        }

        // =========================================================
        // FAILURE
        // =========================================================

        public static CommandResult Fail(
            string message)
        {
            return new CommandResult(
                false,
                message,
                true
            );
        }

        // =========================================================
        // SILENT SUCCESS
        // =========================================================

        public static CommandResult Silent()
        {
            return new CommandResult(
                true,
                string.Empty,
                false
            );
        }

        // =========================================================
        // CUSTOM
        // =========================================================

        public static CommandResult Create(
            bool success,
            string message,
            bool shouldReply = true)
        {
            return new CommandResult(
                success,
                message,
                shouldReply
            );
        }

        // =========================================================
        // DISPLAY
        // =========================================================

        public override string ToString()
        {
            return Message;
        }
    }
}