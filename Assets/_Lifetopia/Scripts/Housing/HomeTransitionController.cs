using System;
using System.Collections;
using Lifetopia.CameraSystem;
using Lifetopia.Controls;
using Lifetopia.Core;
using Lifetopia.InventorySystem;
using Lifetopia.Player;
using Lifetopia.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lifetopia.Housing
{
    /// <summary>Everything the interior scene needs from the village scene (explicit, no global lookup).</summary>
    public sealed class HousingContext
    {
        public HouseDecorService Service;
        public PlayerProfileHost Profile;
        public PlayerInputReader Input;
        public GameplayInputGate Gate;
        public PlayerMotor2D Player;
        public CircleCollider2D PlayerCollider;
        public Camera Camera;
        public HousingVisualSet Visuals;
        public GameObject[] HideWhileDecorating;
        public TouchControlsVisibility Touch;
        public Func<bool> RequestExit;
    }

    /// <summary>
    /// Enters and leaves the starter house. The interior is a separate scene loaded ADDITIVELY at a far
    /// offset, while the village scene (profile, wallet, clock, farm, HUD, player, camera) stays loaded,
    /// so there is exactly one of each service and crops keep growing on the active game clock.
    /// Leaving unloads the interior scene; placements live in <see cref="HousingSessionHost"/>.
    /// - Input is locked during the short fade (one transition at a time, cooldown afterwards, no
    ///   double teleport); entering is refused while any menu/modal blocks gameplay.
    /// - The outdoor camera follow, size and background are saved and restored on exit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HomeTransitionController : MonoBehaviour
    {
        [Header("Interior")]
        [Tooltip("Scene name of the interior (must be in the build profile scene list).")]
        [SerializeField] private string interiorScene = "HomeInterior";
        [Tooltip("World position of the room origin while inside (far away from the village).")]
        [SerializeField] private Vector2 interiorOrigin = new Vector2(200f, 0f);
        [Tooltip("Original artwork for the interior (LOCAL scenes only). Empty = greybox placeholders.")]
        [SerializeField] private HousingVisualSet visuals;

        [Header("Village")]
        [SerializeField] private HousingSessionHost session;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameplayInputGate gate;
        [SerializeField] private PlayerMotor2D player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CameraFollow2D cameraFollow;
        [Tooltip("Where the player stands after leaving the house (just below the front steps).")]
        [SerializeField] private Transform outdoorReturnPoint;
        [SerializeField] private TouchControlsVisibility touchControls;
        [Tooltip("Hidden while Decorating Mode is open (hotbar, Bag button, joystick/Interact).")]
        [SerializeField] private GameObject[] hideWhileDecorating = Array.Empty<GameObject>();

        [Header("Transition")]
        [SerializeField, Range(0f, 1f)] private float fadeSeconds = 0.2f;
        [Tooltip("Real seconds after a transition before the next one is accepted.")]
        [SerializeField, Range(0f, 2f)] private float cooldownSeconds = 0.6f;

        private Image _fade;
        private float _readyAt;
        private float _savedSize;
        private Color _savedBackground;
        private HouseInteriorController _interior;

        public bool IsInside { get; private set; }
        public bool IsTransitioning { get; private set; }
        public HouseInteriorController Interior => _interior;
        /// <summary>Completed transitions (tests: one request = one transition).</summary>
        public int TransitionCount { get; private set; }

        public event Action<bool> InsideChanged;

        public bool CanEnter => !IsInside && !IsTransitioning && Ready && (gate == null || !gate.IsBlocked);
        public bool CanExit => IsInside && !IsTransitioning && Ready && (gate == null || !gate.IsBlocked);
        private bool Ready => Time.unscaledTime >= _readyAt;

        private void Awake() => BuildFade();

        private void OnDestroy()
        {
            if (gate != null) gate.Unblock(this);
        }

        public bool RequestEnter()
        {
            if (!CanEnter || session == null || session.Service == null || player == null || worldCamera == null) return false;
            StartCoroutine(EnterRoutine());
            return true;
        }

        public bool RequestExit()
        {
            if (!CanExit) return false;
            StartCoroutine(ExitRoutine());
            return true;
        }

        private IEnumerator EnterRoutine()
        {
            Begin();
            yield return Fade(1f);

            AsyncOperation op = null;
            try { op = SceneManager.LoadSceneAsync(interiorScene, LoadSceneMode.Additive); }
            catch (Exception e) { Debug.LogError("[Lifetopia] Could not load " + interiorScene + ": " + e.Message, this); }
            if (op == null)
            {
                session.ProfileHost?.Notify("The house is not available right now.");
                yield return Fade(0f);
                Finish();
                yield break;
            }
            while (!op.isDone) yield return null;

            _interior = FindInterior(SceneManager.GetSceneByName(interiorScene));
            if (_interior == null)
            {
                Debug.LogError("[Lifetopia] " + interiorScene + " has no HouseInteriorController at its root.", this);
                yield return SceneManager.UnloadSceneAsync(interiorScene);
                yield return Fade(0f);
                Finish();
                yield break;
            }

            _savedSize = worldCamera.orthographicSize;
            _savedBackground = worldCamera.backgroundColor;
            Vector2 before = player.transform.position;
            bool entered = false;
            try
            {
                if (cameraFollow != null) cameraFollow.enabled = false;
                _interior.transform.position = new Vector3(interiorOrigin.x, interiorOrigin.y, 0f);
                _interior.Enter(new HousingContext
                {
                    Service = session.Service,
                    Profile = session.ProfileHost,
                    Input = input,
                    Gate = gate,
                    Player = player,
                    PlayerCollider = player.GetComponent<CircleCollider2D>(),
                    Camera = worldCamera,
                    Visuals = visuals,
                    HideWhileDecorating = hideWhileDecorating,
                    Touch = touchControls,
                    RequestExit = RequestExit,
                });
                entered = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            if (!entered)
            {
                // Recover: back to the village exactly as before, nothing lost.
                try { _interior.Leave(); } catch (Exception e) { Debug.LogException(e, this); }
                _interior = null;
                player.Teleport(before);
                RestoreCamera();
                yield return SceneManager.UnloadSceneAsync(interiorScene);
                session.ProfileHost?.Notify("The house is not available right now.");
                yield return Fade(0f);
                Finish();
                yield break;
            }

            IsInside = true;
            TransitionCount++;
            InsideChanged?.Invoke(true);

            yield return Fade(0f);
            Finish();
        }

        private IEnumerator ExitRoutine()
        {
            Begin();
            yield return Fade(1f);

            if (_interior != null) _interior.Leave();
            _interior = null;
            AsyncOperation op = SceneManager.UnloadSceneAsync(interiorScene);
            if (op != null) while (!op.isDone) yield return null;

            if (outdoorReturnPoint != null) player.Teleport(outdoorReturnPoint.position);
            RestoreCamera();
            IsInside = false;
            TransitionCount++;
            InsideChanged?.Invoke(false);

            yield return Fade(0f);
            Finish();
        }

        private void RestoreCamera()
        {
            worldCamera.orthographicSize = _savedSize;
            worldCamera.backgroundColor = _savedBackground;
            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
                cameraFollow.SnapToTarget();
            }
        }

        private void Begin()
        {
            IsTransitioning = true;
            // Movement/interaction locked; game time keeps running (a fade is not a menu).
            if (gate != null) gate.Block(this, false);
        }

        private void Finish()
        {
            if (gate != null) gate.Unblock(this);
            IsTransitioning = false;
            _readyAt = Time.unscaledTime + cooldownSeconds;
        }

        private static HouseInteriorController FindInterior(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<HouseInteriorController>(true);
                if (c != null) return c;
            }
            return null;
        }

        private IEnumerator Fade(float to)
        {
            if (_fade == null) yield break;
            _fade.raycastTarget = true;
            float from = _fade.color.a;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, fadeSeconds > 0f ? t / fadeSeconds : 1f));
                yield return null;
            }
            SetAlpha(to);
            _fade.raycastTarget = to > 0f;
            _fade.enabled = to > 0f;
        }

        private void SetAlpha(float a)
        {
            _fade.enabled = true;
            Color c = _fade.color;
            c.a = a;
            _fade.color = c;
        }

        /// <summary>Full-screen fade drawn above every HUD canvas; also swallows taps during the transition.</summary>
        private void BuildFade()
        {
            var go = new GameObject("HomeTransitionFade", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var img = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(go.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _fade = img.GetComponent<Image>();
            _fade.color = new Color(0.12f, 0.08f, 0.06f, 0f);
            _fade.raycastTarget = false;
            _fade.enabled = false;
        }
    }
}
