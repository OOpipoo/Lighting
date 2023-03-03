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
                CalculateManualRotation(mouseDelta);
            }
            else
            {
                CalculateInertia(mouseDelta);
                ApplyInertia();
                ApplyElastic();
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
            if (!(_currentInertia.magnitude > float.Epsilon))
            {
                return;
            }

            _currentInertia = DampVectorToZero(_currentInertia, _settings.InertiaDamp);
            _currentRotationEuler += _currentInertia * Time.deltaTime;

            ResetInertiaIfRotationIsOutOfBounds();
        }

        private void ResetInertiaIfRotationIsOutOfBounds()
        {
            if (!WithinRange(
                    _currentRotationEuler.x,
                    _settings.MinRotateAngle.x,
                    _settings.MaxRotateAngle.x))
            {
                _currentInertia.x = 0;
            }

            if (!WithinRange(
                    _currentRotationEuler.y,
                    _settings.MinRotateAngle.y,
                    _settings.MaxRotateAngle.y))
            {
                _currentInertia.y = 0;
            }
        }

        private void ApplyElastic()
        {
            var targetRotation = ClampVector(
                _currentRotationEuler,
                _settings.MinElasticAngle,
                _settings.MaxElasticAngle);

            var rotationX = Mathf.Lerp(
                _currentRotationEuler.x,
                targetRotation.x,
                _settings._autoRotationSpeed.x * Time.deltaTime);

            var rotationY = Mathf.Lerp(
                _currentRotationEuler.y,
                targetRotation.y,
                _settings._autoRotationSpeed.y * Time.deltaTime);

            _currentRotationEuler = new Vector2(rotationX, rotationY);
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

        private void CalculateManualRotation(Vector2 delta)
        {
            var speed = _settings.Speed;
            speed = DampRotationSpeed(speed);
        
            var speedX = delta.x * speed.x;
            var speedY = delta.y * speed.y;
            _rotationVelocityEuler = new Vector2(speedX, speedY);

            var newRotationEuler = _currentRotationEuler + _rotationVelocityEuler * Time.deltaTime;
            newRotationEuler = ClampVector(
                newRotationEuler, 
                _settings.MinRotateAngle, 
                _settings.MaxRotateAngle);
        
            _currentRotationEuler = newRotationEuler;
        }

        private Vector2 DampRotationSpeed(Vector2 speed)
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

        private static Vector2 ClampVector(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y));
        }

        private static Vector2 DampVectorToZero(Vector2 value, Vector2 dampValue)
        {
            value.x = DampValueToZero(value.x, dampValue.x);
            value.y = DampValueToZero(value.y, dampValue.y);
            return value;
        }

        private static float DampValueToZero(float value, float dampFactor)
        {
            if (value != 0)
            {
                return value > 0
                    ? Mathf.Max(0, value - dampFactor)
                    : Mathf.Min(0, value + dampFactor);
            }

            return 0f;
        }

        private static bool WithinRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }
    }
}