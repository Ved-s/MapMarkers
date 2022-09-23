using Microsoft.Xna.Framework.Input;

namespace MapMarkers.Structures
{
    public struct Keybind
    {
        public Keys[] Keys;
        public KeybindState State => Keybinds.GetKeybind(Keys);

        public Keybind(params Keys[] keys)
        {
            Keys = keys;
        }
    }
}
