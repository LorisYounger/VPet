using System.Collections.Generic;

namespace VPet_Simulator.Core
{
    public abstract class GameCoreBase<TTouchArea> where TTouchArea : ITouchArea
    {
        /// <summary>
        /// 控制器
        /// </summary>
        public IController? Controller;
        /// <summary>
        /// 触摸范围和事件列表
        /// </summary>
        public List<TTouchArea> TouchEvent = new List<TTouchArea>();
        /// <summary>
        /// 游戏数据
        /// </summary>
        public IGameSave? Save;
    }
}
