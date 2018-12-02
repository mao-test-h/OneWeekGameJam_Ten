namespace MainContents
{
    /// <summary>
    /// 各種定数値
    /// </summary>
    public sealed class Constants
    {
        // ------------------------------
        #region // ECS

        public const string WorldName = "MainWorld";

        #endregion // ECS

        // ------------------------------
        #region // Player Controller

        // InputeManagerの各種設定名
        public const string MoveHorizontal = "MoveHorizontal";
        public const string MoveVertical = "MoveVertical";
        public const string Shot = "Shot";
        public const string Cancel = "Cancel";

        #endregion // Player Controller

        // ------------------------------
        #region // Barrage 

        public const int ThreeWayBulletCount = 3;
        public const int FiveWayBulletCount = 5;
        public const int SevenWayBulletCount = 7;

        #endregion // Barrage

        // ------------------------------
        #region // Material Property

        public const string BarrierMaterialAlpha = "_Alpha";

        #endregion // Material Property
    }

    // ------------------------------
    #region // Enums

    /// <summary>
    /// 敵の種類
    /// </summary>
    /// <remarks>名称は放ってくる弾幕の名称となっている</remarks>
    public enum EnemyID : byte
    {
        Aiming = 0,
        Circle,
        Spiral,
        ThreeWay,
        FiveWay,
        SevenWay,
        SpiralCircle,
        RandomWay,
        WaveCircle,
        WaveWay,
    }

    /// <summary>
    /// 敵の生成位置
    /// </summary>
    public enum SpawnPoint : byte
    {
        LeftTop = 0,
        LeftMiddle,
        LeftBottom,

        RightTop,
        RightMiddle,
        RightBottom,

        TopLeft,
        TopMiddle,
        TopRight,

        BottomLeft,
        BottomMiddle,
        BottomRight,
    }

    /// <summary>
    /// SE
    /// </summary>
    public enum SE_ID
    {
        PlayerShot = 0,
        PlayerDestroy,
        EnemyDestroy,
    }

    #endregion // Enums
}
