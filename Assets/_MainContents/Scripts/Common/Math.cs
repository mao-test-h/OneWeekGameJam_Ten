namespace MainContents
{
    using UnityEngine;
    using Unity.Mathematics;

    using UnityRandom = UnityEngine.Random;

    /// <summary>
    /// 算術補助クラス
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// p2からp1への角度を求める
        /// </summary>
        /// <param name="p1">自分の座標</param>
        /// <param name="p2">相手の座標</param>
        /// <returns>2点の角度(radian)</returns>
        public static float Aiming(Vector2 p1, Vector2 p2)
        {
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;
            return Mathf.Atan2(dy, dx);
        }

        /// <summary>
        /// p2からp1への角度を求める
        /// </summary>
        /// <param name="p1">自分の座標</param>
        /// <param name="p2">相手の座標</param>
        /// <returns>2点の角度(radian)</returns>
        public static float Aiming(float2 p1, float2 p2)
        {
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;
            return math.atan2(dy, dx);
        }

        /// <summary>
        /// 二次元のランダムな位置の取得
        /// </summary>
        public static Vector2 GetRandomPosition2D(Vector2 boundSize)
        {
            var halfX = boundSize.x / 2f;
            var halfY = boundSize.y / 2f;
            return new Vector2(
                UnityRandom.Range(-halfX, halfX),
                UnityRandom.Range(-halfY, halfY));
        }

        /// <summary>
        /// Sin(軽量版)
        /// </summary>
        /// <param name="deg">角度(度数)</param>
        /// <returns>radian</returns>
        public static float Sin(int deg)
        {
            // 0~90(度)
            if (deg <= 90) { return Cos(deg - 90); }
            // 90~180
            else if (deg <= 180) { return Cos((180 - deg) - 90); }
            // 180~270
            else if (deg <= 270) { return -Cos((deg - 180) - 90); }
            // 270~360
            else { return -Cos((360 - deg) - 90); }
        }

        /// <summary>
        /// Cos(軽量版)
        /// </summary>
        /// <param name="deg">角度(度数)</param>
        /// <returns>radian</returns>
        public static float Cos(int deg)
        {
            // Mathf.PIは定数
            float rad = deg * ((Mathf.PI * 2) / 360f);
            // math.powで冪乗を算出すると激重になる
            // →代わりに全部乗算だけで完結させると速い
            float pow1 = rad * rad;
            float pow2 = pow1 * pow1;
            float pow3 = pow2 * pow1;
            float pow4 = pow2 * pow2;
            // 階乗は算出コストを省くために数値リテラルで持つ
            float ret = 1 - (pow1 / 2f)
                        + (pow2 / 24f)        // 4!
                        - (pow3 / 720f)       // 6!
                        + (pow4 / 40320f);    // 8!
            return ret;
        }
    }
}
