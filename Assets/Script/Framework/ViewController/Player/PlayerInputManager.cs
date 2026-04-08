using QFramework;
using Unity.VisualScripting;
using UnityEngine;
using QFramework.UtilityKit;

namespace QFramework.ViewController.Player
{
    public class PlayerInputManager : MonoSingleton<PlayerInputManager>
    {
        private Camera _cam;
        private Transform _player;

        public void InitPlayerInput(Camera cam, Transform player)
        {
            _cam = cam;
            _player = player;
        }


        public Vector3 GetMovementDir()
        {

            // 获取键盘输入的移动方向
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);

            return input;
        }

        public Vector3 GetMousePos()
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, _player.position);
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
    }
}
