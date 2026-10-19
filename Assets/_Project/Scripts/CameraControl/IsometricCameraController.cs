using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaghettiWires.CameraControl
{
    /// <summary>
    /// Pseudo-izometryczna kamera 3D (perspektywa pod stałym kątem).
    /// Kamera krąży wokół punktu skupienia na płaszczyźnie ziemi (y = groundHeight).
    /// Sterowanie wg GDD (sekcja 4):
    ///   - przesuwanie: WSAD / strzałki / krawędź ekranu / przeciąganie środkowym przyciskiem myszy,
    ///   - obrót: Q / E (skok o rotationStep) lub przeciąganie prawym przyciskiem myszy,
    ///   - zoom: kółko myszy.
    /// Skrypt należy dodać bezpośrednio do obiektu z komponentem Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Perspektywa")]
        [Tooltip("Kąt nachylenia kamery w dół (30–35° daje klasyczny wygląd izometryczny).")]
        [Range(10f, 89f)] [SerializeField] private float pitch = 35f;
        [Tooltip("Wąskie FOV spłaszcza perspektywę i przybliża wygląd do izometrii.")]
        [Range(10f, 90f)] [SerializeField] private float fieldOfView = 30f;
        [SerializeField] private float startYaw = 45f;
        [SerializeField] private Vector3 startFocus = Vector3.zero;
        [SerializeField] private float groundHeight = 0f;

        [Header("Przesuwanie")]
        [Tooltip("Prędkość w jednostkach/s przy domyślnym przybliżeniu; skaluje się z odległością.")]
        [SerializeField] private float panSpeed = 15f;
        [SerializeField] private bool edgeScrolling = true;
        [Tooltip("Szerokość pasa przy krawędzi ekranu (px), który przesuwa kamerę.")]
        [SerializeField] private float edgeSize = 12f;
        [SerializeField] private bool middleMouseDragPan = true;
        [Tooltip("Granice obszaru, po którym może poruszać się punkt skupienia (XZ).")]
        [SerializeField] private Rect bounds = new Rect(-50f, -50f, 100f, 100f);

        [Header("Obrót")]
        [Tooltip("Kąt skoku obrotu przy Q / E.")]
        [SerializeField] private float rotationStep = 45f;
        [Tooltip("Stopnie obrotu na piksel ruchu myszy przy przeciąganiu PPM.")]
        [SerializeField] private float mouseRotateSensitivity = 0.25f;

        [Header("Zoom")]
        [SerializeField] private float minDistance = 10f;
        [SerializeField] private float maxDistance = 80f;
        [SerializeField] private float startDistance = 35f;
        [Tooltip("Część aktualnej odległości zmieniana przy jednym ząbku kółka.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float zoomStep = 0.12f;

        [Header("Wygładzanie")]
        [Tooltip("Czas wygładzania ruchu (s). 0 = bez wygładzania.")]
        [SerializeField] private float smoothTime = 0.12f;

        private Camera _camera;

        // Wartości docelowe (ustawiane przez input)…
        private Vector3 _targetFocus;
        private float _targetYaw;
        private float _targetDistance;

        // …i aktualne (wygładzane w stronę docelowych).
        private Vector3 _focus;
        private float _yaw;
        private float _distance;

        private Vector3 _focusVelocity;
        private float _yawVelocity;
        private float _distanceVelocity;

        // Zanim mysz się poruszy, Input System podaje jej pozycję (0, 0), czyli róg ekranu.
        // Bez tej flagi przewijanie krawędzią od razu odsuwało kamerę od punktu startowego.
        private bool _pointerMoved;

        public Vector3 Focus => _focus;
        public float Yaw => _yaw;
        public float Distance => _distance;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = false;
            _camera.fieldOfView = fieldOfView;

            _targetFocus = _focus = ClampToBounds(new Vector3(startFocus.x, groundHeight, startFocus.z));
            _targetYaw = _yaw = startYaw;
            _targetDistance = _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);

            ApplyTransform();
        }

        private void LateUpdate()
        {
            HandleRotation();
            HandleZoom();
            HandlePan();
            Smooth();
            ApplyTransform();
        }

        /// <summary>Natychmiast ustawia punkt skupienia (np. na centrali).</summary>
        public void SnapTo(Vector3 worldPoint)
        {
            _targetFocus = _focus = ClampToBounds(new Vector3(worldPoint.x, groundHeight, worldPoint.z));
            _focusVelocity = Vector3.zero;
        }

        /// <summary>Płynnie przesuwa kamerę do wskazanego punktu.</summary>
        public void MoveTo(Vector3 worldPoint)
        {
            _targetFocus = ClampToBounds(new Vector3(worldPoint.x, groundHeight, worldPoint.z));
        }

        private void HandleRotation()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Q/E obracają do kolejnej "półki" kąta, więc kamera zawsze wraca do równych kątów.
                if (keyboard.qKey.wasPressedThisFrame)
                    _targetYaw = SnapYaw(_targetYaw, +1);
                if (keyboard.eKey.wasPressedThisFrame)
                    _targetYaw = SnapYaw(_targetYaw, -1);
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                _targetYaw += mouse.delta.ReadValue().x * mouseRotateSensitivity;
                // Przy obrocie myszą bez wygładzania kąta, żeby kamera "trzymała się" kursora.
                _yaw = _targetYaw;
                _yawVelocity = 0f;
            }
        }

        private float SnapYaw(float yaw, int direction)
        {
            if (rotationStep <= 0f)
                return yaw;

            float steps = yaw / rotationStep;
            float snapped = direction > 0 ? Mathf.Floor(steps + 0.001f) + 1f : Mathf.Ceil(steps - 0.001f) - 1f;
            return snapped * rotationStep;
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null || !IsPointerInsideScreen(mouse))
                return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            // Scroll bywa w ząbkach (±120 na Windows) albo w jednostkach znormalizowanych – liczymy tylko znak.
            float factor = 1f - Mathf.Sign(scroll) * zoomStep;
            _targetDistance = Mathf.Clamp(_targetDistance * factor, minDistance, maxDistance);
        }

        private void HandlePan()
        {
            Vector2 input = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            }

            var mouse = Mouse.current;
            bool rotatingOrDragging = mouse != null && (mouse.rightButton.isPressed || mouse.middleButton.isPressed);
            if (mouse != null && mouse.delta.ReadValue() != Vector2.zero)
                _pointerMoved = true;

            if (edgeScrolling && _pointerMoved && mouse != null && !rotatingOrDragging && Application.isFocused && IsPointerInsideScreen(mouse))
            {
                Vector2 p = mouse.position.ReadValue();
                if (p.x <= edgeSize) input.x -= 1f;
                else if (p.x >= Screen.width - edgeSize) input.x += 1f;
                if (p.y <= edgeSize) input.y -= 1f;
                else if (p.y >= Screen.height - edgeSize) input.y += 1f;
            }

            input = Vector2.ClampMagnitude(input, 1f);

            // Kierunki ruchu w płaszczyźnie ziemi, zgodne z aktualnym obrotem kamery.
            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;

            // Im dalej kamera, tym szybciej się przesuwa – na ekranie prędkość wygląda podobnie.
            float speed = panSpeed * (_targetDistance / Mathf.Max(startDistance, 0.01f));
            Vector3 delta = (right * input.x + forward * input.y) * (speed * Time.unscaledDeltaTime);

            if (middleMouseDragPan && mouse != null && mouse.middleButton.isPressed)
                delta += MouseDragDelta(mouse);

            if (delta.sqrMagnitude > 0f)
                _targetFocus = ClampToBounds(_targetFocus + delta);
        }

        /// <summary>Przesunięcie tak, by punkt ziemi pod kursorem "przyklejał się" do kursora.</summary>
        private Vector3 MouseDragDelta(Mouse mouse)
        {
            Vector2 current = mouse.position.ReadValue();
            Vector2 previous = current - mouse.delta.ReadValue();

            if (!RaycastGround(previous, out Vector3 from) || !RaycastGround(current, out Vector3 to))
                return Vector3.zero;

            // Przy przeciąganiu wyłączamy wygładzanie, żeby ruch był 1:1 z kursorem.
            Vector3 delta = from - to;
            _focus += delta;
            _focusVelocity = Vector3.zero;
            return delta;
        }

        private bool RaycastGround(Vector2 screenPoint, out Vector3 hit)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            if (plane.Raycast(ray, out float enter))
            {
                hit = ray.GetPoint(enter);
                return true;
            }

            hit = default;
            return false;
        }

        private void Smooth()
        {
            if (smoothTime <= 0f)
            {
                _focus = _targetFocus;
                _yaw = _targetYaw;
                _distance = _targetDistance;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _focus = Vector3.SmoothDamp(_focus, _targetFocus, ref _focusVelocity, smoothTime, Mathf.Infinity, dt);
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, smoothTime, Mathf.Infinity, dt);
            _distance = Mathf.SmoothDamp(_distance, _targetDistance, ref _distanceVelocity, smoothTime, Mathf.Infinity, dt);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, _yaw, 0f);
            transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * _distance, rotation);
        }

        private Vector3 ClampToBounds(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, bounds.xMin, bounds.xMax);
            point.z = Mathf.Clamp(point.z, bounds.yMin, bounds.yMax);
            point.y = groundHeight;
            return point;
        }

        private static bool IsPointerInsideScreen(Mouse mouse)
        {
            Vector2 p = mouse.position.ReadValue();
            return p.x >= 0f && p.y >= 0f && p.x <= Screen.width && p.y <= Screen.height;
        }

        private void OnValidate()
        {
            maxDistance = Mathf.Max(maxDistance, minDistance);
            startDistance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            if (_camera != null)
                _camera.fieldOfView = fieldOfView;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var center = new Vector3(bounds.center.x, groundHeight, bounds.center.y);
            Gizmos.DrawWireCube(center, new Vector3(bounds.width, 0f, bounds.height));

            if (Application.isPlaying)
                Gizmos.DrawWireSphere(_focus, 0.5f);
        }
    }
}
