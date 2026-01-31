using System;
using EnemyAI;
using Microlight.MicroBar;
using Unity.Cinemachine;
using UnityEngine;

namespace Player
{
    public class PossessionManager : MonoBehaviour
    {
        public static PossessionManager Instance;

        [Header("References")]
        public GameObject maskPlayer;
        public CinemachineCamera virtualCamera;
        public MicroBar stabilityBar;

        [Header("Settings")]
        public KeyCode ejectKey = KeyCode.Space;

        public Action<Transform> OnPossessChanged;
        
        // State
        private PossessableEnemy currentHost;
        public bool IsPossessing => currentHost != null;

        private void Awake()
        {
            Instance = this;
            stabilityBar.Initialize(100f);
            stabilityBar.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsPossessing)
            {
                stabilityBar.UpdateBar(currentHost.CurrentStability);
                
                if (Input.GetKeyDown(ejectKey))
                    Eject();
            }
        }

        public void Possess(PossessableEnemy targetEnemy)
        {
            if (IsPossessing) return;

            currentHost = targetEnemy;

            maskPlayer.SetActive(false);

            targetEnemy.OnPossess();

            virtualCamera.Follow = targetEnemy.transform;
            virtualCamera.LookAt = targetEnemy.transform;
            
            stabilityBar.SetNewMaxHP(currentHost.maxStability);
            stabilityBar.UpdateBar(currentHost.CurrentStability, true);
            stabilityBar.gameObject.SetActive(true);

            Debug.Log(targetEnemy.name + " bedeni ele geçirildi!");
            OnPossessChanged?.Invoke(targetEnemy.transform);
        }

        public void Eject()
        {
            if (!IsPossessing) return;

            maskPlayer.transform.position = currentHost.transform.position + Vector3.forward * 2f;
            maskPlayer.SetActive(true);
        
            Rigidbody maskRb = maskPlayer.GetComponent<Rigidbody>();
            if(maskRb) maskRb.AddForce(Vector3.up * 5f, ForceMode.Impulse);

            bool isFullEnlightened = currentHost.IsEnlightened();
            currentHost.OnDepossess(isFullEnlightened);

            virtualCamera.Follow = maskPlayer.transform;
            virtualCamera.LookAt = maskPlayer.transform;
            
            stabilityBar.gameObject.SetActive(false);

            currentHost = null;
            OnPossessChanged?.Invoke(maskPlayer.transform);
        }
    }
}