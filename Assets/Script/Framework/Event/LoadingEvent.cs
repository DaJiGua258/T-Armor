namespace QFramework.Event
{
    public class LoadingEvent
    {
        public struct ProgressTo
        {
            public float Progress;
        }
        public struct FadeIn 
        { 
            public float Duration;
        }
        public struct FadeOut 
        { 
            public float Duration;
        }
        
    }
}