using System.Collections;
using QFramework.System;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class BeaconMissionInstance : AbstractMissionInstance
    {
        [SerializeField] private GameObject _beaconActiveEffect;
        [SerializeField] private Interactable _interactable;

        private bool _isActivated = false;
        private bool _step0Done = false;
        private int _defendDuration;
        private Coroutine _activateCoroutine;

        public override void Init(MissionDataModel mission)
        {
            base.Init(mission);
            _isActivated = false;
            _step0Done = false;
            // 从任务配置中读取防守时长（Step 1 的 Progress）
            _defendDuration = mission.MissionConfig.MissionSteps[1].Progress;
            if(_beaconActiveEffect) _beaconActiveEffect.SetActive(false);
        }

        private void Start()
        {
            if(_interactable != null)
                _interactable.OnInteracted += OnInteracted;
        }

        private void OnDestroy()
        {
            if(_interactable != null)
                _interactable.OnInteracted -= OnInteracted;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!other.CompareTag("Player") || _isActivated || _step0Done) return;
            _step0Done = true;
            // 进入范围 → 自动完成 Step 0（找到信标）
            AddProgress(1);
        }

        private void OnInteracted(GameObject player)
        {
            if(_isActivated) return;
            _isActivated = true;

            if(_beaconActiveEffect) _beaconActiveEffect.SetActive(true);
            _interactable.Lock();

            // 每秒添加进度
            _activateCoroutine = StartCoroutine(ActivateRoutine());
        }

        private IEnumerator ActivateRoutine()
        {
            for(int i = 0; i < _defendDuration; i++)
            {
                yield return new WaitForSeconds(1f);
                AddProgress(1);  // Step 1 进度 +1
            }
        }
    }
}
