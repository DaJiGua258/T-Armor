using System;
using System.Collections.Generic;
using System.Linq;
namespace QFramework.UtilityKit
{

    public static class SeedRandom
    {
        private static Random _random;
        private static int _currentSeed;

        /// <summary>
        /// 初始化随机种子
        /// </summary>
        /// <param name="seed">种子值</param>
        public static void Initialize(int seed)
        {
            _currentSeed = seed;
            _random = new Random(_currentSeed);
        }

        /// <summary>
        /// 初始化随机种子（支持字符串）
        /// </summary>
        public static void Initialize(string seed)
        {
            Initialize(seed.GetHashCode());
        }

        /// <summary>
        /// 生成指定范围内的随机整数 [min, max)
        /// </summary>
        public static int Range(int min, int max)
        {
            CheckInitialized();
            return _random.Next(min, max);
        }

        /// <summary>
        /// 生成 0.0 到 1.0 之间的随机浮点数
        /// </summary>
        public static float Value()
        {
            CheckInitialized();
            return (float)_random.NextDouble();
        }

        private static void CheckInitialized()
        {
            if (_random == null)
            {
                // 如果未手动初始化，默认使用系统时间作为种子
                Initialize(DateTime.Now.Millisecond);
            }
        }
    }
}