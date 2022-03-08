using Terraria.ModLoader;

namespace MapMarkers.Net
{
    public partial class MapServer
    {
        internal class MapCommand : ModCommand
        {
            public override string Description => "Server-sige Map Markers config";

            public override string Command => "mapmarkers";
            public override CommandType Type => CommandType.Console;

            public override void Action(CommandCaller caller, string input, string[] args)
            {
                if (args.Length == 0)
                {
                    caller.Reply("Usage:\n" +
                        "\tmapmarkers limit [limit] - Get/Set max markers limit per player");
                }
                else switch (args[0])
                    {
                        case "limit":
                            if (args.Length < 2)
                            {
                                caller.Reply("Current limit: " + ModContent.GetInstance<MapServer>().MaxMarkersLimit);
                                return;
                            }
                            if (!int.TryParse(args[1], out int cap))
                            {
                                caller.Reply("Usage:\n" +
                                    "\tmapmarkers limit [limit] - Expected argument [limit] to be an integer");
                                return;
                            }
                            ModContent.GetInstance<MapServer>().MaxMarkersLimit = cap;
                            caller.Reply("Set markers limit to " + cap);
                            break;

                        default:
                            caller.Reply("Unknown subcommand: "+args[0]);
                            break;
                    }
                
            }
        }
    }
}
