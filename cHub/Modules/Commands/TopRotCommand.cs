using cHub.Services.Commands;

namespace cHub.Modules.Commands
{
    public class TopRotCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "toprot";

        public override string Description =>
            "Public Top ROT command.";

        public override string Usage =>
            "/toprot";

        // =========================================================
        // PERMISSION
        // =========================================================

        // null = PUBLIC
        // Every player can use this command.
        public override string RequiredPermission =>
            null;

        // =========================================================
        // EXECUTE
        // =========================================================

        public override CommandResult Execute(
            CommandContext context)
        {
            // Temporary implementation.
            // The real Top ROT logic will be connected here.

            return CommandResult.Ok(
                "Top ROT command executed."
            );
        }
    }
}