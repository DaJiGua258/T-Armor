namespace QFramework.Event
{
    public class InteractionEvent
    {
        public struct ShowPrompt
        {
            public string ActionText;
            public string NameText;
        }

        public struct HidePrompt { }
    }
}
