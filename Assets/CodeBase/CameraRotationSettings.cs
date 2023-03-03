using UnityEngine;

namespace CodeBase
{
    [CreateAssetMenu]
    public class CameraRotationSettings : ScriptableObject
    {
        [Header("Rotation constrains:")]
        [SerializeField]
        private Vector2 _elasticAngle = new(20f, 20f);

        [SerializeField] 
        private Vector2 _maxRotateAngle = new(45f, 45f);

        [Header("Auto Rotation Settings:")] 
        public Vector2 _autoRotationSpeed = new(20, 20);

        [Header("Rotate settings:")] 
        public Vector2 Speed = new(20, 0);
        public Vector2 InertiaDamp = new(0.1f, 0.1f);

        public Vector2 MaxElasticAngle => _elasticAngle;
        public Vector2 MinElasticAngle => -_elasticAngle;
        public Vector2 MaxRotateAngle => _maxRotateAngle;
        public Vector2 MinRotateAngle => -_maxRotateAngle;
    }
}