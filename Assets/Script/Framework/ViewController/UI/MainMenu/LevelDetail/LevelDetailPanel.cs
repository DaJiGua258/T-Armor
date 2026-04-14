namespace QFramework.ViewController.UI  
{
    public class LevelDetailPanel : AbstractBasePanel
    {
        private MapInfoDetailContainer _mapInfoDetailContainer;
        
        public override void OnInit()
        {
            _mapInfoDetailContainer = transform.Find
                ("MapInfoContainer/MapInfoDetailContainer").GetComponent<MapInfoDetailContainer>();
            _mapInfoDetailContainer.InitMapInfoDetail();
        }

        public void UpdateMapDetailInfo()
        {
            
        }
    }
}
