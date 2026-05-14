using QFramework.System;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class EntryMissionInstance : AbstractMissionInstance
    {
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }
}
