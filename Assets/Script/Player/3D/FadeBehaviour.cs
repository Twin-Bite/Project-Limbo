using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FadeBehaviour : MonoBehaviour
{
    // ── Enums ────────────────────────────────────────────────
    public enum SelectType  { Normal, Gradient, Eye, Clock }
    public enum GradientMode { X, Y, Z }
    public enum EaseType
    {
        Unset,
        Linear,
        InSine,    OutSine,    InOutSine,
        InQuad,    OutQuad,    InOutQuad,
        InCubic,   OutCubic,   InOutCubic
    }

    [HideInInspector] public SelectType  selectType   = SelectType.Normal;

    // Normal / Gradient shared
    [HideInInspector] public CanvasGroup fadingPanel;
    [HideInInspector] public bool        modifyOtherCanvasGroup = false;
    [HideInInspector] public bool        inOut                  = false;

    // On Start
    [HideInInspector] public bool onStart      = false;
    [HideInInspector] public bool startFadeIn  = false;
    [HideInInspector] public bool startFadeOut = false;

    // Timing & ease
    [HideInInspector] public float    fadingSpeed          = 1f;
    [HideInInspector] public float    delay                = 0f;
    [HideInInspector] public float    endDelay             = 0f;
    [HideInInspector] public EaseType selectEase           = EaseType.Unset;
    [HideInInspector] public float    fadingPanelPosition  = 0f;

    // Gradient
    [HideInInspector] public GradientMode gradientMode = GradientMode.X;

    // Eye
    [HideInInspector] public Transform eyeController;

    // Clock
    [HideInInspector] public Image clockPanel;

    // Events
    [HideInInspector] public UnityEvent onBeginFadingIn    = new UnityEvent();
    [HideInInspector] public UnityEvent onCompleteFadingIn = new UnityEvent();
    [HideInInspector] public UnityEvent onBeginFadingOut   = new UnityEvent();
    [HideInInspector] public UnityEvent onCompleteFadingOut = new UnityEvent();

    // Internal
    private Vector3 _initialLocalPos;
    void Start()
    {
        if (fadingPanel != null)
            _initialLocalPos = fadingPanel.transform.localPosition;

        if (onStart)
        {
            if (startFadeIn)  BeginFadingIn();
            if (startFadeOut) BeginFadingOut();
        }
    }

    public void BeginFadingIn()
    {
        StopAllCoroutines();
        StartCoroutine(CoFadeIn());
    }

    public void BeginFadingOut()
    {
        StopAllCoroutines();
        StartCoroutine(CoFadeOut());
    }


    IEnumerator CoFadeIn()
    {
        yield return new WaitForSeconds(delay);
        onBeginFadingIn?.Invoke();

        switch (selectType)
        {
            case SelectType.Normal:
                if (fadingPanel != null) fadingPanel.alpha = 0f;
                yield return StartCoroutine(FadeAlpha(fadingPanel, 0f, 1f, fadingSpeed));
                break;

            case SelectType.Gradient:
                if (fadingPanel != null)
                {
                    fadingPanel.alpha = 1f;
                    fadingPanel.transform.localPosition = _initialLocalPos;
                }
                yield return StartCoroutine(MoveToTarget(fadingPanel?.transform, fadingPanelPosition, fadingSpeed));
                break;

            case SelectType.Eye:
                if (eyeController != null)
                    yield return StartCoroutine(ScaleY(eyeController, 0f, fadingSpeed));
                break;

            case SelectType.Clock:
                if (clockPanel != null)
                    yield return StartCoroutine(FillImage(clockPanel, 1f, fadingSpeed));
                break;
        }

        HandleFadeInComplete();
    }

    IEnumerator CoFadeOut()
    {
        yield return new WaitForSeconds(endDelay);
        onBeginFadingOut?.Invoke();

        switch (selectType)
        {
            case SelectType.Normal:
                yield return StartCoroutine(FadeAlpha(fadingPanel, 1f, 0f, fadingSpeed));
                break;

            case SelectType.Gradient:
                if (fadingPanel != null)
                    yield return StartCoroutine(MoveToInitial(fadingPanel.transform, fadingSpeed));
                break;

            case SelectType.Eye:
                if (eyeController != null)
                    yield return StartCoroutine(ScaleY(eyeController, 0.5f, fadingSpeed));
                break;

            case SelectType.Clock:
                if (clockPanel != null)
                    yield return StartCoroutine(FillImage(clockPanel, 0f, fadingSpeed));
                break;
        }

        HandleFadeOutComplete();
    }

    IEnumerator FadeAlpha(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        cg.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, ApplyEase(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator MoveToTarget(Transform tr, float targetVal, float duration)
    {
        if (tr == null) yield break;
        Vector3 start = tr.localPosition;
        Vector3 end   = gradientMode switch
        {
            GradientMode.Y => new Vector3(start.x, targetVal, start.z),
            GradientMode.Z => new Vector3(start.x, start.y, targetVal),
            _              => new Vector3(targetVal, start.y, start.z),
        };
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.localPosition = Vector3.Lerp(start, end, ApplyEase(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        tr.localPosition = end;
    }

    IEnumerator MoveToInitial(Transform tr, float duration)
    {
        if (tr == null) yield break;
        Vector3 start = tr.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.localPosition = Vector3.Lerp(start, _initialLocalPos, ApplyEase(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        tr.localPosition = _initialLocalPos;
    }

    IEnumerator ScaleY(Transform tr, float targetY, float duration)
    {
        if (tr == null) yield break;
        Vector3 start = tr.localScale;
        Vector3 end   = new Vector3(start.x, targetY, start.z);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.Lerp(start, end, ApplyEase(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        tr.localScale = end;
    }

    IEnumerator FillImage(Image img, float target, float duration)
    {
        if (img == null) yield break;
        float start = img.fillAmount;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            img.fillAmount = Mathf.Lerp(start, target, ApplyEase(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        img.fillAmount = target;
    }

    void HandleFadeInComplete()
    {
        if (inOut)
        {
            BeginFadingOut();
        }
        else
        {
            onCompleteFadingIn?.Invoke();
            if (modifyOtherCanvasGroup && fadingPanel != null)
            {
                fadingPanel.interactable    = true;
                fadingPanel.blocksRaycasts  = true;
            }
        }
    }

    void HandleFadeOutComplete()
    {
        onCompleteFadingOut?.Invoke();
        if (modifyOtherCanvasGroup && fadingPanel != null)
        {
            fadingPanel.interactable   = false;
            fadingPanel.blocksRaycasts = false;
        }
    }

    float ApplyEase(float t)
    {
        return selectEase switch
        {
            EaseType.Linear      => t,
            EaseType.InSine      => 1 - Mathf.Cos(t * Mathf.PI / 2f),
            EaseType.OutSine     => Mathf.Sin(t * Mathf.PI / 2f),
            EaseType.InOutSine   => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f,
            EaseType.InQuad      => t * t,
            EaseType.OutQuad     => 1f - (1f - t) * (1f - t),
            EaseType.InOutQuad   => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f,
            EaseType.InCubic     => t * t * t,
            EaseType.OutCubic    => 1f - Mathf.Pow(1f - t, 3f),
            EaseType.InOutCubic  => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f,
            _                    => t // Unset = Linear
        };
    }
}