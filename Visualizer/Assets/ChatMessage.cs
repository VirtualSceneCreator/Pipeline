
using UnityEngine;

namespace Assets
{
    public class ChatMessage : ScriptableObject
    {
        public string Text;
        public string User;

        public ChatMessage(string text, string user)
        {
            Text = text;
            User = user;
        }
    }
}