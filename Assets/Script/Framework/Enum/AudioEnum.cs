namespace QFramework.Enum
{
    public enum BGMType
    {
    }

    public enum EnvType
    {
        main_menu,
        game,
    }

    public enum SFXType
    {
        // ui
        ui_click,
        ui_open,
        ui_close,
        ui_hover,

        // player
        player_moving_engine,
        player_moving_ground,

        // weapon
        weapon_reload,
        weapon_shoot_ar,
        weapon_shoot_sg,
        weapon_shoot_hsa,

        //
        enemy_dropper_falling,

        // enemy
        enemy_hit,

        // item
        item_pickup,


        
        // vfx
        explosion,
        explosion_large,
        explosion_mid,
        explosion_small,
        explosion_tiny,
        misslie_launch,

    }
}
