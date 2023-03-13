using UnityEngine;

namespace CodeBase
{
    public class CameraRotation : MonoBehaviour
    {
        [SerializeField] private Transform _rotateAroundTarget = default;
        [SerializeField] private CameraRotationSettings _settings = default;

        private Transform _anchor;
        private Vector3 _mousePosInPreviousFrame;
        private Vector2 _rotationVelocityEuler;
        private Vector2 _currentRotationEuler;
        private Vector2 _currentInertia;

        private Vector3 RotateAroundPoint => _rotateAroundTarget
            ? _rotateAroundTarget.position
            : Vector3.zero;
    
        private void Start()
        {
            CreateAnchor();

            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            var mouseDelta = ProcessInput(out var mousePressed);

            if (mousePressed)
            {
                CalculateRotation(mouseDelta);
            }
            else
            {
                CalculateInertia(mouseDelta);
                ApplyInertia();
            }

            ApplyRotation(_currentRotationEuler);
        }

        private void CalculateInertia(Vector2 mouseDelta)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _currentInertia = mouseDelta * _settings.Speed;
            }
        }

        private void ApplyRotation(Vector2 rotation)
        {
            _anchor.rotation = Quaternion.Euler(rotation.y, rotation.x, 0);
        }

        private void ApplyInertia()
        {
            if (_currentInertia.sqrMagnitude < float.Epsilon)
            {
                return;
            }

            _currentInertia = DampToZero(_currentInertia, _settings.InertiaDamp);
            _currentRotationEuler += _currentInertia * Time.deltaTime;

            ResetInertiaIfRotationIsOutOfBounds();
        }

        private void ResetInertiaIfRotationIsOutOfBounds()
        {
            void ResetInertiaIfOutOfBounds(float rotation, float minAngle, float maxAngle, ref float inertia)
            {
                if (!WithinRange(rotation, minAngle, maxAngle))
                {
                    inertia = 0;
                }
            } 
            ResetInertiaIfOutOfBounds(_currentRotationEuler.x, _settings.MinRotateAngle.x, _settings.MaxRotateAngle.x, ref _currentInertia.x);
            ResetInertiaIfOutOfBounds(_currentRotationEuler.y, _settings.MinRotateAngle.y, _settings.MaxRotateAngle.y, ref _currentInertia.y);
        }

        private void CreateAnchor()
        {
            var anchorGameObject = new GameObject("CameraAnchor");
            anchorGameObject.transform.SetPositionAndRotation(RotateAroundPoint, Quaternion.identity);
            _anchor = anchorGameObject.transform;
            transform.SetParent(_anchor);
        }

        private Vector2 ProcessInput(out bool mousePressed)
        {
            mousePressed = Input.GetMouseButton(0);
            return CalculateInputDelta();
        }

        private Vector3 CalculateInputDelta()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _mousePosInPreviousFrame = Input.mousePosition;
            }
        
            var mouseDelta = Input.mousePosition - _mousePosInPreviousFrame;
            _mousePosInPreviousFrame = Input.mousePosition;
            return mouseDelta;
        }

        private void CalculateRotation(Vector2 delta)
        {
            var speed = _settings.Speed;
            speed = DampRotation(speed);

            var speedX = delta.x * speed.x;
            var speedY = delta.y * speed.y;
            _rotationVelocityEuler = new Vector2(speedX, speedY);

            var newRotationEuler = _currentRotationEuler + _rotationVelocityEuler * Time.deltaTime;
            newRotationEuler = Clamp(
                newRotationEuler,
                _settings.MinRotateAngle,
                _settings.MaxRotateAngle);

            _currentRotationEuler = newRotationEuler;
        }

        private Vector2 DampRotation(Vector2 speed)
        {
            var dampValueX = InverseLerpBetweenTwoRanges(
                _currentRotationEuler.x,
                new Vector2(_settings.MaxElasticAngle.x, _settings.MaxRotateAngle.x),
                new Vector2(_settings.MinElasticAngle.x, _settings.MinRotateAngle.x));

            var dampValueY = InverseLerpBetweenTwoRanges(
                _currentRotationEuler.y,
                new Vector2(_settings.MaxElasticAngle.y, _settings.MaxRotateAngle.y),
                new Vector2(_settings.MinElasticAngle.y, _settings.MinRotateAngle.y));

            return new Vector2(speed.x * dampValueX, speed.y * dampValueY);
        }

        private static float InverseLerpBetweenTwoRanges(float value, Vector2 positiveRange, Vector2 negativeRange)
        {
            if (value > positiveRange.x)
            {
                return 1f - Mathf.InverseLerp(positiveRange.x, positiveRange.y, value);
            }

            if (value < negativeRange.x)
            {
                return 1f - Mathf.InverseLerp(negativeRange.x, negativeRange.y, value);
            }

            return 1f;
        }

        private static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y));
        }

        private static Vector2 DampToZero(Vector2 value, Vector2 dampValue)
        {
            return new Vector2(
                DampToZero(value.x, dampValue.x),
                DampToZero(value.y, dampValue.y));
        }

        private static float DampToZero(float value, float dampFactor)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return 0f;
            }

            return value > 0
                ? Mathf.Max(0f, value - dampFactor)
                : Mathf.Min(0f, value + dampFactor);
        }

        private static bool WithinRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }
    }
}