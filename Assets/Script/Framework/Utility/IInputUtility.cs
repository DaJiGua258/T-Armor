using Unity.VisualScripting;
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
        public bool GetESCInput();  // 获取ESC输入
        public bool GetLeftReloadInput();  // 获取左手武器输入
        public bool GetRightReloadInput();  // 获取右手武器输入
        public bool GetSprintInput();  // 获取冲刺输入
        public bool GetInventoryInput();  // 获取背包输入
        public int GetHotbarSelectInput();  // 获取快捷栏数字键输入，返回0-based索引，无输入返回-1
        public bool GetToggleAimModeInput();  // 获取切换瞄准模式输入（X键）
        public bool GetHotbarCycleInput();  // 获取快捷栏切换输入（Tab键）
        public bool GetLeftMouseDownInput();  // 获取左键按下输入
        public bool GetLeftMouseUpInput();  // 获取左键抬起输入
        public bool GetHangerLeftInputDown();  // 获取左吊架武器按下输入（Q键按下）
        public bool GetHangerRightInputDown();  // 获取右吊架武器按下输入（E键按下）
    }

    public class InputUtility : IInputUtility
    {
        private Camera _cam;
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

        /// <summary>
        /// 获取鼠标在世界坐标系中的位置
        /// </summary>
        public Vector3 GetMousePos()
        {
            if(Camera.main == null)
            {
                Debug.LogWarning("Camera is null");
                return Vector3.zero;
            }

            _cam = Camera.main;
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

        public bool GetESCInput()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }

        public bool GetLeftReloadInput()
        {
            if(Input.GetKey(KeyCode.R) && Input.GetMouseButtonDown(0))
            {
                return true;
            }
            return false;
        }

        public bool GetRightReloadInput()
        {
            if(Input.GetKey(KeyCode.R) && Input.GetMouseButtonDown(1))
            {
                return true;
            }
            return false;
        }

        public bool GetSprintInput()
        {
            if(GetMovementDir().sqrMagnitude > 0.01f && Input.GetKeyDown(KeyCode.LeftShift))
            {
                return true;
            }

            return false;
        }

        public bool GetInventoryInput()
        {
            return Input.GetKeyDown(KeyCode.B);
        }

        // 检测数字键1-9，返回0-based索引，无输入返回-1
        public int GetHotbarSelectInput()
        {
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    return i;
            }
            return -1;
        }

        public bool GetToggleAimModeInput()
        {
            return Input.GetKeyDown(KeyCode.X);
        }

        public bool GetHotbarCycleInput()
        {
            return Input.GetKeyDown(KeyCode.Tab);
        }

        public bool GetLeftMouseDownInput()
        {
            return Input.GetMouseButtonDown(0);
        }

        public bool GetLeftMouseUpInput()
        {
            return Input.GetMouseButtonUp(0);
        }

        public bool GetHangerLeftInputDown()
        {
            return Input.GetKeyDown(KeyCode.Q);
        }

        public bool GetHangerRightInputDown()
        {
            return Input.GetKeyDown(KeyCode.E);
        }
    }
}