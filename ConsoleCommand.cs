using MapMarkers.Markers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Terraria.ModLoader;

namespace MapMarkers
{
    internal class ConsoleCommand : ModCommand
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public override string Command => "mapmarkers";
        public override CommandType Type => CommandType.Console;

        public override string Description => "Map Markers control command";

        static string[] Subcommands = new[] { "add-marker", "list-markers" };

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            ArgReader reader = new(Patches.LastConsoleCommand ?? input);
            reader.ReadString(); // "mapmarkers" arg
            try
            {
                string sub = reader.ReadChoice(Subcommands);
                switch (sub)
                {
                    case "add-marker": AddMarkerSubcommand(reader, caller); return;
                    case "list-markers": ListMarkersSubcommand(reader, caller); return;
                    default: caller.Reply($"{sub}: Not implemented"); return;
                }
            }
            catch { } // tML catches first-chance exception here, so no need for logging it
        }

        static void ListMarkersSubcommand(ArgReader args, CommandCaller caller)
        {
            if (MapMarkers.Markers.Count == 0)
            {
                caller.Reply("No markers");
                return;
            }

            StringBuilder sb = new();

            foreach (MapMarker marker in MapMarkers.Markers.Values)
            {
                sb.AppendLine($"{marker.GetType().Name} ({marker.Mod.Name}) {marker.DisplayName}");
                sb.AppendLine($"{marker.SaveLocation}-side, {marker.Id} ({MapMarkers.MarkerGuids.GetShortGuid(marker.Id)})");
                if (marker is PlacedMarker placed)
                    sb.AppendLine($"Item: {placed.DisplayItem.HoverName}");
                sb.AppendLine($"{marker.Position}");
                sb.AppendLine();
            }

            caller.Reply(sb.ToString());
        }

        static void AddMarkerSubcommand(ArgReader args, CommandCaller caller)
        {
            if (!args.HasMoreArgs())
                args.Error($"Usage: {args.CurrentPrefix()} <Name> <Item> <PositionX> <PositionY>\n" +
                    $"Item can be numeric ID or internal item name");

            string name = args.ReadString();
            int item = args.ReadItemId();
            Vector2 pos = new(args.ReadFloat(), args.ReadFloat());

            PlacedMarker marker = new();

            marker.DisplayName = name;
            marker.DisplayItemType = item;
            marker.Position = pos;
            marker.SaveLocation = Structures.SaveLocation.Server;

            MapMarkers.AddMarker(marker, true);
            caller.Reply("Success");
        }
    }

    class ArgReader
    {
        public string[] Args;
        public int Pos;

        public ArgReader(string args)
        {
            Args = SmartSplit(args);
        }

        [DoesNotReturn]
        public void Error(string error) => throw new ArgReadException(error);

        public bool HasMoreArgs() => Pos < Args.Length;

        public string ReadString()
        {
            if (Pos >= Args.Length)
                Error($"Argument {Pos} expected.");
            Pos++;
            return Args[Pos - 1];
        }

        public string ReadChoice(IEnumerable<string> choices)
        {
            if (!HasMoreArgs())
                Error($"Expected one of ({string.Join(", ", choices)}) as arg {Pos}");

            string arg = ReadString();
            foreach (string choice in choices)
                if (choice.StartsWith(arg, StringComparison.InvariantCultureIgnoreCase))
                    return choice;

            Error($"Expected one of ({string.Join(", ", choices)}) as arg {Pos}");
            return null!;
        }

        public int ReadItemId()
        {
            string arg = ReadString();
            if (int.TryParse(arg, out int id))
            {
                if (!ItemHelper.IsValidId(id))
                    Error("Invalid item id");
                return id;
            }
            return ItemHelper.GetByName(arg);
        }

        public float ReadFloat()
        {
            if (!float.TryParse(ReadString(), out float value))
                Error($"invalid float argument {Pos-1}");
            return value;
        }

        public string CurrentPrefix()
        {
            StringBuilder sb = new();
            foreach (string arg in Args.Take(Pos))
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                if (arg.Any(c => char.IsWhiteSpace(c)))
                {
                    sb.Append('"');
                    sb.Append(arg);
                    sb.Append('"');
                }
                else sb.Append(arg);
            }
            return sb.ToString();
        }

        static string[] SmartSplit(string str)
        {
            List<string> split = new();
            int lastSplit = 0;
            bool quotes = false;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (char.IsWhiteSpace(c) && !quotes)
                {
                    if (i - lastSplit > 0)
                        split.Add(str.Substring(lastSplit, i - lastSplit));

                    lastSplit = i + 1;
                }
                else if (c == '"')
                {
                    if (quotes)
                    {
                        quotes = false;
                        split.Add(str.Substring(lastSplit, i - lastSplit));
                        lastSplit = i + 1;
                    }
                    else
                    {
                        if (i - lastSplit > 0)
                            split.Add(str.Substring(lastSplit, i - lastSplit));
                        lastSplit = i + 1;
                        quotes = true;
                    }
                }
            }

            if (lastSplit < str.Length)
                split.Add(str.Substring(lastSplit));

            return split.ToArray();
        }

        public class ArgReadException : Exception
        {
            public ArgReadException(string message) : base(message) { }
        }
    }
}
