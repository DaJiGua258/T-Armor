using UnityEngine;

namespace QFramework.Utility
{
    public interface IInputUtility : IUtility
    {
        public Vector3 GetMovementDir();  // 获取移动方向
        public Vector3 GetMousePos();  // 获取鼠标位置
        public bool GetShootLeftInput();  // 获取射击左键输入
        public bool GetShootRightInput();  // 获取射击右键输入
        public bool GetPickUpItemInput();  // 获取拾取物品输入
        public bool GetDashInput();  // 获取冲刺输入
    }

    public class InputUtility : IInputUtility
    {
        private Camera _cam = Camera.main;
        private Transform _player;
        public Transform Player
        {
            get
            {
                if(_player == null)
                {
                    _player = GameObject.FindGameObjectWithTag("Player").transform;
                }

                return _player;
            }
        }


        public Vector3 GetMovementDir()
        {
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
            return input;
        }

        public Vector3 GetMousePos()
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, Player.position);
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        public bool GetShootLeftInput()
        {
            return Input.GetMouseButton(0);
        }

        public bool GetShootRightInput()
        {
            return Input.GetMouseButton(1);
        }

        public bool GetPickUpItemInput()
        {
            return Input.GetKeyDown(KeyCode.F);
        }

        public bool GetDashInput()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }
    }
}