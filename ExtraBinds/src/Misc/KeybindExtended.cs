using MSCLoader;
using UnityEngine;

namespace ExtraBinds
{
    class KeybindExtended : Keybind
    {
        public KeybindExtended(string id, string name, KeyCode key) : base(id, name, key) { }
        public KeybindExtended(string id, string name, KeyCode key, KeyCode modifier) : base(id, name, key, modifier) { }

        public bool IsUp()
        {
            if (Modifier != KeyCode.None)
            {
                return Input.GetKey(Modifier) && Input.GetKeyUp(Key);
            }

            return Input.GetKeyUp(Key);
        }
    }
}
