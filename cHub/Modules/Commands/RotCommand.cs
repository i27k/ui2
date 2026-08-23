using cHub.Services.Commands;

namespace cHub.Modules.Commands
{
    public class RotCommand : CommandDefinition
    {
        public override string Name =>
            "rot";

        public override string Description =>
            "Public ROT command.";

        public override string Usage =>
            "/rot";

        // null = public command
        public override string RequiredPermission =>
            null;

        public override CommandResult Execute(
            CommandContext context)
        {
            // Aici punem logica reală a comenzii /rot.

            return CommandResult.Ok(
                "ROT command executed."
            );
        }
    }
}