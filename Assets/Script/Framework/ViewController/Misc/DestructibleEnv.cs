using UnityEngine;

namespace QFramework.ViewController
{
    public abstract class DestructibleEnv : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 50;
        private int _currentHealth;

        private void Awake() => _currentHealth = _maxHealth;

        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;
            if (_currentHealth <= 0)
                OnDeath();
        }

        protected abstract void OnDeath();
    }
}
